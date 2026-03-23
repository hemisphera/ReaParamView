using System.Net.Sockets;
using Microsoft.Extensions.Options;
using ReaParamView.Types;

namespace ReaParamView.Plugin;

public class UdpTransport : ITransport
{
  private readonly UdpClient _udpClient = new();
  private readonly MonitorSettings _settings;

  public UdpTransport(IOptions<MonitorSettings> settings)
  {
    _settings = settings.Value;
  }

  public async Task SendMessage(MessageDto message, CancellationToken token)
  {
    await _udpClient.SendAsync(message.SerializeBinary(), _settings.Host, _settings.Port, token);
  }
}