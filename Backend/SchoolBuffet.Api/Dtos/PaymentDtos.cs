namespace SchoolBuffet.Api.Dtos;

public record PaymentDto(
    int Id,
    int OrderId,
    string OrderCode,
    string StudentName,
    string ClassName,
    decimal Amount,
    string MethodKey,
    string MethodLabel,
    string StatusKey,
    string StatusLabel,
    string Note,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    string? PaidBy,
    string OrderStatusKey,
    string OrderStatusLabel,
    DateTime OrderCreatedAtUtc);

public record PaymentTotalsDto(
    decimal Expected,
    decimal Collected,
    decimal Pending,
    int PaidCount,
    int PendingCount,
    int VoidCount);

public record PaymentListDto(
    List<PaymentDto> Payments,
    int Total,
    int Page,
    int PageSize,
    string From,
    string To,
    PaymentTotalsDto Totals);

public record PaymentActionRequest(string? Method, string? Note);
