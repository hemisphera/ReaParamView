// Delays a parameter's value updates so that only the latest value within a 100ms
// window is applied to the UI. Newer messages within the window overwrite the pending
// one; the window is not reset, so during a continuous stream the latest value is
// flushed every 100ms. Applied per parameter (slot).

using Hemisphera.Hulp.WebApp.Models;

namespace Hemisphera.Hulp.WebApp.Services;

public sealed class FxValueDebouncer
{
  private const int DelayMs = 100;

  private readonly FxParameter _parameter;
  private readonly Action _notifyChanged;
  private readonly Lock _lock = new();

  private double _pendingPercentage;
  private bool _pendingPercentageSet;
  private string _pendingFormatted = string.Empty;
  private bool _pendingFormattedSet;
  private Timer? _timer;
  private bool _scheduled;

  public FxValueDebouncer(FxParameter parameter, Action notifyChanged)
  {
    _parameter = parameter;
    _notifyChanged = notifyChanged;
  }

  public void SchedulePercentage(double value)
  {
    lock (_lock)
    {
      _pendingPercentage = value;
      _pendingPercentageSet = true;
      EnsureScheduled();
    }
  }

  public void ScheduleFormatted(string value)
  {
    lock (_lock)
    {
      _pendingFormatted = value;
      _pendingFormattedSet = true;
      EnsureScheduled();
    }
  }

  private void EnsureScheduled()
  {
    if (_scheduled) return;
    _scheduled = true;
    if (_timer == null)
      _timer = new Timer(OnTick, null, DelayMs, Timeout.Infinite);
    else
      _timer.Change(DelayMs, Timeout.Infinite);
  }

  private void OnTick(object? state)
  {
    double pct;
    string fmt;
    bool applyPct, applyFmt;

    lock (_lock)
    {
      pct = _pendingPercentage;
      fmt = _pendingFormatted;
      applyPct = _pendingPercentageSet;
      applyFmt = _pendingFormattedSet;
      _pendingPercentageSet = false;
      _pendingFormattedSet = false;
      _scheduled = false;
    }

    if (applyPct)
      _parameter.Percentage = pct;
    if (applyFmt)
      _parameter.FormattedValue = fmt;

    _notifyChanged();
  }

  public void Dispose() => _timer?.Dispose();
}