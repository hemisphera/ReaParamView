using ReaSharp.Models;
using ReaSharp.RppXml;

namespace ReaParamView.Plugin;

public class LinkedParameter
{
  public static LinkedParameter[] Load(Track track)
  {
    if (!RppReader.TryRead(track.GetTrackStateChunk() ?? string.Empty, out var trackNode))
      return [];

    var relativatorIndex = FindRelativatorIndex(trackNode);
    if (relativatorIndex == null) return [];

    var allFx = trackNode.FindChild("FXCHAIN")?.FindChildren("FX").ToArray() ?? [];
    var linkedParameters = allFx
      .SelectMany(fx => fx.FindChildren("PROGRAMENV"))
      .Select(envelope =>
      {
        var plink = envelope.GetPropertyValue("PLINK", 1)?.AsString().Split(':') ?? [];
        var linkSourceFxIndex = plink.Length > 0 ? (int?)int.Parse(plink[0]) : null;
        if (linkSourceFxIndex != relativatorIndex) return null;
        return new LinkedParameter
        {
          Track = track,
          SourceFxIndex = relativatorIndex.Value,
          SourceFxParameterIndex = envelope.GetPropertyValue("PLINK", 2)?.AsInt32() ?? -1,
          TargetFxIndex = allFx.IndexOf(envelope.Parent),
          TargetFxParameterIndex = int.Parse(envelope.DefaultValues[0].AsString().Split(':').First())
        };
      }).OfType<LinkedParameter>();
    return linkedParameters.ToArray();
  }

  private static int? FindRelativatorIndex(RppNode x)
  {
    var allFx = x.FindChild("FXCHAIN")?.FindChildren("FX").ToArray() ?? [];
    for (var i = 0; i < allFx.Length; i++)
    {
      var fx = allFx[i];
      var plugin = fx.Children.FirstOrDefault();
      var strvalue = plugin?.DefaultValues.Count >= 1 ? plugin.DefaultValues[0].AsString() : string.Empty;
      if (strvalue.Equals("Hemisphera/Relativator", StringComparison.OrdinalIgnoreCase))
      {
        return i;
      }
    }

    return null;
  }

  public required Track Track { get; init; }
  public int TargetFxParameterIndex { get; init; }
  public int TargetFxIndex { get; init; }
  public int SourceFxParameterIndex { get; init; }
  public int SourceFxIndex { get; init; }


  public override string ToString()
  {
    return $"({SourceFxIndex}:{SourceFxParameterIndex}) => ({TargetFxIndex}:{TargetFxParameterIndex})";
  }

  public FxInstanceParameter GetFxParameter()
  {
    var fx = Track.GetFx(TargetFxIndex);
    return fx.GetParameter(TargetFxParameterIndex);
  }
}