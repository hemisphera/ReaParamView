using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ReaParamView.Types;
using ReaSharp.Models;

namespace ReaParamView.Plugin;

public class ActiveEnvelopeMonitor
{
  private readonly ILogger<ActiveEnvelopeMonitor> _logger;
  private readonly ITransport _transport;

  private const int MaxSlots = 8;
  private static readonly Regex ExplicitSlotPattern = new(@"^@(\d+)\s*", RegexOptions.Compiled);

  private TrackFxEnvelope[] _envelopes = [];
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
      _envelopes = selectedTrack?.EnumerateTrackEnvelopes().Where(env => env.Active).ToArray() ?? [];
      _message.TrackName = selectedTrack?.Name ?? string.Empty;

      _message.Envelopes = BuildEnvelopes(_envelopes);

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

  private static List<EnvelopeDto> BuildEnvelopes(TrackFxEnvelope[] envelopes)
  {
    var slots = new EnvelopeDto?[MaxSlots];
    var unassigned = new List<(TrackFxEnvelope Env, string DisplayName)>();

    foreach (var env in envelopes)
    {
      var rawName = (env.Name ?? "<no name>").Split('/').First().Trim();
      var match = ExplicitSlotPattern.Match(rawName);
      if (match.Success && int.TryParse(match.Groups[1].Value, out var idx) && idx >= 1 && idx <= MaxSlots)
      {
        var displayName = rawName[match.Length..];
        if (string.IsNullOrWhiteSpace(displayName)) displayName = rawName;
        var val = env.GetValue();
        var slotIndex = idx - 1; // Convert 1-based to 0-based array index
        slots[slotIndex] ??= new EnvelopeDto { Name = displayName, Slot = idx, Value = val.Value, Percentage = val.Percentage };
      }
      else
      {
        unassigned.Add((env, rawName));
      }
    }

    var slotIdx = 0;
    foreach (var (env, displayName) in unassigned.OrderBy(r => r.DisplayName))
    {
      while (slotIdx < MaxSlots && slots[slotIdx] != null) slotIdx++;
      if (slotIdx >= MaxSlots) break;
      var val = env.GetValue();
      slots[slotIdx] = new EnvelopeDto { Name = displayName, Slot = slotIdx + 1, Value = val.Value, Percentage = val.Percentage };
      slotIdx++;
    }

    return slots.Where(s => s != null).Cast<EnvelopeDto>().ToList();
  }
}