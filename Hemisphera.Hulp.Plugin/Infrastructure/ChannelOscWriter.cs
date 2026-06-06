using System.Threading.Channels;
using Hsp.Osc;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public class ChannelOscWriter : IOscWriter
{
  private readonly ChannelWriter<IMessage> _writer;


  public ChannelOscWriter(ChannelWriter<IMessage> writer)
  {
    _writer = writer;
  }


  public async Task WriteAsync(IMessage message, CancellationToken token = default)
  {
    await _writer.WriteAsync(message, token);
  }
}