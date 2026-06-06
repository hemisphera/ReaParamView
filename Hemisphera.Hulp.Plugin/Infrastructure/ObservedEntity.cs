using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public class ObservedEntity : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;

  protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  protected void SetFieldValue<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
  {
    if (EqualityComparer<T>.Default.Equals(field, value)) return;
    field = value;
    OnPropertyChanged(propertyName);
  }
}
