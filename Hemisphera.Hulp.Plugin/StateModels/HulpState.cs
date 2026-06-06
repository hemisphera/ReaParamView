using Hemisphera.Hulp.Plugin.Infrastructure;

namespace Hemisphera.Hulp.Plugin.StateModels;

public class HulpState : ObservedEntity
{
  public string? CurrentTrackName
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }
}
