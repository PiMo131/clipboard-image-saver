using System.Drawing;
using System.Windows.Forms;

namespace ClipboardImageSaver.Services;

/// <summary>
/// Leest afbeeldingsdata uit het Windows Clipboard.
/// Moet worden aangeroepen op een STA-thread (de WinForms UI-thread).
/// </summary>
public sealed class ClipboardImageService
{
    /// <summary>
    /// Geeft de clipboard-afbeelding terug, of <c>null</c> als het clipboard
    /// geen afbeelding bevat. De aanroeper is verantwoordelijk voor Dispose().
    /// </summary>
    public Image? GetClipboardImage()
    {
        try
        {
            // Primaire methode: standaard CF_BITMAP / CF_DIB formaat
            if (Clipboard.ContainsImage())
            {
                Image? img = Clipboard.GetImage();
                if (img != null)
                {
                    Logging.AppLogger.Instance.Debug(
                        $"Clipboard-afbeelding gelezen: {img.Width}x{img.Height}px");
                    return img;
                }
            }

            // Secundaire methode: probeer als DataObject (screenshots vanuit browsers e.d.)
            IDataObject? data = Clipboard.GetDataObject();
            if (data == null)
                return null;

            // Probeer ook het Bitmap-formaat expliciet
            if (data.GetData(DataFormats.Bitmap) is Image bmpImg)
            {
                Logging.AppLogger.Instance.Debug("Clipboard-afbeelding gelezen via DataFormats.Bitmap");
                return bmpImg;
            }

            return null;
        }
        catch (Exception ex)
        {
            Logging.AppLogger.Instance.Error("Fout bij lezen van clipboard", ex);
            return null;
        }
    }

    /// <summary>
    /// Geeft <c>true</c> als het clipboard een afbeelding bevat.
    /// </summary>
    public bool ContainsImage()
    {
        try { return Clipboard.ContainsImage(); }
        catch { return false; }
    }
}
