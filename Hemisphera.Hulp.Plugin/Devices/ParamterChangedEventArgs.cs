namespace Hemisphera.Hulp.Plugin.Devices;

public readonly struct ParamterChangedEventArgs
{
  public int ParameterIndex { get; }
  public int Value { get; }

  public ParamterChangedEventArgs(int parameterIndex, int value)
  {
    ParameterIndex = parameterIndex;
    Value = value;
  }
}