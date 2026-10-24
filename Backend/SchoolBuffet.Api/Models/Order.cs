namespace SchoolBuffet.Api.Models;

public class Order
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public int? MarkedByUserId { get; set; }

    public User? MarkedByUser { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
