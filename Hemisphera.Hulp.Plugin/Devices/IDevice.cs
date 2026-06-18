using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.Devices;

public interface IDevice
{
  void ChangeTrack(Track? currentTrack);
  void SetParameter(int index, int value);
  event EventHandler<ParameterChangedEventArgs>? ParameterChanged;
  void Connect();
}