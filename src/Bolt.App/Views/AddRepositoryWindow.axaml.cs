using KD.Avalonia.Rice.Controls;

namespace Bolt.App.Views;

public partial class AddRepositoryWindow : RiceWindow
{
    public AddRepositoryWindow()
    {
        InitializeComponent();
        CloseButton.Click += (_, _) => Close();
        Opened += (_, _) => UrlBox.Focus();
    }
}
