using System.Text;

namespace ReaParamView.Types;

/// <summary>
/// Binary format:
///   [track name length: 1 byte][track name: N bytes]
///   [envelope count: 1 byte]
///   Per envelope:
///     [slot: 1 byte]
///     [name length: 1 byte][name: N bytes]
///     [value: float32, 4 bytes]
///     [percentage: float32, 4 bytes]
/// float32 is used instead of float64 — more than sufficient precision for audio parameters.
/// </summary>
public class MessageDto
{
  public string? TrackName { get; set; }
  public List<EnvelopeDto> Envelopes { get; set; } = [];


  public byte[] SerializeBinary()
  {
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

    WriteShortString(writer, TrackName ?? string.Empty);

    var envelopes = Envelopes;
    writer.Write((byte)Math.Min(envelopes.Count, 255));

    foreach (var env in envelopes)
    {
      writer.Write((byte)env.Slot);
      WriteShortString(writer, env.Name ?? string.Empty);
      writer.Write((float)env.Value);
      writer.Write((float)env.Percentage);
    }

    return ms.ToArray();
  }

  public static MessageDto Deserialize(byte[] data)
  {
    using var ms = new MemoryStream(data);
    using var reader = new BinaryReader(ms, Encoding.UTF8);

    var trackName = ReadShortString(reader);
    var count = reader.ReadByte();
    var envelopes = new List<EnvelopeDto>(count);

    for (var i = 0; i < count; i++)
    {
      envelopes.Add(new EnvelopeDto
      {
        Slot = reader.ReadByte(),
        Name = ReadShortString(reader),
        Value = reader.ReadSingle(),
        Percentage = reader.ReadSingle()
      });
    }
    return new MessageDto { TrackName = trackName, Envelopes = envelopes };
  }

  private static void WriteShortString(BinaryWriter writer, string s)
  {
    var bytes = Encoding.UTF8.GetBytes(s);
    var len = Math.Min(bytes.Length, 255);
    writer.Write((byte)len);
    writer.Write(bytes, 0, len);
  }

  private static string ReadShortString(BinaryReader reader)
  {
    var len = reader.ReadByte();
    return Encoding.UTF8.GetString(reader.ReadBytes(len));
  }
}