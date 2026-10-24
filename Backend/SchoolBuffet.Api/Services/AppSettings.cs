namespace SchoolBuffet.Api.Services;

public sealed class AppSettings
{
    public required string ConnectionString { get; init; }

    public string TimeZoneId { get; init; } = "Europe/Tirane";

    public string Currency { get; init; } = "EUR";

    public string CurrencyLabel { get; init; } = "€";

    public string AdminUsername { get; init; } = "admin";

    public string AdminFullName { get; init; } = "Administrator i Byfese";

    public string? AdminPassword { get; init; }

    public string FrontendPath { get; init; } = string.Empty;

    public string UploadPath { get; init; } = string.Empty;

    public int MaxUploadKilobytes { get; init; } = 3000;

    public List<string> AllowedOrigins { get; init; } = new();

    public bool MigrateOnStartup { get; init; } = true;

    public bool SeedOnStartup { get; init; } = true;

    public static AppSettings From(IConfiguration configuration)
    {
        return new AppSettings
        {
            ConnectionString = configuration.GetConnectionString("SchoolBuffet") ?? string.Empty,
            TimeZoneId = configuration["Business:TimeZone"] ?? "Europe/Tirane",
            Currency = configuration["Business:Currency"] ?? "EUR",
            CurrencyLabel = configuration["Business:CurrencyLabel"] ?? "€",
            AdminUsername = configuration["Admin:Username"] ?? "admin",
            AdminFullName = configuration["Admin:FullName"] ?? "Administrator i Byfese",
            AdminPassword = Environment.GetEnvironmentVariable("SCHOOLBUFFET_ADMIN_PASSWORD")
                ?? configuration["Admin:Password"],
            FrontendPath = configuration["Frontend:Path"] ?? string.Empty,
            UploadPath = configuration["Frontend:UploadPath"] ?? string.Empty,
            MaxUploadKilobytes = configuration.GetValue("Uploads:MaxKilobytes", 3000),
            AllowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<List<string>>() ?? new List<string>(),
            MigrateOnStartup = configuration.GetValue("Database:MigrateOnStartup", true),
            SeedOnStartup = configuration.GetValue("Database:SeedOnStartup", true)
        };
    }
}
