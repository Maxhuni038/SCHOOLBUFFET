using System.Threading.Channels;

namespace SchoolBuffet.Api.Services;

public static class RealtimeKinds
{
    public const string Order = "porosi";

    public const string Payment = "pagese";

    public const string Stock = "inventar";

    public const string Catalog = "meny";

    public const string Heartbeat = "zemrat";
}

public sealed record RealtimeSignal(string Kind, int? OrderId, string? Code, string? StatusKey, DateTime AtUtc);

public sealed class RealtimeService
{
    private readonly List<Channel<RealtimeSignal>> _channels = new();
    private readonly object _gate = new();

    public int ClientCount
    {
        get
        {
            lock (_gate)
            {
                return _channels.Count;
            }
        }
    }

    public RealtimeSubscription Subscribe()
    {
        Channel<RealtimeSignal> channel = Channel.CreateBounded<RealtimeSignal>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        lock (_gate)
        {
            _channels.Add(channel);
        }

        return new RealtimeSubscription(this, channel);
    }

    public void Publish(string kind, int? orderId = null, string? code = null, string? statusKey = null)
    {
        RealtimeSignal signal = new(kind, orderId, code, statusKey, DateTime.UtcNow);
        Channel<RealtimeSignal>[] targets;

        lock (_gate)
        {
            targets = _channels.ToArray();
        }

        foreach (Channel<RealtimeSignal> channel in targets)
        {
            channel.Writer.TryWrite(signal);
        }
    }

    internal void Unsubscribe(Channel<RealtimeSignal> channel)
    {
        lock (_gate)
        {
            _channels.Remove(channel);
        }

        channel.Writer.TryComplete();
    }
}

public sealed class RealtimeSubscription : IAsyncDisposable
{
    private readonly RealtimeService _service;
    private readonly Channel<RealtimeSignal> _channel;

    internal RealtimeSubscription(RealtimeService service, Channel<RealtimeSignal> channel)
    {
        _service = service;
        _channel = channel;
    }

    public ChannelReader<RealtimeSignal> Reader => _channel.Reader;

    public ValueTask DisposeAsync()
    {
        _service.Unsubscribe(_channel);
        return ValueTask.CompletedTask;
    }
}
