namespace ReaParamView.Plugin;

public class EnvelopeData
{
  public string DisplayName { get; }
  public int? ExplicitSlot { get; }
  public double Value { get; }
  public double Percentage { get; }
  public string FormattedValue { get; }


  public EnvelopeData(LinkedParameter lp)
  {
    var parameter = lp.GetFxParameter();
    var rawName = (parameter.Name ?? "<no name>").Split('/').First().Trim();

    DisplayName = rawName;
    ExplicitSlot = lp.SourceFxParameterIndex - 1;

    var parameterValue = parameter.GetValue();
    var minValue = parameter.Minimum;
    var maxValue = parameter.Maximum;
    Value = parameterValue;
    FormattedValue = parameter.GetFormattedValue();
    Percentage = (parameterValue - minValue) / (maxValue - minValue);
  }
}