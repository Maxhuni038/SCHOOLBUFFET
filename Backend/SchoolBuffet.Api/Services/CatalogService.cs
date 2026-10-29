using Microsoft.EntityFrameworkCore;
using SchoolBuffet.Api.Data;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public sealed class CatalogService
{
    public const string DefaultImage = "/assets/img/products/default-product.svg";

    private static readonly string[] AllowedIcons =
    {
        "sandwich", "drink", "food", "dessert", "fruit", "coffee", "milk", "bread", "salad", "snack"
    };

    private static readonly string[] AllowedExtensions =
    {
        ".jpg", ".jpeg", ".png", ".webp", ".svg"
    };

    private readonly SchoolBuffetDbContext _db;
    private readonly RealtimeService _realtime;

    public CatalogService(SchoolBuffetDbContext db, RealtimeService realtime)
    {
        _db = db;
        _realtime = realtime;
    }

    public async Task<MenuDto> GetMenuAsync(string currencyLabel)
    {
        List<Category> categories = await _db.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        List<Product> products = await _db.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.Category != null && x.Category.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        Dictionary<int, List<Product>> grouped = products
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<MenuCategoryDto> menuCategories = new();
        foreach (Category category in categories)
        {
            List<Product> items = grouped.TryGetValue(category.Id, out List<Product>? found)
                ? found
                : new List<Product>();

            menuCategories.Add(new MenuCategoryDto(
                category.Id,
                category.Name,
                category.Icon,
                category.AccentColor,
                category.Description,
                items
                    .Select(x => new MenuItemDto(x.Id, x.Name, x.Description, x.Price, x.ImagePath, x.Badge, x.IsRecommended))
                    .ToList()));
        }

        return new MenuDto(menuCategories, DateTime.UtcNow, currencyLabel);
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(bool includeInactive)
    {
        IQueryable<Category> query = _db.Categories.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        List<Category> categories = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        Dictionary<int, int> counts = await _db.Products
            .AsNoTracking()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { Key = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total);

        return categories
            .Select(x => new CategoryDto(
                x.Id,
                x.Name,
                x.Icon,
                x.AccentColor,
                x.Description,
                x.SortOrder,
                x.IsActive,
                counts.TryGetValue(x.Id, out int total) ? total : 0,
                x.CreatedAtUtc))
            .ToList();
    }

    public async Task<CategoryDto> CreateCategoryAsync(CategoryInput input)
    {
        ValidationBag bag = new();
        string name = Normalize(input.Name);
        bag.Required(nameof(input.Name), name, "Emri i kategorise", 40, 3);
        string description = Clamp(input.Description, 160);
        int sortOrder = input.SortOrder ?? 0;
        bag.Ensure(sortOrder is >= 0 and <= 9999, nameof(input.SortOrder), "Radha e renditjes duhet te jete numer midis 0 dhe 9999");

        if (await _db.Categories.AnyAsync(x => x.Name.ToLower() == name.ToLower()))
        {
            bag.Add(nameof(input.Name), "Kategoria me kete emer ekziston tashme");
        }

        bag.ThrowIfInvalid("Te dhenat e kategorise nuk jane te vlefshme");

        Category category = new()
        {
            Name = name,
            Icon = NormalizeIcon(input.Icon),
            AccentColor = NormalizeColor(input.AccentColor),
            Description = description,
            SortOrder = sortOrder,
            IsActive = input.IsActive ?? true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        _realtime.Publish(RealtimeKinds.Catalog, null, category.Name, "Kategori e re");

        return new CategoryDto(category.Id, category.Name, category.Icon, category.AccentColor,
            category.Description, category.SortOrder, category.IsActive, 0, category.CreatedAtUtc);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(int id, CategoryInput input)
    {
        Category category = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id)
            ?? NotFound<Category>("Kategoria nuk u gjet");

        ValidationBag bag = new();
        string name = Normalize(input.Name);
        bag.Required(nameof(input.Name), name, "Emri i kategorise", 40, 3);
        string description = Clamp(input.Description, 160);
        int sortOrder = input.SortOrder ?? category.SortOrder;
        bag.Ensure(sortOrder is >= 0 and <= 9999, nameof(input.SortOrder), "Radha e renditjes duhet te jete numer midis 0 dhe 9999");

        if (await _db.Categories.AnyAsync(x => x.Id != id && x.Name.ToLower() == name.ToLower()))
        {
            bag.Add(nameof(input.Name), "Kategoria me kete emer ekziston tashme");
        }

        bag.ThrowIfInvalid("Te dhenat e kategorise nuk jane te vlefshme");

        category.Name = name;
        category.Icon = NormalizeIcon(input.Icon);
        category.AccentColor = NormalizeColor(input.AccentColor);
        category.Description = description;
        category.SortOrder = sortOrder;
        category.IsActive = input.IsActive ?? category.IsActive;
        category.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _realtime.Publish(RealtimeKinds.Catalog, null, category.Name, "Kategori e ndryshuar");

        int productCount = await _db.Products.CountAsync(x => x.CategoryId == category.Id);

        return new CategoryDto(category.Id, category.Name, category.Icon, category.AccentColor,
            category.Description, category.SortOrder, category.IsActive, productCount, category.CreatedAtUtc);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        Category category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? NotFound<Category>("Kategoria nuk u gjet");

        int productCount = await _db.Products.CountAsync(x => x.CategoryId == category.Id);
        if (productCount > 0)
        {
            AppThrow.Conflict(
                $"Kategoria {category.Name} ka {productCount} produkt(e) te lidhur. Zhvendos produktet para se ta fshije kategorine.");
        }

        await _db.Categories.Where(x => x.Id == id).ExecuteDeleteAsync();
        _realtime.Publish(RealtimeKinds.Catalog, null, category.Name, "Kategori e fshire");
    }

    public async Task<List<ProductDto>> GetProductsAsync(string? search, int? categoryId, bool includeInactive)
    {
        IQueryable<Product> query = _db.Products.AsNoTracking().Include(x => x.Category);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (categoryId is > 0)
        {
            query = query.Where(x => x.CategoryId == categoryId);
        }

        string term = Clamp(search, 80).ToLowerInvariant();
        if (term.Length > 0)
        {
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Description.ToLower().Contains(term));
        }

        List<Product> products = await query
            .OrderBy(x => x.Category!.SortOrder)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        Dictionary<int, int> orderCounts = await _db.OrderItems
            .AsNoTracking()
            .Where(i => i.ProductId != null)
            .GroupBy(i => i.ProductId!.Value)
            .Select(g => new { Key = g.Key, Times = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Times);

        return products
            .Select(x => new ProductDto(
                x.Id,
                x.CategoryId,
                x.Category?.Name ?? string.Empty,
                x.Category?.Icon ?? "snack",
                x.Category?.AccentColor ?? "#6c5ce7",
                x.Name,
                x.Description,
                x.Price,
                x.ImagePath,
                x.Badge,
                x.IsRecommended,
                x.SortOrder,
                x.IsActive,
                orderCounts.TryGetValue(x.Id, out int times) ? times : 0,
                x.TrackStock,
                x.StockQuantity,
                x.LowStockThreshold,
                StockKey(x),
                StockLabel(x),
                x.CreatedAtUtc))
            .ToList();
    }

    public async Task<ProductDto> GetProductAsync(int id)
    {
        Product product = await _db.Products.AsNoTracking().Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id)
            ?? NotFound<Product>("Produkti nuk u gjet");

        int times = await _db.OrderItems.CountAsync(x => x.ProductId == id);
        return ToDto(product, times);
    }

    public async Task<ProductDto> CreateProductAsync(ProductInput input)
    {
        Product product = new() { CreatedAtUtc = DateTime.UtcNow };
        await ApplyAsync(product, input, null);

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        _realtime.Publish(RealtimeKinds.Catalog, product.Id, product.Name, "Produkt i ri");

        return await GetProductAsync(product.Id);
    }

    public async Task<ProductDto> UpdateProductAsync(int id, ProductInput input)
    {
        Product product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id)
            ?? NotFound<Product>("Produkti nuk u gjet");

        await ApplyAsync(product, input, id);
        product.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _realtime.Publish(RealtimeKinds.Catalog, product.Id, product.Name, "Produkt i ndryshuar");

        return await GetProductAsync(id);
    }

    public async Task DeleteProductAsync(int id)
    {
        Product product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? NotFound<Product>("Produkti nuk u gjet");

        await _db.Products.Where(x => x.Id == product.Id).ExecuteDeleteAsync();
        _realtime.Publish(RealtimeKinds.Catalog, null, product.Name, "Produkt i fshire");
    }

    public string ResolveImagePath(string? value)
    {
        string path = Clamp(value, 200).Replace('\\', '/');
        if (path.Length == 0)
        {
            return DefaultImage;
        }

        if (!path.StartsWith("/assets/img/", StringComparison.Ordinal))
        {
            AppThrow.BadRequest("Adresa e fotos nuk eshte e sakte. Zgjidh nje foto nga biblioteka e byfese.");
        }

        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            AppThrow.BadRequest("Formati i fotos nuk lejohet. Formatet e mundshme: jpg, jpeg, png, webp, svg.");
        }

        return path;
    }

    private async Task ApplyAsync(Product product, ProductInput input, int? currentId)
    {
        ValidationBag bag = new();

        string name = Normalize(input.Name);
        bag.Required(nameof(input.Name), name, "Emri i produktit", 80, 3);

        string description = Clamp(input.Description, 240);
        string badge = Clamp(input.Badge, 24);
        int sortOrder = input.SortOrder ?? 0;
        bag.Ensure(sortOrder is >= 0 and <= 9999, nameof(input.SortOrder), "Radha e renditjes duhet te jete numer midis 0 dhe 9999");

        bool trackStock = input.TrackStock ?? product.TrackStock;
        int stockQuantity = input.StockQuantity ?? product.StockQuantity;
        int threshold = input.LowStockThreshold ?? product.LowStockThreshold;
        bag.Ensure(stockQuantity is >= 0 and <= 100000, nameof(input.StockQuantity), "Sasia ne stok duhet te jete numer midis 0 dhe 100000");
        bag.Ensure(threshold is >= 0 and <= 100000, nameof(input.LowStockThreshold), "Pragu i stokut te ulet duhet te jete numer midis 0 dhe 100000");

        decimal price = 0m;
        if (input.Price is null)
        {
            bag.Add(nameof(input.Price), "Cmimi eshte i detyueshem");
        }
        else
        {
            price = Math.Round(input.Price.Value, 2, MidpointRounding.AwayFromZero);
            bag.Ensure(price >= 0.20m && price <= 500m, nameof(input.Price), "Cmimi duhet te jete midis 0,20 dhe 500 euro");
        }

        Category? category = null;
        if (input.CategoryId is > 0)
        {
            category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == input.CategoryId.Value);
            if (category is null)
            {
                bag.Add(nameof(input.CategoryId), "Kategoria nuk u gjet");
            }
        }
        else
        {
            bag.Add(nameof(input.CategoryId), "Zgjidh kategorine e produktit");
        }

        if (category is not null &&
            await _db.Products.AnyAsync(x => x.Id != currentId
                && x.CategoryId == category.Id
                && x.Name.ToLower() == name.ToLower()))
        {
            bag.Add(nameof(input.Name), "Produkti me kete emer ekziston tashme ne kete kategori");
        }

        bag.ThrowIfInvalid("Te dhenat e produktit nuk jane te vlefshme");

        product.CategoryId = category!.Id;
        product.Name = name;
        product.Description = description;
        product.Price = price;
        product.ImagePath = ResolveImagePath(input.ImagePath);
        product.Badge = badge;
        product.IsRecommended = input.IsRecommended ?? product.IsRecommended;
        product.SortOrder = sortOrder;
        product.IsActive = input.IsActive ?? product.IsActive;
        product.TrackStock = trackStock;
        product.LowStockThreshold = threshold;

        if (trackStock && product.StockQuantity != stockQuantity)
        {
            int delta = stockQuantity - product.StockQuantity;
            product.StockQuantity = stockQuantity;

            if (currentId is > 0)
            {
                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    Change = delta,
                    StockAfter = stockQuantity,
                    Reason = StockReasons.Correction,
                    Reference = "Menaxhimi i produktit",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }
    }

    private static ProductDto ToDto(Product product, int timesOrdered)
    {
        return new ProductDto(
            product.Id,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.Category?.Icon ?? "snack",
            product.Category?.AccentColor ?? "#6c5ce7",
            product.Name,
            product.Description,
            product.Price,
            product.ImagePath,
            product.Badge,
            product.IsRecommended,
            product.SortOrder,
            product.IsActive,
            timesOrdered,
            product.TrackStock,
            product.StockQuantity,
            product.LowStockThreshold,
            StockKey(product),
            StockLabel(product),
            product.CreatedAtUtc);
    }

    private static string StockKey(Product product)
    {
        if (!product.TrackStock)
        {
            return "PaGjurmim";
        }

        if (product.StockQuantity <= 0)
        {
            return "Mbaruar";
        }

        return product.StockQuantity <= product.LowStockThreshold ? "IUlet" : "NeDorze";
    }

    private static string StockLabel(Product product)
    {
        return StockKey(product) switch
        {
            "Mbaruar" => "Mbaruar",
            "IUlet" => "Sasi e ulet",
            "NeDorze" => "Ne dorze",
            _ => "Pa gjurmim"
        };
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

    private static string NormalizeIcon(string? value)
    {
        string icon = (value ?? string.Empty).Trim().ToLowerInvariant();
        return AllowedIcons.Contains(icon) ? icon : "snack";
    }

    private static string NormalizeColor(string? value)
    {
        string color = (value ?? string.Empty).Trim().ToLowerInvariant();
        bool valid = color.Length == 7 && color[0] == '#' && color[1..].All(Uri.IsHexDigit);
        return valid ? color : "#6c5ce7";
    }
}
