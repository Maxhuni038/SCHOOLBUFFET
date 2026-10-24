using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolBuffet.Api.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SchoolBuffetDbContext>
{
    public SchoolBuffetDbContext CreateDbContext(string[] args)
    {
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.SetBasePath(Directory.GetCurrentDirectory());
        configurationBuilder.AddJsonFile("appsettings.json", optional: true);
        configurationBuilder.AddJsonFile("appsettings.Local.json", optional: true);
        configurationBuilder.AddEnvironmentVariables();

        IConfigurationRoot configuration = configurationBuilder.Build();

        string connectionString = configuration.GetConnectionString("SchoolBuffet")
            ?? throw new InvalidOperationException(
                "Lidhja ConnectionStrings:SchoolBuffet nuk u gjet per Entity Framework.");

        DbContextOptionsBuilder<SchoolBuffetDbContext> options = new DbContextOptionsBuilder<SchoolBuffetDbContext>();
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("SchoolBuffet.Api"));

        return new SchoolBuffetDbContext(options.Options);
    }
}
