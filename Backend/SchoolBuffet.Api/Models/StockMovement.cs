namespace SchoolBuffet.Api.Models;

public static class StockReasons
{
    public const string Order = "Porosi";

    public const string OrderCancelled = "Porosi e anuluar";

    public const string ManualAdd = "Shtim manual";

    public const string ManualRemove = "Zbritje manual";

    public const string Correction = "Korrektim inventari";
}

public class StockMovement
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int Change { get; set; }

    public int StockAfter { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public int? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }
}
