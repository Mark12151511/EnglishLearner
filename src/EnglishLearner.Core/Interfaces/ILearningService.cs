using EnglishLearner.Core.Models;

namespace EnglishLearner.Core.Interfaces;

public interface ILearningService
{
    Task<int> GetCurrentGroupIndexAsync();
    Task SetCurrentGroupIndexAsync(int index);
    Task<int> GetTotalWordCountAsync();
    Task<IReadOnlyList<Word>> GetWordGroupAsync(int groupIndex, int groupSize = 10);
    Task MarkGroupAsLearnedAsync(IReadOnlyList<Word> words);

    Task<IReadOnlyList<Word>> GetLearnedWordsAsync(int[]? difficultyFilter = null);
    Task<IReadOnlyList<Word>> GetMasteredWordsAsync();
    Task<IReadOnlyList<WordProgress>> GetAllWordProgressAsync();
    Task RecordQuizAnswerAsync(int wordId, bool isCorrect);
}
