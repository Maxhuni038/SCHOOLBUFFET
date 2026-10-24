using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public static class OrderStatusText
{
    private static readonly Dictionary<OrderStatus, string> Labels = new()
    {
        [OrderStatus.NePritje] = "Ne pritje",
        [OrderStatus.DukeUPergatitur] = "Duke u pergatitur",
        [OrderStatus.Gati] = "Gati",
        [OrderStatus.Perfunduar] = "Perfunduar"
    };

    public static string Label(this OrderStatus status)
    {
        return Labels.TryGetValue(status, out var label) ? label : "I panjohur";
    }

    public static bool TryParse(string? value, out OrderStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var pair in Labels)
        {
            if (string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Key.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                status = pair.Key;
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<OrderStatus> Flow { get; } = new[]
    {
        OrderStatus.NePritje,
        OrderStatus.DukeUPergatitur,
        OrderStatus.Gati,
        OrderStatus.Perfunduar
    };
}
