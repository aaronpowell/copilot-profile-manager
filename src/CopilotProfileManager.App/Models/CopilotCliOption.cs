namespace CopilotProfileManager.App.Models;

public sealed record CopilotCliOption(string Syntax, string Description)
{
    public override string ToString() => $"{Syntax} — {Description}";
}
