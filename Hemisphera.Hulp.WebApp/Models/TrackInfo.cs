namespace Hemisphera.Hulp.WebApp.Models;

public class TrackInfo
{
  public int Index { get; }

  public string Name { get; set; } = string.Empty;
  public bool Selected { get; set; }
  public bool Mute { get; set; }
  public bool Solo { get; set; }
  public bool RecArm { get; set; }


  public TrackInfo(int index)
  {
    Index = index;
  }
}
