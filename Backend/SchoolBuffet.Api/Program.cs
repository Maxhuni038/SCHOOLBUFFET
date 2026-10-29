using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using SchoolBuffet.Api.Data;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

AppSettings settings = AppSettings.From(builder.Configuration);

if (string.IsNullOrWhiteSpace(settings.ConnectionString))
{
    throw new InvalidOperationException(
        "Lidhja me databazen PostgreSQL nuk eshte konfiguruar. Vendos ConnectionStrings:SchoolBuffet ne appsettings.Local.json ose ne ndryshoren ConnectionStrings__SchoolBuffet.");
}

builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(new SchoolClock(settings.TimeZoneId));
builder.Services.AddSingleton<RealtimeService>();
builder.Services.AddHostedService<RealtimeHeartbeat>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<SalesService>();
builder.Services.AddScoped<KitchenService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddScoped<DbSeeder>();

builder.Services.AddDbContext<SchoolBuffetDbContext>(options =>
    options.UseNpgsql(settings.ConnectionString, npgsql =>
    {
        npgsql.MigrationsAssembly("SchoolBuffet.Api");
        npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    }));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "schoolbuffet.admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return context.Response.WriteAsJsonAsync(
                new ErrorResponse("Duhet te hyresh si administrator per te pare kete pjese."));
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return context.Response.WriteAsJsonAsync(
                new ErrorResponse("Llogaria juaj nuk ka te drejta per kete pjese."));
        };
    });

builder.Services.AddAuthorization();

if (settings.AllowedOrigins.Count > 0)
{
    builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
        .WithOrigins(settings.AllowedOrigins.ToArray())
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
}

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(api =>
    {
        api.InvalidModelStateResponseFactory = context =>
        {
            ValidationBag bag = new();
            foreach (KeyValuePair<string, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry> entry in context.ModelState)
            {
                if (entry.Value is null)
                {
                    continue;
                }

                foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in entry.Value.Errors)
                {
                    bag.Add(entry.Key, string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Vlera e derguar nuk eshte e sakte"
                        : error.ErrorMessage);
                }
            }

            return new BadRequestObjectResult(new ErrorResponse("Te dhenat e derguara nuk jane te vlefshme", bag.Errors));
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "{\"message\":\"Shume perpjekje radhazi. Prisni disa minuta dhe provoni perseri.\"}",
            token);
    };
    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonim",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 8,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Logging.AddSimpleConsole(console =>
{
    console.SingleLine = true;
    console.TimestampFormat = "HH:mm:ss ";
});

WebApplication app = builder.Build();

string frontendPath = FrontendLocator.Resolve(app.Environment, settings.FrontendPath);
if (!Directory.Exists(frontendPath))
{
    throw new InvalidOperationException($"Dosja e Frontend-it nuk u gjet: {frontendPath}");
}

FileExtensionContentTypeProvider contentTypes = new();
contentTypes.Mappings[".svg"] = "image/svg+xml";
contentTypes.Mappings[".webp"] = "image/webp";

PhysicalFileProvider frontendFiles = new(frontendPath);

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (AppException ex)
    {
        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(ex.Message, ex.Errors));
    }
    catch (Exception ex)
    {
        ILogger logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SchoolBuffet");
        logger.LogError(ex, "Gabim i pareparueshem ne {Path}", context.Request.Path);

        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(
            "Gabim i brendshem ne server. Njofto praneuesin e byfese."));
    }
});

app.Use(async (context, next) =>
{
    IHeaderDictionary headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "same-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; " +
        "form-action 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'";

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        headers["Cache-Control"] = "no-store";
    }

    await next();
});

if (settings.AllowedOrigins.Count > 0)
{
    app.UseCors("frontend");
}

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = frontendFiles,
    DefaultFileNames = new List<string> { "index.html" }
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = frontendFiles,
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = context =>
    {
        var path = context.Context.Request.Path;
        var value = path.Value ?? string.Empty;

        if (path.StartsWithSegments("/assets/img/uploads"))
        {
            context.Context.Response.Headers["Cache-Control"] = "public, max-age=600";
            return;
        }

        if (value.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/js")
            || path.StartsWithSegments("/css"))
        {
            context.Context.Response.Headers["Cache-Control"] = "no-cache";
        }
    }
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/health", async (SchoolBuffetDbContext db, SchoolClock clock) =>
{
    bool databaseUp = true;
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1");
    }
    catch (Exception)
    {
        databaseUp = false;
    }

    return new
    {
        status = databaseUp ? "ne pune" : "databaza nuk arrihet",
        databaza = databaseUp ? "PostgreSQL" : "jo e lidhur",
        koha = clock.ToLocal(clock.UtcNow).ToString("yyyy-MM-dd HH:mm:ss"),
        zonaOrare = clock.ZoneId
    };
});

app.MapGet("/admin", () => Results.Redirect("/admin.html"));

await RunStartupTasksAsync(app, settings);

app.Run();

static async Task RunStartupTasksAsync(WebApplication app, AppSettings settings)
{
    using IServiceScope scope = app.Services.CreateScope();
    SchoolBuffetDbContext db = scope.ServiceProvider.GetRequiredService<SchoolBuffetDbContext>();
    ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Nisja");

    if (settings.MigrateOnStartup)
    {
        logger.LogInformation("Perditesoi struktura e databazes me Entity Framework migrations");
        await db.Database.MigrateAsync();
    }

    if (settings.SeedOnStartup)
    {
        DbSeeder seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        SeedResult result = await seeder.SeedAsync();

        if (!string.IsNullOrWhiteSpace(result.AdminPassword))
        {
            logger.LogWarning("-------------------------------------------------");
            logger.LogWarning("LLOGARI TE REJA U KRIJUAN AUTOMATIKISHT");
            logger.LogWarning("Perdoruesi: {Username} | Fjalakalimi: {Password}", settings.AdminUsername, result.AdminPassword);
            logger.LogWarning("Fjalakalimi i punetonjesit eshte ne rreshtin tjeter.");
            logger.LogWarning("-------------------------------------------------");
        }

        if (!string.IsNullOrWhiteSpace(result.StaffPassword))
        {
            logger.LogWarning("Punetonjesi: {Username} | Fjalakalimi: {Password}", settings.StaffUsername, result.StaffPassword);
        }

        if (!string.IsNullOrWhiteSpace(result.KitchenPassword))
        {
            logger.LogWarning("Kuzhina: {Username} | Fjalakalimi: {Password}", settings.KitchenUsername, result.KitchenPassword);
        }

        if (result.AdminPassword is not null || result.StaffPassword is not null || result.KitchenPassword is not null)
        {
            logger.LogWarning("Ruaji keto fjalakalime dhe ndryshoji pas hyrjes se pare.");
        }

        if (result.DemoOrdersCreated > 0)
        {
            logger.LogInformation("Porosi demo u krijuan ne databaze: {Total}", result.DemoOrdersCreated);
        }
    }
}
