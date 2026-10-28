# Bolt

[English](README.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | **Español** | [Português](README.pt.md) | [中文](README.zh.md) | [日本語](README.ja.md) | [Magyar](README.hu.md)

Una tienda de software de escritorio (Avalonia 12 + [KD.Avalonia.Rice](https://www.nuget.org/packages/KD.Avalonia.Rice)).
No tiene catálogo propio: se añaden repositorios de GitHub, GitLab (gitlab.com o autoalojado) y Codeberg
(Forgejo/Gitea), Bolt elige entre sus versiones (releases) el paquete adecuado para el sistema y lo instala con el
**instalador del sistema operativo anfitrión**:

| Sistema | Paquete | Instalador |
|---------|---------|------------|
| Debian / Ubuntu y derivados | `.deb` | `pkexec apt-get` |
| Fedora / RHEL | `.rpm` | `pkexec dnf` |
| openSUSE / SLES | `.rpm` | `pkexec zypper` |
| Windows | `.msi`, `.exe` | `msiexec` / el propio instalador (UAC) |

## Funcionamiento

- **Añadir un repositorio**: la tienda solo acepta un repositorio si tiene una versión con un paquete para el sistema
  actual (formato + arquitectura). En caso contrario, indica el motivo.
- **Pantalla principal**: arriba las aplicaciones instaladas, abajo las instalables, 20 por página en cada grupo.
- **Actualizaciones**: buscar e instalar actualizaciones por aplicación instalada o para todas a la vez.
- **Estado**: al iniciarse, Bolt comprueba si los paquetes instalados siguen presentes (eliminado externamente → la
  aplicación vuelve a la lista de instalables).
- **Tokens de API**: token opcional por host (límites de peticiones, repositorios privados) en la configuración.
- **Datos**: SQLite, `~/.local/share/bolt/bolt.db` (Windows: `%LOCALAPPDATA%\bolt\bolt.db`), iconos al lado en la
  carpeta `icons`.

## Metadatos `.bolt`

Un repositorio puede indicar en el archivo `.bolt/bolt.json` cómo debe aparecer en la tienda:

```json
{
  "name": "My App",
  "version": "1.2.3",
  "author": "Alguien",
  "icon": "icon.png"
}
```

- Todos los campos son opcionales; sin nombre → nombre del repositorio, sin autor → propietario del repositorio.
- `icon` es una ruta relativa a la carpeta `.bolt` (png, jpg, webp, ico, bmp) y no puede apuntar fuera de ella.
- La tienda vuelve a leer el archivo al añadir el repositorio y en cada búsqueda de actualizaciones.
- Si el usuario establece un icono propio para una aplicación, se guarda localmente y a partir de entonces la tienda
  deja de leer la carpeta `.bolt` (se revierte con la opción de menú *Ikon visszaállítása* / *Restablecer icono*).

## Desarrollo

```sh
dotnet build
dotnet test
dotnet run --project src/Bolt.App
```

- `src/Bolt.Core` – modelo de datos (EF Core + SQLite), clientes de forjas, selección de paquetes, instaladores,
  `StoreService`.
- `src/Bolt.App` – interfaz Avalonia (MVVM, CommunityToolkit.Mvvm).
- `tests/Bolt.Core.Tests` – pruebas xUnit.

Nueva migración: `dotnet ef migrations add <Nombre> --project src/Bolt.Core --output-dir Data/Migrations`.
