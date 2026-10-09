using TheHackerist.Abstractions;

namespace TheHackerist.Services;

public sealed class FileSettingsStore(string path) : ISettingsStore
{
    public string Path { get; } = path;

    public static string DefaultPath()
    {
        var root = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TheHackerist");
        return System.IO.Path.Combine(root, "settings.json");
    }

    public string? Read()
    {
        try
        {
            return File.Exists(Path) ? File.ReadAllText(Path) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Write(string json)
    {
        if (System.IO.Path.GetDirectoryName(Path) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        var temp = Path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, Path, overwrite: true);
    }
}
