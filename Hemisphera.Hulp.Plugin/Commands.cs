using Hemisphera.Hulp.Plugin.Infrastructure;
using Hemisphera.Hulp.Plugin.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReaSharp;
using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin;

public class Commands
{
  public static async Task StartLooper(IServiceProvider arg, ActionContext actionContext)
  {
    var state = arg.GetRequiredService<LooperState>();
    await state.Start();
  }

  public static async Task StopLooper(IServiceProvider arg, ActionContext actionContext)
  {
    var state = arg.GetRequiredService<LooperState>();
    await state.Stop();
  }

  public static Task DumpDebug(IServiceProvider arg, ActionContext actionContext)
  {
    var state = arg.GetRequiredService<LooperState>();
    var logger = arg.GetRequiredService<ILogger<LooperState>>();
    logger.LogInformation("Debug");
    state.CurrentSong?.Dump(logger);
    logger.LogInformation("Context: {context}", actionContext);
    return Task.CompletedTask;
  }

  public static async Task FocusCurrentSong(IServiceProvider arg, ActionContext actionContext)
  {
    var state = arg.GetRequiredService<LooperState>();
    await state.FocusRegion();
  }

  public static async Task Initialize(IServiceProvider provider, ActionContext context)
  {
    var state = provider.GetRequiredService<LooperState>();
    await state.Initialize();
  }

  public static async Task Restart(IServiceProvider arg1, ActionContext arg2)
  {
    var ct = CancellationToken.None;
    var transport = PluginState.Instance.Services.GetRequiredService<ITransport>();
    await transport.StopAsync(ct);
    await transport.StartAsync(ct);
  }
}