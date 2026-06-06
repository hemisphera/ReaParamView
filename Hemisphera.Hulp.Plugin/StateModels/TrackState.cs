using Hemisphera.Hulp.Plugin.Infrastructure;

namespace Hemisphera.Hulp.Plugin.StateModels;

public class TrackState : ObservedEntity
{
  public int Index { get; }

  public string? Name
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }

  public bool Selected
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }

  public bool Mute
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }

  public bool Solo
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }

  public bool RecordArm
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }


  public TrackState(int index)
  {
    Index = index;
  }
}
