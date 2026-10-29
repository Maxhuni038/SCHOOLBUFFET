namespace SchoolBuffet.Api.Dtos;

public record StockItemDto(
    int ProductId,
    string ProductName,
    string CategoryName,
    string CategoryIcon,
    string CategoryAccentColor,
    decimal Price,
    bool TrackStock,
    int StockQuantity,
    int LowStockThreshold,
    bool IsActive,
    string StatusKey,
    string StatusLabel,
    int SoldToday,
    DateTime? UpdatedAtUtc);

public record StockSummaryDto(
    int TotalProducts,
    int TrackedProducts,
    int OutOfStock,
    int LowStock,
    int Healthy,
    int UnitsOnHand,
    decimal ValueOnHand,
    int SoldToday);

public record StockListDto(
    List<StockItemDto> Items,
    StockSummaryDto Summary,
    DateTime GeneratedAtUtc);

public record StockAdjustRequest(int? Change, string? Reason, string? Note);

public record StockMovementDto(
    int Id,
    int ProductId,
    string ProductName,
    int Change,
    int StockAfter,
    string Reason,
    string Reference,
    DateTime CreatedAtUtc,
    string? User);

public record StockMovementsDto(
    List<StockMovementDto> Movements,
    int Total,
    int Page,
    int PageSize);
