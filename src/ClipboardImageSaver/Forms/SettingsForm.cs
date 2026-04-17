using System.Drawing;
using System.Windows.Forms;
using ClipboardImageSaver.Models;
using ClipboardImageSaver.Services;

namespace ClipboardImageSaver.Forms;

/// <summary>
/// Dialoogvenster voor het beheren van applicatie-instellingen.
/// Alle controls worden programmatisch aangemaakt (geen .Designer.cs).
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly SettingsService _settingsService;
    private AppSettings _settings;

    // ── Controls ──────────────────────────────────────────────────────────────
    private TextBox  _txtFallbackFolder    = null!;
    private Button   _btnBrowse            = null!;
    private TextBox  _txtFileNamePrefix    = null!;
    private CheckBox _chkShowNotifications = null!;
    private CheckBox _chkOpenAfterSave     = null!;
    private CheckBox _chkStartWithWindows  = null!;
    private Button   _btnSave              = null!;
    private Button   _btnCancel            = null!;

    // ── Constructeur ──────────────────────────────────────────────────────────
    public SettingsForm(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = _settingsService.Load();

        InitializeComponent();
        PopulateControls();
    }

    // ── Opbouw UI ─────────────────────────────────────────────────────────────
    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "ClipboardImageSaver — Instellingen";
        ClientSize = new Size(490, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Padding = new Padding(12);

        // ── Fallback map ──────────────────────────────────────────────────
        var lblFallback = Label("Fallback map (gebruikt als er geen Explorer actief is):",
            new Point(12, 15));

        _txtFallbackFolder = new TextBox
        {
            Location = new Point(12, 36),
            Size = new Size(375, 23),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _btnBrowse = new Button
        {
            Text = "…",
            Location = new Point(393, 35),
            Size = new Size(80, 25)
        };
        _btnBrowse.Click += BtnBrowse_Click;

        // ── Prefix ────────────────────────────────────────────────────────
        var lblPrefix = Label("Bestandsnaam prefix:", new Point(12, 74));

        _txtFileNamePrefix = new TextBox
        {
            Location = new Point(12, 95),
            Size = new Size(200, 23)
        };

        var lblPrefixHint = Label("(bijv. clipboard_ → clipboard_20250101_120000.png)",
            new Point(220, 98), foreColor: SystemColors.GrayText, fontSize: 8f);

        // ── Opties ────────────────────────────────────────────────────────
        _chkShowNotifications = new CheckBox
        {
            Text = "Toon ballonmelding bij succesvol opslaan",
            Location = new Point(12, 133),
            Size = new Size(350, 20),
            AutoSize = false
        };

        _chkOpenAfterSave = new CheckBox
        {
            Text = "Open bestand direct na opslaan",
            Location = new Point(12, 158),
            Size = new Size(350, 20),
            AutoSize = false
        };

        _chkStartWithWindows = new CheckBox
        {
            Text = "Automatisch starten bij aanmelden bij Windows",
            Location = new Point(12, 183),
            Size = new Size(350, 20),
            AutoSize = false
        };

        // ── Versieregel ───────────────────────────────────────────────────
        var lblVersion = Label("v1.0.0  ·  Hotkey: Ctrl+Alt+V  ·  %AppData%\\ClipboardImageSaver",
            new Point(12, 218), foreColor: SystemColors.GrayText, fontSize: 8f);

        // ── Knoppen ───────────────────────────────────────────────────────
        _btnSave = new Button
        {
            Text = "Opslaan",
            Location = new Point(295, 238),
            Size = new Size(85, 28),
            DialogResult = DialogResult.OK
        };
        _btnSave.Click += BtnSave_Click;

        _btnCancel = new Button
        {
            Text = "Annuleren",
            Location = new Point(386, 238),
            Size = new Size(88, 28),
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        Controls.AddRange([
            lblFallback, _txtFallbackFolder, _btnBrowse,
            lblPrefix, _txtFileNamePrefix, lblPrefixHint,
            _chkShowNotifications, _chkOpenAfterSave, _chkStartWithWindows,
            lblVersion, _btnSave, _btnCancel
        ]);

        ResumeLayout(performLayout: false);
    }

    // ── Helper voor uniforme labels ───────────────────────────────────────────
    private static Label Label(
        string text, Point location,
        Color? foreColor = null, float fontSize = 9f)
    {
        return new Label
        {
            Text = text,
            Location = location,
            AutoSize = true,
            ForeColor = foreColor ?? SystemColors.ControlText,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, fontSize)
        };
    }

    // ── Data binding ──────────────────────────────────────────────────────────
    private void PopulateControls()
    {
        _txtFallbackFolder.Text       = _settings.FallbackFolder;
        _txtFileNamePrefix.Text       = _settings.FileNamePrefix;
        _chkShowNotifications.Checked = _settings.ShowNotifications;
        _chkOpenAfterSave.Checked     = _settings.OpenAfterSave;
        // Lees de werkelijke registerwaarde (niet de gecachte instelling)
        _chkStartWithWindows.Checked  = StartupService.IsEnabled();
    }

    // ── Event handlers ────────────────────────────────────────────────────────
    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Selecteer de fallback map voor clipboard-afbeeldingen",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_txtFallbackFolder.Text)
                ? _txtFallbackFolder.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
            _txtFallbackFolder.Text = dlg.SelectedPath;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!Validate_()) return;

        _settings.FallbackFolder    = _txtFallbackFolder.Text.Trim();
        _settings.FileNamePrefix    = _txtFileNamePrefix.Text.Trim();
        _settings.ShowNotifications = _chkShowNotifications.Checked;
        _settings.OpenAfterSave     = _chkOpenAfterSave.Checked;
        _settings.StartWithWindows  = _chkStartWithWindows.Checked;

        try
        {
            _settingsService.Save(_settings);
            ApplyStartupSetting(_settings.StartWithWindows);
        }
        catch (Exception ex)
        {
            DialogResult = DialogResult.None; // Voorkom automatisch sluiten
            MessageBox.Show(
                $"Instellingen konden niet worden opgeslagen:\n\n{ex.Message}",
                "Opslaan mislukt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void ApplyStartupSetting(bool enable)
    {
        try
        {
            if (enable) StartupService.Enable();
            else        StartupService.Disable();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Kon autostart niet {(enable ? "inschakelen" : "uitschakelen")}:\n\n{ex.Message}",
                "Register-fout",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    // ── Validatie ─────────────────────────────────────────────────────────────
    private bool Validate_()
    {
        if (string.IsNullOrWhiteSpace(_txtFallbackFolder.Text))
            return ShowValidationError("Vul een fallback map in.", _txtFallbackFolder);

        if (string.IsNullOrWhiteSpace(_txtFileNamePrefix.Text))
            return ShowValidationError("Vul een bestandsnaam prefix in.", _txtFileNamePrefix);

        if (_txtFileNamePrefix.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return ShowValidationError(
                "De prefix bevat ongeldige tekens voor een bestandsnaam.", _txtFileNamePrefix);

        return true;
    }

    private bool ShowValidationError(string message, Control focusTarget)
    {
        MessageBox.Show(message, "Validatiefout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        focusTarget.Focus();
        return false;
    }
}
