# Bolt

<p align="center">
  <img src="docs/assets/bolt-header.png" alt="Bolt — servers and flowing data" width="900" />
</p>

**English** | [Deutsch](README.de.md) | [Français](README.fr.md) | [Español](README.es.md) | [Português](README.pt.md) | [中文](README.zh.md) | [日本語](README.ja.md) | [Magyar](README.hu.md)

A desktop software store (Avalonia 12 + [KD.Avalonia.Rice](https://www.nuget.org/packages/KD.Avalonia.Rice)).
It has no catalog of its own: you add GitHub, GitLab (gitlab.com or self-hosted) and Codeberg (Forgejo/Gitea)
repositories, Bolt picks the package matching your system from their releases, and installs it with the
**host OS installer**:

| System | Package | Installer |
|--------|---------|-----------|
| Debian / Ubuntu and derivatives | `.deb` | `pkexec apt-get` |
| Fedora / RHEL | `.rpm` | `pkexec dnf` |
| openSUSE / SLES | `.rpm` | `pkexec zypper` |
| Windows | `.msi`, `.exe` | `msiexec` / the installer itself (UAC) |

## How it works

- **Adding a repository**: the store only accepts a repository if it has a release containing a package for the
  current system (format + architecture). Otherwise it tells you why not.
- **Main screen**: installed apps at the top, installable apps below, paginated at 20 per page in each group.
- **Updates**: check for and install updates per installed app, or for all of them at once.
- **State**: on startup Bolt checks whether installed packages are still present (removed externally → the app goes
  back to the installable list).
- **API tokens**: optional per-host token (rate limits, private repositories) in the settings.
- **Data**: SQLite, `~/.local/share/bolt/bolt.db` (Windows: `%LOCALAPPDATA%\bolt\bolt.db`), icons next to it in the
  `icons` folder.

## `.bolt` metadata

A repository can describe how it should appear in the store in `.bolt/bolt.json`:

```json
{
  "name": "My App",
  "version": "1.2.3",
  "author": "Someone",
  "icon": "icon.png"
}
```

- Every field is optional; missing name → repository name, missing author → repository owner.
- `icon` is a path relative to the `.bolt` folder (png, jpg, webp, ico, bmp) and must not point outside it.
- The store re-reads the file when the repository is added and on every update check.
- If the user sets a custom icon for an app, it is stored locally and the store stops reading the `.bolt` folder
  from then on (switch back with the *Ikon visszaállítása* / *Reset icon* menu item).

## Development

```sh
dotnet build
dotnet test
dotnet run --project src/Bolt.App
```

- `src/Bolt.Core` – data model (EF Core + SQLite), forge clients, package selection, installers, `StoreService`.
- `src/Bolt.App` – Avalonia UI (MVVM, CommunityToolkit.Mvvm).
- `tests/Bolt.Core.Tests` – xUnit tests.

New migration: `dotnet ef migrations add <Name> --project src/Bolt.Core --output-dir Data/Migrations`.
