using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EnglishLearner.App.Views;

public partial class MainWindow : Window
{
    private static readonly SolidColorBrush ActiveBg = new((Color)ColorConverter.ConvertFromString("#2196F3"));
    private static readonly SolidColorBrush ActiveFg = Brushes.White;
    private static readonly SolidColorBrush InactiveBg = new((Color)ColorConverter.ConvertFromString("#E0E0E0"));
    private static readonly SolidColorBrush InactiveFg = new((Color)ColorConverter.ConvertFromString("#666"));

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        NavigateToLearning();
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

    private void NavLearning_Click(object sender, RoutedEventArgs e) => NavigateToLearning();
    private void NavQuiz_Click(object sender, RoutedEventArgs e) => NavigateToQuiz();
    private void NavMastered_Click(object sender, RoutedEventArgs e) => NavigateToMastered();

    private void NavigateToLearning()
    {
        ContentArea.Children.Clear();
        ContentArea.Children.Add(new WordLearningView());
        SetActiveNav(NavLearning);
    }

    private void NavigateToQuiz()
    {
        ContentArea.Children.Clear();
        ContentArea.Children.Add(new QuizView());
        SetActiveNav(NavQuiz);
    }

    private void NavigateToMastered()
    {
        ContentArea.Children.Clear();
        ContentArea.Children.Add(new MasteredWordsView());
        SetActiveNav(NavMastered);
    }

    private void SetActiveNav(Button active)
    {
        foreach (var btn in new[] { NavLearning, NavQuiz, NavMastered })
        {
            if (btn == active)
            {
                btn.Background = ActiveBg;
                btn.Foreground = ActiveFg;
            }
            else
            {
                btn.Background = InactiveBg;
                btn.Foreground = InactiveFg;
            }
        }
    }
}
