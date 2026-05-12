using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EnglishLearner.App.ViewModels;

public partial class QuizViewModel : ObservableObject
{
    private readonly IQuizService _quizService;
    private readonly ILearningService _learningService;
    private readonly ISpeechService _speechService;
    private Quiz? _quiz;
    private readonly List<int> _userAnswers = [];

    public event Action<bool>? AnswerSubmitted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private QuizQuestion? _currentQuestion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private int _currentIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitAnswerCommand))]
    private int? _selectedOptionIndex;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitAnswerCommand))]
    private bool _isAnswered;

    [ObservableProperty]
    private bool _isCorrect;

    [ObservableProperty]
    private bool _isQuizFinished;

    [ObservableProperty]
    private QuizResult? _result;

    public int[] DifficultyFilter { get; set; } = [2, 3];

    public string Progress => $"{CurrentIndex + 1} / {TotalCount}";
    public bool CanSubmit => SelectedOptionIndex.HasValue && !IsAnswered;

    public QuizViewModel()
    {
        var sp = ((App)Application.Current).ServiceProvider;
        _quizService = sp.GetRequiredService<IQuizService>();
        _learningService = sp.GetRequiredService<ILearningService>();
        _speechService = sp.GetRequiredService<ISpeechService>();
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            _userAnswers.Clear();
            IsQuizFinished = false;
            Result = null;
            IsAnswered = false;
            IsCorrect = false;
            SelectedOptionIndex = null;
            CurrentIndex = 0;

            _quiz = await _quizService.GenerateQuizAsync(count: 10, difficultyFilter: DifficultyFilter);
            TotalCount = _quiz.Questions.Count;
            CurrentQuestion = _quiz.Questions[0];

            if (CurrentQuestion is not null)
                _speechService.Preload(CurrentQuestion.WordText);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize quiz");
        }
    }

    [RelayCommand]
    private void Speak()
    {
        if (CurrentQuestion is null) return;
        _speechService.Stop();
        _speechService.Speak(CurrentQuestion.WordText);
    }

    [RelayCommand]
    private void SelectOption(int index)
    {
        SelectedOptionIndex = index;
        SubmitAnswerCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void SubmitAnswer()
    {
        if (SelectedOptionIndex is not int selected || CurrentQuestion is null) return;

        IsAnswered = true;
        IsCorrect = selected == CurrentQuestion.CorrectIndex;
        _userAnswers.Add(selected);

        AnswerSubmitted?.Invoke(IsCorrect);
    }

    [RelayCommand]
    private async Task NextQuestion()
    {
        if (_quiz is null) return;

        try
        {
            var nextIndex = CurrentIndex + 1;

            if (nextIndex >= TotalCount)
            {
                Result = await _quizService.SubmitQuizAsync(_userAnswers, _quiz);
                IsQuizFinished = true;

                for (int i = 0; i < _quiz.Questions.Count; i++)
                {
                    bool correct = i < _userAnswers.Count && _userAnswers[i] == _quiz.Questions[i].CorrectIndex;
                    await _learningService.RecordQuizAnswerAsync(_quiz.Questions[i].WordId, correct);
                }

                return;
            }

            CurrentIndex = nextIndex;
            CurrentQuestion = _quiz.Questions[nextIndex];
            SelectedOptionIndex = null;
            IsAnswered = false;
            IsCorrect = false;
            SubmitAnswerCommand.NotifyCanExecuteChanged();

            if (CurrentQuestion is not null)
                _speechService.Preload(CurrentQuestion.WordText);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to navigate to next question");
        }
    }

    [RelayCommand]
    private void RestartQuiz()
    {
        InitializeAsync();
    }
}
