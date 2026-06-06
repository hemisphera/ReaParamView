using System.Diagnostics;
using System.Net;
using Hsp.Osc;
using ReaParamView.Types;
using ReaParamView.WebApp.Models;

namespace ReaParamView.WebApp.Services;

public class OscService : BackgroundService
{
  private const int Port = 9000;
  private readonly ILogger<OscService> _logger;

  // FX Parameters
  public string CurrentTrackName
  {
    get => field;
    private set
    {
      field = value;
      FxParametersChanged?.Invoke();
    }
  } = string.Empty;

  public FxParameter[] FxParameters { get; } = Enumerable.Range(0, Constants.NoOfParameters).Select(index => new FxParameter(index)).ToArray();
  public event Action? FxParametersChanged;

  // Transport
  public TransportState Transport { get; } = new();
  public event Action? TransportChanged;
  private long _lastPositionNotify;

  // Tracks (8 fixed slots, indexed 0–7; logical index 1–8)
  private readonly TrackInfo[] _tracks = Enumerable.Range(0, Constants.NoOfTracks).Select(i => new TrackInfo(i)).ToArray();
  public IReadOnlyList<TrackInfo> Tracks => _tracks;
  public event Action? TracksChanged;

  // Upcoming events
  private readonly UpcomingEvent[] _events;
  private readonly Lock _eventsLock = new();

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

    server.RegisterHandler("^/hulp/curr/name$", ctx => { CurrentTrackName = ctx.Message.Atoms.FirstOrDefault().StringValue ?? string.Empty; });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/name$", ctx =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      FxParameters[slot - 1].Name = ctx.Message.Atoms.FirstOrDefault().StringValue;
      FxParametersChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/value/str$", ctx =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      FxParameters[slot - 1].FormattedValue = ctx.Message.Atoms.LastOrDefault().StringValue ?? string.Empty;
      FxParametersChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/value$", ctx =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      FxParameters[slot - 1].Percentage = ctx.Message.Atoms.FirstOrDefault().Double64Value;
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

    server.RegisterHandler("^/hulp/region$", ctx =>
    {
      var atoms = ctx.Message.Atoms;
      if (atoms.Count < 2) return;
      Transport.RegionStart = atoms[0].Float32Value;
      Transport.RegionEnd = atoms[1].Float32Value;
      TransportChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/track/(\d+)/name$", ctx =>
    {
      var index = int.Parse(ctx.Match.Groups[1].Value); // 1-based
      _tracks[index - 1].Name = ctx.Message.Atoms.FirstOrDefault().StringValue ?? string.Empty;
      TracksChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/track/(\d+)/sel$", ctx =>
    {
      var index = int.Parse(ctx.Match.Groups[1].Value); // 1-based
      _tracks[index - 1].Selected = ctx.Message.Atoms.FirstOrDefault().BoolValue;
      TracksChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/track/(\d+)/mute$", ctx =>
    {
      var index = int.Parse(ctx.Match.Groups[1].Value); // 1-based
      _tracks[index - 1].Mute = ctx.Message.Atoms.FirstOrDefault().BoolValue;
      TracksChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/track/(\d+)/solo$", ctx =>
    {
      var index = int.Parse(ctx.Match.Groups[1].Value); // 1-based
      _tracks[index - 1].Solo = ctx.Message.Atoms.FirstOrDefault().BoolValue;
      TracksChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/track/(\d+)/recarm$", ctx =>
    {
      var index = int.Parse(ctx.Match.Groups[1].Value); // 1-based
      _tracks[index - 1].RecArm = ctx.Message.Atoms.FirstOrDefault().BoolValue;
      TracksChanged?.Invoke();
    });
    // ── Upcoming Events ──────────────────────────────────────────────────
    server.RegisterHandler(@"^/hulp/event/(\d+)$", ctx =>
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