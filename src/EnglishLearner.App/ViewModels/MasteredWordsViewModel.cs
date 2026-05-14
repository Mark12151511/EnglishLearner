using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EnglishLearner.App.ViewModels;

public partial class MasteredWordsViewModel : ObservableObject
{
    private readonly ILearningService _learningService;

    [ObservableProperty]
    private IReadOnlyList<WordProgress> _allProgress = [];

    [ObservableProperty]
    private int _learnedCount;

    [ObservableProperty]
    private int _masteredCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private double _masteryPercent;

    public MasteredWordsViewModel()
    {
        var sp = ((App)Application.Current).ServiceProvider;
        _learningService = sp.GetRequiredService<ILearningService>();
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize mastered words view");
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        try
        {
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh mastered words");
        }
    }

    private async Task RefreshDataAsync()
    {
        var filter = await _learningService.GetDifficultyFilterAsync();
        var progress = await _learningService.GetAllWordProgressAsync(filter);
        AllProgress = progress;
        TotalCount = progress.Count;
        LearnedCount = progress.Count(p => p.IsLearned);
        MasteredCount = progress.Count(p => p.IsMastered);
        MasteryPercent = TotalCount > 0 ? (double)MasteredCount / TotalCount : 0;
    }
}
