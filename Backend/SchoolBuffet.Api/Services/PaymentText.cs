using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public static class PaymentText
{
    private static readonly Dictionary<PaymentMethod, string> MethodLabels = new()
    {
        [PaymentMethod.KarteKantine] = "Karte kantina",
        [PaymentMethod.ParaMeDore] = "Para me dore",
        [PaymentMethod.Tjetra] = "Tjetra"
    };

    private static readonly Dictionary<PaymentStatus, string> StatusLabels = new()
    {
        [PaymentStatus.NePaguar] = "Ne pagese",
        [PaymentStatus.Paguar] = "E paguar",
        [PaymentStatus.Anuluar] = "E anuluar"
    };

    public static string Label(this PaymentMethod method)
    {
        return MethodLabels.TryGetValue(method, out string? label) ? label : "Pa metode";
    }

    public static string Label(this PaymentStatus status)
    {
        return StatusLabels.TryGetValue(status, out string? label) ? label : "I panjohur";
    }

    public static bool TryMethod(string? value, out PaymentMethod method)
    {
        method = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (KeyValuePair<PaymentMethod, string> pair in MethodLabels)
        {
            if (string.Equals(pair.Key.ToString(), value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase))
            {
                method = pair.Key;
                return true;
            }
        }

        return false;
    }

    public static bool TryStatus(string? value, out PaymentStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (KeyValuePair<PaymentStatus, string> pair in StatusLabels)
        {
            if (string.Equals(pair.Key.ToString(), value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase))
            {
                status = pair.Key;
                return true;
            }
        }

        return false;
    }
}
