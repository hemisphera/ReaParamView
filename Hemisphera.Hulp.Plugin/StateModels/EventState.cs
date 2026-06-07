using Hemisphera.Hulp.Plugin.Infrastructure;

namespace Hemisphera.Hulp.Plugin.StateModels;

public class EventState : ObservedEntity
{
  public int Index { get; }


  public string? Text
  {
    get;
    set => SetFieldValue(ref field, value);
  }

  public double Position
  {
    get;
    set => SetFieldValue(ref field, value);
  }


  public EventState(int index)
  {
    Index = index;
  }
}