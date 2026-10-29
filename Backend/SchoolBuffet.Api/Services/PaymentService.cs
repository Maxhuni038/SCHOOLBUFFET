using Microsoft.EntityFrameworkCore;
using SchoolBuffet.Api.Data;
using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public sealed class PaymentService
{
    private readonly SchoolBuffetDbContext _db;
    private readonly SchoolClock _clock;
    private readonly RealtimeService _realtime;

    public PaymentService(SchoolBuffetDbContext db, SchoolClock clock, RealtimeService realtime)
    {
        _db = db;
        _clock = clock;
        _realtime = realtime;
    }

    public Payment OpenPaymentFor(Order order)
    {
        Payment payment = new()
        {
            OrderId = order.Id,
            Order = order,
            Amount = order.TotalAmount,
            Method = PaymentMethod.KarteKantine,
            Status = PaymentStatus.NePaguar,
            Note = string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        return payment;
    }

    public async Task<PaymentListDto> ListAsync(string? from, string? to, string? status, int page, int pageSize)
    {
        DateOnly today = _clock.Today;
        DateOnly toDate = _clock.TryDate(to, out DateOnly parsedTo) ? parsedTo : today;
        DateOnly fromDate = _clock.TryDate(from, out DateOnly parsedFrom) ? parsedFrom : toDate.AddDays(-6);

        if (fromDate > toDate)
        {
            AppThrow.BadRequest("Data e fillimit nuk mund te jete me e vonshme se data e fundit.");
        }

        if ((toDate.DayNumber - fromDate.DayNumber) + 1 > 92)
        {
            AppThrow.BadRequest("Periudha maksimale per pagesat eshte 92 dite.");
        }

        (DateTime startUtc, DateTime endUtc) = _clock.UtcDayRange(fromDate);
        DateTime rangeEnd = _clock.ToUtc(toDate.AddDays(1), TimeOnly.MinValue);

        IQueryable<Payment> query = _db.Payments
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.PaidByUser)
            .Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < rangeEnd);

        bool statusKnown = PaymentText.TryStatus(status, out PaymentStatus paymentStatus);
        if (!string.IsNullOrWhiteSpace(status) && !statusKnown)
        {
            AppThrow.BadRequest("Statusi i pageses nuk njihet. Statuset: NePaguar, Paguar, Anuluar.");
        }

        if (statusKnown)
        {
            query = query.Where(x => x.Status == paymentStatus);
        }

        int total = await query.CountAsync();
        int safePage = Math.Max(1, page);
        int safeSize = Math.Clamp(pageSize <= 0 ? 25 : pageSize, 1, 100);

        List<Payment> rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync();

        List<Payment> scoped = await _db.Payments
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < rangeEnd)
            .ToListAsync();

        return new PaymentListDto(
            rows.Select(Map).ToList(),
            total,
            safePage,
            safeSize,
            SchoolClock.FormatDay(fromDate),
            SchoolClock.FormatDay(toDate),
            new PaymentTotalsDto(
                Math.Round(scoped.Where(x => x.Status != PaymentStatus.Anuluar).Sum(x => x.Amount), 2, MidpointRounding.AwayFromZero),
                Math.Round(scoped.Where(x => x.Status == PaymentStatus.Paguar).Sum(x => x.Amount), 2, MidpointRounding.AwayFromZero),
                Math.Round(scoped.Where(x => x.Status == PaymentStatus.NePaguar).Sum(x => x.Amount), 2, MidpointRounding.AwayFromZero),
                scoped.Count(x => x.Status == PaymentStatus.Paguar),
                scoped.Count(x => x.Status == PaymentStatus.NePaguar),
                scoped.Count(x => x.Status == PaymentStatus.Anuluar)));
    }

    public async Task<PaymentDto> MarkPaidAsync(int paymentId, string? method, string? note, int userId)
    {
        Payment payment = await LoadAsync(paymentId);

        if (payment.Status == PaymentStatus.Paguar)
        {
            AppThrow.Conflict($"Pagesa per porosin {payment.Order?.Code} eshte shenuar tashme si e paguar.");
        }

        if (payment.Status == PaymentStatus.Anuluar)
        {
            AppThrow.Conflict($"Pagesa per porosin {payment.Order?.Code} eshte anuluar. Rihapojeni permark para se ta shenoni te paguar.");
        }

        PaymentMethod chosen = payment.Method;
        if (PaymentText.TryMethod(method, out PaymentMethod parsed))
        {
            chosen = parsed;
        }

        payment.Method = chosen;
        payment.Status = PaymentStatus.Paguar;
        payment.Amount = payment.Order?.TotalAmount ?? payment.Amount;
        payment.PaidAtUtc = DateTime.UtcNow;
        payment.PaidByUserId = userId;
        payment.Note = ApplyNote(payment.Note, note);

        await _db.SaveChangesAsync();
        _realtime.Publish(RealtimeKinds.Payment, payment.OrderId, payment.Order?.Code, payment.Status.ToString());

        return Map(payment);
    }

    public async Task<PaymentDto> MarkPendingAsync(int paymentId, string? note, int userId)
    {
        Payment payment = await LoadAsync(paymentId);

        if (payment.Status == PaymentStatus.NePaguar)
        {
            AppThrow.Conflict($"Pagesa per porosin {payment.Order?.Code} eshte tashme ne pagese.");
        }

        payment.Status = PaymentStatus.NePaguar;
        payment.PaidAtUtc = null;
        payment.PaidByUserId = null;
        payment.Note = ApplyNote(payment.Note, note);

        await _db.SaveChangesAsync();
        _realtime.Publish(RealtimeKinds.Payment, payment.OrderId, payment.Order?.Code, payment.Status.ToString());

        return Map(payment);
    }

    public async Task<PaymentDto> VoidAsync(int paymentId, string? note, int userId)
    {
        Payment payment = await LoadAsync(paymentId);

        if (payment.Status == PaymentStatus.Anuluar)
        {
            AppThrow.Conflict($"Pagesa per porosin {payment.Order?.Code} eshte tashme e anuluar.");
        }

        payment.Status = PaymentStatus.Anuluar;
        payment.PaidAtUtc = null;
        payment.PaidByUserId = null;
        payment.Note = ApplyNote(payment.Note, note);

        await _db.SaveChangesAsync();
        _realtime.Publish(RealtimeKinds.Payment, payment.OrderId, payment.Order?.Code, payment.Status.ToString());

        return Map(payment);
    }

    public async Task<PaymentDto> SetMethodAsync(int paymentId, string? method)
    {
        if (!PaymentText.TryMethod(method, out PaymentMethod parsed))
        {
            AppThrow.BadRequest("Metoda e pageses nuk njihet. Metodat: KarteKantine, ParaMeDore, Tjetra.");
        }

        Payment payment = await LoadAsync(paymentId);
        payment.Method = parsed;
        payment.Amount = payment.Order?.TotalAmount ?? payment.Amount;

        await _db.SaveChangesAsync();
        _realtime.Publish(RealtimeKinds.Payment, payment.OrderId, payment.Order?.Code, payment.Status.ToString());

        return Map(payment);
    }

    public async Task SettleOnHandoverAsync(int orderId, int userId)
    {
        Payment? payment = await _db.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId);
        if (payment is null || payment.Status != PaymentStatus.NePaguar)
        {
            return;
        }

        payment.Status = PaymentStatus.Paguar;
        payment.PaidAtUtc = DateTime.UtcNow;
        payment.PaidByUserId = userId;

        await _db.SaveChangesAsync();
    }

    private static bool TryMethodOnly(string? value, out PaymentMethod method)
    {
        method = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Enum.TryParse(value, true, out method);
    }

    private async Task<Payment> LoadAsync(int paymentId)
    {
        return await _db.Payments
            .Include(x => x.Order)
            .Include(x => x.PaidByUser)
            .FirstOrDefaultAsync(x => x.Id == paymentId)
            ?? NotFound<Payment>("Pagesa nuk u gjet.");
    }

    private static string ApplyNote(string current, string? replacement)
    {
        if (replacement is null)
        {
            return current;
        }

        string text = replacement.Trim();
        return text.Length > 240 ? text[..240] : text;
    }

    private static PaymentDto Map(Payment payment)
    {
        return new PaymentDto(
            payment.Id,
            payment.OrderId,
            payment.Order?.Code ?? string.Empty,
            payment.Order?.StudentName ?? string.Empty,
            payment.Order?.ClassName ?? string.Empty,
            payment.Amount,
            payment.Method.ToString(),
            payment.Method.Label(),
            payment.Status.ToString(),
            payment.Status.Label(),
            payment.Note,
            payment.CreatedAtUtc,
            payment.PaidAtUtc,
            payment.PaidByUser?.FullName,
            payment.Order?.Status.ToString() ?? string.Empty,
            payment.Order?.Status.Label() ?? string.Empty,
            payment.Order?.CreatedAtUtc ?? payment.CreatedAtUtc);
    }

    private static T NotFound<T>(string message)
    {
        AppThrow.NotFound(message);
        return default!;
    }
}
