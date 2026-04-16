using System.Net;
using Hsp.Osc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReaParamView.Types;

namespace ReaParamView.Plugin;

public class OscTransport : ITransport
{
  private readonly ILogger<OscTransport> _logger;
  private readonly OscUdpClient _client;
  private readonly MessageDto _lastMessage = new();


  public OscTransport(IOptions<MonitorSettings> settings, ILogger<OscTransport> logger)
  {
    _logger = logger;
    _client = new OscUdpClient(
      IPAddress.Parse(settings.Value.Host),
      settings.Value.Port
    );
    _logger.LogInformation("Created OSC transport to {ip}:{post}", _client.Address, _client.Port);
  }


  public async Task StartAsync(CancellationToken ct)
  {
    _logger.LogInformation("Connecting ...");
    await _client.ConnectAsync();
    _logger.LogInformation("Connected");
  }

  public async Task StopAsync(CancellationToken ct)
  {
    _logger.LogInformation("Disconnecting ...");
    await _client.DisconnectAsync();
    _logger.LogInformation("Disconnected");
  }

  public async Task SendMessage(MessageDto message, CancellationToken token)
  {
    MessageBundle? bundle = null;
    var fullRefresh = false;
    if (_lastMessage.TrackName != message.TrackName)
    {
      bundle ??= [];
      bundle.Add(new Message("/currenttrack/name").PushAtom(message.TrackName ?? string.Empty));
      fullRefresh = true;
    }

    for (var i = 0; i < ActiveEnvelopeMonitor.MaxSlots; i++)
    {
      var lastEnvelope = _lastMessage.Envelopes.FirstOrDefault(e => e.Slot == i);
      var newEnvelope = message.Envelopes.FirstOrDefault(e => e.Slot == i);
      if (!fullRefresh && !HasChanged(lastEnvelope, newEnvelope)) continue;

      bundle ??= [];

      bundle.Add(new Message($"/currenttrack/fx/{i + 1}/name").PushAtom(newEnvelope?.Name ?? string.Empty));
      bundle.Add(new Message($"/currenttrack/fx/{i + 1}/value/str").PushAtom(newEnvelope?.FormattedValue ?? string.Empty));
      bundle.Add(new Message($"/currenttrack/fx/{i + 1}/value").PushAtom(newEnvelope?.Value ?? 0.0));
    }

    if (bundle != null)
    {
      await bundle.Send(_client);
    }

    _lastMessage.TrackName = message.TrackName;
    _lastMessage.Envelopes = message.Envelopes;
  }

  private static bool HasChanged(EnvelopeDto? lastEnvelope, EnvelopeDto? newEnvelope)
  {
    if (Math.Abs((lastEnvelope?.Value ?? 0.0) - (newEnvelope?.Value ?? 0.0)) > 0.01)
      return true;
    if (!(lastEnvelope?.Name ?? string.Empty).Equals(newEnvelope?.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase))
      return true;
    if (!(lastEnvelope?.FormattedValue ?? string.Empty).Equals(newEnvelope?.FormattedValue ?? string.Empty, StringComparison.OrdinalIgnoreCase))
      return true;
    return false;
  }
}