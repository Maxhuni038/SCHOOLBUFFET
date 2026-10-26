namespace SchoolBuffet.Api.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = "sandwich";

    public string AccentColor { get; set; } = "#6c5ce7";

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public List<Product> Products { get; set; } = new();
}
