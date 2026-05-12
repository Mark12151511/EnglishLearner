using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EnglishLearner.App.ViewModels;

public partial class WordLearningViewModel : ObservableObject
{
    private readonly ILearningService _learningService;
    private readonly ISpeechService _speechService;
    private List<Word> _currentGroupWords = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWord))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private Word? _currentWord;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    [NotifyPropertyChangedFor(nameof(GroupProgressText))]
    private int _currentIndexInGroup;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GroupProgressText))]
    private int _currentGroupIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GroupProgressText))]
    private int _totalGroups;

    [ObservableProperty]
    private bool _isGroupComplete;

    [ObservableProperty]
    private int _learnedCount;

    public bool HasWord => CurrentWord is not null;
    public bool IsEmpty => CurrentWord is null;
    public string ProgressText => $"{CurrentIndexInGroup + 1} / {_currentGroupWords.Count}";
    public string GroupProgressText => $"第 {CurrentGroupIndex + 1} 组 / 共 {TotalGroups} 组";

    public WordLearningViewModel()
    {
        var sp = ((App)Application.Current).ServiceProvider;
        _learningService = sp.GetRequiredService<ILearningService>();
        _speechService = sp.GetRequiredService<ISpeechService>();
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            var totalWords = await _learningService.GetTotalWordCountAsync();
            TotalGroups = (int)Math.Ceiling(totalWords / 10.0);

            CurrentGroupIndex = await _learningService.GetCurrentGroupIndexAsync();
            if (CurrentGroupIndex >= TotalGroups)
                CurrentGroupIndex = Math.Max(0, TotalGroups - 1);

            LearnedCount = CurrentGroupIndex * 10;
            await LoadGroupAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize word learning");
        }
    }

    private async Task LoadGroupAsync()
    {
        _currentGroupWords = (await _learningService.GetWordGroupAsync(CurrentGroupIndex)).ToList();
        CurrentIndexInGroup = 0;
        IsGroupComplete = false;

        if (_currentGroupWords.Count > 0)
        {
            CurrentWord = _currentGroupWords[0];
            _speechService.Preload(CurrentWord.Text);
        }
        else
        {
            CurrentWord = null;
        }
    }

    [RelayCommand]
    private void Speak()
    {
        if (CurrentWord is null) return;
        _speechService.Stop();
        _speechService.Speak(CurrentWord.Text);
    }

    [RelayCommand]
    private void NextWord()
    {
        var nextIndex = CurrentIndexInGroup + 1;

        if (nextIndex >= _currentGroupWords.Count)
        {
            IsGroupComplete = true;
            return;
        }

        CurrentIndexInGroup = nextIndex;
        CurrentWord = _currentGroupWords[nextIndex];
        _speechService.Preload(CurrentWord.Text);
    }

    [RelayCommand]
    private async Task ContinueToNextGroup()
    {
        try
        {
            await _learningService.MarkGroupAsLearnedAsync(_currentGroupWords);
            LearnedCount += _currentGroupWords.Count;

            CurrentGroupIndex++;
            if (CurrentGroupIndex >= TotalGroups)
            {
                CurrentWord = null;
                IsGroupComplete = true;
                return;
            }

            await LoadGroupAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to continue to next group");
        }
    }
}
