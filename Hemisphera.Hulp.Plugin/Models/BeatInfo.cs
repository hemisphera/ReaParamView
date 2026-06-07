namespace Hemisphera.Hulp.Plugin.Models;

public readonly struct BeatInfo
{
  public int Beat { get; }
  public int Length { get; }

  public BeatInfo(int beat, int length)
  {
    Beat = beat;
    Length = length;
  }
}