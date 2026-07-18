namespace Hemisphera.Hulp.Plugin.Devices;

public readonly struct ActiveTrackChangedEventArgs
{
  public int TrackIndex { get; }


  public ActiveTrackChangedEventArgs(int trackIndex)
  {
    TrackIndex = trackIndex;
  }
}