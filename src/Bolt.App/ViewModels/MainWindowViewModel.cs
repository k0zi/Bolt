using CommunityToolkit.Mvvm.ComponentModel;

namespace Bolt.App.ViewModels;

public sealed partial class MainWindowViewModel(StoreViewModel store, SettingsViewModel settings) : ObservableObject
{
    public StoreViewModel Store { get; } = store;

    public SettingsViewModel Settings { get; } = settings;

    [ObservableProperty]
    private ObservableObject? _currentPage = store;

    public void ShowStore() => CurrentPage = Store;

    public void ShowSettings()
    {
        CurrentPage = Settings;
        _ = Settings.LoadAsync();
    }

    public Task InitializeAsync() => Store.InitializeAsync();
}
