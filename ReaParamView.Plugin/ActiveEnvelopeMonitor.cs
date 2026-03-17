using Microsoft.Extensions.Logging;
using ReaParamView.Types;
using ReaSharp.Models;

namespace ReaParamView.Plugin;

public class ActiveEnvelopeMonitor
{
  private readonly ILogger<ActiveEnvelopeMonitor> _logger;
  private readonly ITransport _transport;

  private TrackFxEnvelope[] _envelopes = [];
  private Track? _selectedTrack;
  private readonly MessageDto _message = new();
  private CancellationTokenSource? _cancellationTokenSource;


  public ActiveEnvelopeMonitor(ILogger<ActiveEnvelopeMonitor> logger, ITransport transport)
  {
    _logger = logger;
    _transport = transport;
  }


  public async Task Start()
  {
    _cancellationTokenSource = new CancellationTokenSource();
    var token = _cancellationTokenSource.Token;
    _ = StartSending(token);
  }

  public async Task Stop()
  {
    if (_cancellationTokenSource == null) return;
    await _cancellationTokenSource.CancelAsync();
  }


  private async Task StartSending(CancellationToken token)
  {
    while (!token.IsCancellationRequested)
    {
      await Task.Delay(250, token);

      var selectedTrack = Project.Default.GetSelectedTracks().FirstOrDefault();
      if (selectedTrack?.ReaperHandle != _selectedTrack?.ReaperHandle)
      {
        _selectedTrack = selectedTrack;
        _envelopes = _selectedTrack?.EnumerateTrackEnvelopes().Where(env => env.Active).ToArray() ?? [];
        _message.TrackName = _selectedTrack?.Name ?? string.Empty;
        _logger.LogInformation($"Selected track: {_message.TrackName}, found {_envelopes.Length} active envelopes");
      }

      if (_selectedTrack == null) continue;

      _message.Envelopes = _envelopes.Select(env =>
      {
        var val = env.GetValue();
        return new EnvelopeDto
        {
          Name = env.Name ?? "<no name>",
          Value = val.Value,
          Percentage = val.Percentage
        };
      }).ToList();

      try
      {
        await _transport.SendMessage(_message, token);
      }
      catch (Exception ex)
      {
        _logger.LogWarning($"Failed to send envelope values to server: {ex.Message}");
      }
    }
  }
}