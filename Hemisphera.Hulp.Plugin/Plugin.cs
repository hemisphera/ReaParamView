using System.Runtime.InteropServices;
using System.Threading.Channels;
using Hemisphera.Hulp.Plugin.Devices;
using Hemisphera.Hulp.Plugin.Infrastructure;
using Hemisphera.Hulp.Plugin.Models;
using Hemisphera.Hulp.Plugin.Settings;
using Hsp.Osc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        sc.AddSingleton<Channel<IMessage>>(_ =>
          Channel.CreateBounded<IMessage>(new BoundedChannelOptions(1000)
          {
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
          }));
        sc.AddSingleton<ChannelWriter<IMessage>>(sp => sp.GetRequiredService<Channel<IMessage>>().Writer);
        sc.AddSingleton<ChannelReader<IMessage>>(sp => sp.GetRequiredService<Channel<IMessage>>().Reader);
        sc.AddSingleton<IOscWriter, ChannelOscWriter>();
        sc.AddSingleton<IDevice, Apc64Device>();
        sc.Configure<LooperSettings>(context.Configuration.GetSection(nameof(LooperSettings)));
        sc.AddSingleton<ICommandRegistry, DefaultCommandRegistry>();
        sc.AddSingleton<HulpMonitor>();
        sc.AddSingleton<OscTransport>();
        sc.AddSingleton<LooperState>();
        sc.AddSingleton<MidiListener>();
        sc.AddSingleton<ITransport, OscTransport>(services => services.GetRequiredService<OscTransport>());
      })
      .ConfigureAppConfiguration(cfg => { cfg.AddJsonFile(settingsPath, optional: true, reloadOnChange: true); })
      .Build();

    try
    {
      var state = PluginState.Initialize(ReaperPluginInfo.FromPointer(rec), host);

      var commands = state.Services.GetRequiredService<ICommandRegistry>();
      commands.Register("HULP_START", "Hulp: Start", Commands.StartLooper);
      commands.Register("HULP_DEBUG", "Hulp: Print Debug Info", Commands.DumpDebug);
      commands.Register("HULP_FOCUS", "Hulp: Focus song", Commands.FocusCurrentSong);
      commands.Register("HULP_INIT", "Hulp: Initialize", Commands.Initialize);
      commands.Register("HULP_SELECT", "Hulp: Select Song", Commands.SelectSong);
      commands.Register("HULP_RESTART", "Hulp: Restart Engine", Commands.RestartEngine);

      var monitor = state.Services.GetRequiredService<HulpMonitor>();
      _ = monitor.Start();
      var transport = state.Services.GetRequiredService<ITransport>();
      _ = transport.StartAsync(CancellationToken.None);

      var device = state.Services.GetRequiredService<IDevice>();
      device.Initialize();

      return 1;
    }
    catch (Exception ex)
    {
      ReaperConsoleLogger.WriteLog("Failed to initialize Hulp plugin: " + ex.Message);
      return 0;
    }
  }
}