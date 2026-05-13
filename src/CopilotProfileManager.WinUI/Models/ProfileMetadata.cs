namespace CopilotProfileManager.App.Models;

public sealed class ProfileMetadata
{
    public HashSet<Guid> ManagedProfileGuids { get; set; } = [];
}
