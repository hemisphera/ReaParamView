using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReaSharp.Models;
using ReaSharp.RppXml;

namespace ReaParamView.Plugin;

public class Commands
{
  public static async Task Debug(IServiceProvider arg)
  {
    var logger = arg.GetRequiredService<ILogger<Commands>>();
    var track = Project.Current.GetSelectedTrack();
    if (track == null) return;

    var lps = LinkedParameter.Load(track);
    foreach (var lp in lps)
      logger.LogInformation(lp.ToString());
  }
}