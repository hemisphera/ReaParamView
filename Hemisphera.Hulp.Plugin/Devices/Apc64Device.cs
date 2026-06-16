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
  private MidiDevice? _inputDevice;
  private MidiDevice? _outputDevice;
  private readonly MidiListener _midiListener;
  private bool _inputDeviceLoaded;
  private bool _outputDeviceLoaded;


  public Apc64Device(IOptionsMonitor<LooperSettings> settings, ILogger<Apc64Device> logger, MidiListener midiListener)
  {
    _settings = settings;
    _logger = logger;
    _midiListener = midiListener;
    _midiListener.MidiReceived += MidiListenerOnMidiReceived;
  }

  private void MidiListenerOnMidiReceived(object? sender, MidiEvent e)
  {
    if (!_inputDeviceLoaded)
    {
      var deviceName = _settings.Get(null).MidiInputDeviceName;
      _inputDevice = MidiDevice.EnumerateInputs().FirstOrDefault(d => d.Name == deviceName);
      if (_inputDevice == null) _logger.LogWarning("No MIDI input device found with name '{deviceName}'", deviceName);
      _logger.LogDebug("Using input device {name} id {id}", _outputDevice?.Name, _inputDevice?.Id ?? -1);
      _inputDeviceLoaded = true;
    }

    if (_inputDevice == null) return;
    if (e.DeviceIndex != _inputDevice.Id) return;

    if ((e.Status & 0x0F) != MidiChannel - 1) return; // MIDI channel 2
    if (e.Status >> 4 != 11) return; // CC
    if (e.Data1 is < FirstCcNumber or > FirstCcNumber + ParameterCount) return;
    var args = new ParamterChangedEventArgs(e.Data1 - FirstCcNumber, e.Data2);
    _logger.LogDebug("Received CC {no} value {val}", args.ParameterIndex, args.Value);
    ParameterChanged?.Invoke(this, args);
  }


  public void ChangeTrack()
  {
    for (var i = 0; i < ParameterCount; i++)
    {
      SetParameter(i, 0);
    }
  }

  public void SetParameter(int index, int value)
  {
    if (!_outputDeviceLoaded)
    {
      var deviceName = _settings.Get(null).MidiOutputDeviceName;
      _outputDevice = MidiDevice.EnumerateOutput().FirstOrDefault(d => d.Name == deviceName);
      if (_outputDevice == null) _logger.LogWarning("No MIDI output device found with name '{deviceName}'", deviceName);
      _logger.LogDebug("Using output device {name} id {id}", _outputDevice?.Name, _outputDevice?.Id ?? -1);
      _outputDeviceLoaded = true;
    }

    if (_outputDevice == null) return;

    var ccNo = index + FirstCcNumber;
    _logger.LogDebug("Setting CC {ccNo} id {value}", ccNo, value);
    Reaper.StuffMIDIMessage.Invoke(
      _outputDevice.Id + 16,
      0xb0 + (MidiChannel - 1),
      ccNo,
      value);
  }

  public event EventHandler<ParamterChangedEventArgs>? ParameterChanged;
}