using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.Models;

public readonly struct FxParameterIndices
{
  public int FxIndex { get; }
  public int ParameterIndex { get; }
  public Track Track { get; }

  public FxParameterIndices(Track track, int fxIndex, int parameterIndex)
  {
    Track = track;
    FxIndex = fxIndex;
    ParameterIndex = parameterIndex;
  }

  public FxInstanceParameter GetParameter()
  {
    var fx = Track.GetFx(FxIndex);
    return fx.GetParameter(ParameterIndex);
  }
}