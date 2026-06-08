using Hemisphera.Hulp.Plugin.Infrastructure;

namespace Hemisphera.Hulp.Plugin.StateModels;

public class SongState : ObservedEntity
{
  public int Index { get; }

  public int RegionId
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }

  public string? Name
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }


  public SongState(int index)
  {
    Index = index;
  }
}