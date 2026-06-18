using System.IO;
using System.Text;

namespace VoiceType.Services;

/// <summary>Writes diagnostic messages to a daily log file under AppData.</summary>
public interface ILogService
{
    /// <summary>Full path to today's log file.</summary>
    string LogFilePath { get; }

    /// <summary>Directory containing log files.</summary>
    string LogDirectory { get; }

    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? ex = null);
}

/// <summary>
/// Thread-safe file logger. Logs land in
/// <c>%APPDATA%\VoiceType\logs\voicetype-YYYYMMDD.log</c>.
/// </summary>
public sealed class LogService : ILogService, IDisposable
{
    private readonly object _lock = new();
    private readonly string _logDirectory;
    private StreamWriter? _writer;
    private string? _currentDate;

    public LogService()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VoiceType",
            "logs");
        Directory.CreateDirectory(_logDirectory);
        LogFilePath = GetLogPath(DateTime.Now);
        OpenWriter(DateTime.Now);
        WriteUnlocked("INFO", $"VoiceType started. Log file: {LogFilePath}");
    }

    public string LogFilePath { get; }

    public string LogDirectory => _logDirectory;

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? ex = null)
    {
        if (ex is null)
        {
            Write("ERROR", message);
            return;
        }

        Write("ERROR", $"{message} | {ex.GetType().Name}: {ex.Message}");
        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            Write("ERROR", ex.StackTrace);
    }

    private void Write(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                RotateIfNeeded();
                WriteUnlocked(level, message);
            }
            catch
            {
                // Logging must never crash the app.
            }
        }
    }

    private void WriteUnlocked(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        _writer?.WriteLine(line);
        _writer?.Flush();
        System.Diagnostics.Debug.WriteLine(line);
    }

    private void RotateIfNeeded()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        if (today == _currentDate)
            return;

        _writer?.Dispose();
        OpenWriter(DateTime.Now);
    }

    private void OpenWriter(DateTime date)
    {
        _currentDate = date.ToString("yyyyMMdd");
        var path = GetLogPath(date);
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    }

    private string GetLogPath(DateTime date) =>
        Path.Combine(_logDirectory, $"voicetype-{date:yyyyMMdd}.log");

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}
