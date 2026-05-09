using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using EnglishLearner.App.ViewModels;

namespace EnglishLearner.App.Views;

public partial class FlashCardView : UserControl
{
    private FlashCardViewModel? _vm;

    public FlashCardView()
    {
        InitializeComponent();
        HookViewModel(DataContext as FlashCardViewModel);
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is FlashCardViewModel oldVm) UnhookViewModel(oldVm);
            HookViewModel(e.NewValue as FlashCardViewModel);
        };
    }

    private void HookViewModel(FlashCardViewModel? vm)
    {
        if (vm is null) return;
        _vm = vm;
        vm.PropertyChanged += OnPropertyChanged;
        vm.RateCompleted += OnRateCompleted;
    }

    private void UnhookViewModel(FlashCardViewModel vm)
    {
        vm.PropertyChanged -= OnPropertyChanged;
        vm.RateCompleted -= OnRateCompleted;
        if (_vm == vm) _vm = null;
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (e.PropertyName is nameof(FlashCardViewModel.HasWord) or nameof(FlashCardViewModel.IsEmpty))
                UpdateCardVisibility();
        });
    }

    private void OnRateCompleted(bool isAgain)
    {
        Dispatcher.Invoke(() =>
        {
            // Clear animations so local ScaleX values take effect
            CardFront.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            CardBack.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);

            if (isAgain)
            {
                AnimateBackToFront();
            }
            else
            {
                _vm?.SetFlipped(false);
                CardFront.Visibility = Visibility.Visible;
                CardBack.Visibility = Visibility.Collapsed;
                FrontScale.ScaleX = 1;
                BackScale.ScaleX = 0;
                RatingPanel.Visibility = Visibility.Collapsed;
            }
        });
    }

    private void UpdateCardVisibility()
    {
        if (_vm is null) return;

        if (_vm.HasWord)
        {
            CardFront.Visibility = Visibility.Visible;
            CardBack.Visibility = Visibility.Collapsed;
            CardEmpty.Visibility = Visibility.Collapsed;
            RatingPanel.Visibility = Visibility.Collapsed;
        }
        else if (_vm.IsEmpty)
        {
            CardFront.Visibility = Visibility.Collapsed;
            CardBack.Visibility = Visibility.Collapsed;
            CardEmpty.Visibility = Visibility.Visible;
            RatingPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void CardFront_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        AnimateFrontToBack();
    }

    private void AnimateFrontToBack()
    {
        var collapse = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.12));
        var expand = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.12))
        {
            BeginTime = TimeSpan.FromSeconds(0.12)
        };

        collapse.Completed += (_, _) =>
        {
            CardFront.Visibility = Visibility.Collapsed;
            CardBack.Visibility = Visibility.Visible;
            BackScale.ScaleX = 0;
            CardBack.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, expand);
        };

        expand.Completed += (_, _) =>
        {
            _vm?.SetFlipped(true);
            RatingPanel.Visibility = Visibility.Visible;
        };

        FrontScale.ScaleX = 1;
        CardFront.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, collapse);
    }

    private void AnimateBackToFront()
    {
        RatingPanel.Visibility = Visibility.Collapsed;

        var collapse = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.12));
        var expand = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.12))
        {
            BeginTime = TimeSpan.FromSeconds(0.12)
        };

        collapse.Completed += (_, _) =>
        {
            CardBack.Visibility = Visibility.Collapsed;
            CardFront.Visibility = Visibility.Visible;
            FrontScale.ScaleX = 0;
            CardFront.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, expand);
        };

        expand.Completed += (_, _) =>
        {
            _vm?.SetFlipped(false);
        };

        BackScale.ScaleX = 1;
        CardBack.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, collapse);
    }
}
