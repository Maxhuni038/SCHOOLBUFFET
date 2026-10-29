using Microsoft.EntityFrameworkCore;
using SchoolBuffet.Api.Data;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public sealed class OrderService
{
    private const int MaxLines = 25;
    private const int MaxQuantityPerLine = 30;

    private readonly SchoolBuffetDbContext _db;
    private readonly SchoolClock _clock;
    private readonly StockService _stock;
    private readonly PaymentService _payments;
    private readonly RealtimeService _realtime;

    public OrderService(
        SchoolBuffetDbContext db,
        SchoolClock clock,
        StockService stock,
        PaymentService payments,
        RealtimeService realtime)
    {
        _db = db;
        _clock = clock;
        _stock = stock;
        _payments = payments;
        _realtime = realtime;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest input)
    {
        ValidationBag bag = new();

        string studentName = Normalize(input.StudentName);
        bag.Required(nameof(input.StudentName), studentName, "Emri i studentit", 60, 3);
        bag.Ensure(studentName.All(IsValidNameChar), nameof(input.StudentName), "Emri mund te permbaj vetem shkronja, numer dhe presje");

        string className = Normalize(input.ClassName);
        bag.Required(nameof(input.ClassName), className, "Klasa", 40, 2);

        string note = Clamp(input.Note, 240);
        List<OrderItemInput> requested = input.Items ?? new List<OrderItemInput>();
        bag.Ensure(requested.Count > 0 && requested.Count <= MaxLines, nameof(input.Items),
            $"Porosia duhet te kete midis 1 dhe {MaxLines} rreshte produktesh");

        Dictionary<int, int> quantities = new();
        foreach (OrderItemInput line in requested)
        {
            string field = $"{nameof(input.Items)}[{line.ProductId?.ToString() ?? "0"}]";
            if (line.ProductId is not > 0)
            {
                bag.Add(nameof(input.Items), "Cdo rresht duhet te tregoje nje produkt");
                continue;
            }

            int quantity = line.Quantity ?? 0;
            bag.Ensure(quantity is >= 1 && quantity <= MaxQuantityPerLine, field,
                $"Sasia per produkt duhet te jete midis 1 dhe {MaxQuantityPerLine}");

            if (quantities.TryGetValue(line.ProductId.Value, out int existing))
            {
                bag.Ensure(existing + quantity <= MaxQuantityPerLine, field,
                    $"Sasia total per nje produkt nuk mund te tejkaloje {MaxQuantityPerLine}");
                quantity = existing + quantity;
            }

            quantities[line.ProductId.Value] = quantity;
        }

        bag.ThrowIfInvalid("Porosia nuk eshte e vlefshme");

        List<int> productIds = quantities.Keys.ToList();
        List<Product> products = await _db.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id) && x.IsActive)
            .ToListAsync();

        List<int> missing = productIds.Except(products.Select(x => x.Id)).ToList();
        if (missing.Count > 0)
        {
            AppThrow.BadRequest("Nje ose me shume produkte nuk jane me te disponueshem ne byfe. Rifresko menune dhe provoni perseri.");
        }

        Dictionary<int, Product> byId = products.ToDictionary(x => x.Id);
        DateTime utcNow = DateTime.UtcNow;

        Order order = new()
        {
            StudentName = studentName,
            ClassName = className,
            Note = note,
            Status = OrderStatus.NePritje,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        foreach ((int productId, int quantity) in quantities.OrderBy(x => x.Key))
        {
            Product product = byId[productId];
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity,
                LineTotal = Math.Round(product.Price * quantity, 2, MidpointRounding.AwayFromZero)
            });
        }

        order.TotalAmount = order.Items.Sum(x => x.LineTotal);
        order.Code = await GenerateCodeAsync();

        List<StockMovement> ledger = await _stock.ReserveAsync(order, null);

        _db.Orders.Add(order);
        _payments.OpenPaymentFor(order);

        if (ledger.Count > 0)
        {
            _db.StockMovements.AddRange(ledger);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception)
        {
            await _stock.CompensateAsync(order);
            throw;
        }

        _realtime.Publish(RealtimeKinds.Order, order.Id, order.Code, order.Status.ToString());

        if (ledger.Count > 0)
        {
            _realtime.Publish(RealtimeKinds.Stock, order.Id, order.Code, "Rezervuar nga porosia");
        }

        return Map(order);
    }

    public async Task<OrderDto> TrackAsync(string? code)
    {
        string normalized = Normalize(code).ToUpperInvariant();
        if (normalized.Length < 6)
        {
            AppThrow.BadRequest("Shkruani kodin e porosise qe e moret gjate porosites.");
        }

        Order? order = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.MarkedByUser)
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.Code == normalized);

        if (order is null)
        {
            AppThrow.NotFound($"Porosia me kodin {normalized} nuk u gjet. Kontrollo kodin dhe provoni perseri.");
        }

        return Map(order!);
    }

    public async Task<OrderDto> GetAsync(int id)
    {
        Order order = await LoadByIdAsync(id);
        return Map(order);
    }

    public async Task<OrderListDto> ListAsync(string? statusFilter, string? search, string? date, int page, int pageSize)
    {
        IQueryable<Order> query = _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Payment)
            .Include(x => x.MarkedByUser);

        if (OrderStatusText.TryParse(statusFilter, out OrderStatus status))
        {
            query = query.Where(x => x.Status == status);
        }
        else if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            AppThrow.BadRequest("Statusi i porosise nuk njihet.");
        }

        if (_clock.TryDate(date, out DateOnly day))
        {
            (DateTime startUtc, DateTime endUtc) = _clock.UtcDayRange(day);
            query = query.Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc);
        }

        string term = Clamp(search, 60).ToLowerInvariant();
        if (term.Length > 0)
        {
            query = query.Where(x => x.Code.ToLower().Contains(term)
                || x.StudentName.ToLower().Contains(term)
                || x.ClassName.ToLower().Contains(term));
        }

        int total = await query.CountAsync();

        int safePage = Math.Max(1, page);
        int safeSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);

        List<Order> orders = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync();

        return new OrderListDto(
            orders.Select(Map).ToList(),
            total,
            safePage,
            safeSize,
            OrderStatusText.TryParse(statusFilter, out OrderStatus applied) ? applied.Label() : null,
            term.Length > 0 ? term : null,
            string.IsNullOrWhiteSpace(date) ? null : date);
    }

    public async Task<OrderDto> UpdateStatusAsync(int id, string? statusValue, int userId, bool kitchenOnly)
    {
        if (!OrderStatusText.TryParse(statusValue, out OrderStatus next))
        {
            AppThrow.BadRequest("Statusi i kerkuar nuk njihet. Statuset: Ne pritje, Duke u pergatitur, Gati, Perfunduar.");
        }

        Order order = await _db.Orders
            .Include(x => x.Items)
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? NotFound<Order>("Porosia nuk u gjet");

        DateTime utcNow = DateTime.UtcNow;

        if (order.Status == next)
        {
            AppThrow.Conflict($"Porosia {order.Code} tashme ka statusin {next.Label()}.");
        }

        if (kitchenOnly)
        {
            GuardKitchenScope(order.Status, next, order.Code);
        }

        order.Status = next;
        order.UpdatedAtUtc = utcNow;
        order.MarkedByUserId = userId;
        OrderFlow.ApplyTimestamps(order, next, utcNow);

        await _db.SaveChangesAsync();

        if (next == OrderStatus.Perfunduar)
        {
            await _payments.SettleOnHandoverAsync(order.Id, userId);
        }

        _realtime.Publish(RealtimeKinds.Order, order.Id, order.Code, next.ToString());

        return Map(order);
    }

    public async Task<OrderDto> AdvanceAsync(int id, string? action, int userId, bool canClose)
    {
        string verb = (action ?? string.Empty).Trim().ToLowerInvariant();

        Order order = await _db.Orders
            .Include(x => x.Items)
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? NotFound<Order>("Porosia nuk u gjet");

        int index = OrderFlow.Index(order.Status);
        OrderStatus next = verb switch
        {
            "start" or "fillo" => OrderFlow.Sequence[Math.Min(index + 1, OrderFlow.Sequence.Length - 1)],
            "ready" or "gati" when order.Status == OrderStatus.NePritje => OrderStatus.DukeUPergatitur,
            "ready" or "gati" => OrderStatus.Gati,
            "back" or "mbrapsht" => index == 0 ? order.Status : OrderFlow.Sequence[index - 1],
            "done" or "mbyll" => OrderStatus.Perfunduar,
            _ => NotFound<OrderStatus>("Vepria e kuzhines nuk njihet")
        };

        bool touchesClose = next == OrderStatus.Perfunduar || order.Status == OrderStatus.Perfunduar;
        if (touchesClose && !canClose)
        {
            AppThrow.Forbidden($"Mbyllja dhe rihapja e porosive behet nga punetonjesi i byfese ose administratori. Porosia {order.Code} ka statusin {order.Status.Label()}.");
        }

        if (next == order.Status)
        {
            AppThrow.Conflict($"Porosia {order.Code} nuk mund te {verb} me tej. Statusi aktual: {order.Status.Label()}.");
        }

        DateTime utcNow = DateTime.UtcNow;
        order.Status = next;
        order.UpdatedAtUtc = utcNow;
        order.MarkedByUserId = userId;
        OrderFlow.ApplyTimestamps(order, next, utcNow);

        await _db.SaveChangesAsync();

        if (next == OrderStatus.Perfunduar)
        {
            await _payments.SettleOnHandoverAsync(order.Id, userId);
        }

        _realtime.Publish(RealtimeKinds.Order, order.Id, order.Code, next.ToString());

        return Map(order);
    }

    public async Task DeleteAsync(int id, int? userId)
    {
        Order order = await _db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? NotFound<Order>("Porosia nuk u gjet");

        await _stock.RestoreAsync(order, userId, StockReasons.OrderCancelled);

        await _db.Orders.Where(x => x.Id == order.Id).ExecuteDeleteAsync();
        _db.ChangeTracker.Clear();

        _realtime.Publish(RealtimeKinds.Order, order.Id, order.Code, "E fshire");
    }

    private static void GuardKitchenScope(OrderStatus from, OrderStatus to, string code)
    {
        if (to == OrderStatus.Perfunduar)
        {
            AppThrow.Forbidden($"Porosia {code} u shenua Gati. Per mbylljen e porosise duhet punetonjesi i byfese ose administratori.");
        }

        bool adjacent = Math.Abs(OrderFlow.Index(to) - OrderFlow.Index(from)) == 1;
        if (!adjacent)
        {
            AppThrow.BadRequest($"Kuzhina mund te ndryshoj vetem statusin ne hapsiren ngjitur. Porosia {code} ka statusin {from.Label()}.");
        }
    }

    private async Task<string> GenerateCodeAsync()
    {
        DateOnly today = _clock.Today;

        for (int attempt = 1; attempt <= 8; attempt++)
        {
            string candidate = OrderCodeGenerator.Next(today);
            bool taken = await _db.Orders.AnyAsync(x => x.Code == candidate);
            if (!taken)
            {
                return candidate;
            }
        }

        return $"{OrderCodeGenerator.Next(today)}{Random.Shared.Next(10, 99)}";
    }

    private async Task<Order> LoadByIdAsync(int id)
    {
        return await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.MarkedByUser)
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? NotFound<Order>("Porosia nuk u gjet");
    }

    internal static OrderDto Map(Order order)
    {
        List<OrderItemDto> items = order.Items
            .OrderBy(x => x.ProductName)
            .Select(x => new OrderItemDto(x.Id, x.ProductId, x.ProductName, x.UnitPrice, x.Quantity, x.LineTotal))
            .ToList();

        return new OrderDto(
            order.Id,
            order.Code,
            order.StudentName,
            order.ClassName,
            order.Note,
            order.Status.ToString(),
            order.Status.Label(),
            order.TotalAmount,
            items.Sum(x => x.Quantity),
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.PreparingAtUtc,
            order.ReadyAtUtc,
            order.CompletedAtUtc,
            order.MarkedByUser?.FullName,
            order.Payment?.Status.ToString(),
            order.Payment?.Status.Label(),
            items);
    }

    private static bool IsValidNameChar(char value)
    {
        return char.IsLetter(value) || char.IsDigit(value) || value is ' ' or '\'' or '.' or '-';
    }

    private static T NotFound<T>(string message)
    {
        AppThrow.NotFound(message);
        return default!;
    }

    private static string Normalize(string? value)
    {
        return string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Clamp(string? value, int max)
    {
        string result = (value ?? string.Empty).Trim();
        return result.Length > max ? result[..max].Trim() : result;
    }
}
