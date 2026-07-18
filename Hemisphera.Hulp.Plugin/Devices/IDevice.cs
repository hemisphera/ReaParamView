using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.Devices;

public interface IDevice
{
  void SetParameter(int index, int value);
  void SetActiveTrack(int index);
  event EventHandler<ParameterChangedEventArgs>? ParameterChanged;
  event EventHandler<ActiveTrackChangedEventArgs>? ActiveTrackChanged;
  void Connect();
}