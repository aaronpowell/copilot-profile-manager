using System.Diagnostics;
using System.Text.RegularExpressions;
using CopilotProfileManager.App.Models;

namespace CopilotProfileManager.App.Services;

public sealed class CopilotCliService
{
    private static readonly Regex OptionSplitRegex = new(@"\s{2,}", RegexOptions.Compiled);

    public async Task<IReadOnlyList<CopilotCliOption>> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "copilot",
                Arguments = "--help",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        try
        {
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return ParseOptions($"{output}{Environment.NewLine}{error}");
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<CopilotCliOption> ParseOptions(string helpOutput)
    {
        var options = new List<CopilotCliOption>();
        CopilotCliOption? current = null;
        var inOptions = false;

        foreach (var rawLine in helpOutput.Split(Environment.NewLine))
        {
            var line = rawLine.TrimEnd();

            if (line.Trim() == "Options:")
            {
                inOptions = true;
                continue;
            }

            if (!inOptions)
            {
                continue;
            }

            if (line.Trim() == "Commands:")
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("-"))
            {
                var parts = OptionSplitRegex.Split(trimmed, 2);
                var syntax = parts[0].Trim();
                var description = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                current = new CopilotCliOption(syntax, description);
                options.Add(current);
                continue;
            }

            if (current is not null)
            {
                current = current with { Description = $"{current.Description} {trimmed}".Trim() };
                options[^1] = current;
            }
        }

        return options
            .Where(option => option.Syntax.Contains("copilot", StringComparison.OrdinalIgnoreCase) is false)
            .OrderBy(option => option.Syntax, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
