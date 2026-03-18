using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
      .ConfigureLogging(lb =>
      {
        lb.ClearProviders();
        lb.AddProvider(new ReaperConsoleLoggerProvider());
      })
      .ConfigureServices((_, sc) =>
      {
        //sc.AddOptions<Settings>().BindConfiguration(nameof(Settings));
        sc.AddSingleton<ICommandRegistry, DefaultCommandRegistry>();
        sc.AddSingleton<ActiveEnvelopeMonitor>();
        sc.AddSingleton<ITransport, UdpTransport>();
      })
      .ConfigureAppConfiguration(cfg => cfg.AddJsonFile(settingsPath, optional: true, reloadOnChange: false))
      .Build();

    try
    {
      var state = PluginState.Initialize(ReaperPluginInfo.FromPointer(rec), host);
      var commands = state.Commands ?? throw new Exception();
      //commands.Register("REAFXVIEW_DEBUG", "ReaParamView.Plugin: Print debug info", async () => await Instance.Test());
      var logger = state.Services.GetService<ILogger<ActiveEnvelopeMonitor>>();
      logger?.LogInformation("FxMonitor initialized");

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