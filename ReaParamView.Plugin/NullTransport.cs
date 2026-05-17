using Microsoft.Extensions.Logging;
using ReaParamView.Types;

namespace ReaParamView.Plugin;

public class NullTransport : ITransport
{
  private readonly ILogger<NullTransport> _logger;

  public NullTransport(ILogger<NullTransport> logger)
  {
    _logger = logger;
  }

  public Task StartAsync(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public async Task SendMessage(MessageDto message, CancellationToken token)
  {
    _logger.LogInformation("Tick");
    _logger.LogInformation("Track: {track}", message.TrackName);
    _logger.LogInformation("Envelopes");
    foreach (var envelope in message.Envelopes)
    {
      _logger.LogInformation("Env: {name}: {value}", envelope.Name, envelope.FormattedValue);
    }
  }
}