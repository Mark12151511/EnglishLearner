using EnglishLearner.Core.Models;

namespace EnglishLearner.Core.Interfaces;

public interface ISentenceRepository
{
    Task<IReadOnlyList<Sentence>> GetByDifficultyAsync(int level, int count = 20);
    Task<IReadOnlyList<Sentence>> GetMixedAsync(int countPerLevel = 10);
}
