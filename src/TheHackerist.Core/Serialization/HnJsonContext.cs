using System.Text.Json.Serialization;
using TheHackerist.Models;

namespace TheHackerist.Serialization;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(HnItem))]
[JsonSerializable(typeof(long[]))]
public sealed partial class HnJsonContext : JsonSerializerContext;
