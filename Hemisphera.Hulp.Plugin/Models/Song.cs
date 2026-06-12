using Hsp.Osc;
using Microsoft.Extensions.Logging;
using ReaSharp;
using ReaSharp.Models;
using ReaSharp.RppXml.Nodes;

namespace Hemisphera.Hulp.Plugin.Models;

public sealed class Song : IDisposable
{
  public required Track RootTrack { get; init; }
  public string Name => Region.Name;
  public required Region Region { get; init; }
  public required Track[] Tracks { get; init; }
  public List<TrackArea> RecordingAreas { get; set; } = [];
  public List<TrackSelector> Selectors { get; set; } = [];
  public List<SongNote> Notes { get; set; } = [];


  public static Song? FromRegion(Region region, string containerName)
  {
    var trackTree = TrackTreeItem.Build(region.Project);
    var container = trackTree.FirstOrDefault(t => t.Track.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase));
    var rootTrack = container?.Children.FirstOrDefault(ch => ch.Track.Name.Equals(region.Name, StringComparison.OrdinalIgnoreCase));
    if (rootTrack == null) return null;

    var song = new Song
    {
      Region = region,
      RootTrack = rootTrack.Track,
      Tracks = rootTrack.FlattenChildren().Select(c => c.Track).ToArray()
    };
    return song;
  }


  private Song()
  {
  }


  public void SetActive(bool isActive)
  {
    foreach (var track in Tracks)
    {
      track.Mute = !isActive;
      track.ShowInTcp = isActive;
      track.FxBypassed = !isActive;
    }
  }

  public void Initialize(TimeSpan pos)
  {
    RecordingAreas.Clear();
    Selectors.Clear();
    Notes.Clear();

    foreach (var track in Tracks)
    {
      LoadRecordingAreasForTrack(track);
      LoadTrackSelectorsForTrack(track);
      LoadNotesForTrack(track);
    }

    foreach (var area in RecordingAreas)
    {
      area.Initialize();
      if (area.Item.Start > pos)
      {
        area.Clean();
      }
    }

    Reaper.UpdateArrange.Invoke();
  }

  private void LoadNotesForTrack(Track track)
  {
    var items = track.EnumerateMediaItems()
      .Where(i => i.GetActiveTake() == null)
      .ToList();
    foreach (var item in items)
    {
      var notes = item.GetStateChunk()?.FindChild<RppNotesNode>()?.Text;
      if (string.IsNullOrEmpty(notes)) continue;
      Notes.Add(new SongNote { Text = notes, Position = item.Start });
    }
  }

  private void LoadRecordingAreasForTrack(Track track)
  {
    RecordingAreas.AddRange(TrackArea.LoadRecordingAreas(track, this));
  }

  private void LoadTrackSelectorsForTrack(Track track)
  {
    var selectors = track.EnumerateMediaItems()
      .Where(i => i.GetActiveTake()?.Name?.Equals("select", StringComparison.OrdinalIgnoreCase) == true)
      .ToList();
    foreach (var selector in selectors)
    {
      Selectors.Add(new TrackSelector { Start = selector.Start, Track = track });
    }
  }

  public void Dump(ILogger logger)
  {
    if (!logger.IsEnabled(LogLevel.Debug)) return;

    logger.LogDebug("Song: {Name}", Name);
    logger.LogDebug("Tracks:");
    foreach (var track in Tracks)
    {
      logger.LogDebug("- {TrackName}", track.Name);
    }

    logger.LogDebug("Selectors:");
    foreach (var selector in Selectors)
    {
      logger.LogDebug("- {Selector}", selector);
    }

    logger.LogDebug("Areas:");
    foreach (var area in RecordingAreas)
    {
      logger.LogDebug("- {Name} ({Type})", area.Name, area.SourceType);
      if (area.AudioItem != null)
        logger.LogDebug("  Audio: {pos})", area.AudioItem);
      foreach (var subArea in area.LoopAreas)
      {
        logger.LogDebug("  - Loop: {pos})", subArea.Item);
        if (subArea.AudioItem != null)
          logger.LogDebug("    Audio: {pos})", subArea.AudioItem);
      }
    }
  }

  public void Dispose()
  {
    foreach (var area in RecordingAreas.ToArray())
    {
      area.Dispose();
      RecordingAreas.Remove(area);
    }

    foreach (var selector in Selectors.ToArray())
    {
      selector.Dispose();
      Selectors.Remove(selector);
    }
  }
}