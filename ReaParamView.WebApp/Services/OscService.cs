using System.Net;
using Hsp.Osc;
using ReaParamView.Types;

namespace ReaParamView.WebApp.Services;

public class TransportState
{
  public bool IsPlaying { get; set; }
  public bool IsPaused { get; set; }
  public bool IsRecording { get; set; }
  public double Position { get; set; }
  public double RegionStart { get; set; }
  public double RegionEnd { get; set; }

  public double RemainingSeconds => RegionEnd > 0 ? Math.Max(0, RegionEnd - Position) : 0;
}

public class TrackInfo
{
  public bool IsActive { get; set; }
  public string Name { get; set; } = string.Empty;
  public int LogicalIndex { get; set; }
  public int ReaperIndex { get; set; }
  public bool IsSelected { get; set; }
  public bool IsRecArmed { get; set; }
}

public class UpcomingEvent
{
  public string Text { get; set; } = string.Empty;
  public float Time { get; set; }
}

public class OscService : BackgroundService
{
  private const int Port = 9000;
  private readonly ILogger<OscService> _logger;

  // FX Parameters
  private readonly MessageDto _fxParameters = new();
  public MessageDto FxParameters => _fxParameters;
  public event Action? FxParametersChanged;

  // Transport
  public TransportState Transport { get; } = new();
  public event Action? TransportChanged;
  private DateTime _lastPositionNotify = DateTime.MinValue;

  // Tracks (8 fixed slots, indexed 0–7; logical index 1–8)
  private readonly TrackInfo[] _tracks = Enumerable.Range(0, 8).Select(_ => new TrackInfo()).ToArray();
  public IReadOnlyList<TrackInfo> Tracks => _tracks;
  public event Action? TracksChanged;

  // Upcoming events
  private readonly List<UpcomingEvent> _upcomingEvents = [];
  private readonly object _eventsLock = new();

  public IReadOnlyList<UpcomingEvent> GetUpcomingEvents()
  {
    lock (_eventsLock)
      return _upcomingEvents.Take(5).ToList();
  }

  public event Action? UpcomingEventsChanged;

  public OscService(ILogger<OscService> logger)
  {
    _logger = logger;
    // Pre-populate 8 envelope slots (1-based, matching OSC path indices)
    for (var i = 1; i <= 8; i++)
      _fxParameters.Envelopes.Add(new EnvelopeDto { Slot = i });
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var server = new OscUdpServer(IPAddress.Any, Port);

    // ── FX Parameters ────────────────────────────────────────────────────
    server.RegisterHandler(@"^/hulp/curr/name$", (MessageHandlerContext ctx) =>
    {
      _fxParameters.TrackName = ctx.Message.Atoms.FirstOrDefault().StringValue;
      FxParametersChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/name$", (MessageHandlerContext ctx) =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      GetEnvelope(slot).Name = ctx.Message.Atoms.FirstOrDefault().StringValue;
      FxParametersChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/value/str$", (MessageHandlerContext ctx) =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      GetEnvelope(slot).FormattedValue = ctx.Message.Atoms.FirstOrDefault().StringValue ?? string.Empty;
      FxParametersChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/value$", (MessageHandlerContext ctx) =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      GetEnvelope(slot).Value = ctx.Message.Atoms.FirstOrDefault().Float32Value;
      FxParametersChanged?.Invoke();
    });

    // ── Transport (REAPER standard OSC) ──────────────────────────────────
    server.RegisterHandler(@"^/play$", (MessageHandlerContext ctx) =>
    {
      Transport.IsPlaying = ctx.Message.Atoms.FirstOrDefault().Float32Value > 0.5f;
      TransportChanged?.Invoke();
    });

    server.RegisterHandler(@"^/record$", (MessageHandlerContext ctx) =>
    {
      Transport.IsRecording = ctx.Message.Atoms.FirstOrDefault().Float32Value > 0.5f;
      TransportChanged?.Invoke();
    });

    server.RegisterHandler(@"^/pause$", (MessageHandlerContext ctx) =>
    {
      Transport.IsPaused = ctx.Message.Atoms.FirstOrDefault().Float32Value > 0.5f;
      TransportChanged?.Invoke();
    });

    server.RegisterHandler(@"^/time$", (MessageHandlerContext ctx) =>
    {
      Transport.Position = ctx.Message.Atoms.FirstOrDefault().Float32Value;
      PruneExpiredEvents();
      // Throttle position-driven UI updates to ~2 Hz
      var now = DateTime.UtcNow;
      if ((now - _lastPositionNotify).TotalMilliseconds >= 500)
      {
        _lastPositionNotify = now;
        TransportChanged?.Invoke();
      }
    });

    // ── Region ───────────────────────────────────────────────────────────
    server.RegisterHandler(@"^/hulp/region$", (MessageHandlerContext ctx) =>
    {
      var atoms = ctx.Message.Atoms;
      if (atoms.Count < 2) return;
      Transport.RegionStart = atoms[0].Float32Value;
      Transport.RegionEnd = atoms[1].Float32Value;
      TransportChanged?.Invoke();
    });

    // ── Tracks (from hulp plugin, path index = logical index, 1-based) ──
    server.RegisterHandler(@"^/hulp/track/(\d+)$", (MessageHandlerContext ctx) =>
    {
      // Use the path index as the authoritative slot so the handler fires
      // even when atoms are incomplete (plugin not yet implemented).
      var pathIndex = int.Parse(ctx.Match.Groups[1].Value); // 1-based
      var slot = pathIndex - 1;
      if ((uint)slot >= 8u) return;

      var atoms = ctx.Message.Atoms;
      _tracks[slot].Name = atoms.Count > 0 ? atoms[0].StringValue ?? string.Empty : string.Empty;
      _tracks[slot].LogicalIndex = pathIndex;
      _tracks[slot].ReaperIndex = atoms.Count > 2 ? atoms[2].Int32Value : 0;
      _tracks[slot].IsActive = true;
      TracksChanged?.Invoke();
    });

    // Track state from REAPER standard OSC
    server.RegisterHandler(@"^/track/(\d+)/recarm$", (MessageHandlerContext ctx) =>
    {
      var reaperIdx = int.Parse(ctx.Match.Groups[1].Value);
      var track = _tracks.FirstOrDefault(t => t.IsActive && t.ReaperIndex == reaperIdx);
      if (track == null) return;
      track.IsRecArmed = ctx.Message.Atoms.FirstOrDefault().Float32Value > 0.5f;
      TracksChanged?.Invoke();
    });

    server.RegisterHandler(@"^/track/(\d+)/select$", (MessageHandlerContext ctx) =>
    {
      var reaperIdx = int.Parse(ctx.Match.Groups[1].Value);
      var track = _tracks.FirstOrDefault(t => t.IsActive && t.ReaperIndex == reaperIdx);
      if (track == null) return;
      track.IsSelected = ctx.Message.Atoms.FirstOrDefault().Float32Value > 0.5f;
      TracksChanged?.Invoke();
    });

    // ── Upcoming Events ──────────────────────────────────────────────────
    server.RegisterHandler(@"^/hulp/upcoming$", (MessageHandlerContext ctx) =>
    {
      var atoms = ctx.Message.Atoms;
      if (atoms.Count < 2) return;
      var text = atoms[0].StringValue ?? string.Empty;
      var time = atoms[1].Float32Value;
      lock (_eventsLock)
      {
        var existing = _upcomingEvents.FirstOrDefault(e => e.Text == text);
        if (existing != null)
          existing.Time = time;
        else
          _upcomingEvents.Add(new UpcomingEvent { Text = text, Time = time });
        _upcomingEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
      }

      UpcomingEventsChanged?.Invoke();
    });

    server.BeginListen();
    _logger.LogInformation("OSC service listening on port {Port}", Port);

    try
    {
      await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    catch (OperationCanceledException)
    {
    }
  }

  private void PruneExpiredEvents()
  {
    bool removed;
    lock (_eventsLock)
      removed = _upcomingEvents.RemoveAll(e => e.Time < Transport.Position) > 0;
    if (removed)
      UpcomingEventsChanged?.Invoke();
  }

  private EnvelopeDto GetEnvelope(int slot)
  {
    return _fxParameters.Envelopes.First(e => e.Slot == slot);
  }
}