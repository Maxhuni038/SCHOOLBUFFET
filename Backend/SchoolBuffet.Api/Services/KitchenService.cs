using Microsoft.EntityFrameworkCore;
using SchoolBuffet.Api.Data;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public sealed class KitchenService
{
    private const int BoardLimit = 60;

    private readonly SchoolBuffetDbContext _db;
    private readonly SchoolClock _clock;
    private readonly RealtimeService _realtime;

    public KitchenService(SchoolBuffetDbContext db, SchoolClock clock, RealtimeService realtime)
    {
        _db = db;
        _clock = clock;
        _realtime = realtime;
    }

    public async Task<KitchenBoardDto> GetBoardAsync()
    {
        DateOnly today = _clock.Today;
        (DateTime startUtc, DateTime endUtc) = _clock.UtcDayRange(today);
        DateTime utcNow = DateTime.UtcNow;

        List<Order> rows = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

        List<KitchenTicketDto> waiting = Ticketize(rows, OrderStatus.NePritje, utcNow);
        List<KitchenTicketDto> preparing = Ticketize(rows, OrderStatus.DukeUPergatitur, utcNow);
        List<KitchenTicketDto> ready = Ticketize(rows, OrderStatus.Gati, utcNow);

        List<Order> open = rows.Where(x => x.Status != OrderStatus.Perfunduar).ToList();
        int? oldestSeconds = open
            .Select(x => (int?)Math.Max(0, (utcNow - x.CreatedAtUtc).TotalSeconds))
            .OrderByDescending(x => x)
            .FirstOrDefault();

        List<Order> served = rows.Where(x => x.Status == OrderStatus.Perfunduar && x.CompletedAtUtc != null).ToList();
        double average = served.Count == 0
            ? 0d
            : Math.Round(served.Average(x => (x.CompletedAtUtc!.Value - x.CreatedAtUtc).TotalMinutes), 1);

        return new KitchenBoardDto(
            SchoolClock.FormatDay(today),
            utcNow,
            waiting,
            preparing,
            ready,
            new KitchenStatsDto(
                waiting.Count,
                preparing.Count,
                ready.Count,
                rows.Count,
                rows.Count(x => x.Status == OrderStatus.Perfunduar),
                rows.Sum(x => x.Items.Sum(i => i.Quantity)),
                average,
                oldestSeconds ?? 0,
                _realtime.ClientCount));
    }

    private List<KitchenTicketDto> Ticketize(List<Order> rows, OrderStatus status, DateTime utcNow)
    {
        return rows
            .Where(x => x.Status == status)
            .Take(BoardLimit)
            .Select(x => new KitchenTicketDto(
                x.Id,
                x.Code,
                x.StudentName,
                x.ClassName,
                x.Note,
                x.Status.ToString(),
                OrderFlow.WaitingLabel(x.Status),
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.PreparingAtUtc,
                x.ReadyAtUtc,
                Elapsed(utcNow, x.CreatedAtUtc),
                Elapsed(utcNow, x.PreparingAtUtc),
                x.Items.Sum(i => i.Quantity),
                x.TotalAmount,
                x.Items
                    .OrderBy(i => i.ProductName, StringComparer.OrdinalIgnoreCase)
                    .Select(i => new KitchenTicketItemDto(i.ProductName, i.Quantity))
                    .ToList()))
            .ToList();
    }

    private static int Elapsed(DateTime utcNow, DateTime? from)
    {
        return from is null ? 0 : Math.Max(0, (int)(utcNow - from.Value).TotalSeconds);
    }
}
