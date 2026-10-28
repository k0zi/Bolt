using System.Collections.ObjectModel;
using Bolt.App.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bolt.App.ViewModels;

/// <summary>Shows a list page by page.</summary>
public sealed partial class PagedList<T> : ObservableObject
{
    public const int DefaultPageSize = 20;

    private IReadOnlyList<T> _source = [];

    public PagedList(int pageSize = DefaultPageSize) => PageSize = pageSize;

    public int PageSize { get; }

    /// <summary>The items of the current page.</summary>
    public ObservableCollection<T> Items { get; } = [];

    /// <summary>Zero based.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageText))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand), nameof(NextPageCommand))]
    private int _pageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageText), nameof(HasPages))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand), nameof(NextPageCommand))]
    private int _pageCount = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private int _totalCount;

    public bool HasPages => PageCount > 1;

    public bool IsEmpty => TotalCount == 0;

    public string PageText => string.Format(Strings.PageText, PageIndex + 1, PageCount);

    /// <summary>Replaces the items; the current page is kept when it still exists.</summary>
    public void SetSource(IReadOnlyList<T> source)
    {
        _source = source;
        TotalCount = source.Count;
        PageCount = Math.Max(1, (source.Count + PageSize - 1) / PageSize);
        PageIndex = Math.Clamp(PageIndex, 0, PageCount - 1);
        Fill();
    }

    /// <summary>Rebuilds <see cref="PageText"/> after the UI language changed.</summary>
    public void RefreshTexts() => OnPropertyChanged(nameof(PageText));

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void PreviousPage()
    {
        PageIndex--;
        Fill();
    }

    private bool CanGoPrevious() => PageIndex > 0;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextPage()
    {
        PageIndex++;
        Fill();
    }

    private bool CanGoNext() => PageIndex < PageCount - 1;

    private void Fill()
    {
        Items.Clear();
        foreach (var item in _source.Skip(PageIndex * PageSize).Take(PageSize))
            Items.Add(item);
    }
}
