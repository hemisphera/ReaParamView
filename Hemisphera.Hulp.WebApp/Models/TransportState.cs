namespace ReaParamView.WebApp.Services;

public class TransportState
{
  public bool IsPlaying { get; set; }
  public bool IsPaused { get; set; }
  public bool IsRecording { get; set; }
  public double Position { get; set; }
  public double RegionStart { get; set; }
  public double RegionEnd { get; set; }

  public double RemainingSeconds => RegionEnd > 0 ? Math.Max(0, RegionEnd - Position) : 0;
}