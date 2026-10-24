namespace SchoolBuffet.Api.Dtos;

public record DailySalesDto(string Date, decimal Revenue, int Orders, int Items);

public record TopProductDto(string ProductName, int Quantity, decimal Revenue);

public record StatusCountDto(string StatusKey, string StatusLabel, int Count);

public record SalesSummaryDto(
    string Date,
    decimal RevenueToday,
    int OrdersToday,
    int ItemsToday,
    decimal AverageOrderToday,
    decimal RevenueAll,
    int OrdersAll,
    List<StatusCountDto> StatusCounts,
    List<DailySalesDto> LastDays,
    List<TopProductDto> TopProducts,
    int TotalProducts,
    int ActiveProducts,
    int TotalCategories);

public record SalesHistoryDto(
    string From,
    string To,
    decimal Revenue,
    int Orders,
    int Items,
    List<DailySalesDto> Days,
    List<OrderDto> OrdersDetail);
