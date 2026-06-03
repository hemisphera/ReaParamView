using Microsoft.Extensions.Logging;
using ReaParamView.Types;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

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

  public async Task SendMessage(ParameterSetDto parameterSet, CancellationToken token)
  {
    _logger.LogInformation("Tick");
    _logger.LogInformation("Track: {track}", parameterSet.TrackName);
    _logger.LogInformation("Envelopes");
    foreach (var envelope in parameterSet.Envelopes)
    {
      _logger.LogInformation("Env: {name}: {value}", envelope.Name, envelope.FormattedValue);
    }
  }
}