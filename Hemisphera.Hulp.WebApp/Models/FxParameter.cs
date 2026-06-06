namespace ReaParamView.WebApp.Models;

public class FxParameter
{
  public int Index { get; }
  public string? Name { get; set; }
  public double Percentage { get; set; }
  public string FormattedValue { get; set; } = string.Empty;


  public FxParameter(int index)
  {
    Index = index;
  }
}