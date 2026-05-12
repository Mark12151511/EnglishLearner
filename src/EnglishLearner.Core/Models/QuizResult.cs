namespace EnglishLearner.Core.Models;

public class QuizResult
{
    public int TotalCount { get; init; }
    public int CorrectCount { get; init; }
    public int WrongCount => TotalCount - CorrectCount;
    public double Accuracy => TotalCount == 0 ? 0 : (double)CorrectCount / TotalCount;
    public IReadOnlyList<QuizQuestion> WrongQuestions { get; init; } = [];
}
