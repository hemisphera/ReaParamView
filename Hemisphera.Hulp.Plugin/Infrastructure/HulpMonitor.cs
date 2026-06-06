using System.ComponentModel;
using Hemisphera.Hulp.Plugin.Models;
using Hemisphera.Hulp.Plugin.StateModels;
using Hsp.Osc;
using Microsoft.Extensions.Logging;
using ReaParamView.Types;
using ReaSharp;
using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public class HulpMonitor
{
  private readonly ILogger<HulpMonitor> _logger;
  private readonly IOscWriter _osc;


  private Track? _currentTrack;

  private List<MonitoredParameter> _monitoredParameters { get; } = [];

  private readonly HulpState _state;
  private readonly ParameterSate[] _parameters;
  private readonly TrackState[] _tracks;
  private Song? _currentSong;
  private readonly SemaphoreSlim _lock = new(1, 1);

  private CancellationTokenSource? _cancellationTokenSource;


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
  }


  private void StatePropertyChangedCallback(object? sender, PropertyChangedEventArgs e)
  {
    if (sender is not HulpState state) return;
    if (e.PropertyName is nameof(state.CurrentTrackName) or "")
      _osc.WriteAsync(state.CurrentTrackName.ToOscMessage("/hulp/curr"));
  }

  private void ParameterPropertyChangedCallback(object? sender, PropertyChangedEventArgs e)
  {
    if (sender is not ParameterSate state) return;
    var baseAddress = $"/hulp/curr/fx/{state.Index + 1}";
    if (e.PropertyName is nameof(state.Name) or "")
      _osc.WriteAsync(state.Name.ToOscMessage(baseAddress + "/name"));
    if (e.PropertyName is nameof(state.FormattedValue) or "")
      _osc.WriteAsync(state.FormattedValue.ToOscMessage(baseAddress + "/value/str"));
    if (e.PropertyName is nameof(state.Percentage) or "")
      _osc.WriteAsync(state.Percentage.ToOscMessage(baseAddress + "/value"));
  }

  private void TrackPropertyChangedCallback(object? sender, PropertyChangedEventArgs e)
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


  public async Task Start()
  {
    _cancellationTokenSource = new CancellationTokenSource();
    var token = _cancellationTokenSource.Token;
    _ = Task.Run(async () => { await MonitorLoop(token); }, token);
    _logger.LogDebug("Monitor started.");
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

  public void LoadSong(Song? currentSong)
  {
    try
    {
      _lock.Wait();
      _currentSong = currentSong;
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
    StatePropertyChangedCallback(_state, new PropertyChangedEventArgs(string.Empty));
    foreach (var fx in _parameters)
      ParameterPropertyChangedCallback(fx, new PropertyChangedEventArgs(string.Empty));
    foreach (var track in _tracks)
      TrackPropertyChangedCallback(track, new PropertyChangedEventArgs(string.Empty));
  }
}