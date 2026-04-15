namespace ClipboardImageSaver.Models;

/// <summary>
/// Persistente applicatie-instellingen. Geserialiseerd naar JSON in %AppData%.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Map die gebruikt wordt als er geen actief Explorer-venster is.
    /// </summary>
    public string FallbackFolder { get; set; } = DefaultFallbackFolder();

    /// <summary>
    /// Prefix voor gegenereerde bestandsnamen (bijv. "clipboard_").
    /// </summary>
    public string FileNamePrefix { get; set; } = "clipboard_";

    /// <summary>
    /// Toon Windows-ballonmelding na succesvol opslaan.
    /// </summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>
    /// Open het opgeslagen bestand direct na opslaan.
    /// </summary>
    public bool OpenAfterSave { get; set; } = false;

    private static string DefaultFallbackFolder()
        => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
}
