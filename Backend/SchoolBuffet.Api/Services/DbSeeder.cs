using Microsoft.EntityFrameworkCore;
using SchoolBuffet.Api.Data;
using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public sealed class DbSeeder
{
    private static readonly (string Name, string Icon, string Color, string Description)[] CategorySeed =
    {
        ("Sandwicha", "sandwich", "#e07b39", "Sandwich dhe toste te fresket, pergatiten per 3 minuta"),
        ("Pije", "drink", "#2b7fd0", "Uje, lengje dhe pije te ftohta pa alkool"),
        ("Ushqime", "food", "#2f8f5b", "Byrek, perime dhe ushqim i ngrohte per dreken"),
        ("Embelsira", "dessert", "#b5446f", "Keks, muffin dhe embelsira te lehta per pushimin")
    };

    private static readonly (string Name, string Description, decimal Price, string Image, string Badge, bool Recommended)[] ProductSeed =
    {
        ("Sandwich Me Pule", "Buke e bute me pule te pjekur, sallatame dhe domate", 1.50m, "sandwich-pule.svg", "Me e kerkuara", true),
        ("Sandwich Me Turshi", "Turshi, kos dhe perime te freska ne buke", 1.30m, "sandwich-turshi.svg", string.Empty, false),
        ("Toste Me Mjalt dhe Banana", "Toste e ngrohte me mjalt dhe banana", 1.00m, "toste-pishit-banane.svg", string.Empty, false),
        ("Panini Me Perime", "Panini me perime te pjekura dhe ullinj", 1.10m, "panini-perime.svg", "Vegetariane", false),
        ("Uje Natyrale 0.33 L", "Uje per pirje ne shishe te vogel", 0.40m, "uje-natyrale.svg", string.Empty, false),
        ("Leng Portokalli 0.25 L", "Leng portokalli me 100 perqind frutor", 0.70m, "leng-portokalli.svg", "Pa sheqer te shtuar", true),
        ("Leng Molle 0.25 L", "Leng molle i fresket pa gaz", 0.70m, "leng-molle.svg", string.Empty, false),
        ("Jogurt Per Pirje 0.2 L", "Jogurt i fresket per pirje me fruta", 0.60m, "jogurt-perpirje.svg", string.Empty, false),
        ("Byrek Me Kos", "Byrek i pjekur me kos dhe spinaq", 0.90m, "byrek-me-kos.svg", "I ngrohte", true),
        ("Snack Perimesh", "Pakete me domate, kastravec dhe spek", 0.70m, "snack-perimesh.svg", "Vegetariane", false),
        ("Ushqim i Ftohte Familjar", "Vakt me perime, oriz dhe mish", 1.80m, "ushqim-ftohte-familjar.svg", string.Empty, false),
        ("Keks Me Shokollate", "Pese keku me cope shokollate", 0.50m, "keks-me-shokollate.svg", string.Empty, false),
        ("Muffin Me Arra", "Muffin i fresket me arra dhe miell", 0.60m, "muffin-me-arra.svg", string.Empty, false),
        ("Biskota Me Sjere Natyrale", "Biskota me mjalt dhe drithera", 0.45m, "biskota-me-sjere.svg", string.Empty, false)
    };

    private static readonly string[] StudentNames =
    {
        "Arben Krasniqi", "Elira Gashi", "Dritan Voca", "Besa Haliti", "Liridon Syla",
        "Anisa Berisha", "Blerim Morina", "Fatmir Hoxha", "Gentiana Kadia", "Ardit Nuhiu",
        "Melisa Arifi", "Visar Bajrami", "Nora Shala", "Endrit Kelmendi", "Albana Rexha"
    };

    private static readonly string[] ClassNames =
    {
        "VI-1", "VI-2", "VII-1", "VII-2", "VIII-1", "VIII-2", "IX-1", "IX-2"
    };

    private readonly SchoolBuffetDbContext _db;
    private readonly AppSettings _settings;
    private readonly SchoolClock _clock;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(SchoolBuffetDbContext db, AppSettings settings, SchoolClock clock, ILogger<DbSeeder> logger)
    {
        _db = db;
        _settings = settings;
        _clock = clock;
        _logger = logger;
    }

    public async Task<SeedResult> SeedAsync()
    {
        SeedResult result = new();
        result.AdminPassword = await EnsureAdminAsync();
        await EnsureCategoriesAsync();
        await EnsureProductsAsync();
        result.DemoOrdersCreated = await EnsureDemoOrdersAsync();
        return result;
    }

    private async Task<string?> EnsureAdminAsync()
    {
        if (await _db.Users.AnyAsync())
        {
            return null;
        }

        string username = string.IsNullOrWhiteSpace(_settings.AdminUsername) ? "admin" : _settings.AdminUsername.Trim();
        string? configured = _settings.AdminPassword;
        string password = string.IsNullOrWhiteSpace(configured) ? PasswordHasher.GeneratePassword(14) : configured;
        bool generated = string.IsNullOrWhiteSpace(configured);

        _db.Users.Add(new User
        {
            FullName = string.IsNullOrWhiteSpace(_settings.AdminFullName) ? "Administrator i Byfese" : _settings.AdminFullName,
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            Role = "Admin",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Perdoruesi administrator u krijua me emrin {Username}", username);

        return generated ? password : null;
    }

    private async Task EnsureCategoriesAsync()
    {
        if (await _db.Categories.AnyAsync())
        {
            return;
        }

        int sort = 1;
        foreach ((string name, string icon, string color, string description) in CategorySeed)
        {
            _db.Categories.Add(new Category
            {
                Name = name,
                Icon = icon,
                AccentColor = color,
                Description = description,
                SortOrder = sort++,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Kategorite e byfese u mbushen");
    }

    private async Task EnsureProductsAsync()
    {
        if (await _db.Products.AnyAsync())
        {
            return;
        }

        List<Category> categories = await _db.Categories.OrderBy(x => x.SortOrder).ToListAsync();
        if (categories.Count == 0)
        {
            return;
        }

        Dictionary<string, int> categoryIds = categories.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);
        int sort = 1;

        foreach ((string name, string description, decimal price, string image, string badge, bool recommended) in ProductSeed)
        {
            string category = name.Contains("Sandwich", StringComparison.OrdinalIgnoreCase) ||
                              name.Contains("Toste", StringComparison.OrdinalIgnoreCase) ||
                              name.Contains("Panini", StringComparison.OrdinalIgnoreCase)
                ? "Sandwicha"
                : name.Contains("Uje", StringComparison.OrdinalIgnoreCase) ||
                  name.Contains("Leng", StringComparison.OrdinalIgnoreCase) ||
                  name.Contains("Jogurt", StringComparison.OrdinalIgnoreCase)
                    ? "Pije"
                    : name.Contains("Keks", StringComparison.OrdinalIgnoreCase) ||
                      name.Contains("Muffin", StringComparison.OrdinalIgnoreCase) ||
                      name.Contains("Biskota", StringComparison.OrdinalIgnoreCase)
                        ? "Embelsira"
                        : "Ushqime";

            _db.Products.Add(new Product
            {
                CategoryId = categoryIds[category],
                Name = name,
                Description = description,
                Price = price,
                ImagePath = "/assets/img/products/" + image,
                Badge = badge,
                IsRecommended = recommended,
                SortOrder = sort++,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Produktet e byfese u mbushen");
    }

    private async Task<int> EnsureDemoOrdersAsync()
    {
        if (await _db.Orders.AnyAsync())
        {
            return 0;
        }

        List<Product> products = await _db.Products.AsNoTracking().Where(x => x.IsActive).ToListAsync();
        if (products.Count == 0)
        {
            return 0;
        }

        int adminId = await _db.Users.Select(x => x.Id).FirstAsync();
        DateOnly today = _clock.Today;
        List<Order> orders = new();

        for (int offset = 20; offset >= 0; offset--)
        {
            DateOnly day = today.AddDays(-offset);
            DayOfWeek weekday = day.DayOfWeek;
            if (weekday is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            int count = offset == 0 ? 9 : Random.Shared.Next(5, 14);

            for (int i = 0; i < count; i++)
            {
                int hour = Random.Shared.Next(7, 13);
                int minute = Random.Shared.Next(0, 60);
                DateTime createdUtc = _clock.ToUtc(day, new TimeOnly(hour, minute));

                OrderStatus status = offset == 0
                    ? i switch
                    {
                        0 or 1 => OrderStatus.NePritje,
                        2 or 3 => OrderStatus.DukeUPergatitur,
                        4 or 5 => OrderStatus.Gati,
                        _ => OrderStatus.Perfunduar
                    }
                    : OrderStatus.Perfunduar;

                Order order = new()
                {
                    Code = NextCode(orders, day),
                    StudentName = StudentNames[Random.Shared.Next(StudentNames.Length)],
                    ClassName = ClassNames[Random.Shared.Next(ClassNames.Length)],
                    Note = string.Empty,
                    Status = status,
                    CreatedAtUtc = createdUtc,
                    UpdatedAtUtc = status == OrderStatus.Perfunduar ? createdUtc.AddMinutes(12) : createdUtc,
                    CompletedAtUtc = status == OrderStatus.Perfunduar ? createdUtc.AddMinutes(12) : null,
                    MarkedByUserId = status == OrderStatus.Perfunduar ? adminId : null
                };

                int lines = Random.Shared.Next(1, 4);
                HashSet<int> picked = new();

                for (int l = 0; l < lines; l++)
                {
                    Product product = products[Random.Shared.Next(products.Count)];
                    if (!picked.Add(product.Id))
                    {
                        continue;
                    }

                    int quantity = Random.Shared.Next(1, 3);
                    order.Items.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.Price,
                        Quantity = quantity,
                        LineTotal = Math.Round(product.Price * quantity, 2, MidpointRounding.AwayFromZero)
                    });
                }

                if (order.Items.Count == 0)
                {
                    Product product = products[0];
                    order.Items.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.Price,
                        Quantity = 1,
                        LineTotal = product.Price
                    });
                }

                order.TotalAmount = order.Items.Sum(x => x.LineTotal);
                orders.Add(order);
            }
        }

        _db.Orders.AddRange(orders);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Porosite demo u mbushen: {Total}", orders.Count);

        return orders.Count;
    }

    private static string NextCode(List<Order> existing, DateOnly day)
    {
        string candidate;
        do
        {
            candidate = OrderCodeGenerator.Next(day);
        }
        while (existing.Any(x => x.Code == candidate));

        return candidate;
    }
}

public sealed class SeedResult
{
    public string? AdminPassword { get; set; }

    public int DemoOrdersCreated { get; set; }
}
