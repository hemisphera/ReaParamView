namespace Hemisphera.Hulp.Plugin.Devices;

public class Apc64DeviceSettings
{
  /// <summary>
  /// The MIDI input device name used to receive parameter changes from the APC64.
  /// </summary>
  public string? MidiInputDeviceName { get; set; }

  /// <summary>
  /// The MIDI output device name used to send parameter values to the APC64.
  /// </summary>
  public string? MidiOutputDeviceName { get; set; }
}