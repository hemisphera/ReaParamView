using ReaParamView.Types;

namespace ReaParamView.WebApp.Services;

public class ParameterDisplayService
{
  private ParameterSetDto? _currentMessage;

  public event Action? OnChange;

  public ParameterSetDto? CurrentMessage => _currentMessage;

  public void UpdateMessage(ParameterSetDto parameterSet)
  {
    _currentMessage = parameterSet;
    OnChange?.Invoke();
  }
}