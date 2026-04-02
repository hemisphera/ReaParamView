using System.Text.RegularExpressions;
using ReaSharp.Models;

namespace ReaParamView.Plugin;

public class EnvelopeData
{
  private static readonly Regex ExplicitSlotPattern = new(@"^@(\d+)\s*", RegexOptions.Compiled);


  public string DisplayName { get; }
  public int? ExplicitSlot { get; }
  public double Value { get; }
  public double Percentage { get; }
  public string FormattedValue { get; }


  public EnvelopeData(TrackFxEnvelope env)
  {
    var rawName = (env.Name ?? "<no name>").Split('/').First().Trim();
    var match = ExplicitSlotPattern.Match(rawName);

    if (match.Success && int.TryParse(match.Groups[1].Value, out var idx) && idx >= 1 && idx <= ActiveEnvelopeMonitor.MaxSlots)
    {
      ExplicitSlot = idx;
      DisplayName = rawName[match.Length..];
      if (string.IsNullOrWhiteSpace(DisplayName)) DisplayName = rawName;
    }
    else
    {
      ExplicitSlot = null;
      DisplayName = rawName;
    }

    var parameter = env.InstanceParameter;
    var parameterValue = parameter.GetValue();
    var minValue = parameter.Minimum;
    var maxValue = parameter.Maximum;
    Value = parameterValue;
    FormattedValue = parameter.GetFormattedValue();
    Percentage = (parameterValue - minValue) / (maxValue - minValue);
  }
}