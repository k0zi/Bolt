using Bolt.App.ViewModels;

namespace Bolt.App.Services;

public interface IDialogService
{
    /// <summary>Asks for an image file; <c>null</c> when cancelled.</summary>
    Task<string?> PickImageAsync();

    Task<bool> ConfirmAsync(string title, string message, string confirmText);

    /// <summary>Shows the "add repository" dialog; returns <c>true</c> when an app was added.</summary>
    Task<bool> ShowAddRepositoryAsync(AddRepositoryViewModel viewModel);

    /// <summary>Opens a web page in the default browser.</summary>
    Task OpenUrlAsync(string url);
}
