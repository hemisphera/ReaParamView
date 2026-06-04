using System.Text.Json.Serialization;
using ReaParamView.Types;

namespace Hemisphera.Hulp.Plugin;

[JsonSerializable(typeof(ParameterSetDto))]
[JsonSerializable(typeof(ParameterDto))]
internal partial class HulpJsonContext : JsonSerializerContext
{
}