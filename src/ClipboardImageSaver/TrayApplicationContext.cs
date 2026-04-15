using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ClipboardImageSaver.Forms;
using ClipboardImageSaver.Logging;
using ClipboardImageSaver.Services;

namespace ClipboardImageSaver;

/// <summary>
/// Centrale applicatie-context. Beheert de tray-icon, hotkey en de save-workflow.
/// Leeft zolang de applicatie draait.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon       _trayIcon;
    private readonly SettingsService  _settings;
    private readonly ClipboardImageService _clipboard;
    private readonly ExplorerPathService   _explorer;
    private readonly ImageSaverService     _saver;
    private HotkeyService? _hotkey;

    private static readonly AppLogger Log = AppLogger.Instance;

    // ── Constructeur ──────────────────────────────────────────────────────────
    public TrayApplicationContext()
    {
        _settings  = new SettingsService();
        _clipboard = new ClipboardImageService();
        _explorer  = new ExplorerPathService();
        _saver     = new ImageSaverService(_settings);

        _trayIcon = BuildTrayIcon();
        RegisterHotkey();

        Log.Info("ClipboardImageSaver gestart. Hotkey: Ctrl+Alt+V");
    }

    // ── Tray-icon opbouwen ────────────────────────────────────────────────────
    private NotifyIcon BuildTrayIcon()
    {
        var icon = new NotifyIcon
        {
            Icon    = CreateAppIcon(),
            Text    = "ClipboardImageSaver  (Ctrl+Alt+V)",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        icon.DoubleClick += (_, _) => OpenSettings();
        return icon;
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var itemSave = new ToolStripMenuItem("Nu opslaan  (Ctrl+Alt+V)");
        itemSave.Font = new Font(itemSave.Font, FontStyle.Bold);
        itemSave.Click += (_, _) => ExecuteSave();

        var itemSettings = new ToolStripMenuItem("Instellingen…");
        itemSettings.Click += (_, _) => OpenSettings();

        var itemLog = new ToolStripMenuItem("Open logbestand");
        itemLog.Click += (_, _) => OpenLogFile();

        var itemSep = new ToolStripSeparator();

        var itemExit = new ToolStripMenuItem("Afsluiten");
        itemExit.Click += (_, _) => Shutdown();

        menu.Items.AddRange([itemSave, itemSettings, itemLog, itemSep, itemExit]);
        return menu;
    }

    // ── Hotkey ────────────────────────────────────────────────────────────────
    private void RegisterHotkey()
    {
        try
        {
            _hotkey = new HotkeyService();
            _hotkey.HotkeyPressed += (_, _) =>
            {
                Log.Info("Hotkey Ctrl+Alt+V gedetecteerd.");
                ExecuteSave();
            };
        }
        catch (Exception ex)
        {
            Log.Error("Hotkey registratie mislukt", ex);
            MessageBox.Show(
                $"{ex.Message}\n\nDe applicatie blijft actief; gebruik het traymenu om op te slaan.",
                "Hotkey niet beschikbaar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            // App draait door zonder hotkey; gebruiker kan traymenu gebruiken.
        }
    }

    // ── Save-workflow ─────────────────────────────────────────────────────────
    private void ExecuteSave()
    {
        try
        {
            using var image = _clipboard.GetClipboardImage();

            if (image == null)
            {
                Log.Warning("Clipboard bevat geen afbeelding.");
                ShowBalloon("Geen afbeelding",
                    "Het clipboard bevat geen afbeelding.", ToolTipIcon.Warning);
                return;
            }

            var cfg = _settings.Load();
            string? explorerPath = _explorer.GetActiveFolderPath();
            string targetFolder  = explorerPath ?? cfg.FallbackFolder;

            if (explorerPath == null)
                Log.Warning($"Geen actieve Explorer gevonden — fallback: {targetFolder}");

            string savedPath = _saver.Save(image, targetFolder);

            ShowBalloon("Opgeslagen!",
                $"{Path.GetFileName(savedPath)}\n→ {targetFolder}", ToolTipIcon.Info);

            if (cfg.OpenAfterSave)
                OpenFile(savedPath);
        }
        catch (Exception ex)
        {
            Log.Error("Fout in ExecuteSave", ex);
            ShowBalloon("Fout bij opslaan",
                $"Kon afbeelding niet opslaan:\n{ex.Message}", ToolTipIcon.Error);
        }
    }

    // ── Menu-acties ───────────────────────────────────────────────────────────
    private void OpenSettings()
    {
        using var form = new SettingsForm(_settings);
        form.ShowDialog();
    }

    private void OpenLogFile()
    {
        string path = Log.LogFilePath;
        if (File.Exists(path))
            OpenFile(path);
        else
            MessageBox.Show(
                "Er is nog geen logbestand aangemaakt voor vandaag.",
                "Logbestand",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
    }

    private void Shutdown()
    {
        _trayIcon.Visible = false;
        _hotkey?.Dispose();
        _trayIcon.Dispose();
        Application.Exit();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void ShowBalloon(string title, string message, ToolTipIcon icon)
    {
        if (!_settings.Load().ShowNotifications) return;
        _trayIcon.ShowBalloonTip(4000, title, message, icon);
    }

    private static void OpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error($"Kon bestand niet openen: {path}", ex);
        }
    }

    /// <summary>
    /// Maakt een eenvoudig programmatisch klembord-icoon (16×16).
    /// Vervang dit door een eigen .ico-resource voor productie.
    /// </summary>
    private static Icon CreateAppIcon()
    {
        try
        {
            using var bmp = new Bitmap(16, 16);
            using var g   = Graphics.FromImage(bmp);

            g.Clear(Color.Transparent);

            // Klembord-lichaam
            using var body   = new SolidBrush(Color.FromArgb(0x00, 0x78, 0xD4));   // Windows-blauw
            using var border = new Pen(Color.FromArgb(0x00, 0x4E, 0x8C), 1f);
            using var white  = new SolidBrush(Color.White);
            using var clip   = new SolidBrush(Color.FromArgb(0xCC, 0xE4, 0xF7));   // Lichtblauw

            // Klem (bovenste deel)
            g.FillRectangle(clip, 5, 0, 6, 3);
            g.DrawRectangle(new Pen(Color.FromArgb(0x00, 0x4E, 0x8C), 1f), 5, 0, 5, 2);

            // Lichaam
            g.FillRectangle(body,   1, 2, 14, 13);
            g.DrawRectangle(border, 1, 2, 13, 12);

            // Witte lijntjes (tekst-illusie)
            g.FillRectangle(white, 3, 6,  10, 1);
            g.FillRectangle(white, 3, 8,  10, 1);
            g.FillRectangle(white, 3, 10,  7, 1);

            IntPtr hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
            // GDI HICON leeft voor de gehele applicatieloop; opgeruimd bij procesesinde.
        }
        catch
        {
            // Absolute fallback naar systeemicoon
            return new Icon(SystemIcons.Application, 16, 16);
        }
    }

    // ── Dispose ───────────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkey?.Dispose();
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
