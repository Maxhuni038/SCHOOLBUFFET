namespace SchoolBuffet.Api.Dtos;

public record OrderItemInput(int? ProductId, int? Quantity);

public record CreateOrderRequest(
    string? StudentName,
    string? ClassName,
    string? Note,
    List<OrderItemInput>? Items);

public record OrderItemDto(
    int Id,
    int? ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public record OrderDto(
    int Id,
    string Code,
    string StudentName,
    string ClassName,
    string Note,
    string StatusKey,
    string StatusLabel,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? PreparingAtUtc,
    DateTime? ReadyAtUtc,
    DateTime? CompletedAtUtc,
    string? HandledBy,
    string? PaymentStatusKey,
    string? PaymentStatusLabel,
    List<OrderItemDto> Items);

public record UpdateStatusRequest(string? Status);

public record OrderListDto(
    List<OrderDto> Orders,
    int Total,
    int Page,
    int PageSize,
    string? StatusFilter,
    string? Search,
    string? Date);
