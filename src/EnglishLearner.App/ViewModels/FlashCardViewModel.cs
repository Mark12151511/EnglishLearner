using System.Globalization;
using System.Speech.Synthesis;
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
    private readonly List<Word> _dueWords = [];
    private readonly SpeechSynthesizer _synth = new();

    // 评分后通知 View 执行动作
    public event Action<bool>? RateCompleted; // true = 翻回正面(Again), false = 切新词

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
        _sm2Service = ((App)Application.Current).ServiceProvider.GetRequiredService<ISm2Service>();
        InitSpeech();
        InitializeAsync();
    }

    private void InitSpeech()
    {
        var voice = _synth.GetInstalledVoices(new CultureInfo("en-US"))
            .FirstOrDefault(v => v.Enabled);
        if (voice != null)
            _synth.SelectVoice(voice.VoiceInfo.Name);
        _synth.Rate = -1;
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
                _dueWords.AddRange(all.Take(20));
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
        _synth.SpeakAsyncCancelAll();
        _synth.SpeakAsync(CurrentWord.Text);
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

        // Again(quality=0): 翻回正面，同一个词重新看
        if (quality == 0)
        {
            RateCompleted?.Invoke(true);
            return;
        }

        // Hard/Good/Easy: 记录并切下一个词
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
        }
        else
        {
            CurrentWord = null;
        }
    }
}
