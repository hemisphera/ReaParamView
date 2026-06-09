namespace Hemisphera.Hulp.Plugin.Infrastructure;

public class PropertyValueChangedEventArgs
{
  public static readonly PropertyValueChangedEventArgs Empty = new(null, null, null);

  public string PropertyName { get; }
  public object? OldValue { get; }
  public object? NewValue { get; }


  public PropertyValueChangedEventArgs(string? name, object? oldValue, object? newValue)
  {
    PropertyName = name ?? string.Empty;
    OldValue = oldValue;
    NewValue = newValue;
  }
}