using Hemisphera.Hulp.Plugin.Infrastructure;

namespace Hemisphera.Hulp.Plugin.StateModels;

public class ParameterSate : ObservedEntity
{
  public int Index { get; }

  public string? Name
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }

  public double Percentage
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }

  public string FormattedValue
  {
    get => field;
    set => SetFieldValue(ref field, value);
  } = string.Empty;


  public ParameterSate(int index)
  {
    Index = index;
  }
}