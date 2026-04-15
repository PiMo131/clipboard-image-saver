using System.Text.Json;
using ClipboardImageSaver.Logging;
using ClipboardImageSaver.Models;

namespace ClipboardImageSaver.Services;

/// <summary>
/// Laadt en slaat applicatie-instellingen op als JSON in %AppData%\ClipboardImageSaver\.
/// Resultaten worden gecached; roep <see cref="Invalidate"/> aan om de cache te wissen.
/// </summary>
public sealed class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClipboardImageSaver",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private AppSettings? _cached;

    /// <summary>
    /// Geeft gecachte instellingen terug (of laadt ze van schijf).
    /// Als het bestand niet bestaat, worden standaardinstellingen aangemaakt.
    /// </summary>
    public AppSettings Load()
    {
        if (_cached != null)
            return _cached;

        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                _cached = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                AppLogger.Instance.Info("Instellingen geladen vanuit: " + SettingsPath);
                return _cached;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error("Fout bij laden van instellingen, standaard gebruikt", ex);
        }

        // Eerste keer: sla standaardinstellingen direct op
        _cached = new AppSettings();
        TrySave(_cached);
        return _cached;
    }

    /// <summary>Slaat de opgegeven instellingen op en werkt de cache bij.</summary>
    /// <exception cref="IOException">Als schrijven naar schijf mislukt.</exception>
    public void Save(AppSettings settings)
    {
        string dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);

        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
        _cached = settings;

        AppLogger.Instance.Info("Instellingen opgeslagen");
    }

    /// <summary>Wist de in-memory cache zodat de volgende <see cref="Load"/> van schijf leest.</summary>
    public void Invalidate() => _cached = null;

    // Intern: slaat op zonder exception te gooien (voor initiële aanmaak)
    private static void TrySave(AppSettings settings)
    {
        try
        {
            string dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
            AppLogger.Instance.Info("Standaard instellingen aangemaakt in: " + SettingsPath);
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error("Kon standaard instellingen niet aanmaken", ex);
        }
    }
}
