namespace EnglishLearner.Core.Models;

public class Quiz
{
    public IReadOnlyList<QuizQuestion> Questions { get; init; } = [];
}

public class QuizQuestion
{
    public int Number { get; init; }
    public string Question { get; init; } = string.Empty;
    public IReadOnlyList<string> Options { get; init; } = [];
    public int CorrectIndex { get; init; }
    public string Explanation { get; init; } = string.Empty;
    public string CorrectAnswer => Options.Count > CorrectIndex ? Options[CorrectIndex] : string.Empty;
    public int WordId { get; init; }
    public string WordText { get; init; } = string.Empty;
}
