using Bolt.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bolt.App.ViewModels;

public sealed partial class AddRepositoryViewModel(StoreService store) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private string _url = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string? _message;

    [ObservableProperty]
    private bool _isError;

    /// <summary>At least one repository was added while the dialog was open.</summary>
    public bool AddedAny { get; private set; }

    public bool HasMessage => !string.IsNullOrEmpty(Message);

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        IsBusy = true;
        Message = null;
        try
        {
            var result = await store.AddRepositoryAsync(Url);
            Message = result.Message;
            IsError = result.Status != AddRepositoryStatus.Added;
            if (!IsError)
            {
                AddedAny = true;
                Url = "";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAdd() => !IsBusy && !string.IsNullOrWhiteSpace(Url);
}
