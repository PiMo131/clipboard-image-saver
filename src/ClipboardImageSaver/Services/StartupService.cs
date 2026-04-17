using Microsoft.Win32;

namespace ClipboardImageSaver.Services;

/// <summary>
/// Beheert de Windows-autostart registersleutel voor de huidige gebruiker.
/// Sleutel: HKCU\Software\Microsoft\Windows\CurrentVersion\Run
/// </summary>
public static class StartupService
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClipboardImageSaver";

    /// <summary>Voegt de app toe aan Windows-autostart.</summary>
    public static void Enable()
    {
        string exePath = GetExePath();

        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true)
            ?? throw new InvalidOperationException("Kan registersleutel voor autostart niet openen.");

        key.SetValue(ValueName, $"\"{exePath}\"");
        Logging.AppLogger.Instance.Info($"Autostart ingeschakeld: {exePath}");
    }

    /// <summary>Verwijdert de app uit Windows-autostart.</summary>
    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
        if (key?.GetValue(ValueName) != null)
        {
            key.DeleteValue(ValueName);
            Logging.AppLogger.Instance.Info("Autostart uitgeschakeld.");
        }
    }

    /// <summary>Geeft true als de autostart-registerwaarde bestaat.</summary>
    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
        return key?.GetValue(ValueName) != null;
    }

    private static string GetExePath()
        => Environment.ProcessPath
           ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
           ?? throw new InvalidOperationException("Kan het pad van de huidige exe niet bepalen.");
}
