namespace SchoolBuffet.Api.Dtos;

public record KitchenTicketItemDto(string ProductName, int Quantity);

public record KitchenTicketDto(
    int Id,
    string Code,
    string StudentName,
    string ClassName,
    string Note,
    string StatusKey,
    string StatusLabel,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? PreparingAtUtc,
    DateTime? ReadyAtUtc,
    int WaitingSeconds,
    int PrepSeconds,
    int ItemCount,
    decimal TotalAmount,
    List<KitchenTicketItemDto> Items);

public record KitchenStatsDto(
    int NewCount,
    int PreparingCount,
    int ReadyCount,
    int OrdersToday,
    int CompletedToday,
    int ItemsToday,
    double AveragePrepMinutes,
    int OldestWaitingSeconds,
    int ConnectedScreens);

public record KitchenBoardDto(
    string Day,
    DateTime GeneratedAtUtc,
    List<KitchenTicketDto> New,
    List<KitchenTicketDto> Preparing,
    List<KitchenTicketDto> Ready,
    KitchenStatsDto Stats);

public record KitchenActionDto(
    KitchenTicketDto Ticket,
    KitchenStatsDto Stats,
    string Message);
