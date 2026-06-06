using Hemisphera.Hulp.Shared;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public interface ITransport
{
  Task StartAsync(CancellationToken ct);
  Task StopAsync(CancellationToken ct);
}
