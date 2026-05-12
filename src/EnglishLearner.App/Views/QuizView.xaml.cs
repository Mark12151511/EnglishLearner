using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using EnglishLearner.App.ViewModels;

namespace EnglishLearner.App.Views;

public partial class QuizView : UserControl
{
    private QuizViewModel? _vm;

    public QuizView()
    {
        InitializeComponent();
        HookViewModel(DataContext as QuizViewModel);
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is QuizViewModel oldVm) UnhookViewModel(oldVm);
            HookViewModel(e.NewValue as QuizViewModel);
        };
        Loaded += (_, _) => RefreshOptions();
    }

    private void HookViewModel(QuizViewModel? vm)
    {
        if (vm is null) return;
        _vm = vm;
        vm.PropertyChanged += OnPropertyChanged;
        vm.AnswerSubmitted += OnAnswerSubmitted;
    }

    private void UnhookViewModel(QuizViewModel vm)
    {
        vm.PropertyChanged -= OnPropertyChanged;
        vm.AnswerSubmitted -= OnAnswerSubmitted;
        if (_vm == vm) _vm = null;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                if (e.PropertyName is nameof(QuizViewModel.CurrentQuestion)
                    or nameof(QuizViewModel.SelectedOptionIndex)
                    or nameof(QuizViewModel.IsAnswered))
                {
                    RefreshOptions();
                }
                else if (e.PropertyName == nameof(QuizViewModel.Result) && _vm?.Result is not null)
                {
                    AccuracyText.Text = $"{_vm.Result.Accuracy:P0}";
                    AccuracyText.Foreground = _vm.Result.Accuracy < 0.5
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    CorrectCountRun.Text = _vm.Result.CorrectCount.ToString();
                    WrongCountRun.Text = _vm.Result.WrongCount.ToString();
                }
            }
            catch { }
        });
    }

    private void OnAnswerSubmitted(bool isCorrect)
    {
        Dispatcher.Invoke(() =>
        {
            if (isCorrect)
                PlayCorrectFlash();
            else
                PlayWrongShake();
        });
    }

    private void PlayCorrectFlash()
    {
        try
        {
            var green = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C9"));

            var flash = new ColorAnimation
            {
                From = (Color)ColorConverter.ConvertFromString("#C8E6C9"),
                To = Colors.White,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            if (QuestionCard.Background is SolidColorBrush oldBrush)
                oldBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            QuestionCard.Background = green;
            green.BeginAnimation(SolidColorBrush.ColorProperty, flash);
        }
        catch { }
    }

    private void PlayWrongShake()
    {
        try
        {
            var red = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCDD2"));

            var flash = new ColorAnimation
            {
                From = (Color)ColorConverter.ConvertFromString("#FFCDD2"),
                To = Colors.White,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            var shake = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(0.2)
            };
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-6, KeyTime.FromPercent(0.0)));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(6, KeyTime.FromPercent(0.25)));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-4, KeyTime.FromPercent(0.5)));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(4, KeyTime.FromPercent(0.75)));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1.0)));

            if (QuestionCard.Background is SolidColorBrush oldBrush)
                oldBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            QuestionCard.Background = red;
            red.BeginAnimation(SolidColorBrush.ColorProperty, flash);
            ShakeTransform.BeginAnimation(TranslateTransform.XProperty, shake);
        }
        catch { }
    }

    private void DifficultyFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_vm is null || DifficultyCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string tag) return;

        _vm.DifficultyFilter = tag.Split(',').Select(int.Parse).ToArray();
        _vm.RestartQuizCommand.Execute(null);
    }

    private void RefreshOptions()
    {
        if (_vm is null || _vm.CurrentQuestion is null) return;

        var options = _vm.CurrentQuestion.Options;
        var correctIdx = _vm.CurrentQuestion.CorrectIndex;
        var selectedIdx = _vm.SelectedOptionIndex;
        var answered = _vm.IsAnswered;

        var items = new List<OptionItem>();
        var labels = new[] { "A", "B", "C", "D" };

        for (int i = 0; i < options.Count; i++)
        {
            var isSelected = selectedIdx == i;
            var isCorrect = i == correctIdx;

            Brush bg, border;

            if (answered)
            {
                if (isCorrect)
                {
                    bg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                    border = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                }
                else if (isSelected && !isCorrect)
                {
                    bg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                    border = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                }
                else
                {
                    bg = Brushes.White;
                    border = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                }
            }
            else
            {
                if (isSelected)
                {
                    bg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD"));
                    border = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                }
                else
                {
                    bg = Brushes.White;
                    border = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                }
            }

            items.Add(new OptionItem
            {
                Index = i,
                Label = $"{labels[i]}. {options[i]}",
                BgColor = bg,
                BorderColor = border
            });
        }

        OptionsPanel.ItemsSource = items;
    }
}

public class OptionItem
{
    public int Index { get; set; }
    public string Label { get; set; } = string.Empty;
    public Brush BgColor { get; set; } = Brushes.White;
    public Brush BorderColor { get; set; } = Brushes.Transparent;
}
