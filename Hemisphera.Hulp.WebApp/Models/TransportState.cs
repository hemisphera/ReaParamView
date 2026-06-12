namespace Hemisphera.Hulp.WebApp.Models;

public class TransportState
{
  public bool IsPlaying { get; set; }
  public bool IsPaused { get; set; }
  public bool IsRecording { get; set; }
  public double Position { get; set; }
  public double RegionStart { get; set; }
  public double RegionEnd { get; set; }
  public int SongId { get; set; }
  public string SongName { get; set; } = string.Empty;

  public double Remaining => RegionEnd > 0 ? Math.Max(0, RegionEnd - Position) : 0;
}