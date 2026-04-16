using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReaSharp;

namespace ReaParamView.Plugin;

public static class Plugin
{
  public static ActiveEnvelopeMonitor Instance => PluginState.Instance.Services.GetRequiredService<ActiveEnvelopeMonitor>();

  [UnmanagedCallersOnly(EntryPoint = "ReaperPluginEntry")]
  public static int ReaperPluginEntry(IntPtr hInstance, IntPtr rec)
  {
    var settingsPath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      "reaparamview.json");
    var host = Host.CreateDefaultBuilder()
      .ConfigureLogging((context, lb) =>
      {
        lb.ClearProviders();
        lb.AddConfiguration(context.Configuration.GetSection("Logging"));
        lb.AddProvider(new ReaperConsoleLoggerProvider());
      })
      .ConfigureServices((context, sc) =>
      {
        sc.Configure<MonitorSettings>(context.Configuration.GetSection(nameof(MonitorSettings)));
        sc.AddSingleton<ICommandRegistry, DefaultCommandRegistry>();
        sc.AddSingleton<ActiveEnvelopeMonitor>();
        sc.AddSingleton<ITransport, OscTransport>();
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
      logger?.LogDebug("FxMonitor initialized");

      var monitor = state.Services.GetRequiredService<ActiveEnvelopeMonitor>();
      _ = monitor.Start();

      return 1;
    }
    catch (Exception ex)
    {
      ReaperConsoleLogger.WriteLog("Failed to initialize ReaParamView plugin: " + ex.Message);
      return 0;
    }
  }
}