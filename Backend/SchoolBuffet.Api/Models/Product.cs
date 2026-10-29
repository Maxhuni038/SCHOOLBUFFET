namespace SchoolBuffet.Api.Models;

public class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    public string Badge { get; set; } = string.Empty;

    public bool IsRecommended { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool TrackStock { get; set; }

    public int StockQuantity { get; set; }

    public int LowStockThreshold { get; set; } = 10;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();
}
