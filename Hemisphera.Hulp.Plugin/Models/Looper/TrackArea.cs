using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReaSharp;
using ReaSharp.Models;
using ReaSharp.Utils;

namespace Hemisphera.Hulp.Plugin.Models.Looper;

public sealed class TrackArea : IDisposable
{
  public int Index { get; private set; }
  public string Name { get; }
  public Song Song { get; }
  public TrackMediaItem Item { get; }
  public TrackMediaItem? AudioItem { get; private set; }
  public TrackMediaItemWatcher? Watcher { get; }
  public AreaType AreaType { get; }
  public SourceType SourceType { get; }
  public List<TrackArea> LoopAreas { get; } = [];
  public Project Project { get; }
  public AreaState State { get; private set; }

  private readonly ILogger _logger;


  public static TrackArea[] LoadRecordingAreas(Track track, Song song)
  {
    var grouped = EnumerateAll(track, song)
      .GroupBy(area => area.Name, StringComparer.OrdinalIgnoreCase)
      .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

    var recordingAreas = grouped
      .Select(group => group.Value.FirstOrDefault(a => a.AreaType == AreaType.Record))
      .OfType<TrackArea>()
      .ToList();

    foreach (var recordingArea in recordingAreas)
    {
      var byName = grouped[recordingArea.Name];
      recordingArea.AudioItem = byName
        .FirstOrDefault(t => t.AreaType == AreaType.Audio && t.Item.Start == recordingArea.Item.Start)?
        .Item;

      recordingArea.LoopAreas.AddRange(byName.Where(t => t.AreaType == AreaType.Loop));
      foreach (var loopArea in recordingArea.LoopAreas)
      {
        loopArea.AudioItem = byName
          .FirstOrDefault(t => t.AreaType == AreaType.Audio && t.Item.Start == loopArea.Item.Start)?
          .Item;
      }
    }

    return recordingAreas.ToArray();
  }

  private static TrackArea[] EnumerateAll(Track track, Song song)
  {
    var result = new List<TrackArea>();
    foreach (var item in track.EnumerateMediaItems())
    {
      if (item.Start < song.Region.Start || item.Start > song.Region.End) continue;
      var take = item.GetActiveTake();
      if (take == null) continue;

      var parts = take.Name?.Split(':');
      if (parts == null || parts.Length < 2) continue;
      if (!Enum.TryParse<AreaType>(parts[0], true, out var type)) continue;
      if (string.IsNullOrEmpty(parts[1])) continue;
      result.Add(new TrackArea(parts[1], song, item, type));
    }

    var indices = new List<string>();
    foreach (var area in result.OrderBy(r => r.Name))
    {
      var idx = indices.FindIndex(i => i.Equals(area.Name, StringComparison.OrdinalIgnoreCase));
      if (idx < 0)
      {
        indices.Add(area.Name);
        idx = indices.Count - 1;
      }

      area.Index = idx;
    }

    return result.ToArray();
  }


  private TrackArea(string name, Song region, TrackMediaItem item, AreaType areaType)
  {
    _logger = PluginState.Instance.Services.GetRequiredService<ILogger<TrackArea>>();
    var track = item.Track;
    Name = name;
    Song = region;
    Item = item;
    Project = track.Project;
    AreaType = areaType;
    SourceType = track.RecordingMode >= RecordingMode.MidiOverdub ? SourceType.Midi : SourceType.Audio;
    if (AreaType == AreaType.Record)
    {
      var watcher = new TrackMediaItemWatcher(track, _logger);
      watcher.ItemAdded += ItemAddedHandler;
      Watcher = watcher;
    }
  }


  private void ItemAddedHandler(object? sender, TrackMediaItem item)
  {
    if (AudioItem != null || item.Start != Item.Start) return;

    AudioItem = item;
    _logger.LogInformation("Got audio item {Item} for area {Area}", item, this);
  }

  public async Task BeginRecording()
  {
    State = AreaState.Recording;
    Project.SetSelection(Item);
    await Task.CompletedTask;
  }

  public async Task FinalizeRecording(Transport transport, ILogger logger)
  {
    if (State != AreaState.Recording) return;
    State = AreaState.Done;
    await PropageToLoopAreas(transport, logger);
  }

  public async Task PropageToLoopAreas(Transport transport, ILogger logger)
  {
    try
    {
      Reaper.PreventUIRefresh.Invoke(1);
      await PropagateMidiData();
      await PropagateAudio(transport, logger);
    }
    finally
    {
      Reaper.PreventUIRefresh.Invoke(-1);
    }
  }

  private async Task PropagateMidiData()
  {
    if (SourceType != SourceType.Midi) return;

    var fromTake = Item.GetActiveTake();
    if (fromTake == null) return;
    foreach (var child in LoopAreas)
    {
      var toTake = child.Item.GetActiveTake();
      if (toTake == null) continue;

      var data = fromTake.GetAllMidiEvents() ?? [];
      toTake.SetAllMidiEvents(data);
    }

    await Task.CompletedTask;
  }

  private async Task PropagateAudio(Transport transport, ILogger logger)
  {
    if (SourceType != SourceType.Audio) return;

    var project = transport.Project;

    // wait for item to become available
    var recordedItem = await WaitForRecordedItem();
    if (recordedItem == null) return;
    AudioItem = recordedItem;

    Item.Track.SelectExclusive();
    recordedItem.SelectExclusive();
    recordedItem.GetActiveTake()?.Name = "audio:" + Name;
    var savedCursor = transport.CursorPosition;

    Reaper.Main_OnCommandEx.Invoke(40014, 0, project.ReaperHandle); // copy item
    foreach (var child in LoopAreas)
    {
      child.AudioItem = await PasteAudioItem(child, transport);
    }

    transport.CursorPosition = savedCursor;
  }

  private async Task<TrackMediaItem?> PasteAudioItem(TrackArea toArea, Transport transport)
  {
    transport.CursorPosition = toArea.Item.Start;
    Reaper.Main_OnCommandEx.Invoke(42398, 0, transport.Project.ReaperHandle); // Paste items

    var newItem = transport.Project.GetSelectedMediaItems(1).FirstOrDefault();
    if (newItem == null) return null;

    newItem.Length = toArea.Item.End - toArea.Item.Start;
    var newTake = newItem.GetActiveTake();
    if (newTake == null) return null;
    newTake.Name = "audio:" + Name;
    return newItem;
  }

  public void Clean()
  {
    DeleteAllMidiEvents();
    AudioItem?.Delete();
    AudioItem = null;
    State = AreaState.Clean;

    foreach (var subArea in LoopAreas)
    {
      subArea.Clean();
    }
  }

  public void DeleteAllMidiEvents()
  {
    var take = Item.GetActiveTake();
    if (take == null) return;

    var attempts = 3;
    while (attempts-- > 0)
    {
      var numEvents = take.MidiEventCount;
      for (var i = 0; i < numEvents; i++)
        take.DeleteMidiEvent(0);
    }
  }

  private async Task<TrackMediaItem?> WaitForRecordedItem(TimeSpan? timeout = null)
  {
    timeout ??= TimeSpan.FromSeconds(3);
    var sw = Stopwatch.StartNew();
    while (AudioItem == null)
    {
      await Task.Delay(1);
      if (sw.Elapsed > timeout)
      {
        return null;
      }
    }

    return AudioItem;
  }

  public void Initialize()
  {
    Watcher?.Restart();
  }


  public override string ToString()
  {
    return $"{Name} ({SourceType}): {State}";
  }

  public void Dispose()
  {
    Watcher?.Dispose();
  }
}