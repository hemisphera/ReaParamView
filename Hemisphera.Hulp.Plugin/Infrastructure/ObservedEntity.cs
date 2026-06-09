using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public class ObservedEntity
{
  public event EventHandler<PropertyValueChangedEventArgs>? PropertyChanged;

  protected int DisablePropertyNotifications { get; set; }


  protected virtual void OnPropertyChanged(object? oldValue, object? newValue, [CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyValueChangedEventArgs(propertyName, oldValue, newValue));
  }

  protected void SetFieldValue<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
  {
    if (EqualityComparer<T>.Default.Equals(field, value)) return;
    var oldValue = field;
    field = value;
    if (DisablePropertyNotifications == 0)
    {
      OnPropertyChanged(oldValue, field, propertyName);
    }
  }
}