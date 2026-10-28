using Bolt.App.Resources;
using Bolt.App.Services;
using Bolt.Core.Model;
using Bolt.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bolt.App.ViewModels;

/// <summary>The main screen: installed applications on top, installable ones below, 20 per page.</summary>
public sealed partial class StoreViewModel(StoreService store, IDialogService dialogs)
    : ObservableObject
{
    private readonly List<AppItemViewModel> _items = [];

    public PagedList<AppItemViewModel> Installed { get; } = new();

    public PagedList<AppItemViewModel> Available { get; } = new();

    public string PlatformText => store.Platform.ToString();

    public bool IsPlatformSupported => store.Platform.IsSupported;

    public string UnsupportedText => string.Format(Strings.UnsupportedSystem, PlatformText);

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckAllUpdatesCommand), nameof(RefreshCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpdates), nameof(UpdateAllText))]
    private int _updateCount;

    public bool HasUpdates => UpdateCount > 0;

    public string UpdateAllText => string.Format(Strings.UpdateAll, UpdateCount);

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public bool IsEmpty => _items.Count == 0;

    public async Task InitializeAsync()
    {
        await LoadAsync();
        IsLoading = true;
        try
        {
            // Programs removed outside the store move back to the installable group.
            await store.SyncInstalledStateAsync();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            ShowStatus(string.Format(Strings.InstalledStateCheckFailed, ex.Message), true);
        }
        finally
        {
            IsLoading = false;
        }
        await LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => Regroup();

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            await store.SyncInstalledStateAsync();
            await LoadAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRun() => !IsLoading;

    [RelayCommand]
    private async Task AddRepositoryAsync()
    {
        var dialog = new AddRepositoryViewModel(store);
        if (await dialogs.ShowAddRepositoryAsync(dialog))
            await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CheckAllUpdatesAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var results = await store.CheckForUpdatesAsync();
            foreach (var result in results)
                _items.FirstOrDefault(i => i.Id == result.App.Id)?.Refresh(result.App);
            Regroup();

            var errors = results.Where(r => r.Error is not null).ToList();
            var updates = results.Count(r => r.UpdateAvailable);
            var text = results.Count == 0 ? Strings.NoInstalledApps
                : updates == 0 ? Strings.AllUpToDate
                : string.Format(Strings.UpdatesAvailableCount, updates);
            if (errors.Count > 0)
                text += " " + string.Format(Strings.ReposUnreachable, errors.Count,
                                            string.Join("; ", errors.Select(e => $"{e.App.Name}: {e.Error}")));
            ShowStatus(text, errors.Count > 0);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UpdateAllAsync()
    {
        foreach (var item in _items.Where(i => i.UpdateAvailable).ToList())
        {
            if (item.UpdateCommand.CanExecute(null))
                await item.UpdateCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    private void DismissStatus() => StatusMessage = null;

    /// <summary>Rebuilds the texts made in code after the UI language changed.</summary>
    public void RefreshTexts()
    {
        OnPropertyChanged(nameof(PlatformText));
        OnPropertyChanged(nameof(UnsupportedText));
        OnPropertyChanged(nameof(UpdateAllText));
        Installed.RefreshTexts();
        Available.RefreshTexts();
        foreach (var item in _items)
            item.RefreshTexts();
    }

    private async Task LoadAsync()
    {
        var apps = await store.GetAppsAsync();
        _items.Clear();
        _items.AddRange(apps.Select(CreateItem));
        OnPropertyChanged(nameof(IsEmpty));
        Regroup();
    }

    private AppItemViewModel CreateItem(StoreApp app) =>
        new(app, store, dialogs, _ => Regroup(), item =>
        {
            _items.Remove(item);
            OnPropertyChanged(nameof(IsEmpty));
            Regroup();
        });

    private void Regroup()
    {
        var filter = SearchText.Trim();
        var visible = filter.Length == 0 ? _items : _items.Where(i => i.Matches(filter)).ToList();

        Installed.SetSource(visible.Where(i => i.IsInstalled)
                                   .OrderByDescending(i => i.UpdateAvailable)
                                   .ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
                                   .ToList());
        Available.SetSource(visible.Where(i => !i.IsInstalled)
                                   .OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
                                   .ToList());
        UpdateCount = _items.Count(i => i.UpdateAvailable);
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}
