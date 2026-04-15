using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClipboardImageSaver.Services;

/// <summary>
/// Registreert een systeem-brede (globale) hotkey via Win32 RegisterHotKey en
/// stuurt een event wanneer de combinatie Ctrl+Alt+V wordt ingedrukt.
///
/// Gebruikt een verborgen NativeWindow als message-sink voor WM_HOTKEY.
/// Implementeert IDisposable — altijd aanroepen bij afsluiten!
/// </summary>
public sealed class HotkeyService : NativeWindow, IDisposable
{
    // ── Win32-constanten ──────────────────────────────────────────────────────
    private const int  WM_HOTKEY   = 0x0312;
    private const int  HOTKEY_ID   = 0x4356;  // 'CV' — uniek voor deze app
    private const uint MOD_ALT     = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_NOREPEAT = 0x4000; // Geen herhaalde berichten bij ingedrukt houden
    private const uint VK_V        = 0x56;

    // ── P/Invoke ──────────────────────────────────────────────────────────────
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _registered;
    private bool _disposed;

    /// <summary>Wordt gefired op de UI-thread wanneer Ctrl+Alt+V is ingedrukt.</summary>
    public event EventHandler? HotkeyPressed;

    // ── Constructeur ──────────────────────────────────────────────────────────
    public HotkeyService()
    {
        // Maak een onzichtbaar venster aan dat WM_HOTKEY ontvangt
        CreateHandle(new CreateParams());
        Register();
    }

    // ── Registratie ───────────────────────────────────────────────────────────
    private void Register()
    {
        _registered = RegisterHotKey(
            Handle,
            HOTKEY_ID,
            MOD_CONTROL | MOD_ALT | MOD_NOREPEAT,
            VK_V);

        if (!_registered)
        {
            int err = Marshal.GetLastWin32Error();
            throw new Win32Exception(err,
                $"Kan globale hotkey Ctrl+Alt+V niet registreren (Win32-fout {err}). " +
                "Mogelijk gebruikt een andere applicatie deze combinatie al.");
        }

        Logging.AppLogger.Instance.Info("Globale hotkey Ctrl+Alt+V geregistreerd.");
    }

    // ── WndProc ───────────────────────────────────────────────────────────────
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return; // Niet doorgeven aan base
        }
        base.WndProc(ref m);
    }

    // ── IDisposable ───────────────────────────────────────────────────────────
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_registered && Handle != IntPtr.Zero)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            _registered = false;
            Logging.AppLogger.Instance.Info("Globale hotkey Ctrl+Alt+V vrijgegeven.");
        }

        if (Handle != IntPtr.Zero)
            DestroyHandle();
    }
}
