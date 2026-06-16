namespace Hemisphera.Hulp.Plugin.Devices;

public interface IDevice
{
  void ChangeTrack();
  void SetParameter(int index, int value);
  event EventHandler<ParamterChangedEventArgs>? ParameterChanged;
}