using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.Models;

public sealed class TrackSelector : IDisposable
{
  public required TimeSpan Start { get; set; }
  public required Track Track { get; set; }


  public override string ToString()
  {
    return $"{Track.Name} @ {Start}";
  }

  public void Dispose()
  {
  }
}