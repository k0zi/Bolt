using System.Collections.ObjectModel;
using Bolt.App.Localization;
using Bolt.App.Resources;
using Bolt.Core;
using Bolt.Core.Model;
using Bolt.Core.Platform;
using Bolt.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bolt.App.ViewModels;

public sealed partial class SettingsViewModel(CredentialService credentials, PlatformInfo platform, AppPaths paths)
    : ObservableObject
{
    public string PlatformText => platform.ToString();

    public bool IsPlatformSupported => platform.IsSupported;

    public string DataDirectory => paths.DataDirectory;

    public IReadOnlyList<Language> Languages => Localizer.Languages;

    public Language SelectedLanguage
    {
        get => Localizer.Instance.Language;
        set
        {
            if (value is not null)
                Localizer.Instance.SetLanguage(value);
        }
    }

    public ObservableCollection<TokenItem> Tokens { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTokenCommand))]
    private string _newHost = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTokenCommand))]
    private string _newToken = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string? _message;

    public bool HasMessage => !string.IsNullOrEmpty(Message);

    public async Task LoadAsync()
    {
        var all = await credentials.GetAllAsync();
        Tokens.Clear();
        foreach (var credential in all)
            Tokens.Add(new TokenItem(credential, RemoveTokenCommand));
    }

    /// <summary>Rebuilds the texts made in code after the UI language changed.</summary>
    public void RefreshTexts() => OnPropertyChanged(nameof(PlatformText));

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveTokenAsync()
    {
        try
        {
            await credentials.SetAsync(NewHost, NewToken);
            Message = string.Format(Strings.TokenSaved, CredentialService.NormalizeHost(NewHost));
            NewHost = "";
            NewToken = "";
            await LoadAsync();
        }
        catch (ArgumentException ex)
        {
            Message = ex.Message;
        }
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(NewHost) && !string.IsNullOrWhiteSpace(NewToken);

    [RelayCommand]
    private async Task RemoveTokenAsync(TokenItem item)
    {
        await credentials.RemoveAsync(item.Id);
        Tokens.Remove(item);
    }

    public sealed class TokenItem(HostCredential credential, IAsyncRelayCommand<TokenItem> remove)
    {
        public int Id => credential.Id;

        public string Host => credential.Host;

        /// <summary>Only the end of the token is shown.</summary>
        public string MaskedToken => credential.Token.Length <= 4 ? "••••" : $"••••{credential.Token[^4..]}";

        public IAsyncRelayCommand<TokenItem> RemoveCommand => remove;
    }
}
