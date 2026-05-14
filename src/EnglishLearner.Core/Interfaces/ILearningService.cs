using EnglishLearner.Core.Models;

namespace EnglishLearner.Core.Interfaces;

public interface ILearningService
{
    Task<int> GetCurrentGroupIndexAsync(int[]? difficultyFilter = null);
    Task SetCurrentGroupIndexAsync(int index, int[]? difficultyFilter = null);
    Task<int> GetTotalWordCountAsync(int[]? difficultyFilter = null);
    Task<IReadOnlyList<Word>> GetWordGroupAsync(int groupIndex, int[]? difficultyFilter = null, int groupSize = 10);
    Task MarkGroupAsLearnedAsync(IReadOnlyList<Word> words);

    Task<IReadOnlyList<Word>> GetLearnedWordsAsync(int[]? difficultyFilter = null);
    Task<IReadOnlyList<Word>> GetMasteredWordsAsync(int[]? difficultyFilter = null);
    Task<IReadOnlyList<WordProgress>> GetAllWordProgressAsync(int[]? difficultyFilter = null);
    Task RecordQuizAnswerAsync(int wordId, bool isCorrect);

    Task<int[]> GetDifficultyFilterAsync();
    Task SetDifficultyFilterAsync(int[] filter);
}
