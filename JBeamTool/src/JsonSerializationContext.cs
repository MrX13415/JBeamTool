using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBeamTool;

/// <summary>
/// JSON serialization context for trimming and AOT compatibility.
/// Provides compile-time serialization information for all types used with JsonSerializer.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<object>))]
internal partial class AppJsonSerializationContext : JsonSerializerContext
{
}
