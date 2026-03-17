using System.Net;
using System.Net.Sockets;
using ReaParamView.Types;

namespace ReaParamView.Plugin;

public class UdpTransport : ITransport
{
  private readonly UdpClient _udpClient = new();
  private readonly IPEndPoint _endPoint = new(IPAddress.Loopback, 9000);

  public async Task SendMessage(MessageDto message, CancellationToken token)
  {
    await _udpClient.SendAsync(message.SerializeBinary(), _endPoint, token);
  }
}