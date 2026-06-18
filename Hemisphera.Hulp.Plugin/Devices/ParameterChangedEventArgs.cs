namespace Hemisphera.Hulp.Plugin.Devices;

public readonly struct ParameterChangedEventArgs
{
  public int ParameterIndex { get; }
  public int Value { get; }

  public ParameterChangedEventArgs(int parameterIndex, int value)
  {
    ParameterIndex = parameterIndex;
    Value = value;
  }
}