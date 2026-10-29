using SchoolBuffet.Api.Dtos;
using SchoolBuffet.Api.Models;

namespace SchoolBuffet.Api.Services;

public static class OrderFlow
{
    public static readonly OrderStatus[] Sequence =
    {
        OrderStatus.NePritje,
        OrderStatus.DukeUPergatitur,
        OrderStatus.Gati,
        OrderStatus.Perfunduar
    };

    public static bool IsForward(OrderStatus from, OrderStatus to)
    {
        return Index(to) > Index(from);
    }

    public static bool IsSingleStepBack(OrderStatus from, OrderStatus to)
    {
        return Index(from) - Index(to) == 1;
    }

    public static int Index(OrderStatus status)
    {
        return Array.IndexOf(Sequence, status);
    }

    public static void ApplyTimestamps(Order order, OrderStatus next, DateTime utcNow)
    {
        if (next == OrderStatus.DukeUPergatitur)
        {
            order.PreparingAtUtc ??= utcNow;
        }

        if (next == OrderStatus.Gati)
        {
            order.PreparingAtUtc ??= utcNow;
            order.ReadyAtUtc ??= utcNow;
        }

        if (Index(next) < Index(OrderStatus.DukeUPergatitur))
        {
            order.PreparingAtUtc = null;
            order.ReadyAtUtc = null;
        }

        if (Index(next) < Index(OrderStatus.Gati))
        {
            order.ReadyAtUtc = null;
        }

        order.CompletedAtUtc = next == OrderStatus.Perfunduar ? utcNow : null;
    }

    public static string WaitingLabel(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.NePritje => "Ne radhe per pergatitje",
            OrderStatus.DukeUPergatitur => "Duke u pergatitur",
            OrderStatus.Gati => "Gati per marrje",
            _ => "Mbyllur"
        };
    }
}
