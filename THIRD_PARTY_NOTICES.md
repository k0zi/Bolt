# Third-party notices

Bolt's own source code is licensed under the MIT license in LICENSE.
The Linux packages (.deb, .rpm) are self-contained builds and also redistribute the .NET runtime and the
libraries below. These components keep their own licenses, and the full license texts are in
`licenses/`. Listing them here does not relicense them or imply endorsement.

## Project assets

The application icon (`src/Bolt.App/Assets/bolt.png`, `bolt.ico`) and the README header
(`docs/assets/bolt-header.png`) were generated for this project with an AI image generation tool, using no
reference image. To the extent the project contributors hold rights in them, they are available under
Bolt's MIT license. This is not a claim of exclusive copyright in purely machine-generated material.

## Redistributed libraries (self-contained linux-x64 build)

| Component | Source | License / local copy |
| --- | --- | --- |
| .NET runtime 10.0.13 and Microsoft.Extensions.*, Microsoft.EntityFrameworkCore.*, Microsoft.Data.Sqlite.Core 10.0.14 | https://github.com/dotnet/runtime, https://github.com/dotnet/dotnet | MIT: licenses/dotnet-runtime-MIT.txt; upstream component notices: licenses/dotnet-runtime-THIRD-PARTY-NOTICES.txt |
| Avalonia 12.1.4 (Avalonia, Avalonia.Desktop, Avalonia.FreeDesktop, Avalonia.FreeDesktop.AtSpi, Avalonia.HarfBuzz, Avalonia.Native, Avalonia.Remote.Protocol, Avalonia.Skia, Avalonia.Win32, Avalonia.X11, Avalonia.Fonts.Inter; Avalonia.Themes.Fluent 12.1.3) | https://github.com/AvaloniaUI/Avalonia | MIT: licenses/Avalonia-MIT.txt; upstream component notices: licenses/Avalonia-NOTICE.txt |
| Inter font, embedded in Avalonia.Fonts.Inter | https://github.com/rsms/inter | OFL-1.1: licenses/Inter-OFL-1.1.txt (the OFL applies to the font, separately from Avalonia's MIT wrapper) |
| KD.Avalonia.Rice 26.10.19 | https://github.com/k0zi/KD.Avalonia.Rice | MIT: licenses/KD.Avalonia.Rice-MIT.txt |
| Theme palettes in KD.Avalonia.Rice: Nord (Sven Greb), Solarized (Ethan Schoonover), Dracula, Gruvbox (morhetz), Catppuccin | https://github.com/nordtheme/nord, https://github.com/altercation/solarized, https://github.com/dracula/dracula-theme, https://github.com/morhetz/gruvbox, https://github.com/catppuccin/palette | MIT: licenses/Nord-MIT.txt, licenses/Solarized-MIT.txt, licenses/Dracula-MIT.txt, licenses/Gruvbox-MIT.txt, licenses/Catppuccin-MIT.txt |
| Material.Icons and Material.Icons.Avalonia 3.0.2 | https://github.com/SKProCH/Material.Icons, https://github.com/AvaloniaUtils/Material.Icons.Avalonia | Wrapper code: MIT, licenses/Material.Icons-MIT.txt |
| Material Design Icons (icon paths in Material.Icons) — Pictogrammers | https://pictogrammers.com/library/mdi/ | Pictogrammers Free License / Apache-2.0: licenses/Pictogrammers-LICENSE.txt, licenses/Apache-2.0.txt |
| CommunityToolkit.Mvvm 8.4.2 | https://github.com/CommunityToolkit/dotnet | MIT: licenses/CommunityToolkit-MIT.txt; upstream component notices: licenses/CommunityToolkit-THIRD-PARTY-NOTICES.txt |
| SkiaSharp 3.119.4 and HarfBuzzSharp 8.3.1.3 with their Linux native assets (Skia, HarfBuzz, FreeType, libpng, …) | https://github.com/mono/SkiaSharp | MIT: licenses/SkiaSharp-MIT.txt; upstream component notices: licenses/SkiaSharp-THIRD-PARTY-NOTICES.txt |
| SQLitePCLRaw 2.1.13 (bundle_e_sqlite3, core, lib.e_sqlite3, provider.e_sqlite3) | https://github.com/ericsink/SQLitePCL.raw | Apache-2.0: licenses/Apache-2.0.txt; upstream component notices: licenses/SQLitePCL.raw-NOTICE.txt |
| SQLite, compiled into `libe_sqlite3.so` | https://sqlite.org/copyright.html | Public domain (see licenses/SQLitePCL.raw-NOTICE.txt) |
| Tmds.DBus.Protocol 0.94.1 | https://github.com/tmds/Tmds.DBus | MIT: licenses/Tmds.DBus-MIT.txt |
| MicroCom.Runtime 0.11.6 | https://github.com/kekekeks/MicroCom | MIT: licenses/MicroCom-MIT.txt |

## Trademarks

Bolt names GitHub, GitLab, Codeberg, Forgejo and Gitea as release sources, and its themes are named after the
operating systems whose look they follow (Ubuntu, Fedora, openSUSE, Manjaro, Windows 11). These names are
trademarks of their respective owners and identify the service or the palette inspiration only. Bolt is not
affiliated with or endorsed by any of them.

## Not redistributed

The following are used only to build or test Bolt, or are native assets for other operating systems, and are
not included in the Linux packages:
Nerdbank.GitVersioning, Microsoft.EntityFrameworkCore.Design (with Humanizer.Core, Mono.TextTemplating,
Microsoft.CodeAnalysis.*, Microsoft.Build.Framework, Microsoft.VisualStudio.SolutionPersistence, System.CodeDom,
System.Composition.*, Newtonsoft.Json), Microsoft.EntityFrameworkCore.Analyzers, Avalonia.BuildServices,
xUnit (xunit, xunit.*), Microsoft.NET.Test.Sdk, Microsoft.TestPlatform.*, Microsoft.CodeCoverage, coverlet.collector,
and the Windows/macOS/WebAssembly native assets (Avalonia.Angle.Windows.Natives, SkiaSharp.NativeAssets.Win32/macOS/WebAssembly,
HarfBuzzSharp.NativeAssets.Win32/macOS/WebAssembly).

## Maintenance

When you add copied code or assets, or change the dependencies, record the source, version,
author and license here, and update the license copies in `licenses/`. Before a release, check
the publish output (`dotnet publish src/Bolt.App -r linux-x64 --self-contained`) for new native libraries.
