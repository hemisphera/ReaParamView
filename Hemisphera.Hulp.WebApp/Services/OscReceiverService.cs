using System.Net;
using Hsp.Osc;
using ReaParamView.Types;

namespace ReaParamView.WebApp.Services;

public class OscReceiverService(ParameterDisplayService displayService, ILogger<OscReceiverService> logger) : BackgroundService
{
  private const int Port = 9000;

  private readonly MessageDto _currentMessage = new();

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var server = new OscUdpServer(IPAddress.Any, Port);

    server.RegisterHandler(@"^/hulp/curr/name$", (MessageHandlerContext ctx) =>
    {
      _currentMessage.TrackName = ctx.Message.Atoms.FirstOrDefault().StringValue;
      displayService.UpdateMessage(_currentMessage);
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/name$", (MessageHandlerContext ctx) =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value) - 1;
      GetOrCreateEnvelope(slot).Name = ctx.Message.Atoms.FirstOrDefault().StringValue;
      displayService.UpdateMessage(_currentMessage);
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/value/str$", (MessageHandlerContext ctx) =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value) - 1;
      GetOrCreateEnvelope(slot).FormattedValue = ctx.Message.Atoms.FirstOrDefault().StringValue ?? string.Empty;
      displayService.UpdateMessage(_currentMessage);
    });

    server.RegisterHandler(@"^/hulp/curr/fx/(\d+)/value$", (MessageHandlerContext ctx) =>
    {
      var slot = int.Parse(ctx.Match.Groups[1].Value) - 1;
      GetOrCreateEnvelope(slot).Value = ctx.Message.Atoms.FirstOrDefault().Float32Value;
      displayService.UpdateMessage(_currentMessage);
    });

    server.BeginListen();
    logger.LogInformation("OSC receiver listening on port {Port}", Port);

    try
    {
      await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    catch (OperationCanceledException)
    {
      // expected on shutdown
    }
  }

  private EnvelopeDto GetOrCreateEnvelope(int slot)
  {
    var env = _currentMessage.Envelopes.FirstOrDefault(e => e.Slot == slot);
    if (env != null) return env;
    env = new EnvelopeDto { Slot = slot };
    _currentMessage.Envelopes.Add(env);
    return env;
  }
}
