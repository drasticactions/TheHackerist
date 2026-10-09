namespace TheHackerist.Abstractions;

public interface ISettingsStore
{
    string? Read();

    void Write(string json);
}
