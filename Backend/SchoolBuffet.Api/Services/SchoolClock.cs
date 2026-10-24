using System.Globalization;

namespace SchoolBuffet.Api.Services;

public sealed class SchoolClock
{
    private readonly TimeZoneInfo _zone;

    public SchoolClock(string? timeZoneId)
    {
        _zone = Resolve(timeZoneId);
    }

    public string ZoneId => _zone.Id;

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTimeOffset LocalNow
    {
        get
        {
            DateTime utc = DateTime.UtcNow;
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, _zone);
            return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), _zone.GetUtcOffset(utc));
        }
    }

    public DateOnly Today => DateOnly.FromDateTime(LocalNow.DateTime);

    public DateOnly ToLocalDate(DateTime utcValue)
    {
        return DateOnly.FromDateTime(ToLocal(utcValue));
    }

    public DateTime ToLocal(DateTime utcValue)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcValue, DateTimeKind.Utc), _zone);
    }

    public DateTime ToUtc(DateOnly day, TimeOnly time)
    {
        DateTime local = day.ToDateTime(time);
        DateTimeOffset offset = new(local, _zone.GetUtcOffset(local));
        return offset.UtcDateTime;
    }

    public (DateTime StartUtc, DateTime EndUtc) UtcDayRange(DateOnly day)
    {
        DateTime startLocal = day.ToDateTime(TimeOnly.MinValue);
        DateTime endLocal = day.ToDateTime(TimeOnly.MinValue).AddDays(1);
        DateTime startUtc = new DateTimeOffset(startLocal, _zone.GetUtcOffset(startLocal)).UtcDateTime;
        DateTime endUtc = new DateTimeOffset(endLocal, _zone.GetUtcOffset(endLocal)).UtcDateTime;
        return (startUtc, endUtc);
    }

    public bool TryDate(string? value, out DateOnly day)
    {
        day = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out day);
    }

    public static string FormatDay(DateOnly day)
    {
        return day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Tirane");
        }
        catch (Exception)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
