namespace CopilotProfileManager.App.Services;

public sealed class AppLogService
{
    private const int MaxBufferedEntries = 500;
    private readonly object syncRoot = new();
    private readonly List<string> bufferedEntries = [];
    private bool fileWriteFailureReported;

    public static AppLogService Instance { get; } = new();

    public event EventHandler? LogChanged;

    public string LogFilePath { get; }

    private AppLogService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CopilotProfileManager",
            "logs");

        Directory.CreateDirectory(appDataPath);
        LogFilePath = Path.Combine(appDataPath, "app.log");

        AppendEntry(
            $"=== Session started {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} ===",
            persistToFile: true);
        Write("Logging", $"Persistent log file: {LogFilePath}");
    }

    public string GetBufferedLog()
    {
        lock (syncRoot)
        {
            return string.Join(Environment.NewLine, bufferedEntries);
        }
    }

    public void ClearBuffer()
    {
        lock (syncRoot)
        {
            bufferedEntries.Clear();
        }

        LogChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Write(string source, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        AppendEntry(
            $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] [{source}] {message}",
            persistToFile: true);
    }

    public void WriteException(string source, Exception exception, string? message = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(exception);

        var prefix = string.IsNullOrWhiteSpace(message)
            ? "Unhandled exception"
            : message.Trim();

        Write(source, $"{prefix}{Environment.NewLine}{exception}");
    }

    private void AppendEntry(string entry, bool persistToFile)
    {
        bool raiseLogChanged;

        lock (syncRoot)
        {
            bufferedEntries.Add(entry);
            TrimBuffer();

            if (persistToFile)
            {
                TryAppendToFile(entry);
            }

            raiseLogChanged = true;
        }

        if (raiseLogChanged)
        {
            LogChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void TryAppendToFile(string entry)
    {
        try
        {
            File.AppendAllText(LogFilePath, entry + Environment.NewLine);
        }
        catch (UnauthorizedAccessException ex)
        {
            ReportFileWriteFailure(ex);
        }
        catch (IOException ex)
        {
            ReportFileWriteFailure(ex);
        }
    }

    private void ReportFileWriteFailure(Exception exception)
    {
        if (fileWriteFailureReported)
        {
            return;
        }

        fileWriteFailureReported = true;
        bufferedEntries.Add(
            $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] [Logging] Failed to write to '{LogFilePath}': {exception.Message}");
        TrimBuffer();
    }

    private void TrimBuffer()
    {
        if (bufferedEntries.Count <= MaxBufferedEntries)
        {
            return;
        }

        bufferedEntries.RemoveRange(0, bufferedEntries.Count - MaxBufferedEntries);
    }
}
