using Hemisphera.Hulp.Plugin.Infrastructure;
using Hemisphera.Hulp.Plugin.Models;
using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.StateModels;

public class HulpState : ObservedEntity
{
  public string? CurrentTrackName
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }

  public Region? Region
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }

  public BeatInfo Beat
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }

  public int QuarterNotePosition
  {
    get => field;
    set => SetFieldValue(ref field, value);
  }
}