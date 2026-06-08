using System.Diagnostics;
using Hemisphera.Hulp.Plugin.Infrastructure;
using Hemisphera.Hulp.Plugin.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReaSharp;
using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.Models;

public class LooperState
{
  private readonly ILogger<LooperState> _logger;
  private readonly IOptionsMonitor<LooperSettings> _settings;
  private readonly HulpMonitor _monitor;
  private readonly Transport _transport;
  private double _lookaheadBeats;

  private TrackSelector? _lastSelector;
  private Stopwatch? _timer;
  private CancellationTokenSource? _cts;

  public TimeSpan LookAhead
  {
    get
    {
      var bpm = Reaper.TimeMap2_GetDividedBpmAtTime.Invoke(0, _transport.PlayheadOrCursorPosition.TotalSeconds);
      return TimeSpan.FromSeconds(60.0 / bpm * _lookaheadBeats); // lookahead 3/4 of a beat
    }
  }

  public ObservableProperty<TrackArea> ActiveArea { get; } = new();
  public ObservableProperty<TrackArea> UpcomingArea { get; } = new();

  public Song? CurrentSong { get; private set; }


  public LooperState(
    ILogger<LooperState> logger,
    IOptionsMonitor<LooperSettings> settings,
    HulpMonitor monitor
  )
  {
    _logger = logger;
    _settings = settings;
    _monitor = monitor;
    _transport = new Transport(Project.Default);
    UpcomingArea.ValueChangedCallback += UpcomingAreaChanged;
    ActiveArea.ValueChangedCallback += ActiveAreaChanged;
    _logger.LogDebug("LooperState loaded");
  }


  public async Task Start()
  {
    await Initialize();

    _cts = new CancellationTokenSource();
    var token = _cts.Token;

    _ = Task.Run(async () =>
    {
      while (!token.IsCancellationRequested)
      {
        await Task.Delay(5, token);
        await Tick();
      }
    }, token);
    _transport.Play();
  }

  public async Task Stop()
  {
    if (_cts != null)
    {
      await _cts.CancelAsync();
    }

    _timer?.Stop();
    CurrentSong?.Dispose();
    CurrentSong = null;
    _lastSelector = null;
    await ActiveArea.Set(null, true);
    await UpcomingArea.Set(null, true);
  }

  public async Task Initialize(TimeSpan? time = null)
  {
    try
    {
      Reaper.PreventUIRefresh.Invoke(1);
      await Stop();

      if (time != null)
      {
        _transport.CursorPosition = time.Value;
      }

      _lookaheadBeats = _settings.CurrentValue.LookaheadBeats;
      _logger.LogDebug("Using lookahead of {beats} beats.", _lookaheadBeats);
      _logger.LogDebug("Using {containerTrackName} as container track.", _settings.CurrentValue.ContainerTrackName);
      _transport.Update();
      var now = _transport.PlayheadOrCursorPosition;
      var songs = EnumerateSongs();
      foreach (var song in songs)
      {
        song.SetActive(now.IsWithin(song.Region));
      }

      CurrentSong = songs.FirstOrDefault(s => now.IsWithin(s.Region));
      CurrentSong?.Initialize(now);
      _monitor.LoadSong(CurrentSong);

      _timer = Stopwatch.StartNew();
    }
    finally
    {
      Reaper.PreventUIRefresh.Invoke(-1);
      Reaper.UpdateArrange.Invoke();
      Reaper.TrackList_AdjustWindows.Invoke(false);
    }
  }

  public List<Song> EnumerateSongs()
  {
    var songs = Region.Enumerate(Project.Default)
      .Select(r => Song.FromRegion(r, _settings.CurrentValue.ContainerTrackName))
      .OfType<Song>()
      .ToList();
    return songs;
  }

  public async Task FocusRegion()
  {
    _logger.LogDebug("Focus region");
    await Initialize();
    if (CurrentSong == null) return;
    _transport.Project.SetSelection(CurrentSong.Region);

    Reaper.Main_OnCommandEx.Invoke(40031, 0, _transport.Project.ReaperHandle); // Zoom to time selection

    _transport.Project.ClearSelection();
    _transport.CursorPosition = CurrentSong.Region.Start;
  }


  private async Task Tick()
  {
    if (CurrentSong == null) return;

    _transport.Update();

    if (_transport.PlayheadPosition > CurrentSong.Region.End && _transport.IsPlaying)
    {
      await Stop();
      Reaper.Main_OnCommandEx.Invoke(40328, 0, _transport.Project.ReaperHandle);
      _logger.LogDebug("Reached end of region");
    }

    if (!_transport.IsPlaying && _timer?.Elapsed.TotalSeconds > 3)
    {
      await Stop();
      _logger.LogDebug("Playback stopped. Looper reset. ");
    }

    var pos = _transport.PlayheadOrCursorPosition;
    HandleTrackSelector(pos);
    await ActiveArea.Set(CurrentSong.RecordingAreas.FirstOrDefault(a => pos.IsWithin(a.Item)));
    await UpcomingArea.Set(CurrentSong.RecordingAreas.FirstOrDefault(a => (pos + LookAhead).IsWithin(a.Item)));
  }


  private async Task UpcomingAreaChanged(TrackArea? oldArea, TrackArea? newArea)
  {
    // do nothing unless we're playing
    if (!_transport.IsPlaying) return;

    _logger.LogDebug("Upcoming area changed: {newArea}", newArea);

    if (newArea?.State == AreaState.Clean)
    {
      await newArea.BeginRecording();
      if (!_transport.IsRecording)
      {
        _transport.ToggleRecordAtNextBeat();
      }
    }

    // if recording and no more upcoming item, stop recording.
    if (newArea == null && _transport.IsRecording)
    {
      _transport.ToggleRecordAtNextBeat();
    }
  }

  private async Task ActiveAreaChanged(TrackArea? oldArea, TrackArea? newArea)
  {
    if (!_transport.IsPlaying) return;

    _logger.LogDebug("Active area changed: {newArea}", newArea);

    if (oldArea is { State: AreaState.Recording })
    {
      _ = oldArea.FinalizeRecording(_transport, _logger);
    }

    if (newArea?.SourceType == SourceType.Midi)
    {
      // perform initial propagate after 1/4 of area has passed for MIDI items
      _ = Task.Run(async () =>
      {
        var delay = TimeSpan.FromSeconds(newArea.Item.Length.TotalSeconds / 4);
        await Task.Delay(delay);
        await newArea.PropageToLoopAreas(_transport, _logger);
      });
    }
  }

  private void HandleTrackSelector(TimeSpan pos)
  {
    var selector = CurrentSong?.Selectors
      .Where(s => s.Start <= pos)
      .OrderBy(s => s.Start)
      .LastOrDefault();
    if (_lastSelector == selector) return;
    if (selector == null) return;
    _lastSelector = selector;
    _logger.LogDebug("Selector activated: {selector}", selector);
    selector.Track.SelectExclusive();
  }
}