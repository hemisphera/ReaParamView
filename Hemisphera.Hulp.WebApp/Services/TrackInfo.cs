namespace ReaParamView.WebApp.Services;

public class TrackInfo
{
  public bool IsActive { get; set; }
  public string Name { get; set; } = string.Empty;
  public int LogicalIndex { get; set; }
  public int ReaperIndex { get; set; }
  public bool IsSelected { get; set; }
  public bool IsRecArmed { get; set; }
  public float VuLevel { get; set; }
}