using System.Net;
using Hsp.Osc;
using Hemisphera.Hulp.Shared;
using Hemisphera.Hulp.WebApp.Models;

namespace Hemisphera.Hulp.WebApp.Services;

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

  public FxParameter[] FxParameters { get; }
  public event Action? FxParametersChanged;

  // Transport
  public TransportState Transport { get; } = new();
  public event Action? TransportChanged;

  // Tracks (8 fixed slots, indexed 0–7; logical index 1–8)
  private readonly TrackInfo[] _tracks;
  public IReadOnlyList<TrackInfo> Tracks => _tracks;
  public event Action? TracksChanged;

  // Upcoming events
  private readonly UpcomingEvent[] _events;
  private readonly Lock _eventsLock = new();

  public IReadOnlyList<UpcomingEvent> GetUpcomingEvents()
  {
    lock (_eventsLock)
    {
      var currPos = Transport.Position;
      var sortedEvents = _events
        .Where(ev => ev.Position > currPos)
        .OrderBy(ev => ev.Position)
        .ToList();
      return sortedEvents;
    }
  }

  public event Action? UpcomingEventsChanged;

  public OscService(ILogger<OscService> logger)
  {
    _logger = logger;
    _tracks = Enumerable.Range(0, Constants.NoOfTracks).Select(i => new TrackInfo(i)).ToArray();
    _events = Enumerable.Range(0, 24).Select(_ => new UpcomingEvent()).ToArray();
    FxParameters = Enumerable.Range(0, Constants.NoOfParameters).Select(index => new FxParameter(index)).ToArray();
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var server = new OscUdpServer(IPAddress.Any, Port);

    server.RegisterHandler("^/hulp/track/curr/name$", ctx => { CurrentTrackName = ctx.Message.Atoms.FirstOrDefault().StringValue ?? string.Empty; });

    server.RegisterHandler(@"^/hulp/track/curr/fx/(\d+)/name$", ctx =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      FxParameters[slot - 1].Name = ctx.Message.Atoms.FirstOrDefault().StringValue;
      FxParametersChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/track/curr/fx/(\d+)/value/str$", ctx =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      FxParameters[slot - 1].FormattedValue = ctx.Message.Atoms.LastOrDefault().StringValue ?? string.Empty;
      FxParametersChanged?.Invoke();
    });

    server.RegisterHandler(@"^/hulp/track/curr/fx/(\d+)/value$", ctx =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value);
      FxParameters[slot - 1].Percentage = ctx.Message.Atoms.FirstOrDefault().Double64Value;
      FxParametersChanged?.Invoke();
    });


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

    server.RegisterHandler("^/hulp/qnpos$", ctx =>
    {
      Transport.Position = ctx.Message.Atoms.FirstOrDefault().Int32Value;
      UpdateEvents();
      TransportChanged?.Invoke();
    });

    server.RegisterHandler("^/hulp/song/curr$", ctx =>
    {
      var atoms = ctx.Message.Atoms;
      if (atoms.Count < 2) return;
      Transport.SongId = atoms[0].Int32Value;
      Transport.RegionStart = atoms[1].Float32Value;
      Transport.RegionEnd = atoms[2].Float32Value;
      TransportChanged?.Invoke();
    });

    server.RegisterHandler("^/hulp/song/curr/name$", ctx =>
    {
      Transport.SongName = ctx.Message.Atoms.FirstOrDefault().StringValue ?? string.Empty;
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
    server.RegisterHandler(@"^/hulp/event/(\d+)/text$", ctx =>
    {
      var number = int.Parse(ctx.Match.Groups[1].Value);
      lock (_eventsLock)
      {
        _events[number - 1].Text = ctx.Message.Atoms.FirstOrDefault().StringValue ?? string.Empty;
      }

      UpcomingEventsChanged?.Invoke();
    });
    server.RegisterHandler(@"^/hulp/event/(\d+)/pos$", ctx =>
    {
      var number = int.Parse(ctx.Match.Groups[1].Value);
      lock (_eventsLock)
      {
        _events[number - 1].Position = ctx.Message.Atoms.FirstOrDefault().Double64Value;
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

  private void UpdateEvents()
  {
    UpcomingEventsChanged?.Invoke();
  }
}