using System.Threading;
using System.Windows.Forms;
using ClipboardImageSaver.Logging;

namespace ClipboardImageSaver;

/// <summary>
/// Applicatie-entrypoint.
/// - Garandeert één actieve instantie via een named Mutex.
/// - Richt globale exception-handlers in.
/// - Start de WinForms message loop met TrayApplicationContext.
/// </summary>
static class Program
{
    private static Mutex? _singleInstanceMutex;
    private const string MutexName = "Global\\ClipboardImageSaver_F3A8C2D1";

    [STAThread]
    static void Main()
    {
        // ── Single-instance bewaking ──────────────────────────────────────
        _singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: MutexName,
            createdNew: out bool isFirstInstance);

        if (!isFirstInstance)
        {
            MessageBox.Show(
                "ClipboardImageSaver draait al in de systeembalk.",
                "Al actief",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // ── WinForms opties ───────────────────────────────────────────────
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        // ── Globale exception-handlers ────────────────────────────────────
        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        // ── Start ─────────────────────────────────────────────────────────
        try
        {
            Application.Run(new TrayApplicationContext());
        }
        finally
        {
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();
        }
    }

    // ── Exception-handlers ────────────────────────────────────────────────────

    private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        AppLogger.Instance.Error("Onafgehandelde thread-exception", e.Exception);

        var result = MessageBox.Show(
            $"Er is een onverwachte fout opgetreden:\n\n{e.Exception.Message}\n\n" +
            "Klik op OK om door te gaan, of Annuleren om af te sluiten.",
            "Onverwachte fout",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Error);

        if (result == DialogResult.Cancel)
            Application.Exit();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            AppLogger.Instance.Error("Fatale onafgehandelde exception", ex);
        else
            AppLogger.Instance.Error($"Fatale niet-Exception fout: {e.ExceptionObject}");
    }
}
