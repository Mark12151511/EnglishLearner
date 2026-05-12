using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EnglishLearner.App.ViewModels;

public partial class FlashCardViewModel : ObservableObject
{
    private readonly ISm2Service _sm2Service;
    private readonly ISpeechService _speechService;
    private readonly List<Word> _dueWords = [];

    public event Action<bool>? RateCompleted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWord))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private Word? _currentWord;

    [ObservableProperty]
    private bool _isShowingBack;

    [ObservableProperty]
    private int _reviewedToday;

    [ObservableProperty]
    private int _remainingCount;

    public bool HasWord => CurrentWord is not null;
    public bool IsEmpty => CurrentWord is null;

    public FlashCardViewModel()
    {
        var sp = ((App)Application.Current).ServiceProvider;
        _sm2Service = sp.GetRequiredService<ISm2Service>();
        _speechService = sp.GetRequiredService<ISpeechService>();

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            var words = await _sm2Service.GetDueWordsAsync();
            _dueWords.Clear();
            _dueWords.AddRange(words);

            if (_dueWords.Count == 0)
            {
                var repo = ((App)Application.Current).ServiceProvider
                    .GetRequiredService<IRepository<Word>>();
                var all = await repo.GetAllAsync();
                var filtered = all
                    .Where(w => w.DifficultyLevel >= 2 && w.DifficultyLevel <= 3)
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(50);
                _dueWords.AddRange(filtered);
            }

            RemainingCount = _dueWords.Count;
            MoveToNext();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize flash cards");
        }
    }

    public void SetFlipped(bool flipped)
    {
        IsShowingBack = flipped;
    }

    [RelayCommand]
    private void Speak()
    {
        if (CurrentWord is null) return;
        _speechService.Stop();
        _speechService.Speak(CurrentWord.Text);
    }

    [RelayCommand]
    private async Task Rate(string qualityStr)
    {
        if (CurrentWord is null) return;
        if (!int.TryParse(qualityStr, out var quality)) return;

        try
        {
            await _sm2Service.ReviewAsync(CurrentWord.Id, quality);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to record review for word {WordId}", CurrentWord.Id);
        }

        ReviewedToday++;

        if (quality == 0)
        {
            RateCompleted?.Invoke(true);
            return;
        }

        RemainingCount = Math.Max(0, RemainingCount - 1);
        MoveToNext();
        RateCompleted?.Invoke(false);
    }

    private void MoveToNext()
    {
        if (_dueWords.Count > 0)
        {
            CurrentWord = _dueWords[0];
            _dueWords.RemoveAt(0);
            if (CurrentWord is not null)
                _speechService.Preload(CurrentWord.Text);
        }
        else
        {
            CurrentWord = null;
        }
    }
}
