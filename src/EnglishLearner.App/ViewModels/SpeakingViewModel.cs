using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EnglishLearner.App.ViewModels;

public partial class SpeakingViewModel : ObservableObject
{
    private readonly IAudioRecorderService _recorder;
    private readonly ISpeechRecognitionService _recognition;
    private readonly IPronunciationScoringService _scoring;
    private readonly IRepository<Word> _wordRepo;
    private readonly ISentenceRepository _sentenceRepo;
    private readonly ISpeechService _speechService;
    private readonly List<Word> _wordQueue = [];
    private readonly List<Sentence> _sentences = [];

    public event Action<PronunciationResult>? AnalysisCompleted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScoreText))]
    [NotifyPropertyChangedFor(nameof(ScoreEmoji))]
    [NotifyPropertyChangedFor(nameof(ScoreLevel))]
    [NotifyPropertyChangedFor(nameof(HasWord))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private Word? _currentWord;

    [ObservableProperty]
    private Sentence? _currentSentence;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdleState))]
    private bool _isRecording;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdleState))]
    private bool _isProcessing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScoreText))]
    [NotifyPropertyChangedFor(nameof(ScoreEmoji))]
    [NotifyPropertyChangedFor(nameof(ScoreLevel))]
    [NotifyPropertyChangedFor(nameof(IsIdleState))]
    private bool _hasResult;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScoreText))]
    [NotifyPropertyChangedFor(nameof(ScoreEmoji))]
    [NotifyPropertyChangedFor(nameof(ScoreLevel))]
    private PronunciationResult? _result;

    [ObservableProperty]
    private string _statusMessage = "准备开始";

    [ObservableProperty]
    private bool _isModelReady;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private int _currentIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private int _totalCount;

    [ObservableProperty]
    private bool _isSentenceMode;

    [ObservableProperty]
    private int _selectedDifficulty = 2;

    // ── Computed properties ──────────────────────────────────────

    public bool HasWord => !IsSentenceMode ? CurrentWord is not null : CurrentSentence is not null;
    public bool IsEmpty => !IsSentenceMode ? CurrentWord is null : CurrentSentence is null;
    public bool IsIdleState => !IsRecording && !IsProcessing && !HasResult && HasWord;
    public string Progress => $"{CurrentIndex} / {TotalCount}";

    private string CurrentText => IsSentenceMode
        ? CurrentSentence?.Text ?? ""
        : CurrentWord?.Text ?? "";

    public string ScoreText => Result is null ? "" : $"{Result.Score:P0}";

    public string ScoreEmoji => Result?.Score switch
    {
        >= 0.9 => "🎉 优秀",
        >= 0.7 => "👍 良好",
        >= 0.5 => "💪 继续加油",
        _ => "🔄 再试一次"
    };

    public string ScoreLevel => Result?.Score switch
    {
        >= 0.8 => "High",
        >= 0.5 => "Mid",
        _      => "Low"
    };

    // ── Constructor ──────────────────────────────────────────────

    public SpeakingViewModel()
    {
        var sp = ((App)Application.Current).ServiceProvider;
        _recorder = sp.GetRequiredService<IAudioRecorderService>();
        _recognition = sp.GetRequiredService<ISpeechRecognitionService>();
        _scoring = sp.GetRequiredService<IPronunciationScoringService>();
        _wordRepo = sp.GetRequiredService<IRepository<Word>>();
        _sentenceRepo = sp.GetRequiredService<ISentenceRepository>();
        _speechService = sp.GetRequiredService<ISpeechService>();

        InitializeAsync();
    }

    // ── Initialization ───────────────────────────────────────────

    private async void InitializeAsync()
    {
        try
        {
            IsModelReady = await _recognition.IsModelReadyAsync();
            if (!IsModelReady)
            {
                StatusMessage = "⚠️ 未找到 Whisper 模型\n请下载 ggml-base.en.bin 放到\nAssets/WhisperModels/ 目录";
            }

            await LoadWordsAsync();
            TotalCount = _wordQueue.Count;
            CurrentIndex = 0;
            MoveToNext();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize speaking practice");
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
        HasResult = false;
        Result = null;
        IsRecording = false;
        IsProcessing = false;
        StatusMessage = "点击麦克风开始朗读";

        if (IsSentenceMode)
        {
            if (_sentences.Count > 0)
            {
                CurrentSentence = _sentences[0];
                _sentences.RemoveAt(0);
                CurrentIndex = TotalCount - _sentences.Count;
            }
            else
            {
                CurrentSentence = null;
                StatusMessage = "练习完成！";
            }
            return;
        }

        if (_wordQueue.Count > 0)
        {
            CurrentWord = _wordQueue[0];
            _wordQueue.RemoveAt(0);
            CurrentIndex = TotalCount - _wordQueue.Count;
        }
        else
        {
            CurrentWord = null;
            StatusMessage = "练习完成！";
        }
    }

    // ── Commands ─────────────────────────────────────────────────

    [RelayCommand]
    private void PlayWord()
    {
        if (string.IsNullOrEmpty(CurrentText)) return;
        _speechService.Stop();
        _speechService.Rate = IsSentenceMode ? -2 : -1;
        _speechService.Speak(CurrentText);
    }

    [RelayCommand]
    private void PlayExample()
    {
        if (CurrentWord?.Example is null) return;
        _speechService.Stop();
        _speechService.Speak(CurrentWord.Example);
    }

    private bool CanStartRecording() =>
        !IsRecording && !IsProcessing && IsModelReady && !string.IsNullOrEmpty(CurrentText);

    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private async Task StartRecordingAsync()
    {
        try
        {
            IsRecording = true;
            StatusMessage = IsSentenceMode
                ? "🔴 录音中，请朗读句子..."
                : "🔴 录音中，请朗读单词...";

            var tempPath = Path.Combine(
                Path.GetTempPath(), "EnglishLearner",
                $"rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            await _recorder.StartRecordingAsync(tempPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "录音启动失败");
            IsRecording = false;
            StatusMessage = "❌ 麦克风启动失败，请检查权限";
        }
    }

    private bool CanStopRecording() => IsRecording;

    [RelayCommand(CanExecute = nameof(CanStopRecording))]
    private async Task StopRecordingAsync()
    {
        try
        {
            IsRecording = false;
            IsProcessing = true;
            StatusMessage = "⏳ 正在识别语音...";

            var audioPath = await _recorder.StopRecordingAsync();

            StatusMessage = "🔍 分析发音中...";
            var recognizedText = await _recognition.RecognizeAsync(audioPath);
            Log.Information("Recognized: {Text}", recognizedText);

            Result = _scoring.Score(CurrentText, recognizedText);
            HasResult = true;
            IsProcessing = false;
            StatusMessage = "";

            AnalysisCompleted?.Invoke(Result);

            if (File.Exists(audioPath))
                File.Delete(audioPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "语音识别失败");
            IsProcessing = false;
            IsRecording = false;
            StatusMessage = "❌ 识别失败，请重试";
        }
    }

    partial void OnIsRecordingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsIdleState));
        StartRecordingCommand.NotifyCanExecuteChanged();
        StopRecordingCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsProcessingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsIdleState));
        StartRecordingCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasResultChanged(bool value)
    {
        OnPropertyChanged(nameof(IsIdleState));
    }

    partial void OnCurrentWordChanged(Word? value)
    {
        OnPropertyChanged(nameof(HasWord));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsIdleState));
    }

    partial void OnCurrentSentenceChanged(Sentence? value)
    {
        OnPropertyChanged(nameof(HasWord));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsIdleState));
    }

    [RelayCommand]
    private void NextWord()
    {
        HasResult = false;
        Result = null;
        StatusMessage = "";
        MoveToNext();
    }

    [RelayCommand]
    private void Retry()
    {
        HasResult = false;
        Result = null;
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task SwitchMode(string mode)
    {
        IsSentenceMode = mode == "sentence";
        HasResult = false;
        Result = null;
        IsRecording = false;
        IsProcessing = false;
        StatusMessage = "";
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
            }
            else
            {
                await LoadWordsAsync();
                CurrentIndex = 0;
                TotalCount = _wordQueue.Count;
            }

            MoveToNext();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to reload speaking content");
            StatusMessage = "加载失败";
        }
    }
}
