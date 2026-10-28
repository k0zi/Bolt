# Bolt

<p align="center">
  <img src="docs/assets/bolt-header.png" alt="Bolt — szerverek és áramló adatok" width="900" />
</p>

[English](README.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Español](README.es.md) | [Português](README.pt.md) | [中文](README.zh.md) | [日本語](README.ja.md) | **Magyar**

Asztali szoftver store (Avalonia 12 + [KD.Avalonia.Rice](https://www.nuget.org/packages/KD.Avalonia.Rice)).
Nincs saját katalógusa: GitHub, GitLab (gitlab.com vagy saját szerver) és Codeberg (Forgejo/Gitea) repókat lehet
hozzáadni, a Bolt a kiadásaik (release) közül választja ki a rendszerhez való csomagot, és a **host OS telepítőjével**
telepíti:

| Rendszer | Csomag | Telepítő |
|----------|--------|----------|
| Debian / Ubuntu és származékaik | `.deb` | `pkexec apt-get` |
| Fedora / RHEL | `.rpm` | `pkexec dnf` |
| openSUSE / SLES | `.rpm` | `pkexec zypper` |
| Windows | `.msi`, `.exe` | `msiexec` / a telepítő maga (UAC) |

## Működés

- **Repó hozzáadása**: a store csak akkor veszi fel a repót, ha van kiadása, és abban a rendszerhez (formátum +
  architektúra) való csomag. Különben üzenetben jelzi, miért nem.
- **Főképernyő**: felül a telepített, alul a telepíthető alkalmazások, csoportonként 20 / oldal lapozással.
- **Frissítés**: telepített alkalmazásonként vagy egyszerre mindre lehet frissítést keresni és telepíteni.
- **Állapot**: indításkor a Bolt ellenőrzi, hogy a telepített csomagok még megvannak-e (kívülről eltávolítva →
  visszakerül a telepíthetők közé).
- **API tokenek**: hostonként opcionális token (rate limit, privát repók) a beállításokban.
- **Nyelv**: a felület angolul (alapértelmezett), németül, franciául, spanyolul, portugálul, kínaiul, japánul és
  magyarul érhető el; a beállításokban váltható, azonnal érvényes.
- **Adatok**: SQLite, `~/.local/share/bolt/bolt.db` (Windows: `%LOCALAPPDATA%\bolt\bolt.db`), ikonok mellette az
  `icons` mappában.

## `.bolt` metaadatok

A repó a `.bolt/bolt.json` fájlban adhatja meg, hogyan jelenjen meg a store-ban:

```json
{
  "name": "My App",
  "version": "1.2.3",
  "author": "Valaki",
  "icon": "icon.png"
}
```

- Minden mező opcionális; hiányzó név → repó neve, hiányzó szerző → a repó tulajdonosa.
- Az `icon` a `.bolt` mappához képest relatív útvonal (png, jpg, webp, ico, bmp), nem mutathat a mappán kívülre.
- A store a hozzáadáskor és minden frissítéskereséskor újraolvassa.
- Ha a felhasználó saját ikont állít be egy alkalmazáshoz, az lokálisan mentődik, és onnantól a `.bolt` mappát nem
  olvassa a store (az *Ikon visszaállítása (.bolt)* menüponttal kapcsolható vissza).

## Fejlesztés

```sh
dotnet build
dotnet test
dotnet run --project src/Bolt.App
```

- `src/Bolt.Core` – adatmodell (EF Core + SQLite), forge kliensek, csomagválasztás, telepítők, `StoreService`.
- `src/Bolt.App` – Avalonia UI (MVVM, CommunityToolkit.Mvvm).
- `src/Bolt.*/Resources/Strings*.resx` – a felület és az üzenetek szövegei, nyelvenként egy fájl (`Strings.resx` az
  angol).
- `tests/Bolt.Core.Tests` – xUnit tesztek.

Új migráció: `dotnet ef migrations add <Név> --project src/Bolt.Core --output-dir Data/Migrations`.
