using System.Windows;
using EnglishLearner.App.ViewModels;
using EnglishLearner.App.Views;

namespace EnglishLearner.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 在 Loaded 中创建 FlashCardView，此时 DI 容器已就绪
        var flashCard = new FlashCardView();
        RootGrid.Children.Add(flashCard);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }
}
