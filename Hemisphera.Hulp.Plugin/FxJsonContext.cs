using System.Text.Json.Serialization;
using ReaParamView.Types;

namespace Hemisphera.Hulp.Plugin;

[JsonSerializable(typeof(MessageDto))]
internal partial class FxJsonContext : JsonSerializerContext
{
}