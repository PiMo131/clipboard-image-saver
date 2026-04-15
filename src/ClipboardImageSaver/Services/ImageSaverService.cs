using System.Drawing;
using System.Drawing.Imaging;
using ClipboardImageSaver.Helpers;

namespace ClipboardImageSaver.Services;

/// <summary>
/// Slaat een <see cref="Image"/>-object op als PNG in de opgegeven doelmap.
/// </summary>
public sealed class ImageSaverService
{
    private readonly SettingsService _settingsService;

    public ImageSaverService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Slaat <paramref name="image"/> op als PNG in <paramref name="targetFolder"/>.
    /// </summary>
    /// <returns>Volledig pad van het opgeslagen bestand.</returns>
    /// <exception cref="IOException">Als schrijven naar schijf mislukt.</exception>
    public string Save(Image image, string targetFolder)
    {
        EnsureDirectoryExists(targetFolder);

        var settings = _settingsService.Load();
        string filePath = FileNameHelper.GenerateUniquePath(targetFolder, settings.FileNamePrefix);

        // Sla op als verliesloze PNG
        image.Save(filePath, ImageFormat.Png);

        Logging.AppLogger.Instance.Info(
            $"PNG opgeslagen: {filePath}  ({image.Width}×{image.Height}px, " +
            $"{new FileInfo(filePath).Length / 1024.0:F1} KB)");

        return filePath;
    }

    private static void EnsureDirectoryExists(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            Logging.AppLogger.Instance.Info($"Map aangemaakt: {folder}");
        }
    }
}
