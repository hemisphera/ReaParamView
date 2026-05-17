using Hemisphera.Hulp.Plugin.Models.Looper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hemisphera.Hulp.Plugin;

public class Commands
{
  public static async Task StartLooper(IServiceProvider arg)
  {
    var state = arg.GetRequiredService<LooperState>();
    await state.Start();
  }

  public static async Task StopLooper(IServiceProvider arg)
  {
    var state = arg.GetRequiredService<LooperState>();
    await state.Stop();
  }

  public static Task DumpDebug(IServiceProvider arg)
  {
    var state = arg.GetRequiredService<LooperState>();
    var logger = arg.GetRequiredService<ILogger<LooperState>>();
    state.CurrentSong?.Dump(logger);
    return Task.CompletedTask;
  }

  public static async Task FocusCurrentSong(IServiceProvider arg)
  {
    var state = arg.GetRequiredService<LooperState>();
    await state.FocusRegion();
  }
}