using KD.Avalonia.Rice.Controls;

namespace Bolt.App.Views;

public partial class ConfirmWindow : RiceWindow
{
    public ConfirmWindow() : this("", "", "OK")
    {
    }

    public ConfirmWindow(string title, string message, string confirmText)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;
        ConfirmButton.Click += (_, _) => Close(true);
        CancelButton.Click += (_, _) => Close(false);
    }
}
