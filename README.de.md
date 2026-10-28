# Bolt

[English](README.md) | **Deutsch** | [Français](README.fr.md) | [Español](README.es.md) | [Português](README.pt.md) | [中文](README.zh.md) | [日本語](README.ja.md) | [Magyar](README.hu.md)

Ein Desktop-Software-Store (Avalonia 12 + [KD.Avalonia.Rice](https://www.nuget.org/packages/KD.Avalonia.Rice)).
Er hat keinen eigenen Katalog: Man fügt GitHub-, GitLab- (gitlab.com oder selbst gehostet) und Codeberg-Repositorys
(Forgejo/Gitea) hinzu, Bolt wählt aus deren Releases das zum System passende Paket aus und installiert es mit dem
**Installer des Host-Betriebssystems**:

| System | Paket | Installer |
|--------|-------|-----------|
| Debian / Ubuntu und Derivate | `.deb` | `pkexec apt-get` |
| Fedora / RHEL | `.rpm` | `pkexec dnf` |
| openSUSE / SLES | `.rpm` | `pkexec zypper` |
| Windows | `.msi`, `.exe` | `msiexec` / der Installer selbst (UAC) |

## Funktionsweise

- **Repository hinzufügen**: Der Store übernimmt ein Repository nur, wenn es ein Release mit einem Paket für das
  aktuelle System (Format + Architektur) gibt. Andernfalls wird angezeigt, warum nicht.
- **Hauptbildschirm**: oben die installierten, unten die installierbaren Anwendungen, je Gruppe 20 pro Seite.
- **Updates**: pro installierter Anwendung oder für alle auf einmal nach Updates suchen und installieren.
- **Status**: Beim Start prüft Bolt, ob die installierten Pakete noch vorhanden sind (extern entfernt → die Anwendung
  kommt zurück in die Liste der installierbaren).
- **API-Tokens**: optionaler Token pro Host (Rate-Limits, private Repositorys) in den Einstellungen.
- **Daten**: SQLite, `~/.local/share/bolt/bolt.db` (Windows: `%LOCALAPPDATA%\bolt\bolt.db`), Icons daneben im
  Ordner `icons`.

## `.bolt`-Metadaten

Ein Repository kann in der Datei `.bolt/bolt.json` festlegen, wie es im Store erscheint:

```json
{
  "name": "My App",
  "version": "1.2.3",
  "author": "Jemand",
  "icon": "icon.png"
}
```

- Alle Felder sind optional; fehlender Name → Name des Repositorys, fehlender Autor → Eigentümer des Repositorys.
- `icon` ist ein Pfad relativ zum Ordner `.bolt` (png, jpg, webp, ico, bmp) und darf nicht aus dem Ordner
  herauszeigen.
- Der Store liest die Datei beim Hinzufügen und bei jeder Update-Suche neu ein.
- Legt der Benutzer ein eigenes Icon für eine Anwendung fest, wird es lokal gespeichert und der Store liest den
  Ordner `.bolt` ab dann nicht mehr (zurückschalten mit dem Menüpunkt *Ikon visszaállítása* / *Icon zurücksetzen*).

## Entwicklung

```sh
dotnet build
dotnet test
dotnet run --project src/Bolt.App
```

- `src/Bolt.Core` – Datenmodell (EF Core + SQLite), Forge-Clients, Paketauswahl, Installer, `StoreService`.
- `src/Bolt.App` – Avalonia-Oberfläche (MVVM, CommunityToolkit.Mvvm).
- `tests/Bolt.Core.Tests` – xUnit-Tests.

Neue Migration: `dotnet ef migrations add <Name> --project src/Bolt.Core --output-dir Data/Migrations`.
