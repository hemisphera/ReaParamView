using ReaParamView.Types;

namespace ReaParamView.Plugin;

public interface ITransport
{
  Task SendMessage(MessageDto message, CancellationToken token);
}