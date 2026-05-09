using EnglishLearner.Core.Models;

namespace EnglishLearner.Core.Interfaces;

public interface IAiService
{
    Task<WritingCorrection> CorrectWritingAsync(string essay);
    Task<WordExplanation> ExplainWordAsync(string word, string context);
    Task<Quiz> GenerateQuizAsync(string article);
}
