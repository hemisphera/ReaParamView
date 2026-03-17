using ReaParamView.Types;

namespace ReaParamView.WebApp.Services;

public class ParameterDisplayService
{
  private MessageDto? _currentMessage;

  public event Action? OnChange;

  public MessageDto? CurrentMessage => _currentMessage;

  public void UpdateMessage(MessageDto message)
  {
    _currentMessage = message;
    OnChange?.Invoke();
  }
}