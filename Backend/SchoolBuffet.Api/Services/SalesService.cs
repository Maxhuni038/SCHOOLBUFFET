using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SchoolBuffet.Api.Data;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public sealed class SalesService
{
    private readonly SchoolBuffetDbContext _db;
    private readonly SchoolClock _clock;

    public SalesService(SchoolBuffetDbContext db, SchoolClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SalesSummaryDto> GetSummaryAsync(int windowDays)
    {
        int days = Math.Clamp(windowDays, 1, 30);
        DateOnly today = _clock.Today;
        DateOnly windowStart = today.AddDays(-(days - 1));

        DateTime todayStartUtc = _clock.ToUtc(today, TimeOnly.MinValue);
        DateTime todayEndUtc = _clock.ToUtc(today.AddDays(1), TimeOnly.MinValue);
        DateTime windowStartUtc = _clock.ToUtc(windowStart, TimeOnly.MinValue);

        List<Order> todayOrders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CreatedAtUtc >= todayStartUtc && x.CreatedAtUtc < todayEndUtc)
            .ToListAsync();

        List<Order> windowOrders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CreatedAtUtc >= windowStartUtc && x.CreatedAtUtc < todayEndUtc)
            .ToListAsync();

        List<Order> recentOrders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CreatedAtUtc >= _clock.ToUtc(today.AddDays(-29), TimeOnly.MinValue) && x.CreatedAtUtc < todayEndUtc)
            .ToListAsync();

        decimal revenueToday = todayOrders.Sum(x => x.TotalAmount);
        int itemsToday = todayOrders.Sum(x => x.Items.Sum(i => i.Quantity));
        decimal average = todayOrders.Count == 0 ? 0m : Math.Round(revenueToday / todayOrders.Count, 2, MidpointRounding.AwayFromZero);

        decimal revenueAll = await _db.Orders.SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;
        int ordersAll = await _db.Orders.CountAsync();

        List<DailySalesDto> lastDays = new();
        for (int offset = 0; offset < days; offset++)
        {
            DateOnly day = windowStart.AddDays(offset);
            List<Order> dayOrders = windowOrders
                .Where(x => _clock.ToLocalDate(x.CreatedAtUtc) == day)
                .ToList();

            lastDays.Add(new DailySalesDto(
                SchoolClock.FormatDay(day),
                dayOrders.Sum(x => x.TotalAmount),
                dayOrders.Count,
                dayOrders.Sum(x => x.Items.Sum(i => i.Quantity))));
        }

        List<StatusCountDto> statusCounts = OrderStatusText.Flow
            .Select(status => new StatusCountDto(
                status.ToString(),
                status.Label(),
                windowOrders.Count(x => x.Status == status)))
            .ToList();

        List<TopProductDto> topProducts = recentOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductDto(
                g.Key,
                g.Sum(i => i.Quantity),
                Math.Round(g.Sum(i => i.LineTotal), 2, MidpointRounding.AwayFromZero)))
            .OrderByDescending(x => x.Revenue)
            .ThenBy(x => x.ProductName)
            .Take(6)
            .ToList();

        return new SalesSummaryDto(
            SchoolClock.FormatDay(today),
            revenueToday,
            todayOrders.Count,
            itemsToday,
            average,
            revenueAll,
            ordersAll,
            statusCounts,
            lastDays,
            topProducts,
            await _db.Products.CountAsync(),
            await _db.Products.CountAsync(x => x.IsActive),
            await _db.Categories.CountAsync());
    }

    public async Task<SalesHistoryDto> GetHistoryAsync(string? from, string? to)
    {
        DateOnly today = _clock.Today;
        DateOnly toDate = _clock.TryDate(to, out DateOnly parsedTo) ? parsedTo : today;
        DateOnly fromDate = _clock.TryDate(from, out DateOnly parsedFrom) ? parsedFrom : toDate.AddDays(-29);

        if (fromDate > toDate)
        {
            AppThrow.BadRequest("Data e fillimit nuk mund te jete me e vonshme se data e fundit.");
        }

        int span = (toDate.DayNumber - fromDate.DayNumber) + 1;
        if (span > 366)
        {
            AppThrow.BadRequest("Gjatesia maksimale e historikut eshte 366 dite.");
        }

        DateTime startUtc = _clock.ToUtc(fromDate, TimeOnly.MinValue);
        DateTime endUtc = _clock.ToUtc(toDate.AddDays(1), TimeOnly.MinValue);

        List<Order> orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        List<DailySalesDto> days = new();
        for (int offset = 0; offset < span; offset++)
        {
            DateOnly day = fromDate.AddDays(offset);
            List<Order> dayOrders = orders.Where(x => _clock.ToLocalDate(x.CreatedAtUtc) == day).ToList();
            days.Add(new DailySalesDto(
                SchoolClock.FormatDay(day),
                dayOrders.Sum(x => x.TotalAmount),
                dayOrders.Count,
                dayOrders.Sum(x => x.Items.Sum(i => i.Quantity))));
        }

        return new SalesHistoryDto(
            SchoolClock.FormatDay(fromDate),
            SchoolClock.FormatDay(toDate),
            orders.Sum(x => x.TotalAmount),
            orders.Count,
            orders.Sum(x => x.Items.Sum(i => i.Quantity)),
            days,
            orders.Take(250).Select(Map).ToList());
    }

    public async Task<string> BuildCsvAsync(string? from, string? to)
    {
        SalesHistoryDto history = await GetHistoryAsync(from, to);

        StringBuilder builder = new();
        builder.AppendLine("Kodi i porosise;Data;Ora;Studenti;Klasa;Statusi;Artikuj;Totali (EUR)");

        foreach (OrderDto order in history.OrdersDetail)
        {
            DateTime local = _clock.ToLocal(order.CreatedAtUtc);

            builder.Append(Escape(order.Code)).Append(';')
                .Append(local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';')
                .Append(local.ToString("HH:mm", CultureInfo.InvariantCulture)).Append(';')
                .Append(Escape(order.StudentName)).Append(';')
                .Append(Escape(order.ClassName)).Append(';')
                .Append(Escape(order.StatusLabel)).Append(';')
                .Append(order.ItemCount.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture))
                .AppendLine();
        }

        builder.AppendLine();
        builder.Append("TOTALI;;;")
            .Append(Escape($"Periudha {history.From} deri {history.To}")).Append(';')
            .Append(history.Orders.ToString(CultureInfo.InvariantCulture)).Append(';')
            .Append(history.Items.ToString(CultureInfo.InvariantCulture)).Append(';')
            .Append(history.Revenue.ToString("0.00", CultureInfo.InvariantCulture));

        return builder.ToString();
    }

    private static OrderDto Map(Order order)
    {
        List<OrderItemDto> items = order.Items
            .OrderBy(x => x.ProductName)
            .Select(x => new OrderItemDto(x.Id, x.ProductId, x.ProductName, x.UnitPrice, x.Quantity, x.LineTotal))
            .ToList();

        return new OrderDto(
            order.Id,
            order.Code,
            order.StudentName,
            order.ClassName,
            order.Note,
            order.Status.ToString(),
            order.Status.Label(),
            order.TotalAmount,
            items.Sum(x => x.Quantity),
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.CompletedAtUtc,
            order.MarkedByUser?.FullName,
            items);
    }

    private static string Escape(string? value)
    {
        string text = (value ?? string.Empty)
            .Replace(";", " ")
            .Replace("\r", " ")
            .Replace("\n", " ");

        if (text.Contains('"', StringComparison.Ordinal))
        {
            text = "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return text;
    }
}
