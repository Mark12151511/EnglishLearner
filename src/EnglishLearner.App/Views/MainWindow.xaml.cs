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
        NavigateToHome();
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

    private void NavHome_Click(object sender, RoutedEventArgs e) => NavigateToHome();
    private void NavLearning_Click(object sender, RoutedEventArgs e) => NavigateToLearning();
    private void NavQuiz_Click(object sender, RoutedEventArgs e) => NavigateToQuiz();
    private void NavMastered_Click(object sender, RoutedEventArgs e) => NavigateToMastered();
    private void NavDictation_Click(object sender, RoutedEventArgs e) => NavigateToDictation();
    private void NavSpeaking_Click(object sender, RoutedEventArgs e) => NavigateToSpeaking();

    private void NavigateToHome()
    {
        ContentArea.Children.Clear();
        var home = new HomeView();
        home.ModuleSelected += OnModuleSelected;
        ContentArea.Children.Add(home);
        NavBar.Visibility = Visibility.Collapsed;
    }

    private void OnModuleSelected(string module)
    {
        switch (module)
        {
            case "WordLearning":
                NavigateToLearning();
                break;
            case "ListeningSpeaking":
                NavigateToDictation();
                break;
        }
    }

    // ── 单词学习模块 ──────────────────────────────────────────────

    private void NavigateToLearning()
    {
        ShowModuleNav(LearningNav);
        ContentArea.Children.Clear();
        ContentArea.Children.Add(new WordLearningView());
        SetActiveNav(NavLearning);
    }

    private void NavigateToQuiz()
    {
        ShowModuleNav(LearningNav);
        ContentArea.Children.Clear();
        ContentArea.Children.Add(new QuizView());
        SetActiveNav(NavQuiz);
    }

    private void NavigateToMastered()
    {
        ShowModuleNav(LearningNav);
        ContentArea.Children.Clear();
        ContentArea.Children.Add(new MasteredWordsView());
        SetActiveNav(NavMastered);
    }

    // ── 听力口语模块 ──────────────────────────────────────────────

    private void NavigateToDictation()
    {
        ShowModuleNav(ListeningNav);
        ContentArea.Children.Clear();
        ContentArea.Children.Add(new DictationView());
        SetActiveNav(NavDictation);
    }

    private void NavigateToSpeaking()
    {
        ShowModuleNav(ListeningNav);
        ContentArea.Children.Clear();
        ContentArea.Children.Add(new SpeakingView());
        SetActiveNav(NavSpeaking);
    }

    // ── 导航栏切换 ────────────────────────────────────────────────

    private void ShowModuleNav(StackPanel moduleNav)
    {
        NavBar.Visibility = Visibility.Visible;
        LearningNav.Visibility = moduleNav == LearningNav ? Visibility.Visible : Visibility.Collapsed;
        ListeningNav.Visibility = moduleNav == ListeningNav ? Visibility.Visible : Visibility.Collapsed;

        NavHome.Background = InactiveBg;
        NavHome.Foreground = InactiveFg;
    }

    private void SetActiveNav(Button active)
    {
        foreach (var btn in new[] { NavLearning, NavQuiz, NavMastered, NavDictation, NavSpeaking })
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
