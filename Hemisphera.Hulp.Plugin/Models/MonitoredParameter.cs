using ReaSharp.Models;
using ReaSharp.RppXml;

namespace Hemisphera.Hulp.Plugin.Models;

public class MonitoredParameter
{
  public Track Track { get; }
  public int TargetFxParameterIndex { get; }
  public int TargetFxIndex { get; }
  public string Name { get; }
  public int Index { get; }
  public double MaxValue { get; }
  public double MinValue { get; }
  public double Value { get; private set; }
  public double Percentage { get; private set; }
  public string FormattedValue { get; private set; } = string.Empty;


  public MonitoredParameter(Track track, int fxIndex, int fxParameterIndex, int index)
  {
    Track = track;
    TargetFxIndex = fxIndex;
    TargetFxParameterIndex = fxParameterIndex;
    Index = index;

    var parameter = GetFxParameter();

    Name = (parameter.Name ?? string.Empty).Split('/').First().Trim();
    MinValue = parameter.Minimum;
    MaxValue = parameter.Maximum;
    UpdateValue();
  }


  public void UpdateValue()
  {
    var parameter = GetFxParameter();
    var parameterValue = parameter.GetValueNormalized();
    Value = parameterValue;
    FormattedValue = parameter.GetFormattedValue();
    //Percentage = (parameterValue - MinValue) / (MaxValue - MinValue);
    Percentage = Value;
  }

  public FxInstanceParameter GetFxParameter()
  {
    var fx = Track.GetFx(TargetFxIndex);
    return fx.GetParameter(TargetFxParameterIndex);
  }

  public override string ToString()
  {
    return $"{Name} (slot {Index + 1} [{MinValue} - {MaxValue}]";
  }
}