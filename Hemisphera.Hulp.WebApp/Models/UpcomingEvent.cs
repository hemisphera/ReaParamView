namespace Hemisphera.Hulp.WebApp.Models;

public class UpcomingEvent
{
  public string Text { get; set; } = string.Empty;
  public double Time { get; set; }
  public double Countdown { get; private set; }

  
  public void UpdateCountdown(double pos)
  {
    Countdown = Math.Clamp(pos - Time, 0, 10);
  }
}
