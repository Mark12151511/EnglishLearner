using System.Windows;
using System.Windows.Controls;

namespace EnglishLearner.App.Views;

public partial class DictationView : UserControl
{
    public DictationView()
    {
        InitializeComponent();
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Placeholder.Visibility = string.IsNullOrEmpty(InputBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
