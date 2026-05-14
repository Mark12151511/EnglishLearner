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
    private bool _isInitializing = true;

    public record DifficultyOption(string Label, int[] Levels);

    public static readonly IReadOnlyList<DifficultyOption> AllDifficulties =
    [
        new("雅思基础 B1+B2", [2, 3]),
        new("雅思进阶 C1", [4]),
        new("托福核心 B2+C1", [3, 4]),
        new("全部词库 A1-C1", [1, 2, 3, 4]),
    ];

    public IReadOnlyList<DifficultyOption> DifficultyList => AllDifficulties;

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
    [NotifyPropertyChangedFor(nameof(LearnedDisplayText))]
    private int _learnedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LearnedDisplayText))]
    private int _filteredTotal;

    [ObservableProperty]
    private int _selectedDifficultyIndex;

    public bool HasWord => CurrentWord is not null;
    public bool IsEmpty => CurrentWord is null;
    public string ProgressText => $"{CurrentIndexInGroup + 1} / {_currentGroupWords.Count}";
    public string GroupProgressText => $"第 {CurrentGroupIndex + 1} 组 / 共 {TotalGroups} 组";
    public string LearnedDisplayText => $"已学 {LearnedCount} / {FilteredTotal}";

    public int[] CurrentDifficultyFilter => AllDifficulties[SelectedDifficultyIndex].Levels;

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
            var savedFilter = await _learningService.GetDifficultyFilterAsync();
            var idx = AllDifficulties.ToList().FindIndex(d => d.Levels.SequenceEqual(savedFilter));
            if (idx < 0) idx = 0;
            SelectedDifficultyIndex = idx;

            await LoadByDifficultyAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize word learning");
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private async Task LoadByDifficultyAsync()
    {
        var filter = CurrentDifficultyFilter;
        var totalWords = await _learningService.GetTotalWordCountAsync(filter);
        TotalGroups = (int)Math.Ceiling(totalWords / 10.0);
        FilteredTotal = totalWords;

        CurrentGroupIndex = await _learningService.GetCurrentGroupIndexAsync(filter);
        if (CurrentGroupIndex >= TotalGroups)
            CurrentGroupIndex = Math.Max(0, TotalGroups - 1);

        LearnedCount = CurrentGroupIndex * 10;
        await LoadGroupAsync();
    }

    private async Task LoadGroupAsync()
    {
        _currentGroupWords = (await _learningService.GetWordGroupAsync(CurrentGroupIndex, CurrentDifficultyFilter)).ToList();
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

    partial void OnSelectedDifficultyIndexChanged(int value)
    {
        if (_isInitializing) return;
        if (value < 0 || value >= AllDifficulties.Count) return;
        _ = ChangeDifficultyAsync();
    }

    private async Task ChangeDifficultyAsync()
    {
        try
        {
            var filter = CurrentDifficultyFilter;
            await _learningService.SetDifficultyFilterAsync(filter);
            await LoadByDifficultyAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to change difficulty filter");
        }
    }
}
