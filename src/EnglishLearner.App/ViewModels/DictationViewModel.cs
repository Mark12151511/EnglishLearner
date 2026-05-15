using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EnglishLearner.App.ViewModels;

public partial class DictationViewModel : ObservableObject
{
    private readonly IPronunciationScoringService _scoring;
    private readonly IRepository<Word> _wordRepo;
    private readonly ISentenceRepository _sentenceRepo;
    private readonly ISpeechService _speechService;
    private readonly List<Word> _wordQueue = [];
    private readonly List<Sentence> _sentences = [];

    [ObservableProperty]
    private Word? _currentWord;

    [ObservableProperty]
    private Sentence? _currentSentence;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _userInput = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private bool _isAnswered;

    [ObservableProperty]
    private DictationResult? _result;

    [ObservableProperty]
    private int _currentIndex;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _statusMessage = "准备开始";

    [ObservableProperty]
    private bool _isSentenceMode;

    [ObservableProperty]
    private int _selectedDifficulty = 2;

    private string CurrentText => IsSentenceMode
        ? CurrentSentence?.Text ?? ""
        : CurrentWord?.Text ?? "";

    public DictationViewModel()
    {
        var sp = ((App)Application.Current).ServiceProvider;
        _scoring = sp.GetRequiredService<IPronunciationScoringService>();
        _wordRepo = sp.GetRequiredService<IRepository<Word>>();
        _sentenceRepo = sp.GetRequiredService<ISentenceRepository>();
        _speechService = sp.GetRequiredService<ISpeechService>();

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await LoadWordsAsync();
            CurrentIndex = 0;
            TotalCount = _wordQueue.Count;
            MoveToNext();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize dictation practice");
            StatusMessage = "加载失败";
        }
    }

    private async Task LoadWordsAsync()
    {
        var all = await _wordRepo.GetAllAsync();
        if (all.Count == 0)
        {
            StatusMessage = "词库为空，请先导入单词";
            return;
        }

        _wordQueue.Clear();
        _wordQueue.AddRange(
            all.Where(w => w.DifficultyLevel >= 2)
                .OrderBy(_ => Random.Shared.Next())
                .Take(30)
                .ToList());
    }

    private void MoveToNext()
    {
        UserInput = "";
        IsAnswered = false;
        Result = null;

        if (IsSentenceMode)
        {
            MoveToNextSentence();
            return;
        }

        if (_wordQueue.Count > 0)
        {
            CurrentWord = _wordQueue[0];
            _wordQueue.RemoveAt(0);
            CurrentIndex++;
        }
        else
        {
            CurrentWord = null;
            return;
        }

        if (CurrentWord is not null)
        {
            _speechService.Rate = -1;
            _speechService.Speak(CurrentWord.Text);
        }
    }

    private void MoveToNextSentence()
    {
        if (_sentences.Count > 0)
        {
            CurrentSentence = _sentences[0];
            _sentences.RemoveAt(0);
            CurrentIndex++;
        }
        else
        {
            CurrentSentence = null;
            return;
        }

        if (CurrentSentence is not null)
        {
            _speechService.Rate = -3;
            _speechService.Speak(CurrentSentence.Text);
        }
    }

    // ── Commands ─────────────────────────────────────────────────

    [RelayCommand]
    private void PlayAudio()
    {
        if (string.IsNullOrEmpty(CurrentText)) return;
        _speechService.Stop();
        _speechService.Rate = IsSentenceMode ? -3 : -1;
        _speechService.Speak(CurrentText);
    }

    [RelayCommand]
    private void PlayExampleAudio()
    {
        if (CurrentWord?.Example is null) return;
        _speechService.Stop();
        _speechService.Rate = -2;
        _speechService.Speak(CurrentWord.Example);
    }

    private bool CanSubmit => !string.IsNullOrWhiteSpace(UserInput) && !IsAnswered;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Submit()
    {
        if (string.IsNullOrEmpty(CurrentText)) return;

        Result = _scoring.ScoreDictation(CurrentText, UserInput);
        IsAnswered = true;
        PlayAudio();
    }

    private bool CanNext => IsAnswered;

    [RelayCommand(CanExecute = nameof(CanNext))]
    private void Next()
    {
        MoveToNext();
    }

    [RelayCommand]
    private void Reveal()
    {
        IsAnswered = true;
    }

    [RelayCommand]
    private void Retry()
    {
        UserInput = "";
        IsAnswered = false;
        Result = null;
    }

    [RelayCommand]
    private async Task SwitchMode(string mode)
    {
        IsSentenceMode = mode == "sentence";
        UserInput = "";
        IsAnswered = false;
        Result = null;
        await ReloadContentAsync();
    }

    [RelayCommand]
    private async Task SetDifficulty(string levelStr)
    {
        if (int.TryParse(levelStr, out var level))
            SelectedDifficulty = level;
        await ReloadContentAsync();
    }

    private async Task ReloadContentAsync()
    {
        try
        {
            if (IsSentenceMode)
            {
                var sentences = await _sentenceRepo
                    .GetByDifficultyAsync(SelectedDifficulty, 20);
                _sentences.Clear();
                _sentences.AddRange(sentences);
                CurrentIndex = 0;
                TotalCount = _sentences.Count;
                MoveToNextSentence();
            }
            else
            {
                await LoadWordsAsync();
                CurrentIndex = 0;
                TotalCount = _wordQueue.Count;
                MoveToNext();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to reload dictation content");
            StatusMessage = "加载失败";
        }
    }
}
