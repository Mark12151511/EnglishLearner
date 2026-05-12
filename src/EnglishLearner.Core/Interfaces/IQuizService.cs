using EnglishLearner.Core.Models;

namespace EnglishLearner.Core.Interfaces;

public interface IQuizService
{
    Task<Quiz> GenerateQuizAsync(int count = 20, int[]? difficultyFilter = null);
    Task<QuizResult> SubmitQuizAsync(IReadOnlyList<int> userAnswers, Quiz quiz);
}
