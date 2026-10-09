using TheHackerist.Models;

namespace TheHackerist.Abstractions;

public interface INavigator
{
    Task GoToStoryAsync(HnItem story);
}
