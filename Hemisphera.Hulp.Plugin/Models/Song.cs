using Hsp.Osc;
using Microsoft.Extensions.Logging;
using ReaSharp;
using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.Models;

public sealed class Song : IDisposable
{
  public required Track RootTrack { get; init; }
  public string Name => Region.Name;
  public required Region Region { get; init; }
  public required Track[] Tracks { get; init; }
  public List<TrackArea> RecordingAreas { get; set; } = [];
  public List<TrackSelector> Selectors { get; set; } = [];


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
    foreach (var track in Tracks)
    {
      RecordingAreas.AddRange(TrackArea.LoadRecordingAreas(track, this));
      LoadTrackSelectorsForTrack(track);
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


  private void LoadTrackSelectorsForTrack(Track track)
  {
    var selectors = track.EnumerateMediaItems()
      .Where(i => i.GetActiveTake()?.Name?.Equals("select", StringComparison.OrdinalIgnoreCase) == true).ToList();
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

  public IMessage GetOscMessage()
  {
    var bundle = new List<Message>
    {
      new Message("/hulp/region")
        .PushAtom((float)Region.Start.TotalSeconds)
        .PushAtom((float)Region.End.TotalSeconds)
    };
    foreach (var track in Tracks)
    {
      var index = Tracks.IndexOf(track);
      var msg = new Message($"/hulp/track/{index + 1}")
        .PushAtom(track.Name)
        .PushAtom(index)
        .PushAtom(track.Index);
      bundle.Add(msg);
    }

    var events = CollectEvents()
      .Concat(Enumerable.Repeat(default(HulpEvent), 24))
      .Take(24);
    bundle.AddRange(events.Select((ev, idx) => new Message($"/hulp/event/{idx + 1}")
      .PushAtom(ev?.Text ?? string.Empty)
      .PushAtom(ev?.Time ?? 0.0)));

    return new MessageBundle(bundle.ToArray());
  }

  private IEnumerable<HulpEvent> CollectEvents()
  {
    foreach (var area in RecordingAreas)
    {
      yield return new HulpEvent
      {
        Text = $"Record: {area.Name} {area.Item.Track.Name}",
        Time = area.Item.Start.TotalSeconds
      };
    }

    foreach (var sel in Selectors)
    {
      yield return new HulpEvent
      {
        Text = $"Select: {sel.Track.Name}",
        Time = sel.Start.TotalSeconds
      };
    }
  }
}