using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReaParamView.Types;
using ReaSharp.Models;

namespace ReaParamView.Plugin;

public class ActiveEnvelopeMonitor
{
  internal const int MaxSlots = 8;

  private readonly ILogger<ActiveEnvelopeMonitor> _logger;
  private readonly ITransport _transport;
  private readonly IOptionsMonitor<MonitorSettings> _settings;


  private Track? _currentTrack;
  private LinkedParameter[] _linkedParameters = [];
  private readonly MessageDto _message = new();
  private CancellationTokenSource? _cancellationTokenSource;


  public ActiveEnvelopeMonitor(ILogger<ActiveEnvelopeMonitor> logger, ITransport transport, IOptionsMonitor<MonitorSettings> settings)
  {
    _logger = logger;
    _transport = transport;
    _settings = settings;
  }


  public async Task Start()
  {
    _cancellationTokenSource = new CancellationTokenSource();
    var token = _cancellationTokenSource.Token;
    _ = SenderLoop(token);
  }

  public async Task Stop()
  {
    if (_cancellationTokenSource == null) return;
    await _cancellationTokenSource.CancelAsync();
  }


  private async Task SenderLoop(CancellationToken token)
  {
    await _transport.StartAsync(token);
    try
    {
      var settings = _settings.CurrentValue;
      while (!token.IsCancellationRequested)
      {
        {
          await Task.Delay(settings.UpdateIntervalMs, token);

          UpdateCurrentTrack();

          _message.TrackName = _currentTrack?.Name ?? string.Empty;
          _message.Envelopes = BuildParameters(_linkedParameters);

          try
          {
            await _transport.SendMessage(_message, token);
          }
          catch (Exception ex)
          {
            _logger.LogDebug($"Failed to send envelope values to server: {ex.Message}");
          }
        }
      }
    }
    finally
    {
      await _transport.StopAsync(token);
    }
  }

  private void UpdateCurrentTrack()
  {
    var selectedTrack = Project.Current.GetSelectedTrack();
    if (selectedTrack?.ReaperHandle == _currentTrack?.ReaperHandle) return;

    _currentTrack = selectedTrack;
    if (_currentTrack == null)
    {
      _linkedParameters = [];
      return;
    }

    _linkedParameters = LinkedParameter.Load(_currentTrack);
    foreach (var linkedParameter in _linkedParameters)
    {
      _logger.LogDebug("Loaded linked parameter: {lp}", linkedParameter);
    }
  }

  private static List<EnvelopeDto> BuildParameters(LinkedParameter[] envelopes)
  {
    var envelopeData = envelopes
      .Select(env => new EnvelopeData(env))
      .ToList();

    var slots = new EnvelopeDto?[MaxSlots];
    foreach (var data in envelopeData.Where(d => d.ExplicitSlot.HasValue).ToArray())
    {
      if (data.ExplicitSlot == null) continue;
      var slotIndex = data.ExplicitSlot.Value - 1; // Convert 1-based to 0-based array index
      slots[slotIndex] = CreateEnvelopeDto(data, data.ExplicitSlot.Value);
      envelopeData.Remove(data);
    }

    var unassigned = new Queue<EnvelopeData>(envelopeData.OrderBy(d => d.DisplayName));
    for (var i = 0; i < slots.Length; i++)
    {
      if (slots[i] != null) continue;
      var next = unassigned.Count > 0 ? unassigned.Dequeue() : null;
      slots[i] = CreateEnvelopeDto(next, i);
    }

    return slots.OfType<EnvelopeDto>().ToList();
  }

  private static EnvelopeDto CreateEnvelopeDto(EnvelopeData? data, int slot)
  {
    return new EnvelopeDto
    {
      Name = data?.DisplayName ?? string.Empty,
      Slot = slot,
      Value = data?.Value ?? 0.0,
      Percentage = data?.Percentage ?? 0,
      FormattedValue = data?.FormattedValue ?? string.Empty
    };
  }
}