namespace SearchHub.Api.Models;

public class IndexStatus
{
    public bool IsRunning { get; init; }
    public int PagesIndexed { get; init; }
    public string CurrentSite { get; init; } = string.Empty;
}
