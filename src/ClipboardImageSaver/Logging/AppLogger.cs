using System.Text;

namespace ClipboardImageSaver.Logging;

/// <summary>
/// Thread-veilige singleton file-logger met dagelijkse rotatie.
/// Schrijft naar %AppData%\ClipboardImageSaver\logs\app_yyyy-MM-dd.log
/// </summary>
public sealed class AppLogger
{
    // ── Singleton ────────────────────────────────────────────────────────────
    private static readonly Lazy<AppLogger> _lazy = new(() => new AppLogger());
    public static AppLogger Instance => _lazy.Value;

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly string _logDirectory;
    private readonly object _writeLock = new();

    public string LogFilePath => Path.Combine(_logDirectory, $"app_{DateTime.Now:yyyy-MM-dd}.log");

    private AppLogger()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipboardImageSaver",
            "logs");

        try { Directory.CreateDirectory(_logDirectory); }
        catch { /* Als dit mislukt schrijft de logger gewoon niets */ }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void Info(string message)    => Write("INFO ", message);
    public void Warning(string message) => Write("WARN ", message);
    public void Debug(string message)   => Write("DEBUG", message);

    public void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    // ── Intern ────────────────────────────────────────────────────────────────
    private void Write(string level, string message, Exception? ex = null)
    {
        var sb = new StringBuilder();
        sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");

        if (ex != null)
        {
            sb.AppendLine();
            sb.Append($"  {ex.GetType().Name}: {ex.Message}");

            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                sb.AppendLine();
                sb.Append($"  StackTrace: {ex.StackTrace.Trim()}");
            }

            if (ex.InnerException != null)
            {
                sb.AppendLine();
                sb.Append($"  InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }

        string line = sb.ToString();

        lock (_writeLock)
        {
            try { File.AppendAllText(LogFilePath, line + Environment.NewLine); }
            catch { /* Logging nooit propageren als exception */ }
        }
    }
}
