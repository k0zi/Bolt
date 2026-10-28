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
- **Language**: the UI is available in English (default), German, French, Spanish, Portuguese, Chinese, Japanese
  and Hungarian; switch it in the settings, it applies immediately.
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
  from then on (switch back with the *Reset icon (.bolt)* menu item).

## Development

```sh
dotnet build
dotnet test
dotnet run --project src/Bolt.App
```

- `src/Bolt.Core` – data model (EF Core + SQLite), forge clients, package selection, installers, `StoreService`.
- `src/Bolt.App` – Avalonia UI (MVVM, CommunityToolkit.Mvvm).
- `src/Bolt.*/Resources/Strings*.resx` – UI and message texts, one file per language (`Strings.resx` is English).
- `tests/Bolt.Core.Tests` – xUnit tests.

New migration: `dotnet ef migrations add <Name> --project src/Bolt.Core --output-dir Data/Migrations`.

## Linux packages

```sh
packaging/install.sh               # build the package for this distro and install it (apt / dnf / zypper)
packaging/install.sh --uninstall   # remove it again
packaging/build-packages.sh        # build both .deb and .rpm into artifacts/packages/
packaging/build-packages.sh deb --skip-tests
```

- The packages are self-contained (no .NET runtime needed) and install to `/usr/lib/bolt`, with the launcher at
  `/usr/bin/bolt`, a menu entry and an icon. Only `linux-x64` (amd64 / x86_64) is built.
- They are built with [nfpm](https://nfpm.goreleaser.com/), which `build-packages.sh` downloads into
  `artifacts/tools/` if it is not installed.
- **Versioning** ([Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)): `version.json` holds
  `<year>.<month>` (e.g. `26.10`) and the number of commits since that line changed becomes the patch number
  (`26.10.0`, `26.10.1`, ...). In a new month `build-packages.sh` updates `version.json` to the current month; commit
  that change. Builds outside `main` and `v*` tags get a `~g<commit>` suffix so they sort before the release.
  `dotnet tool restore && dotnet nbgv get-version` shows the current version.

## Releases

Every push to `main` and every pull request builds, tests and uploads the `.deb` and `.rpm` packages as a workflow
artifact ([`build_n_publish.yml`](.github/workflows/build_n_publish.yml)). A GitHub release `v<version>` with the
packages attached is created by any of:

- **Tag push**: `dotnet nbgv tag` (tags HEAD as `v<its version>`), then `git push origin v<version>`.
- **github.com**: *Releases → Draft a new release*, new tag `v<version>` on `main`. The workflow only attaches the
  packages to that existing release.
- **Manual run**: *Actions → Build and publish → Run workflow* with `publish` ticked; it tags the built commit
  `v<version>` and creates the release.

The tag must match the version computed for that commit, so create it with `dotnet nbgv tag` rather than picking a
number by hand. CI never bumps `version.json`; it only warns when the month is out of date.

## License

Bolt is licensed under the [MIT license](LICENSE). The packages also redistribute third-party components (.NET
runtime, Avalonia, SkiaSharp/HarfBuzz, SQLite, the Inter font, Material Design Icons, …) under their own licenses;
see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and the `licenses/` directory. The packages install these to
`/usr/share/doc/bolt/` (deb) and `/usr/share/licenses/bolt/` (rpm).

GitHub, GitLab, Codeberg, Forgejo, Gitea and the operating system names used for the themes are trademarks of their
respective owners; Bolt is not affiliated with or endorsed by them.
