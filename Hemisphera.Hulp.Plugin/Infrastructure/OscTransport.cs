using System.Net;
using Hemisphera.Hulp.Plugin.Settings;
using Hsp.Osc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReaParamView.Types;
using ReaSharp;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public class OscTransport : ITransport
{
  private readonly ILogger<OscTransport> _logger;
  private OscUdpClient? _client;
  private readonly MessageDto _lastMessage = new();


  public OscTransport(ILogger<OscTransport> logger)
  {
    _logger = logger;
  }


  public async Task StartAsync(CancellationToken ct)
  {
    await StopAsync(ct);
    
    _logger.LogInformation("Connecting ...");
    _client = CreateClient();
    if (_client == null) return;
    await _client.ConnectAsync();
    _logger.LogInformation("Connected");
  }

  private OscUdpClient? CreateClient()
  {
    var ini = ReaperGlobal.ReadSettings();
    if (ini == null) return null;

    var oscDevice = ReaperGlobal.EnumerateOscDevices()
      .FirstOrDefault(dev => dev.Name.Equals("Hulp", StringComparison.OrdinalIgnoreCase));
    if (oscDevice == null)
    {
      _logger.LogWarning("No OSC device 'Hulp' found");
      return null;
    }
    
    var client = new OscUdpClient(IPAddress.Parse(oscDevice.DeviceIp), oscDevice.DevicePort);  
    _logger.LogInformation("Created OSC transport to {ip}:{post}", client.Address, client.Port);
    return client;
  }

  public async Task StopAsync(CancellationToken ct)
  {
    _logger.LogInformation("Disconnecting ...");
    if (_client != null)
      await _client.DisconnectAsync();
    _logger.LogInformation("Disconnected");
  }

  public async Task Send(IMessage message, CancellationToken token = default)
  {
    if (_client == null) return;
    await message.Send(_client);
  }

  public async Task SendMessage(MessageDto message, CancellationToken token)
  {
    MessageBundle? bundle = null;
    var fullRefresh = false;
    if (_lastMessage.TrackName != message.TrackName)
    {
      bundle ??= [];
      bundle.Add(new Message("/hulp/curr/name").PushAtom(message.TrackName ?? string.Empty));
      fullRefresh = true;
    }

    for (var i = 0; i < ActiveEnvelopeMonitor.MaxSlots; i++)
    {
      var lastEnvelope = _lastMessage.Envelopes.FirstOrDefault(e => e.Slot == i);
      var newEnvelope = message.Envelopes.FirstOrDefault(e => e.Slot == i);
      if (!fullRefresh && !HasChanged(lastEnvelope, newEnvelope)) continue;

      bundle ??= [];
      bundle.Add(new Message($"/hulp/curr/fx/{i}/name")
        .PushAtom(newEnvelope?.Name ?? string.Empty));
      bundle.Add(new Message($"/hulp/curr/fx/{i}/value")
        .PushAtom(newEnvelope?.Value ?? 0.0)
        .PushAtom(newEnvelope?.FormattedValue ?? string.Empty));
    }

    if (bundle != null)
    {
      await Send(bundle, token);
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