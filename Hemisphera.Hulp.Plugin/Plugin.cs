using System.Runtime.InteropServices;
using Hemisphera.Hulp.Plugin.Infrastructure;
using Hemisphera.Hulp.Plugin.Models.Looper;
using Hemisphera.Hulp.Plugin.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReaSharp;

namespace Hemisphera.Hulp.Plugin;

public static class Plugin
{
  [UnmanagedCallersOnly(EntryPoint = "ReaperPluginEntry")]
  public static int ReaperPluginEntry(IntPtr hInstance, IntPtr rec)
  {
    var settingsPath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      "hulp.json");

    var host = Host.CreateDefaultBuilder()
      .ConfigureLogging((context, lb) =>
      {
        lb.ClearProviders();
        lb.AddConfiguration(context.Configuration.GetSection("Logging"));
        lb.AddProvider(new ReaperConsoleLoggerProvider());
      })
      .ConfigureServices((context, sc) =>
      {
        sc.Configure<LooperSettings>(context.Configuration.GetSection(nameof(LooperSettings)));
        sc.Configure<MonitorSettings>(context.Configuration.GetSection(nameof(MonitorSettings)));
        sc.AddSingleton<ICommandRegistry, DefaultCommandRegistry>();
        sc.AddSingleton<ActiveEnvelopeMonitor>();
        sc.AddSingleton<OscTransport>();
        sc.AddSingleton<LooperState>();
        sc.AddSingleton<ITransport, OscTransport>(services => services.GetRequiredService<OscTransport>());
      })
      .ConfigureAppConfiguration(cfg => { cfg.AddJsonFile(settingsPath, optional: true, reloadOnChange: false); })
      .Build();

    try
    {
      var state = PluginState.Initialize(ReaperPluginInfo.FromPointer(rec), host);
      var logger = state.Services.GetService<ILogger<ActiveEnvelopeMonitor>>();
      if (logger != null && logger.IsEnabled(LogLevel.Debug))
      {
        var options = state.Services.GetRequiredService<IOptions<MonitorSettings>>().Value;
        logger.LogDebug("{Key}: {Value}", nameof(options.Host), options.Host);
        logger.LogDebug("{Key}: {Value}", nameof(options.Port), options.Port);
        logger.LogDebug("{Key}: {Value}", nameof(options.UpdateIntervalMs), options.UpdateIntervalMs);
      }

      var commands = state.Services.GetRequiredService<ICommandRegistry>();
      commands.Register("HULP_START", "Hulp: Start", Commands.StartLooper);
      commands.Register("HULP_STOP", "Hulp: Stop", Commands.StopLooper);
      commands.Register("HULP_DEBUG", "Hulp: Print Debug Info", Commands.DumpDebug);
      commands.Register("HULP_FOCUS", "Hulp: Focus song", Commands.FocusCurrentSong);

      var monitor = state.Services.GetRequiredService<ActiveEnvelopeMonitor>();
      _ = monitor.Start();

      return 1;
    }
    catch (Exception ex)
    {
      ReaperConsoleLogger.WriteLog("Failed to initialize Hulp plugin: " + ex.Message);
      return 0;
    }
  }
}