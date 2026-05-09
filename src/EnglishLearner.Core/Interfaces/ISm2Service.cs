using EnglishLearner.Core.Models;

namespace EnglishLearner.Core.Interfaces;

public interface ISm2Service
{
    Sm2Result Calculate(int repetition, double easinessFactor, int quality);
    Task<Sm2Result> ReviewAsync(int wordId, int quality);
    Task<IReadOnlyList<Word>> GetDueWordsAsync();
}
