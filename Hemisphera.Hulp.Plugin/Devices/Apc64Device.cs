using Hemisphera.Hulp.Plugin.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReaSharp;
using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin.Devices;

public class Apc64Device : IDevice
{
  private const int MidiChannel = 2;
  private const int FirstCcNumber = 14;
  private const int ParameterCount = 8;

  private readonly IOptionsMonitor<LooperSettings> _settings;
  private readonly ILogger<Apc64Device> _logger;
  private int _deviceId;
  private string? _deviceName;
  private readonly MidiListener _midiListener;


  public Apc64Device(IOptionsMonitor<LooperSettings> settings, ILogger<Apc64Device> logger, MidiListener midiListener)
  {
    _settings = settings;
    _logger = logger;
    _midiListener = midiListener;
    _midiListener.MidiReceived += MidiListenerOnMidiReceived;
  }

  private void MidiListenerOnMidiReceived(object? sender, MidiEvent e)
  {
    _logger.LogDebug("Received event {ev}", e);
    if (e.DeviceIndex != _deviceId) return;
    if ((e.Status & 0x0F) != MidiChannel - 1) return; // MIDI channel 2
    if (e.Status >> 4 != 11) return; // CC
    if (e.Data1 is < FirstCcNumber or > FirstCcNumber + ParameterCount) return;
    var args = new ParamterChangedEventArgs(e.Data1 - FirstCcNumber, e.Data2);
    _logger.LogDebug("Received CC {no} value {val}", args.ParameterIndex, args.Value);
    ParameterChanged?.Invoke(this, args);
  }


  public void Initialize()
  {
    for (var i = 0; i < ParameterCount; i++)
    {
      SetParameter(i, 0);
    }
  }

  public void SetParameter(int index, int value)
  {
    var deviceName = _settings.Get(null).MidiOutputDeviceName;
    if (deviceName == null) return;

    if (deviceName != _deviceName)
    {
      _deviceName = deviceName;
      _deviceId = MidiDevice.EnumerateOutput().FirstOrDefault(d => d.Name == _deviceName)?.Id ?? -1;
      _logger.LogDebug("Using device {name} id {id}", _deviceName, _deviceId);
    }

    if (_deviceId < 0) return;
    var ccNo = index + FirstCcNumber;
    _logger.LogDebug("Setting CC {ccNo} id {value}", ccNo, value);
    Reaper.StuffMIDIMessage.Invoke(
      _deviceId + 16,
      0xb0 + (MidiChannel - 1),
      ccNo,
      value);
  }

  public event EventHandler<ParamterChangedEventArgs>? ParameterChanged;
}