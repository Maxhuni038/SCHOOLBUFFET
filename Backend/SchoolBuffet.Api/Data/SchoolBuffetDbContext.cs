using Microsoft.EntityFrameworkCore;
using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Data;

public class SchoolBuffetDbContext : DbContext
{
    public SchoolBuffetDbContext(DbContextOptions<SchoolBuffetDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Username).HasMaxLength(40).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.PasswordHash).HasColumnName("PasswordHash");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Icon).HasMaxLength(30).IsRequired();
            entity.Property(x => x.AccentColor).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.SortOrder);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Price).HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(x => x.ImagePath).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Badge).HasMaxLength(24).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.IsActive);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(24).IsRequired();
            entity.Property(x => x.StudentName).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ClassName).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.TotalAmount).HasColumnType("numeric(12,2)").IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.Status);
            entity.HasOne(x => x.MarkedByUser)
                .WithMany(x => x.MarkedOrders)
                .HasForeignKey(x => x.MarkedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.UnitPrice).HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(x => x.LineTotal).HasColumnType("numeric(12,2)").IsRequired();
            entity.HasOne(x => x.Order)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.ProductId);
        });
    }
}
