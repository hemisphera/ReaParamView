namespace Hemisphera.Hulp.Plugin.Models;

public class MonitoredParameter
{
  public FxParameterIndices Source { get; }
  public FxParameterIndices Target { get; }
  public string Name { get; }
  public int Index => Source.ParameterIndex - 2;
  public double MaxValue { get; }
  public double MinValue { get; }
  public double Value { get; private set; }
  public double Percentage { get; private set; }
  public string FormattedValue { get; private set; } = string.Empty;


  public MonitoredParameter(FxParameterIndices source, FxParameterIndices target)
  {
    Target = target;
    Source = source;

    var parameter = Target.GetParameter();
    Name = (parameter.Name ?? string.Empty).Split('/').First().Trim();
    MinValue = parameter.Minimum;
    MaxValue = parameter.Maximum;
    UpdateValue();
  }


  public void UpdateValue()
  {
    var parameter = Target.GetParameter();
    var parameterValue = parameter.NormalizedValue;
    Value = parameterValue;
    FormattedValue = parameter.GetFormattedValue();
    Percentage = Value;
  }


  public override string ToString()
  {
    return $"{Name} (slot {Index + 1} => FX {Target.FxIndex}/{Target.ParameterIndex} [{MinValue} - {MaxValue}]";
  }
}