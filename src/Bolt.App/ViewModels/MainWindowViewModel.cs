using Bolt.App.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bolt.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(StoreViewModel store, SettingsViewModel settings)
    {
        Store = store;
        Settings = settings;
        _currentPage = store;
        Localizer.Instance.LanguageChanged += (_, _) =>
        {
            Store.RefreshTexts();
            Settings.RefreshTexts();
        };
    }

    public StoreViewModel Store { get; }

    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private ObservableObject? _currentPage;

    public void ShowStore() => CurrentPage = Store;

    public void ShowSettings()
    {
        CurrentPage = Settings;
        _ = Settings.LoadAsync();
    }

    public Task InitializeAsync() => Store.InitializeAsync();
}
