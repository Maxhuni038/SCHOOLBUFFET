namespace SchoolBuffet.Api.Models;

public enum PaymentMethod
{
    KarteKantine = 1,
    ParaMeDore = 2,
    Tjetra = 3
}

public enum PaymentStatus
{
    NePaguar = 1,
    Paguar = 2,
    Anuluar = 3
}

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.KarteKantine;

    public PaymentStatus Status { get; set; } = PaymentStatus.NePaguar;

    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public int? PaidByUserId { get; set; }

    public User? PaidByUser { get; set; }
}
