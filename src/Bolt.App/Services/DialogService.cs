using Avalonia.Platform.Storage;
using Bolt.App.Resources;
using Bolt.App.ViewModels;
using Bolt.App.Views;
using KD.Avalonia.Rice.Controls;

namespace Bolt.App.Services;

public sealed class DialogService(RiceWindow owner) : IDialogService
{
    public async Task<string?> PickImageAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.ChooseIcon,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(Strings.Images) { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.ico", "*.bmp"] },
            ],
        });
        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public Task<bool> ConfirmAsync(string title, string message, string confirmText) =>
        new ConfirmWindow(title, message, confirmText).ShowDialog<bool>(owner);

    public async Task<bool> ShowAddRepositoryAsync(AddRepositoryViewModel viewModel)
    {
        var window = new AddRepositoryWindow { DataContext = viewModel };
        await window.ShowDialog(owner);
        return viewModel.AddedAny;
    }

    public Task OpenUrlAsync(string url) => owner.Launcher.LaunchUriAsync(new Uri(url));
}
