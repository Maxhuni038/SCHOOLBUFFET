using Microsoft.EntityFrameworkCore;
using SchoolBuffet.Api.Data;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public sealed class StockService
{
    private const int MaxManualChange = 5000;

    private readonly SchoolBuffetDbContext _db;
    private readonly SchoolClock _clock;
    private readonly RealtimeService _realtime;

    public StockService(SchoolBuffetDbContext db, SchoolClock clock, RealtimeService realtime)
    {
        _db = db;
        _clock = clock;
        _realtime = realtime;
    }

    public async Task<List<StockMovement>> ReserveAsync(Order order, int? userId)
    {
        List<(int ProductId, int Quantity, string ProductName)> lines = Group(order.Items);
        if (lines.Count == 0)
        {
            return new List<StockMovement>();
        }

        List<int> ids = lines.Select(x => x.ProductId).ToList();
        HashSet<int> tracked = (await _db.Products
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.TrackStock)
            .Select(x => x.Id)
            .ToListAsync()).ToHashSet();

        DateTime utcNow = DateTime.UtcNow;
        List<StockMovement> ledger = new();
        List<(int ProductId, int Quantity)> applied = new();

        foreach ((int productId, int quantity, string productName) in lines)
        {
            if (!tracked.Contains(productId))
            {
                continue;
            }

            (bool Ok, int After) result = await TryDecrementAsync(productId, quantity);
            if (!result.Ok)
            {
                foreach ((int appliedId, int appliedQuantity) in applied)
                {
                    await IncrementAsync(appliedId, appliedQuantity);
                }

                int available = await _db.Products
                    .Where(x => x.Id == productId)
                    .Select(x => x.StockQuantity)
                    .FirstOrDefaultAsync();

                AppThrow.Conflict(
                    $"Nuk ka mjaft stok per {productName}. Ne dispozicion: {available} copeka, per porosine duhen {quantity}.");
            }

            applied.Add((productId, quantity));
            ledger.Add(new StockMovement
            {
                ProductId = productId,
                Change = -quantity,
                StockAfter = result.After,
                Reason = StockReasons.Order,
                Reference = order.Code,
                CreatedAtUtc = utcNow,
                CreatedByUserId = userId
            });
        }

        return ledger;
    }

    public async Task RestoreAsync(Order order, int? userId, string reason)
    {
        List<(int ProductId, int Quantity, string ProductName)> lines = Group(order.Items);
        if (lines.Count == 0)
        {
            return;
        }

        List<int> ids = lines.Select(x => x.ProductId).ToList();
        HashSet<int> tracked = (await _db.Products
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.TrackStock)
            .Select(x => x.Id)
            .ToListAsync()).ToHashSet();

        DateTime utcNow = DateTime.UtcNow;
        List<StockMovement> ledger = new();

        foreach ((int productId, int quantity, string _) in lines)
        {
            if (!tracked.Contains(productId))
            {
                continue;
            }

            int after = await IncrementAsync(productId, quantity);
            ledger.Add(new StockMovement
            {
                ProductId = productId,
                Change = quantity,
                StockAfter = after,
                Reason = reason,
                Reference = order.Code,
                CreatedAtUtc = utcNow,
                CreatedByUserId = userId
            });
        }

        if (ledger.Count > 0)
        {
            _db.StockMovements.AddRange(ledger);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<StockItemDto> AdjustAsync(int productId, int? change, string? reason, string? note, int userId)
    {
        Product product = await _db.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == productId)
            ?? NotFound<Product>("Produkti per inventar nuk u gjet");

        if (!product.TrackStock)
        {
            AppThrow.Conflict($"Gjarimi i stokut nuk eshte aktiv per {product.Name}. Aktivizeoje nga menaxhimi i produktit.");
        }

        int delta = change ?? 0;
        ValidationBag bag = new();
        bag.Ensure(delta != 0, nameof(change), "Sasia per korrigjim duhet te jete numer pozitiv ose negativ");
        bag.Ensure(Math.Abs(delta) <= MaxManualChange, nameof(change),
            $"Korrigjimi maximal eshte {MaxManualChange} copeka per herete");
        bag.ThrowIfInvalid("Korrigjimi i stokut nuk u pranua");

        int after;
        if (delta < 0)
        {
            (bool Ok, int NewQuantity) result = await TryDecrementAsync(product.Id, -delta);
            if (!result.Ok)
            {
                AppThrow.Conflict($"Nuk mund te zbreshet {product.Name}: ne stok ka {product.StockQuantity} copeka.");
            }

            after = result.NewQuantity;
        }
        else
        {
            after = await IncrementAsync(product.Id, delta);
        }

        string text = Clamp(note, 24);
        StockMovement movement = new()
        {
            ProductId = product.Id,
            Change = delta,
            StockAfter = after,
            Reason = ResolveReason(reason, delta),
            Reference = text.Length > 0 ? text : "Paneli i inventarit",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        _realtime.Publish(RealtimeKinds.Stock, product.Id, product.Name, after.ToString());

        return ToDto(product, after, 0);
    }

    public async Task<StockListDto> GetBoardAsync(string? search, int? categoryId, string? filter)
    {
        IQueryable<Product> query = _db.Products.AsNoTracking().Include(x => x.Category);

        string term = Clamp(search, 80).ToLowerInvariant();
        if (term.Length > 0)
        {
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Category!.Name.ToLower().Contains(term));
        }

        if (categoryId is > 0)
        {
            query = query.Where(x => x.CategoryId == categoryId);
        }

        List<Product> products = await query
            .OrderBy(x => x.Category!.SortOrder)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        Dictionary<int, int> soldToday = await SoldTodayAsync();
        List<StockItemDto> items = products
            .Select(x => ToDto(x, x.StockQuantity, soldToday.TryGetValue(x.Id, out int qty) ? qty : 0))
            .ToList();

        string wanted = Clamp(filter, 24).ToLowerInvariant();
        List<StockItemDto> visible = wanted switch
        {
            "mbaruar" => items.Where(x => x.StatusKey == "Mbaruar").ToList(),
            "iulet" => items.Where(x => x.StatusKey is "Mbaruar" or "IUlet").ToList(),
            "pagjurmim" => items.Where(x => x.StatusKey == "PaGjurmim").ToList(),
            _ => items
        };

        List<StockItemDto> active = items.Where(x => x.IsActive).ToList();

        return new StockListDto(
            visible,
            new StockSummaryDto(
                active.Count,
                active.Count(x => x.TrackStock),
                active.Count(x => x.StatusKey == "Mbaruar"),
                active.Count(x => x.StatusKey == "IUlet"),
                active.Count(x => x.StatusKey == "NeDorze"),
                active.Where(x => x.TrackStock).Sum(x => x.StockQuantity),
                Math.Round(active.Where(x => x.TrackStock).Sum(x => x.StockQuantity * x.Price), 2, MidpointRounding.AwayFromZero),
                soldToday.Values.Sum()),
            DateTime.UtcNow);
    }

    public async Task<StockMovementsDto> GetMovementsAsync(int? productId, int page, int pageSize)
    {
        IQueryable<StockMovement> query = _db.StockMovements
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.CreatedByUser);

        if (productId is > 0)
        {
            query = query.Where(x => x.ProductId == productId);
        }

        int total = await query.CountAsync();
        int safePage = Math.Max(1, page);
        int safeSize = Math.Clamp(pageSize <= 0 ? 30 : pageSize, 1, 100);

        List<StockMovement> rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync();

        return new StockMovementsDto(
            rows
                .Select(x => new StockMovementDto(
                    x.Id,
                    x.ProductId,
                    x.Product?.Name ?? "Produkt i fshire",
                    x.Change,
                    x.StockAfter,
                    x.Reason,
                    x.Reference,
                    x.CreatedAtUtc,
                    x.CreatedByUser?.FullName))
                .ToList(),
            total,
            safePage,
            safeSize);
    }

    public async Task CompensateAsync(Order order)
    {
        foreach ((int productId, int quantity, string _) in Group(order.Items))
        {
            bool tracked = await _db.Products.AnyAsync(x => x.Id == productId && x.TrackStock);
            if (tracked)
            {
                await IncrementAsync(productId, quantity);
            }
        }
    }

    private async Task<(bool Ok, int After)> TryDecrementAsync(int productId, int quantity)
    {
        int affected = await _db.Products
            .Where(x => x.Id == productId && x.StockQuantity >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity));

        if (affected == 0)
        {
            return (false, 0);
        }

        int after = await _db.Products.Where(x => x.Id == productId).Select(x => x.StockQuantity).FirstAsync();
        return (true, after);
    }

    private async Task<int> IncrementAsync(int productId, int quantity)
    {
        await _db.Products
            .Where(x => x.Id == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + quantity));

        return await _db.Products.Where(x => x.Id == productId).Select(x => x.StockQuantity).FirstAsync();
    }

    private async Task<Dictionary<int, int>> SoldTodayAsync()
    {
        (DateTime startUtc, DateTime endUtc) = _clock.UtcDayRange(_clock.Today);

        return await _db.OrderItems
            .AsNoTracking()
            .Where(i => i.ProductId != null && i.Order != null && i.Order.CreatedAtUtc >= startUtc && i.Order.CreatedAtUtc < endUtc)
            .GroupBy(i => i.ProductId!.Value)
            .Select(g => new { Key = g.Key, Qty = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Qty);
    }

    private static List<(int ProductId, int Quantity, string ProductName)> Group(IEnumerable<OrderItem> items)
    {
        return items
            .Where(x => x.ProductId is > 0)
            .GroupBy(x => x.ProductId!.Value)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(x => x.Quantity), ProductName: g.First().ProductName))
            .OrderBy(x => x.ProductId)
            .ToList();    }

    private static string ResolveReason(string? value, int delta)
    {
        string text = (value ?? string.Empty).Trim();
        if (string.Equals(text, StockReasons.Correction, StringComparison.OrdinalIgnoreCase))
        {
            return StockReasons.Correction;
        }

        return delta > 0 ? StockReasons.ManualAdd : StockReasons.ManualRemove;
    }

    private static StockItemDto ToDto(Product product, int quantity, int soldToday)
    {
        (string key, string label) = !product.TrackStock
            ? ("PaGjurmim", "Pa gjurmim")
            : quantity <= 0
                ? ("Mbaruar", "Mbaruar")
                : quantity <= product.LowStockThreshold
                    ? ("IUlet", "Sasi e ulet")
                    : ("NeDorze", "Ne dorze");

        return new StockItemDto(
            product.Id,
            product.Name,
            product.Category?.Name ?? string.Empty,
            product.Category?.Icon ?? "snack",
            product.Category?.AccentColor ?? "#6c5ce7",
            product.Price,
            product.TrackStock,
            quantity,
            product.LowStockThreshold,
            product.IsActive,
            key,
            label,
            soldToday,
            product.UpdatedAtUtc);
    }

    private static T NotFound<T>(string message)
    {
        AppThrow.NotFound(message);
        return default!;
    }

    private static string Clamp(string? value, int max)
    {
        string result = (value ?? string.Empty).Trim();
        return result.Length > max ? result[..max].Trim() : result;
    }
}
