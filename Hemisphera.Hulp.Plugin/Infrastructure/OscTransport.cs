using System.Net;
using System.Threading.Channels;
using Hsp.Osc;
using Microsoft.Extensions.Logging;
using ReaSharp;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public class OscTransport : ITransport
{
  private readonly ILogger<OscTransport> _logger;
  private readonly ChannelReader<IMessage> _reader;
  private OscUdpClient? _client;


  public OscTransport(ILogger<OscTransport> logger, ChannelReader<IMessage> reader)
  {
    _logger = logger;
    _reader = reader;
  }


  public async Task StartAsync(CancellationToken ct)
  {
    await StopAsync(ct);

    _logger.LogInformation("Connecting ...");
    _client = CreateClient();
    if (_client == null) return;
    await _client.ConnectAsync();
    _logger.LogInformation("Connected");
    _ = Task.Run(async () =>
    {
      try
      {
        await foreach (var item in _reader.ReadAllAsync(ct))
        {
          await _client.SendMessageAsync(item);
        }
      }
      catch
      {
        // ignore
      }
    }, ct);
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
    {
      await _client.DisconnectAsync();
    }

    _logger.LogInformation("Disconnected");
  }

  public async Task Send(IMessage message, CancellationToken token = default)
  {
    if (_client == null) return;
    await message.Send(_client);
  }
}