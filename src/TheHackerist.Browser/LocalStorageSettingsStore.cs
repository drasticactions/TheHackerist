using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using TheHackerist.Abstractions;

namespace TheHackerist.Browser;

[SupportedOSPlatform("browser")]
internal sealed partial class LocalStorageSettingsStore : ISettingsStore
{
    public const string ModuleName = "settings";
    private const string Key = "thehackerist.settings";

    public string? Read() => ReadItem(Key);

    public void Write(string json) => WriteItem(Key, json);

    [JSImport("read", ModuleName)]
    private static partial string? ReadItem(string key);

    [JSImport("write", ModuleName)]
    private static partial void WriteItem(string key, string value);
}
