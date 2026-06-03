using System.Text.Json.Serialization;
using ReaParamView.Types;

namespace Hemisphera.Hulp.Plugin;

[JsonSerializable(typeof(ParameterSetDto))]
internal partial class FxJsonContext : JsonSerializerContext
{
}