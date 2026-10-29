using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolBuffet.Api.Services;

namespace SchoolBuffet.Api.Controllers;

[ApiController]
[Route("api/realtime")]
public class RealtimeController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RealtimeService _realtime;

    public RealtimeController(RealtimeService realtime)
    {
        _realtime = realtime;
    }

    [HttpGet("stream")]
    [AllowAnonymous]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers["Content-Type"] = "text/event-stream; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache, no-transform";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        await using RealtimeSubscription subscription = _realtime.Subscribe();

        await Write("retry: 2000\n\n", cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            RealtimeSignal signal;
            try
            {
                signal = await subscription.Reader.ReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            string payload = JsonSerializer.Serialize(signal, JsonOptions);
            await Write($"event: ndryshim\ndata: {payload}\n\n", cancellationToken);
        }
    }

    private async Task Write(string text, CancellationToken cancellationToken)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        await Response.Body.WriteAsync(bytes, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
