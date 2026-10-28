# Bolt

<p align="center">
  <img src="docs/assets/bolt-header.png" alt="Bolt — 服务器与流动的数据" width="900" />
</p>

[English](README.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Español](README.es.md) | [Português](README.pt.md) | **中文** | [日本語](README.ja.md) | [Magyar](README.hu.md)

一款桌面软件商店（Avalonia 12 + [KD.Avalonia.Rice](https://www.nuget.org/packages/KD.Avalonia.Rice)）。
它没有自己的软件目录：你添加 GitHub、GitLab（gitlab.com 或自托管）和 Codeberg（Forgejo/Gitea）仓库，
Bolt 从这些仓库的发布版本（release）中挑选适合当前系统的软件包，并使用**宿主操作系统的安装程序**进行安装：

| 系统 | 软件包 | 安装程序 |
|------|--------|----------|
| Debian / Ubuntu 及其衍生版 | `.deb` | `pkexec apt-get` |
| Fedora / RHEL | `.rpm` | `pkexec dnf` |
| openSUSE / SLES | `.rpm` | `pkexec zypper` |
| Windows | `.msi`、`.exe` | `msiexec` / 安装程序本身（UAC） |

## 工作方式

- **添加仓库**：只有当仓库的某个发布版本中包含适用于当前系统（格式 + 架构）的软件包时，商店才会接受它；
  否则会说明原因。
- **主界面**：上方为已安装的应用，下方为可安装的应用，每组每页 20 个。
- **更新**：可以针对单个已安装的应用，或一次性针对所有应用检查并安装更新。
- **状态**：启动时 Bolt 会检查已安装的软件包是否仍然存在（在外部被卸载 → 应用回到可安装列表）。
- **API 令牌**：可在设置中为每个主机配置可选的令牌（速率限制、私有仓库）。
- **语言**：界面支持英语（默认）、德语、法语、西班牙语、葡萄牙语、中文、日语和匈牙利语，可在设置中切换，
  立即生效。
- **数据**：SQLite，`~/.local/share/bolt/bolt.db`（Windows：`%LOCALAPPDATA%\bolt\bolt.db`），图标位于同目录的
  `icons` 文件夹中。

## `.bolt` 元数据

仓库可以在 `.bolt/bolt.json` 文件中定义它在商店中的显示方式：

```json
{
  "name": "My App",
  "version": "1.2.3",
  "author": "某人",
  "icon": "icon.png"
}
```

- 所有字段均为可选；缺少名称 → 使用仓库名称，缺少作者 → 使用仓库所有者。
- `icon` 是相对于 `.bolt` 文件夹的路径（png、jpg、webp、ico、bmp），不能指向该文件夹之外。
- 商店会在添加仓库时以及每次检查更新时重新读取该文件。
- 如果用户为某个应用设置了自定义图标，该图标会保存在本地，此后商店不再读取其 `.bolt` 文件夹
  （可通过菜单项 *重置图标（.bolt）* 恢复）。

## 开发

```sh
dotnet build
dotnet test
dotnet run --project src/Bolt.App
```

- `src/Bolt.Core` – 数据模型（EF Core + SQLite）、forge 客户端、软件包选择、安装程序、`StoreService`。
- `src/Bolt.App` – Avalonia 界面（MVVM，CommunityToolkit.Mvvm）。
- `src/Bolt.*/Resources/Strings*.resx` – 界面和消息文本，每种语言一个文件（`Strings.resx` 为英语）。
- `tests/Bolt.Core.Tests` – xUnit 测试。

新建迁移：`dotnet ef migrations add <名称> --project src/Bolt.Core --output-dir Data/Migrations`。
