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
    while (!token.IsCancellationRequested)
    {
      await TrySendUpdate(token);
    }
  }

  private async Task TrySendUpdate(CancellationToken token)
  {
    try
    {
      var settings = _settings.CurrentValue;
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
        _logger.LogDebug("Failed to send envelope values to server: {msg}", ex.Message);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Update failed: {msg} - {stack}", ex.Message, ex.StackTrace);
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
    // Create envelope data once with all values and formatting extracted upfront
    var envelopeData = envelopes
      .Select(env => new EnvelopeData(env))
      .ToList();

    var slots = new EnvelopeDto?[MaxSlots];

    // Process explicitly slotted envelopes
    foreach (var data in envelopeData.Where(d => d.ExplicitSlot.HasValue))
    {
      if (data.ExplicitSlot == null) continue;
      var slotIndex = data.ExplicitSlot.Value - 1; // Convert 1-based to 0-based array index
      slots[slotIndex] ??= CreateEnvelopeDto(data, data.ExplicitSlot.Value);
    }

    // Process unassigned envelopes in order
    var unassigned = envelopeData
      .Where(d => !d.ExplicitSlot.HasValue)
      .OrderBy(d => d.DisplayName)
      .ToList();

    var slotIdx = 0;
    foreach (var data in unassigned)
    {
      while (slotIdx < MaxSlots && slots[slotIdx] != null) slotIdx++;
      if (slotIdx >= MaxSlots) break;
      slots[slotIdx] = CreateEnvelopeDto(data, slotIdx + 1);
      slotIdx++;
    }

    return slots.Where(s => s != null).Cast<EnvelopeDto>().ToList();
  }

  private static EnvelopeDto CreateEnvelopeDto(EnvelopeData data, int slot)
  {
    return new EnvelopeDto
    {
      Name = data.DisplayName,
      Slot = slot,
      Value = data.Value,
      Percentage = data.Percentage,
      FormattedValue = data.FormattedValue
    };
  }
}