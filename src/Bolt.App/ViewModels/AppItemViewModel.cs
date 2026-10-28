using Avalonia.Media.Imaging;
using Bolt.App.Resources;
using Bolt.App.Services;
using Bolt.Core.Forges;
using Bolt.Core.Model;
using Bolt.Core.Platform;
using Bolt.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bolt.App.ViewModels;

/// <summary>One application card.</summary>
public sealed partial class AppItemViewModel : ObservableObject
{
    private readonly StoreService _store;
    private readonly IDialogService _dialogs;
    private readonly Action<AppItemViewModel> _changed;
    private readonly Action<AppItemViewModel> _removed;

    public AppItemViewModel(StoreApp app, StoreService store, IDialogService dialogs,
                            Action<AppItemViewModel> changed, Action<AppItemViewModel> removed)
    {
        _store = store;
        _dialogs = dialogs;
        _changed = changed;
        _removed = removed;
        _app = app;
        _icon = LoadIcon(app.IconPath);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name), nameof(Subtitle), nameof(IsInstalled), nameof(UpdateAvailable),
                              nameof(VersionText), nameof(HasCustomIcon), nameof(RepoUrl))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand), nameof(UpdateCommand), nameof(UninstallCommand),
                                nameof(CheckForUpdateCommand), nameof(RemoveCommand), nameof(ResetIconCommand))]
    private StoreApp _app;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIcon))]
    private Bitmap? _icon;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand), nameof(UpdateCommand), nameof(UninstallCommand),
                                nameof(CheckForUpdateCommand), nameof(RemoveCommand), nameof(SetIconCommand),
                                nameof(ResetIconCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyText;

    /// <summary>0–100 while downloading.</summary>
    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isProgressIndeterminate = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string? _message;

    [ObservableProperty]
    private bool _isError;

    public int Id => App.Id;

    public string Name => App.Name;

    public string Subtitle => App.Author is { } author ? $"{author} · {App.Host}" : App.Host;

    public string RepoUrl => App.RepoUrl;

    public bool HasIcon => Icon is not null;

    public bool HasCustomIcon => App.HasCustomIcon;

    public bool IsInstalled => App.Installation is not null;

    public bool UpdateAvailable => StoreService.IsUpdateAvailable(App);

    public bool HasMessage => !string.IsNullOrEmpty(Message);

    public string VersionText => App.Installation is { } installation
        ? UpdateAvailable
            ? string.Format(Strings.VersionInstalledUpdate, installation.InstalledVersion, App.LatestVersion)
            : string.Format(Strings.VersionInstalled, installation.InstalledVersion)
        : string.Format(Strings.VersionLatest, App.LatestVersion ?? "–");

    /// <summary>Text matched by the search box.</summary>
    public bool Matches(string filter) =>
        Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        (App.Author?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
        App.RepoUrl.Contains(filter, StringComparison.OrdinalIgnoreCase);

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private Task InstallAsync() => RunInstallAsync(string.Format(Strings.Installed, Name));

    private bool CanInstall() => !IsBusy && !IsInstalled;

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private Task UpdateAsync() => RunInstallAsync(string.Format(Strings.Updated, Name));

    private bool CanUpdate() => !IsBusy && UpdateAvailable;

    [RelayCommand(CanExecute = nameof(CanUseInstalled))]
    private async Task UninstallAsync()
    {
        if (!await _dialogs.ConfirmAsync(Strings.Uninstall, string.Format(Strings.UninstallConfirm, Name), Strings.Uninstall))
            return;
        await RunAsync(Strings.Uninstalling, async () =>
        {
            App = await _store.UninstallAsync(Id);
            return string.Format(Strings.Uninstalled, Name);
        });
    }

    private bool CanUseInstalled() => !IsBusy && IsInstalled;

    [RelayCommand(CanExecute = nameof(CanUseInstalled))]
    private Task CheckForUpdateAsync() => RunAsync(Strings.CheckingForUpdates, async () =>
    {
        var result = await _store.CheckForUpdateAsync(Id);
        App = result.App;
        Icon = LoadIcon(App.IconPath);
        if (result.Error is not null)
            throw new ForgeException(result.Error);
        return result.UpdateAvailable
            ? string.Format(Strings.NewVersionAvailable, App.LatestVersion)
            : Strings.NoNewerVersion;
    });

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private async Task SetIconAsync()
    {
        var file = await _dialogs.PickImageAsync();
        if (file is null)
            return;
        await RunAsync(Strings.SavingIcon, async () =>
        {
            await _store.SetCustomIconAsync(Id, file);
            await ReloadAsync();
            return null;
        });
    }

    private bool CanEdit() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanResetIcon))]
    private Task ResetIconAsync() => RunAsync(Strings.ResettingIcon, async () =>
    {
        await _store.ResetIconAsync(Id);
        await ReloadAsync();
        return null;
    });

    private bool CanResetIcon() => !IsBusy && HasCustomIcon;

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private async Task RemoveAsync()
    {
        if (!await _dialogs.ConfirmAsync(Strings.RemoveFromStore, string.Format(Strings.RemoveFromStoreConfirm, Name),
                                         Strings.Remove))
            return;
        await _store.RemoveAppAsync(Id);
        _removed(this);
    }

    private bool CanRemove() => !IsBusy && !IsInstalled;

    [RelayCommand]
    private Task OpenRepositoryAsync() => _dialogs.OpenUrlAsync(RepoUrl);

    /// <summary>Reloads the app after a change made elsewhere (e.g. "check all updates").</summary>
    public void Refresh(StoreApp app)
    {
        App = app;
        Icon = LoadIcon(app.IconPath);
    }

    /// <summary>Rebuilds the texts made in code after the UI language changed.</summary>
    public void RefreshTexts() => OnPropertyChanged(nameof(VersionText));

    private async Task ReloadAsync()
    {
        App = await _store.GetAppAsync(Id);
        Icon = LoadIcon(App.IconPath);
    }

    private Task RunInstallAsync(string done) => RunAsync(Strings.Downloading, async () =>
    {
        var progress = new Progress<OperationProgress>(p =>
        {
            BusyText = p.Stage switch
            {
                OperationStage.Downloading => Strings.Downloading,
                OperationStage.Installing => Strings.Installing,
                _ => Strings.Uninstalling,
            };
            IsProgressIndeterminate = p.Fraction is null;
            Progress = (p.Fraction ?? 0) * 100;
        });
        App = await _store.InstallAsync(Id, progress);
        return done;
    });

    private async Task RunAsync(string busyText, Func<Task<string?>> action)
    {
        IsBusy = true;
        BusyText = busyText;
        IsProgressIndeterminate = true;
        Message = null;
        IsError = false;
        try
        {
            Message = await action();
        }
        catch (Exception ex) when (ex is ForgeException or InstallerException or IOException or HttpRequestException
                                       or InvalidOperationException or UnauthorizedAccessException)
        {
            Message = ex.Message;
            IsError = true;
        }
        finally
        {
            IsBusy = false;
            BusyText = null;
            _changed(this);
        }
    }

    private static Bitmap? LoadIcon(string? path)
    {
        if (path is null || !File.Exists(path))
            return null;
        try
        {
            using var stream = File.OpenRead(path);
            return Bitmap.DecodeToWidth(stream, 128);
        }
        catch (Exception)
        {
            return null; // not an image the platform can decode
        }
    }
}
