# Bolt

[English](README.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Español](README.es.md) | [Português](README.pt.md) | [中文](README.zh.md) | **日本語** | [Magyar](README.hu.md)

デスクトップ向けソフトウェアストア（Avalonia 12 + [KD.Avalonia.Rice](https://www.nuget.org/packages/KD.Avalonia.Rice)）。
独自のカタログは持たず、GitHub・GitLab（gitlab.com またはセルフホスト）・Codeberg（Forgejo/Gitea）のリポジトリを
追加すると、Bolt がそのリリースの中からシステムに合ったパッケージを選び、**ホスト OS のインストーラー**で
インストールします：

| システム | パッケージ | インストーラー |
|----------|------------|----------------|
| Debian / Ubuntu および派生ディストリビューション | `.deb` | `pkexec apt-get` |
| Fedora / RHEL | `.rpm` | `pkexec dnf` |
| openSUSE / SLES | `.rpm` | `pkexec zypper` |
| Windows | `.msi`、`.exe` | `msiexec` / インストーラー自身（UAC） |

## 動作

- **リポジトリの追加**：現在のシステム（形式 + アーキテクチャ）に合ったパッケージを含むリリースがある場合にのみ
  ストアに追加されます。そうでない場合は理由が表示されます。
- **メイン画面**：上にインストール済み、下にインストール可能なアプリが表示され、各グループ 20 件ずつページ送りされます。
- **アップデート**：インストール済みアプリごと、またはすべてまとめてアップデートを確認・インストールできます。
- **状態**：起動時に Bolt はインストール済みパッケージがまだ存在するかを確認します（外部で削除された場合 →
  インストール可能な一覧に戻ります）。
- **API トークン**：設定でホストごとに任意のトークン（レート制限、プライベートリポジトリ用）を指定できます。
- **データ**：SQLite、`~/.local/share/bolt/bolt.db`（Windows：`%LOCALAPPDATA%\bolt\bolt.db`）、アイコンは同じ場所の
  `icons` フォルダーに保存されます。

## `.bolt` メタデータ

リポジトリは `.bolt/bolt.json` ファイルで、ストアでの表示方法を指定できます：

```json
{
  "name": "My App",
  "version": "1.2.3",
  "author": "誰か",
  "icon": "icon.png"
}
```

- すべてのフィールドは任意です。名前がない場合 → リポジトリ名、作者がない場合 → リポジトリの所有者。
- `icon` は `.bolt` フォルダーからの相対パス（png、jpg、webp、ico、bmp）で、フォルダーの外を指すことはできません。
- ストアはリポジトリの追加時と、アップデートの確認のたびにファイルを読み直します。
- ユーザーがアプリに独自のアイコンを設定すると、それはローカルに保存され、以降ストアは `.bolt` フォルダーを
  読まなくなります（メニュー項目 *Ikon visszaállítása* / *アイコンをリセット* で元に戻せます）。

## 開発

```sh
dotnet build
dotnet test
dotnet run --project src/Bolt.App
```

- `src/Bolt.Core` – データモデル（EF Core + SQLite）、フォージクライアント、パッケージ選択、インストーラー、`StoreService`。
- `src/Bolt.App` – Avalonia UI（MVVM、CommunityToolkit.Mvvm）。
- `tests/Bolt.Core.Tests` – xUnit テスト。

新しいマイグレーション：`dotnet ef migrations add <名前> --project src/Bolt.Core --output-dir Data/Migrations`。
