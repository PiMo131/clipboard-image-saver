using System.Runtime.InteropServices;

namespace ClipboardImageSaver.Services;

/// <summary>
/// Bepaalt de actieve maplocatie in Windows Explorer via Shell.Application COM-automatisering.
///
/// Strategie:
///   1. Haal het foreground-venster op (dat actief is op het moment van de hotkey).
///   2. Zoek in Shell.Windows() het Explorer-venster met dit HWND.
///   3. Fallback: geef het meest recente zichtbare Explorer-venster terug.
///   4. Geeft null als er helemaal geen Explorer open staat.
/// </summary>
public sealed class ExplorerPathService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// Geeft het pad van de actieve Explorer-map terug, of <c>null</c>.
    /// </summary>
    public string? GetActiveFolderPath()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            Logging.AppLogger.Instance.Debug($"Foreground HWND: 0x{hwnd:X}");

            // Eerst: exact overeenkomend Explorer-venster
            string? exactPath = QueryShellWindows(hwnd, matchExact: true);
            if (exactPath != null) return exactPath;

            // Daarna: meest recent geopend Explorer-venster
            string? fallback = QueryShellWindows(hwnd, matchExact: false);
            if (fallback != null)
            {
                Logging.AppLogger.Instance.Warning(
                    "Foreground venster is geen Explorer. Meest recente Explorer-map gebruikt.");
            }
            return fallback;
        }
        catch (Exception ex)
        {
            Logging.AppLogger.Instance.Warning($"GetActiveFolderPath mislukt: {ex.Message}");
            return null;
        }
    }

    // ── COM-query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Itereert Shell.Application.Windows().
    /// Als <paramref name="matchExact"/> true is, zoek dan het venster met het opgegeven HWND.
    /// Anders: geef het eerste file://-venster terug (laatste geopend).
    /// </summary>
    private static string? QueryShellWindows(IntPtr targetHwnd, bool matchExact)
    {
        Type? progType = Type.GetTypeFromProgID("Shell.Application");
        if (progType == null)
        {
            Logging.AppLogger.Instance.Warning("Shell.Application ProgID niet gevonden.");
            return null;
        }

        dynamic shellApp = Activator.CreateInstance(progType)!;
        dynamic windows = shellApp.Windows();
        int count = (int)windows.Count;

        // Itereer van achteren naar voren (meest recent geopend staat laatste)
        for (int i = count - 1; i >= 0; i--)
        {
            string? path = TryGetPathFromItem(windows, i, targetHwnd, matchExact);
            if (path != null) return path;
        }

        return null;
    }

    private static string? TryGetPathFromItem(dynamic windows, int index, IntPtr targetHwnd, bool matchExact)
    {
        try
        {
            dynamic? item = windows.Item(index);
            if (item == null) return null;

            // Shell.Application retourneert HWND als VT_I4 (32-bit signed)
            // Op x64 Windows zijn lage HWNDs nog steeds 32-bit-compatibel.
            IntPtr itemHwnd = new IntPtr((int)item.HWND);

            if (matchExact && itemHwnd != targetHwnd)
                return null;

            string locationUrl = (string)item.LocationURL;
            if (string.IsNullOrEmpty(locationUrl)) return null;

            if (!locationUrl.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                return null; // Geen bestandsmap (bijv. "This PC", search-resultaat)

            string path = new Uri(locationUrl).LocalPath;
            Logging.AppLogger.Instance.Debug($"Explorer-map gevonden: {path}");
            return path;
        }
        catch
        {
            return null; // Ontoegankelijk of gesloten venster overslaan
        }
    }
}
