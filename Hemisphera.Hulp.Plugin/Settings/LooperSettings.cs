namespace Hemisphera.Hulp.Plugin.Settings;

public class LooperSettings
{
  /// <summary>
  /// Number of beats to look ahead for upcoming recording start/stop triggers.
  /// Defaults to 1 beat. Should not exceed a full measure to avoid interfering with timing.
  /// </summary>
  public double LookaheadBeats { get; set; } = 1.0;

  /// <summary>
  /// Automatically selects the track of a recording area without requiring a dedicated track selector item.
  /// </summary>
  public bool AutoSelectTrackForRecordingAreas { get; set; } = true;

  /// <summary>
  /// Specifies if playback is stopped, once the end of a region was reached. 
  /// </summary>
  public bool StopAtEndOfRegon { get; set; } = true;

  /// <summary>
  /// The name of the container where song tracks are stored.
  /// </summary>
  public string ContainerTrackName { get; set; } = "Songs";
}