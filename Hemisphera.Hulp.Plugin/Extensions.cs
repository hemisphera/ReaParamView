using ReaSharp;
using ReaSharp.Models;

namespace Hemisphera.Hulp.Plugin;

internal static class Extensions
{
  public static void SendCc(this MidiDevice device, int channel, int ccNo, int value)
  {
    Reaper.StuffMIDIMessage.Invoke(
      device.Id + 16,
      0xb0 + (channel - 1),
      ccNo,
      value);
  }

  public static void Send(this MidiDevice device, MidiEvent ev)
  {
    Reaper.StuffMIDIMessage.Invoke(
      device.Id + 16,
      (ev.MessageType << 4) | ev.Channel,
      ev.Data1,
      ev.Data2);
  }
}