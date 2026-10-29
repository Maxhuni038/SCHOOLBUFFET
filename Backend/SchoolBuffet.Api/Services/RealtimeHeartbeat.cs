namespace SchoolBuffet.Api.Services;

public sealed class RealtimeHeartbeat : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(20);

    private readonly RealtimeService _realtime;

    public RealtimeHeartbeat(RealtimeService realtime)
    {
        _realtime = realtime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _realtime.Publish(RealtimeKinds.Heartbeat);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
