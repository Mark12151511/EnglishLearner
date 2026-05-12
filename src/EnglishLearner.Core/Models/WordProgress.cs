namespace EnglishLearner.Core.Models;

public class WordProgress
{
    public int WordId { get; init; }
    public string Text { get; init; } = string.Empty;
    public string Meaning { get; init; } = string.Empty;
    public string Phonetic { get; init; } = string.Empty;
    public string? Example { get; init; }
    public int DifficultyLevel { get; init; }
    public bool IsLearned { get; init; }
    public int QuizCorrectCount { get; init; }
    public bool IsMastered => QuizCorrectCount >= 10;
}
