using System.Net.Sockets;
using ReaParamView.Types;

namespace ReaParamView.WebApp.Services;

public class UdpReceiverService(ParameterDisplayService displayService, ILogger<UdpReceiverService> logger) : BackgroundService
{
  private const int Port = 9000;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var udpClient = new UdpClient(Port);
    logger.LogInformation("UDP receiver listening on port {Port}", Port);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var result = await udpClient.ReceiveAsync(stoppingToken);
        var message = MessageDto.Deserialize(result.Buffer);
        displayService.UpdateMessage(message);
      }
      catch (OperationCanceledException)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error receiving UDP message");
      }
    }
  }
}