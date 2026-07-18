using Hemisphera.Hulp.Shared;
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
  private const int TrackMidiNoteStart = 100;

  private readonly IOptionsMonitor<Apc64DeviceSettings> _settings;
  private readonly ILogger<Apc64Device> _logger;
  private MidiDevice? _inputDevice;
  private MidiDevice? _outputDevice;
  private readonly MidiListener _midiListener;


  public event EventHandler<ActiveTrackChangedEventArgs>? ActiveTrackChanged;
  public event EventHandler<ParameterChangedEventArgs>? ParameterChanged;


  public Apc64Device(IOptionsMonitor<Apc64DeviceSettings> settings, ILogger<Apc64Device> logger, MidiListener midiListener)
  {
    _settings = settings;
    _logger = logger;
    _midiListener = midiListener;
    _midiListener.MidiReceived += MidiListenerOnMidiReceived;
  }


  private void MidiListenerOnMidiReceived(object? sender, MidiEvent e)
  {
    if (_inputDevice == null) return;
    if (e.DeviceIndex != _inputDevice.Id) return;

    if (HandleTouchStripCc(e)) return;
    if (HandleTrackSelect(e)) return;
  }


  private bool HandleTouchStripCc(MidiEvent e)
  {
    if (e.Channel != MidiChannel - 1) return false; // MIDI channel 2
    if (e.MessageType != 14) return false; // pitch wheel
    if (e.Data1 is < FirstCcNumber or > FirstCcNumber + ParameterCount) return false;
    var args = new ParameterChangedEventArgs(e.Data1 - FirstCcNumber, e.Data2);
    _logger.LogDebug("Received CC {no} value {val}", args.ParameterIndex, args.Value);
    ParameterChanged?.Invoke(this, args);
    return true;
  }

  private bool HandleTrackSelect(MidiEvent e)
  {
    if (e.Channel != MidiChannel - 1) return false;
    if (e.MessageType != 8) return false;
    if (e.Data1 < TrackMidiNoteStart) return false;
    if (e.Data1 >= TrackMidiNoteStart + 8) return false;
    ActiveTrackChanged?.Invoke(this, new ActiveTrackChangedEventArgs(e.Data1 - TrackMidiNoteStart));
    return true;
  }

  public void Connect()
  {
    var deviceName = _settings.Get(null).MidiInputDeviceName;
    _inputDevice = MidiDevice.EnumerateInputs().FirstOrDefault(d => d.Name == deviceName);
    if (_inputDevice == null)
      _logger.LogWarning("No MIDI input device found with name '{deviceName}'", deviceName);
    else
      _logger.LogDebug("Using input device {name} id {id}", _outputDevice?.Name, _inputDevice?.Id ?? -1);

    deviceName = _settings.Get(null).MidiOutputDeviceName;
    _outputDevice = MidiDevice.EnumerateOutput().FirstOrDefault(d => d.Name == deviceName);
    if (_outputDevice == null)
      _logger.LogWarning("No MIDI output device found with name '{deviceName}'", deviceName);
    else
      _logger.LogDebug("Using output device {name} id {id}", _outputDevice?.Name, _outputDevice?.Id ?? -1);
  }

  public void SetParameter(int index, int value)
  {
    if (_outputDevice == null) return;

    var ccNo = index + FirstCcNumber;
    _logger.LogDebug("Setting CC {ccNo} id {value}", ccNo, value);
    Reaper.StuffMIDIMessage.Invoke(
      _outputDevice.Id + 16,
      0xb0 + (MidiChannel - 1),
      ccNo,
      value);
  }

  public void SetActiveTrack(int index)
  {
    for (var i = 0; i < ParameterCount; i++)
    {
      SetParameter(i, 0);
    }

    if (_outputDevice == null) return;
    var ev = new MidiEvent();
    for (var i = 0; i < Constants.NoOfTracks; i++)
    {
      ev.Channel = 1;
      ev.MessageType = 8;
      ev.Data1 = (byte)(TrackMidiNoteStart + i);
      ev.Data2 = (byte)(i == index ? 127 : 0);
      _outputDevice.Send(ev);
    }
  }
}