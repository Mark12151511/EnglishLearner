using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using EnglishLearner.App.ViewModels;
using EnglishLearner.Core.Models;

namespace EnglishLearner.App.Views;

public partial class SpeakingView : UserControl
{
    private SpeakingViewModel? _vm;

    public SpeakingView()
    {
        InitializeComponent();
        HookViewModel(DataContext as SpeakingViewModel);
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is SpeakingViewModel oldVm) UnhookViewModel(oldVm);
            HookViewModel(e.NewValue as SpeakingViewModel);
        };
    }

    private void HookViewModel(SpeakingViewModel? vm)
    {
        if (vm is null) return;
        _vm = vm;
        vm.AnalysisCompleted += OnAnalysisCompleted;
    }

    private void UnhookViewModel(SpeakingViewModel vm)
    {
        vm.AnalysisCompleted -= OnAnalysisCompleted;
        if (_vm == vm) _vm = null;
    }

    private void OnAnalysisCompleted(PronunciationResult result)
    {
        Dispatcher.Invoke(() =>
        {
            CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            WordCard.BeginAnimation(Border.BorderBrushProperty, null);

            CardScale.ScaleX = 1;
            CardScale.ScaleY = 1;
            WordCard.BorderBrush = Brushes.Transparent;
            WordCard.BorderThickness = new Thickness(0);

            if (result.Score >= 0.9)
                PlayScaleAnimation(1.0, 1.06, 1.0, 0.4);
            else if (result.Score >= 0.7)
                PlayBorderFlash(Colors.DodgerBlue, 0.3);
            else
                PlayScaleAnimation(1.0, 0.97, 1.0, 0.25);
        });
    }

    private void PlayScaleAnimation(double from, double peak, double to, double seconds)
    {
        var anim = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromSeconds(seconds)
        };
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(from, KeyTime.FromPercent(0)));
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(peak, KeyTime.FromPercent(0.5)));
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(to, KeyTime.FromPercent(1)));

        CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    private void PlayBorderFlash(Color flashColor, double seconds)
    {
        WordCard.BorderThickness = new Thickness(3);

        var brush = new SolidColorBrush(Colors.Transparent);
        WordCard.BorderBrush = brush;

        var anim = new ColorAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromSeconds(seconds)
        };
        anim.KeyFrames.Add(new LinearColorKeyFrame(Colors.Transparent, KeyTime.FromPercent(0)));
        anim.KeyFrames.Add(new LinearColorKeyFrame(flashColor, KeyTime.FromPercent(0.33)));
        anim.KeyFrames.Add(new LinearColorKeyFrame(Colors.Transparent, KeyTime.FromPercent(0.66)));
        anim.KeyFrames.Add(new LinearColorKeyFrame(flashColor, KeyTime.FromPercent(1.0)));

        brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
    }
}
