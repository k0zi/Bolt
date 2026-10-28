using Bolt.App.ViewModels;
using KD.Avalonia.Rice.Controls;

namespace Bolt.App.Views;

public partial class MainWindow : RiceWindow
{
    public MainWindow()
    {
        InitializeComponent();
        SideMenu.SelectionChanged += (_, _) =>
        {
            if (DataContext is not MainWindowViewModel vm)
                return;
            if (SideMenu.SelectedItem == SettingsItem)
                vm.ShowSettings();
            else if (SideMenu.SelectedItem == StoreItem)
                vm.ShowStore();
        };
        Opened += async (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
                await vm.InitializeAsync();
        };
    }
}
