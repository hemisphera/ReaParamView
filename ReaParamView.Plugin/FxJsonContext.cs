using System.Text.Json.Serialization;
using ReaParamView.Types;

namespace ReaParamView.Plugin;

[JsonSerializable(typeof(MessageDto))]
internal partial class FxJsonContext : JsonSerializerContext
{
}