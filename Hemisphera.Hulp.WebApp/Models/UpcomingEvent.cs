namespace Hemisphera.Hulp.WebApp.Models;

public class UpcomingEvent
{
  public string Text { get; set; } = string.Empty;
  public double Position { get; set; }
  public double? Countdown { get; set; }
  public int Index { get; set; }
  public bool Visible { get; set; }


  public void UpdateCountdown(double pos)
  {
    Countdown = Math.Clamp(pos - Position, 0, 10);
  }
}