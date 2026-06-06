using Hsp.Osc;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public interface IOscWriter
{
  Task WriteAsync(IMessage message, CancellationToken token = default);
}