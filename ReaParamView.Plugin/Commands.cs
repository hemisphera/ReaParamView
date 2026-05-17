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

    var rppxml = track.GetTrackStateChunk() ?? string.Empty;
    var rpp = RppReader.Read(rppxml);
    var lps = LinkedParameter.Load(rpp);
    foreach(var lp in lps)
      logger.LogInformation(lp.ToString());
  }
}