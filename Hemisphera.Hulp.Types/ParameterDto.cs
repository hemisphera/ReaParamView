namespace ReaParamView.Types;

public class ParameterDto
{
  public string? Name { get; set; }
  public double Percentage { get; set; }
  public string FormattedValue { get; set; } = string.Empty;
}