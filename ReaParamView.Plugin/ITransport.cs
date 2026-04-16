using ReaParamView.Types;

namespace ReaParamView.Plugin;

public interface ITransport
{
  Task StartAsync(CancellationToken ct);
  Task StopAsync(CancellationToken ct);
  Task SendMessage(MessageDto message, CancellationToken token);
}