using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Hemisphera.Hulp.Plugin.Models;
using Hsp.Osc;
using Microsoft.Extensions.Logging;
using ReaParamView.Types;
using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public class ParameterMonitor
{
  private readonly ILogger<ParameterMonitor> _logger;
  private readonly ChannelWriter<IMessage> _sink;


  private Track? _currentTrack;

  private MonitoredParameter?[] _monitoredParameters = new MonitoredParameter?[Constants.NoOfParameters];
  private readonly ParameterSetDto _lastState = new();
  private readonly ParameterSetDto _state = new();
  private CancellationTokenSource? _cancellationTokenSource;


  public ParameterMonitor(ILogger<ParameterMonitor> logger, ChannelWriter<IMessage> sink)
  {
    _logger = logger;
    _sink = sink;
  }


  public async Task Start()
  {
    _cancellationTokenSource = new CancellationTokenSource();
    var token = _cancellationTokenSource.Token;
    _ = Task.Run(async () => { await MonitorLoop(token); }, token);
    _logger.LogDebug("Parameter monitor started.");
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
        UpdateCurrentTrack();

        _state.TrackName = _currentTrack?.Name ?? string.Empty;
        for (var i = 0; i < _monitoredParameters.Length; i++)
        {
          _monitoredParameters[i]?.UpdateValue();
          _state.Envelopes[i].Name = _monitoredParameters[i]?.Name ?? string.Empty;
          _state.Envelopes[i].Value = _monitoredParameters[i]?.Value ?? 0.0;
          _state.Envelopes[i].FormattedValue = _monitoredParameters[i]?.FormattedValue ?? string.Empty;
          _state.Envelopes[i].Percentage = _monitoredParameters[i]?.Percentage ?? 0.0;
        }

        try
        {
          if (CreateMessage(out var oscMessage))
            await _sink.WriteAsync(oscMessage, token);
        }
        catch (Exception ex)
        {
          _logger.LogDebug("Failed to send envelope values to server: {message}", ex.Message);
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Monitor loop exited unexpectedly: {message} {stack}.", ex.Message, ex.StackTrace);
    }
  }

  private bool CreateMessage([NotNullWhen(true)] out IMessage? oscMsg)
  {
    MessageBundle? bundle = null;
    var fullRefresh = false;
    if (_lastState.TrackName != _state.TrackName)
    {
      bundle ??= [];
      bundle.Add(new Message("/hulp/curr/name").PushAtom(_state.TrackName ?? string.Empty));
      fullRefresh = true;
    }

    for (var i = 0; i < Constants.NoOfParameters; i++)
    {
      var lastEnvelope = _lastState.Envelopes[i];
      var newEnvelope = _state.Envelopes[i];
      if (!fullRefresh && !HasChanged(lastEnvelope, newEnvelope)) continue;

      bundle ??= [];
      bundle.Add(new Message($"/hulp/curr/fx/{i + 1}/name")
        .PushAtom(newEnvelope.Name ?? string.Empty));
      bundle.Add(new Message($"/hulp/curr/fx/{i + 1}/value")
        .PushAtom(newEnvelope.Value)
        .PushAtom(newEnvelope.FormattedValue));
    }

    oscMsg = bundle;
    if (oscMsg != null)
      _lastState.CopyFrom(_state);
    return oscMsg != null;
  }

  private void UpdateCurrentTrack()
  {
    var selectedTrack = Project.Current.GetSelectedTrack();
    if (selectedTrack?.ReaperHandle == _currentTrack?.ReaperHandle) return;

    _currentTrack = selectedTrack;
    if (_currentTrack == null)
    {
      _logger.LogDebug("No track selected");
      _monitoredParameters = new MonitoredParameter[Constants.NoOfParameters];
      return;
    }

    var paramFactory = new MonitoredParameterFactory(_currentTrack);
    _monitoredParameters = paramFactory.Build()
      .Concat(Enumerable.Repeat<MonitoredParameter?>(null, Constants.NoOfParameters))
      .Take(Constants.NoOfParameters)
      .ToArray();

    if (_logger.IsEnabled(LogLevel.Debug))
    {
      _logger.LogDebug("Track '{track}' selected", _currentTrack.Name);
      foreach (var linkedParameter in _monitoredParameters)
      {
        _logger.LogDebug("Loaded linked parameter: {lp}", linkedParameter);
      }
    }
  }


  private static bool HasChanged(ParameterDto? lastEnvelope, ParameterDto? newEnvelope)
  {
    if (Math.Abs((lastEnvelope?.Value ?? 0.0) - (newEnvelope?.Value ?? 0.0)) > 0.01)
      return true;
    if (!(lastEnvelope?.Name ?? string.Empty).Equals(newEnvelope?.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase))
      return true;
    if (!(lastEnvelope?.FormattedValue ?? string.Empty).Equals(newEnvelope?.FormattedValue ?? string.Empty, StringComparison.OrdinalIgnoreCase))
      return true;
    return false;
  }
}