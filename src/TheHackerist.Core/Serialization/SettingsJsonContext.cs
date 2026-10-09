using System.Text.Json.Serialization;
using TheHackerist.Services;

namespace TheHackerist.Serialization;

[JsonSourceGenerationOptions(UseStringEnumConverter = true, WriteIndented = true)]
[JsonSerializable(typeof(SettingsData))]
public sealed partial class SettingsJsonContext : JsonSerializerContext;
