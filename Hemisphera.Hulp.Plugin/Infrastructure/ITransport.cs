using ReaParamView.Types;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public interface ITransport
{
  Task StartAsync(CancellationToken ct);
  Task StopAsync(CancellationToken ct);
  Task SendMessage(MessageDto message, CancellationToken token);
}