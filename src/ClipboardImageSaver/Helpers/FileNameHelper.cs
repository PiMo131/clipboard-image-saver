namespace ClipboardImageSaver.Helpers;

/// <summary>
/// Genereert unieke PNG-bestandsnamen op basis van tijdstempel.
/// </summary>
public static class FileNameHelper
{
    /// <summary>
    /// Geeft een volledig bestandspad terug dat nog niet bestaat in de opgegeven map.
    /// Formaat: {folder}\{prefix}{yyyyMMdd_HHmmss}[_{counter}].png
    /// </summary>
    public static string GenerateUniquePath(string folder, string prefix)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string candidate = BuildPath(folder, prefix, timestamp, counter: null);

        if (!File.Exists(candidate))
            return candidate;

        // Counter-suffix bij botsing (max 9999 pogingen)
        for (int i = 1; i <= 9999; i++)
        {
            candidate = BuildPath(folder, prefix, timestamp, i);
            if (!File.Exists(candidate))
                return candidate;
        }

        // Absolute fallback met GUID (komt in de praktijk nooit voor)
        return BuildPath(folder, prefix, $"{timestamp}_{Guid.NewGuid():N}", counter: null);
    }

    private static string BuildPath(string folder, string prefix, string stamp, int? counter)
    {
        string name = counter.HasValue
            ? $"{prefix}{stamp}_{counter.Value:D4}.png"
            : $"{prefix}{stamp}.png";

        return Path.Combine(folder, name);
    }
}
