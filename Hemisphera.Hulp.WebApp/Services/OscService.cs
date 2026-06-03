using System.Diagnostics;
using System.Net;
using Hsp.Osc;
using ReaParamView.Types;

namespace ReaParamView.WebApp.Services;

public class OscService : BackgroundService
{
  private const int Port = 9000;
  private readonly ILogger<OscService> _logger;

  // FX Parameters
  private readonly ParameterSetDto _fxParameters = new();
  public ParameterSetDto FxParameters => _fxParameters;
  public event Action? FxParametersChanged;

  // Transport
  public TransportState Transport { get; } = new();
  public event Action? TransportChanged;
  private long _lastPositionNotify;
  private long _lastVuNotify;

  // Tracks (8 fixed slots, indexed 0–7; logical index 1–8)
  private readonly TrackInfo[] _tracks = Enumerable.Range(0, 8).Select(_ => new TrackInfo()).ToArray();
  public IReadOnlyList<TrackInfo> Tracks => _tracks;
  public event Action? TracksChanged;

  // Upcoming events
  private readonly UpcomingEvent[] _events;
  private readonly object _eventsLock = new();

  public IReadOnlyList<UpcomingEvent> GetUpcomingEvents()
  {
    lock (_eventsLock)
    {
      return _events.Where(e => !string.IsNullOrEmpty(e.Text)).OrderBy(e => e.Time).ToList();
    }
  }

  public event Action? UpcomingEventsChanged;

  public OscService(ILogger<OscService> logger)
  {
    _logger = logger;
    _events = Enumerable.Range(0, 24).Select(_ => new UpcomingEvent()).ToArray();
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var server = new OscUdpServer(IPAddress.Any, Port);

    server.RegisterHandler("^/hulp/curr/name$", ctx =>
    {
      _fxParameters.TrackName = ctx.Message.Atoms.FirstOrDefault().StringValue;
      FxParametersChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/name$", ctx =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      _fxParameters.Envelopes[slot].Name = ctx.Message.Atoms.FirstOrDefault().StringValue;
      FxParametersChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/value/str$", ctx =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      _fxParameters.Envelopes[slot].FormattedValue = ctx.Message.Atoms.FirstOrDefault().StringValue ?? string.Empty;
      FxParametersChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/value$", ctx =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      _fxParameters.Envelopes[slot].Value = ctx.Message.Atoms.FirstOrDefault().Float32Value;
      FxParametersChanged?.Invoke();
    });


    // ── Transport (REAPER standard OSC) ──────────────────────────────────
    server.RegisterHandler("^/play$", ctx =>
    {
      Transport.IsPlaying = ctx.Message.Atoms.FirstOrDefault().Float32Value > 0.5f;
      TransportChanged?.Invoke();
    });

    server.RegisterHandler("^/record$", ctx =>
    {
      Transport.IsRecording = ctx.Message.Atoms.FirstOrDefault().Float32Value > 0.5f;
      TransportChanged?.Invoke();
    });

    server.RegisterHandler("^/pause$", ctx =>
    {
      Transport.IsPaused = ctx.Message.Atoms.FirstOrDefault().Float32Value > 0.5f;
      TransportChanged?.Invoke();
    });

    server.RegisterHandler("^/time$", ctx =>
    {
      Transport.Position = ctx.Message.Atoms.FirstOrDefault().Float32Value;

      var now = Stopwatch.GetTimestamp();
      if (Stopwatch.GetElapsedTime(now - _lastPositionNotify).TotalMilliseconds >= 500)
      {
        UpdateEvents(Transport.Position);
        _lastPositionNotify = now;
        TransportChanged?.Invoke();
      }
    });


    // ── Region ───────────────────────────────────────────────────────────
    server.RegisterHandler("^/hulp/region$", ctx =>
    {
      var atoms = ctx.Message.Atoms;
      if (atoms.Count < 2) return;
      Transport.RegionStart = atoms[0].Float32Value;
      Transport.RegionEnd = atoms[1].Float32Value;
      TransportChanged?.Invoke();
    });

    // ── Tracks (from hulp plugin, path index = logical index, 1-based) ──
    server.RegisterHandler(@"^/hulp/track/(\d+)$", ctx =>
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
      var trackNumber = int.Parse(ctx.Match.Groups[1].Value);
      var track = _tracks.FirstOrDefault(t => t.IsActive && t.ReaperIndex == trackNumber - 1);
      if (track == null) return;
      track.IsRecArmed = ctx.Message.Atoms.FirstOrDefault().Float32Value > 0.5f;
      TracksChanged?.Invoke();
    });

    server.RegisterHandler(@"^/track/(\d+)/select$", (MessageHandlerContext ctx) =>
    {
      var trackNumber = int.Parse(ctx.Match.Groups[1].Value);
      var track = _tracks.FirstOrDefault(t => t.IsActive && t.ReaperIndex == trackNumber - 1);
      if (track == null) return;
      track.IsSelected = ctx.Message.Atoms.FirstOrDefault().Float32Value > 0.5f;
      TracksChanged?.Invoke();
    });

    server.RegisterHandler(@"^/track/(\d+)/vu$", (MessageHandlerContext ctx) =>
    {
      var trackNumber = int.Parse(ctx.Match.Groups[1].Value);
      var track = _tracks.FirstOrDefault(t => t.IsActive && t.ReaperIndex == trackNumber - 1);
      if (track == null) return;

      track.VuLevel = Math.Clamp(ctx.Message.Atoms.FirstOrDefault().Float32Value, 0f, 1f);

      var now = Stopwatch.GetTimestamp();
      if (Stopwatch.GetElapsedTime(_lastVuNotify, now).TotalMilliseconds < 250)
      {
        return;
      }

      _lastVuNotify = now;
      TracksChanged?.Invoke();
    });

    // ── Upcoming Events ──────────────────────────────────────────────────
    server.RegisterHandler(@"^/hulp/event/(\d+)$", (MessageHandlerContext ctx) =>
    {
      var eventNumber = int.Parse(ctx.Match.Groups[1].Value);
      var atoms = ctx.Message.Atoms;
      if (atoms.Count < 2) return;
      var text = atoms[0].StringValue ?? string.Empty;
      var time = atoms[1].Double64Value;
      lock (_eventsLock)
      {
        var existing = _events[eventNumber - 1];
        existing.Text = text;
        existing.Time = time;
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

  private void UpdateEvents(double time)
  {
    bool removed;
    lock (_eventsLock)
    {
      var victims = _events
        .Where(e => !string.IsNullOrEmpty(e.Text) && e.Time < time)
        .ToList();
      foreach (var victim in victims)
      {
        victim.Time = 0;
        victim.Text = string.Empty;
      }

      foreach (var ev in _events)
      {
        ev.UpdateCountdown(time);
      }

      removed = victims.Count > 0;
    }

    if (removed)
      UpcomingEventsChanged?.Invoke();
  }
}