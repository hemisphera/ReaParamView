using Hemisphera.Hulp.Plugin.Models;
using Hemisphera.Hulp.Plugin.StateModels;
using Hsp.Osc;
using Microsoft.Extensions.Logging;
using Hemisphera.Hulp.Shared;
using ReaSharp;
using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public class HulpMonitor
{
  private readonly ILogger<HulpMonitor> _logger;
  private readonly IOscWriter _osc;

  private readonly HulpState _state;
  private readonly ParameterSate[] _parameters;
  private readonly TrackState[] _tracks;
  private readonly SongState[] _songs;
  private readonly EventState[] _events;

  private Track? _currentTrack;
  private readonly List<MonitoredParameter> _monitoredParameters = [];
  private Song? _currentSong;
  private readonly SemaphoreSlim _lock = new(1, 1);

  private CancellationTokenSource? _cancellationTokenSource;
  private Transport? _transport;


  public HulpMonitor(ILogger<HulpMonitor> logger, IOscWriter osc)
  {
    _logger = logger;
    _osc = osc;

    _state = new HulpState();
    _state.PropertyChanged += StatePropertyChangedCallback;
    _parameters = Enumerable.Range(0, Constants.NoOfParameters).Select(index =>
    {
      var item = new ParameterSate(index);
      item.PropertyChanged += ParameterPropertyChangedCallback;
      return item;
    }).ToArray();
    _tracks = Enumerable.Range(0, Constants.NoOfParameters).Select(index =>
    {
      var item = new TrackState(index);
      item.PropertyChanged += TrackPropertyChangedCallback;
      return item;
    }).ToArray();
    _songs = Enumerable.Range(0, Constants.NoOfSongs).Select(index =>
    {
      var item = new SongState(index);
      item.PropertyChanged += SongPropertyChangedCallback;
      return item;
    }).ToArray();
    _events = Enumerable.Range(0, Constants.NoOfEvents).Select(index =>
    {
      var item = new EventState(index);
      item.PropertyChanged += EventPropertyChangedCallback;
      return item;
    }).ToArray();
  }


  private void StatePropertyChangedCallback(object? sender, PropertyValueChangedEventArgs e)
  {
    if (sender is not HulpState state) return;
    if (e.PropertyName is nameof(state.CurrentTrackName) or "")
    {
      _osc.WriteAsync(state.CurrentTrackName.ToOscMessage("/hulp/track/curr/name"));
    }

    if (e.PropertyName is nameof(state.Beat) or "")
    {
      var text = $"{state.Beat.Beat}/{state.Beat.Length}";
      _osc.WriteAsync(text.ToOscMessage("/hulp/beat"));
    }

    if (e.PropertyName is nameof(state.Region) or "")
    {
      var start = state.Region?.Start.TotalSeconds ?? 0.0;
      var end = state.Region?.End.TotalSeconds ?? 0.0;
      _osc.WriteAsync(new Message("/hulp/song/curr").PushAtom(state.Region?.Id ?? -1).PushAtom(start).PushAtom(end));
      _osc.WriteAsync(new Message("/hulp/song/curr/name").PushAtom(state.Region?.Name ?? string.Empty));
    }

    if (e.PropertyName is nameof(state.QuarterNotePosition) or "")
    {
      _osc.WriteAsync(state.QuarterNotePosition.ToOscMessage("/hulp/qnpos"));
    }
  }

  private void ParameterPropertyChangedCallback(object? sender, PropertyValueChangedEventArgs e)
  {
    if (sender is not ParameterSate state) return;
    var baseAddress = $"/hulp/track/curr/fx/{state.Index + 1}";
    if (e.PropertyName is nameof(state.Name) or "")
      _osc.WriteAsync(state.Name.ToOscMessage(baseAddress + "/name"));
    if (e.PropertyName is nameof(state.FormattedValue) or "")
      _osc.WriteAsync(state.FormattedValue.ToOscMessage(baseAddress + "/value/str"));
    if (e.PropertyName is nameof(state.Percentage) or "")
      _osc.WriteAsync(state.Percentage.ToOscMessage(baseAddress + "/value"));
  }

  private void TrackPropertyChangedCallback(object? sender, PropertyValueChangedEventArgs e)
  {
    if (sender is not TrackState state) return;
    var baseAddress = $"/hulp/track/{state.Index + 1}";
    if (e.PropertyName is nameof(state.Name) or "")
      _osc.WriteAsync(state.Name.ToOscMessage(baseAddress + "/name"));
    if (e.PropertyName is nameof(state.Selected) or "")
      _osc.WriteAsync(state.Selected.ToOscMessage(baseAddress + "/sel"));
    if (e.PropertyName is nameof(state.Mute) or "")
      _osc.WriteAsync(state.Mute.ToOscMessage(baseAddress + "/mute"));
    if (e.PropertyName is nameof(state.Solo) or "")
      _osc.WriteAsync(state.Solo.ToOscMessage(baseAddress + "/solo"));
    if (e.PropertyName is nameof(state.RecordArm) or "")
      _osc.WriteAsync(state.RecordArm.ToOscMessage(baseAddress + "/recarm"));
  }

  private void SongPropertyChangedCallback(object? sender, PropertyValueChangedEventArgs e)
  {
    if (sender is not SongState item) return;
    var baseAddress = $"/hulp/song/{item.Index + 1}";
    if (e.PropertyName is nameof(item.RegionId) or "")
      _osc.WriteAsync(item.RegionId.ToOscMessage(baseAddress + "/id"));
    if (e.PropertyName is nameof(item.Name) or "")
      _osc.WriteAsync(item.RegionId.ToOscMessage(baseAddress + "/name"));
  }

  private void EventPropertyChangedCallback(object? sender, PropertyValueChangedEventArgs e)
  {
    if (sender is not EventState state) return;
    var baseAddress = $"/hulp/event/{state.Index + 1}";
    if (e.PropertyName is nameof(state.Text) or "")
      _osc.WriteAsync(state.Text.ToOscMessage(baseAddress + "/text"));
    if (e.PropertyName is nameof(state.Position) or "")
      _osc.WriteAsync(state.Position.ToOscMessage(baseAddress + "/pos"));
  }

  public async Task Start()
  {
    _cancellationTokenSource = new CancellationTokenSource();
    var token = _cancellationTokenSource.Token;
    _ = Task.Run(async () => { await MonitorLoop(token); }, token);
    _logger.LogDebug("Monitor started.");
    await Task.CompletedTask;
  }

  public async Task Stop()
  {
    if (_cancellationTokenSource == null) return;
    await _cancellationTokenSource.CancelAsync();
  }


  private async Task MonitorLoop(CancellationToken token)
  {
    try
    {
      while (!token.IsCancellationRequested)
      {
        await Task.Delay(50, token);
        try
        {
          await _lock.WaitAsync(token);
          UpdateCurrentTrack();
          UpdateTempo();
          UpdateParameters();
          UpdateTracks();
        }
        finally
        {
          _lock.Release();
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Monitor loop exited unexpectedly: {message} {stack}.", ex.Message, ex.StackTrace);
    }
  }

  private void UpdateTempo()
  {
    _transport?.Update();
    var sig = _transport?.GetTimeSignature();
    _state.Beat = new BeatInfo((int)(sig?.Beats ?? -1) + 1, sig?.Numerator ?? 0);
    _state.QuarterNotePosition = (int)(_transport?.TimeToQuarterNotes() ?? 0);
  }

  private void UpdateCurrentTrack()
  {
    var selectedTrack = Project.Current.GetSelectedTrack();
    if (selectedTrack?.ReaperHandle == _currentTrack?.ReaperHandle) return;

    _currentTrack = selectedTrack;
    _monitoredParameters.Clear();
    _state.CurrentTrackName = _currentTrack?.Name ?? string.Empty;
    if (_currentTrack == null)
    {
      _logger.LogDebug("No track selected");
      return;
    }

    var paramFactory = new MonitoredParameterFactory(_currentTrack, _logger);
    _monitoredParameters.AddRange(paramFactory.Build());
    if (_logger.IsEnabled(LogLevel.Debug))
    {
      _logger.LogDebug("Track '{track}' selected", _currentTrack.Name);
      foreach (var item in _monitoredParameters)
      {
        _logger.LogDebug("Loaded linked parameter: {lp}", item);
      }
    }
  }

  private void UpdateParameters()
  {
    for (var i = 0; i < _parameters.Length; i++)
    {
      var mp = _monitoredParameters.FirstOrDefault(p => p.Index == i);
      mp?.UpdateValue();
      _parameters[i].Name = mp?.Name ?? string.Empty;
      _parameters[i].Percentage = mp?.Percentage ?? 0.0;
      _parameters[i].FormattedValue = mp?.FormattedValue ?? string.Empty;
    }
  }

  private void UpdateTracks()
  {
    for (var i = 0; i < _tracks.Length; i++)
    {
      var songTrack = _currentSong?.Tracks.TryGet(i);
      _tracks[i].Name = songTrack?.Name ?? string.Empty;
      _tracks[i].Selected = songTrack?.Selected ?? false;
      _tracks[i].Mute = songTrack?.Mute ?? false;
      _tracks[i].Solo = songTrack?.Solo != null && songTrack.Solo != TrackSoloState.NotSoloed;
      _tracks[i].RecordArm = songTrack?.RecordArm ?? false;
    }
  }

  private void LoadEvents(Transport transport)
  {
    var events = CollectEvents().ToArray();
    for (var i = 0; i < _events.Length; i++)
    {
      var hulpEvent = events.FirstOrDefault(e => e.Index == i);
      _events[i].Text = hulpEvent?.Text ?? string.Empty;
      _events[i].Position = transport.TimeToQuarterNotes(TimeSpan.FromSeconds(hulpEvent?.Time ?? 0.0));
    }
  }

  private IEnumerable<HulpEvent> CollectEvents()
  {
    var index = 0;
    foreach (var area in _currentSong?.RecordingAreas ?? [])
    {
      yield return new HulpEvent
      {
        Index = index++,
        Text = $"Record: {area.Name} {area.Item.Track.Name}",
        Time = area.Item.Start.TotalSeconds
      };
    }

    foreach (var sel in _currentSong?.Selectors ?? [])
    {
      yield return new HulpEvent
      {
        Index = index++,
        Text = $"Select: {sel.Track.Name}",
        Time = sel.Start.TotalSeconds
      };
    }
  }

  public void LoadSong(Song? currentSong)
  {
    try
    {
      _lock.Wait();
      _currentSong = currentSong;
      _state.Region = currentSong?.Region;
      _transport = new Transport(Project.Current);
      LoadEvents(_transport);
      FullRefresh();
      _logger.LogDebug("Loaded song: [{song}]", currentSong?.Name ?? string.Empty);
    }
    finally
    {
      _lock.Release();
    }
  }

  public void FullRefresh()
  {
    StatePropertyChangedCallback(_state, PropertyValueChangedEventArgs.Empty);
    foreach (var item in _parameters)
      ParameterPropertyChangedCallback(item, PropertyValueChangedEventArgs.Empty);
    foreach (var item in _tracks)
      TrackPropertyChangedCallback(item, PropertyValueChangedEventArgs.Empty);
    foreach (var item in _songs)
      SongPropertyChangedCallback(item, PropertyValueChangedEventArgs.Empty);
    foreach (var item in _events)
      EventPropertyChangedCallback(item, PropertyValueChangedEventArgs.Empty);
  }
}