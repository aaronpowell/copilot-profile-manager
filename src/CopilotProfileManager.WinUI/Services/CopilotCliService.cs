using System.Diagnostics;
using System.Text.RegularExpressions;
using CopilotProfileManager.App.Models;

namespace CopilotProfileManager.App.Services;

public sealed partial class CopilotCliService
{
    private static readonly Regex OptionSplitRegex = OptionsSplit();

    public static async Task<CopilotCliLoadResult> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        var diagnostics = new List<string>();
        var copilotCommandPath = ResolveCopilotCommandPath();
        if (string.IsNullOrWhiteSpace(copilotCommandPath))
        {
            diagnostics.Add("Copilot CLI help: could not resolve a Copilot executable or shim path.");
            return new CopilotCliLoadResult([], diagnostics);
        }

        var startInfo = CreateStartInfo(copilotCommandPath, diagnostics);

        using var process = new Process
        {
            StartInfo = startInfo,
        };

        try
        {
            diagnostics.Add($"Copilot CLI help: launching '{startInfo.FileName}'.");
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            diagnostics.Add($"Copilot CLI help: process exited with code {process.ExitCode}.");

            if (!string.IsNullOrWhiteSpace(error))
            {
                foreach (var line in EnumerateLines(error)
                    .Select(static line => line.Trim())
                    .Where(static line => !string.IsNullOrWhiteSpace(line))
                    .Take(8))
                {
                    diagnostics.Add($"Copilot CLI help stderr: {line}");
                }
            }

            var options = ParseOptions($"{output}{Environment.NewLine}{error}");
            diagnostics.Add($"Copilot CLI help: parsed {options.Count} option(s).");

            if (options.Count == 0)
            {
                foreach (var line in EnumerateLines(output)
                    .Select(static line => line.Trim())
                    .Where(static line => !string.IsNullOrWhiteSpace(line))
                    .Take(8))
                {
                    diagnostics.Add($"Copilot CLI help output: {line}");
                }
            }

            return new CopilotCliLoadResult(options, diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.Add($"Copilot CLI help failed: {ex.Message}");
            return new CopilotCliLoadResult([], diagnostics);
        }
    }

    private static ProcessStartInfo CreateStartInfo(string copilotCommandPath, ICollection<string> diagnostics)
    {
        if (Path.GetExtension(copilotCommandPath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add($"Copilot CLI help: using native executable '{copilotCommandPath}'.");
            var directStartInfo = new ProcessStartInfo
            {
                FileName = copilotCommandPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            directStartInfo.ArgumentList.Add("--help");
            return directStartInfo;
        }

        var powerShellPath = ResolvePowerShellPath();
        if (string.IsNullOrWhiteSpace(powerShellPath))
        {
            throw new InvalidOperationException("Could not locate pwsh.exe.");
        }

        diagnostics.Add($"Copilot CLI help: using shim '{copilotCommandPath}' via PowerShell '{powerShellPath}'.");
        var startInfo = new ProcessStartInfo
        {
            FileName = powerShellPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add($"& '{copilotCommandPath.Replace("'", "''", StringComparison.Ordinal)}' --help");
        return startInfo;
    }

    private static string? ResolvePowerShellPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7-preview", "pwsh.exe"),
        };

        return candidates.FirstOrDefault(File.Exists) ?? ResolveFromPath("pwsh.exe");
    }

    private static string? ResolveCopilotCommandPath()
    {
        var appDataNpm = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "npm");

        var localAppDataLinks = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WinGet",
            "Links");

        var appDataNativeCli = Path.Combine(
            appDataNpm,
            "node_modules",
            "@github",
            "copilot",
            "node_modules",
            "@github",
            "copilot-win32-x64",
            "copilot.exe");

        var localProgramNativeCli = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "m-copilot",
            "Clawpilot",
            "resources",
            "app.asar.unpacked",
            "node_modules",
            "@github",
            "copilot-win32-x64",
            "copilot.exe");

        var candidates = new[]
        {
            appDataNativeCli,
            localProgramNativeCli,
            Path.Combine(appDataNpm, "copilot.ps1"),
            Path.Combine(appDataNpm, "copilot.cmd"),
            Path.Combine(appDataNpm, "copilot"),
            Path.Combine(localAppDataLinks, "copilot.exe"),
            Path.Combine(localAppDataLinks, "copilot"),
        };

        return candidates.FirstOrDefault(File.Exists)
            ?? ResolveFromPath("copilot.ps1")
            ?? ResolveFromPath("copilot.cmd")
            ?? ResolveFromPath("copilot.exe")
            ?? ResolveFromPath("copilot");
    }

    private static string? ResolveFromPath(string fileName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var rawEntry in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(rawEntry, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static List<CopilotCliOption> ParseOptions(string helpOutput)
    {
        var options = new List<CopilotCliOption>();
        CopilotCliOption? current = null;
        var inOptions = false;

        foreach (var rawLine in EnumerateLines(helpOutput))
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

        return [.. options
            .Where(option => option.Syntax.Contains("copilot", StringComparison.OrdinalIgnoreCase) is false)
            .OrderBy(option => option.Syntax, StringComparer.OrdinalIgnoreCase)];
    }

    private static IEnumerable<string> EnumerateLines(string text) =>
        text.Split('\n').Select(static line => line.TrimEnd('\r'));

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex OptionsSplit();
}

public sealed record CopilotCliLoadResult(
    IReadOnlyList<CopilotCliOption> Options,
    IReadOnlyList<string> Diagnostics);
