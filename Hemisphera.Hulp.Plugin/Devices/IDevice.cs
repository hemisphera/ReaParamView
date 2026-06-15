namespace Hemisphera.Hulp.Plugin.Devices;

public interface IDevice
{
  void Initialize();
  void SetParameter(int index, int value);
  event EventHandler<ParamterChangedEventArgs>? ParameterChanged;
}