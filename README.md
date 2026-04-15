# ClipboardImageSaver

Windows system tray app die clipboard-afbeeldingen opslaat als PNG in de actieve Windows Explorer-map via een globale hotkey.

## Functionaliteit

- **Globale hotkey `Ctrl+Alt+V`** — werkt vanuit elke applicatie
- **Detecteert actieve Explorer-map** automatisch via Shell.Application COM
- **Fallback map** als er geen Explorer-venster open staat (instelbaar)
- **Unieke bestandsnamen** op basis van tijdstempel (`clipboard_20250415_143022.png`)
- **Instellingen** opgeslagen als JSON in `%AppData%\ClipboardImageSaver\`
- **Logging** naar `%AppData%\ClipboardImageSaver\logs\app_yyyy-MM-dd.log`
- **Ballonmeldingen** bij succes, waarschuwing of fout
- **Single-instance** bewaking via named Mutex

## Vereisten

- Windows 10 / 11 (x64)
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (of gebruik de self-contained build)

## Installatie

### Optie 1 — Bouwen vanuit broncode

```powershell
git clone https://github.com/PiMo131/clipboard-image-saver.git
cd clipboard-image-saver
dotnet run --project src\ClipboardImageSaver\ClipboardImageSaver.csproj
```

### Optie 2 — Self-contained .exe (geen .NET installatie nodig)

```powershell
dotnet publish src\ClipboardImageSaver\ClipboardImageSaver.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -o publish\
```

Start daarna `publish\ClipboardImageSaver.exe`.

### Opstarten bij Windows-login (optioneel)

```powershell
$exe = "$PWD\publish\ClipboardImageSaver.exe"
Set-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" "ClipboardImageSaver" $exe
```

## Gebruik

1. Start de app — een blauw icoon verschijnt in de systeembalk
2. Kopieer een afbeelding naar het clipboard (screenshot, afbeelding uit browser, etc.)
3. Navigeer in Explorer naar de gewenste map
4. Druk **`Ctrl+Alt+V`** — de afbeelding wordt direct opgeslagen als PNG

### Traymenu

| Optie | Actie |
|---|---|
| Nu opslaan | Zelfde als de hotkey |
| Instellingen… | Beheer fallback map, prefix en opties |
| Open logbestand | Bekijk het logbestand van vandaag |
| Afsluiten | Sluit de app af en geeft de hotkey vrij |

## Instellingen

Opgeslagen in `%AppData%\ClipboardImageSaver\settings.json`:

```json
{
  "FallbackFolder": "C:\\Users\\...\\Pictures",
  "FileNamePrefix": "clipboard_",
  "ShowNotifications": true,
  "OpenAfterSave": false
}
```

## Projectstructuur

```
src/ClipboardImageSaver/
├── Program.cs                        # Entrypoint, single-instance, exception-handlers
├── TrayApplicationContext.cs         # Tray-icon, menu, save-workflow
├── Models/
│   └── AppSettings.cs                # Instellingenmodel
├── Services/
│   ├── HotkeyService.cs              # Win32 RegisterHotKey via P/Invoke
│   ├── ClipboardImageService.cs      # Clipboard-toegang (WinForms STA)
│   ├── ExplorerPathService.cs        # Actieve Explorer-map via Shell.Application COM
│   ├── ImageSaverService.cs          # PNG-opslag
│   └── SettingsService.cs            # JSON-instellingen lezen/schrijven
├── Forms/
│   └── SettingsForm.cs               # Instellingenvenster
├── Helpers/
│   └── FileNameHelper.cs             # Unieke bestandsnaamgeneratie
└── Logging/
    └── AppLogger.cs                  # Thread-veilige file-logger
```

## Bekende beperkingen

- Hotkey `Ctrl+Alt+V` is niet configureerbaar via de UI (gepland voor v1.1)
- DIB-clipboard formaat (sommige oude Office-versies) wordt nog niet ondersteund
- Tray-icoon is programmatisch — geen eigen .ico resource

## Licentie

MIT
