using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using EnglishLearner.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearner.Infrastructure.Services;

public class LearningService : ILearningService
{
    private readonly AppDbContext _db;

    public LearningService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetCurrentGroupIndexAsync(int[]? difficultyFilter = null)
    {
        var key = GetGroupIndexKey(difficultyFilter);
        var setting = await _db.Set<UserSetting>()
            .FirstOrDefaultAsync(s => s.Key == key);
        return setting is not null && int.TryParse(setting.Value, out var idx) ? idx : 0;
    }

    public async Task SetCurrentGroupIndexAsync(int index, int[]? difficultyFilter = null)
    {
        var key = GetGroupIndexKey(difficultyFilter);
        var setting = await _db.Set<UserSetting>()
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting is null)
        {
            _db.Add(new UserSetting { Key = key, Value = index.ToString() });
        }
        else
        {
            setting.Value = index.ToString();
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<int> GetTotalWordCountAsync(int[]? difficultyFilter = null)
    {
        if (difficultyFilter is not null && difficultyFilter.Length > 0)
            return await _db.Words.CountAsync(w => difficultyFilter.Contains(w.DifficultyLevel));
        return await _db.Words.CountAsync();
    }

    public async Task<IReadOnlyList<Word>> GetWordGroupAsync(int groupIndex, int[]? difficultyFilter = null, int groupSize = 10)
    {
        var query = _db.Words.AsQueryable();
        if (difficultyFilter is not null && difficultyFilter.Length > 0)
            query = query.Where(w => difficultyFilter.Contains(w.DifficultyLevel));

        return await query
            .OrderBy(w => w.Id)
            .Skip(groupIndex * groupSize)
            .Take(groupSize)
            .ToListAsync();
    }

    public async Task MarkGroupAsLearnedAsync(IReadOnlyList<Word> words)
    {
        foreach (var word in words)
        {
            _db.Add(new LearningRecord
            {
                WordId = word.Id,
                ActivityType = LearningActivityType.WordLearning,
                IsCorrect = true,
                PracticedAt = DateTime.UtcNow
            });
        }

        var filter = await GetDifficultyFilterAsync();
        var currentIdx = await GetCurrentGroupIndexAsync(filter);
        await SetCurrentGroupIndexAsync(currentIdx + 1, filter);
    }

    public async Task<IReadOnlyList<Word>> GetLearnedWordsAsync(int[]? difficultyFilter = null)
    {
        var learnedWordIds = await _db.Set<LearningRecord>()
            .Where(r => r.ActivityType == LearningActivityType.WordLearning)
            .Select(r => r.WordId)
            .Distinct()
            .ToListAsync();

        var masteredWordIds = await GetMasteredWordIdsAsync();

        var query = _db.Words.Where(w => learnedWordIds.Contains(w.Id) && !masteredWordIds.Contains(w.Id));

        if (difficultyFilter is not null && difficultyFilter.Length > 0)
            query = query.Where(w => difficultyFilter.Contains(w.DifficultyLevel));

        return await query.ToListAsync();
    }

    public async Task<IReadOnlyList<Word>> GetMasteredWordsAsync(int[]? difficultyFilter = null)
    {
        var masteredIds = await GetMasteredWordIdsAsync();
        var query = _db.Words.Where(w => masteredIds.Contains(w.Id));
        if (difficultyFilter is not null && difficultyFilter.Length > 0)
            query = query.Where(w => difficultyFilter.Contains(w.DifficultyLevel));
        return await query.ToListAsync();
    }

    public async Task<IReadOnlyList<WordProgress>> GetAllWordProgressAsync(int[]? difficultyFilter = null)
    {
        var query = _db.Words.AsQueryable();
        if (difficultyFilter is not null && difficultyFilter.Length > 0)
            query = query.Where(w => difficultyFilter.Contains(w.DifficultyLevel));

        var words = await query.OrderBy(w => w.Id).ToListAsync();
        var records = await _db.Set<LearningRecord>().ToListAsync();

        var learnedSet = records
            .Where(r => r.ActivityType == LearningActivityType.WordLearning)
            .Select(r => r.WordId)
            .ToHashSet();

        var quizCorrectCounts = records
            .Where(r => r.ActivityType == LearningActivityType.Quiz && r.IsCorrect)
            .GroupBy(r => r.WordId)
            .ToDictionary(g => g.Key, g => g.Count());

        return words.Select(w => new WordProgress
        {
            WordId = w.Id,
            Text = w.Text,
            Meaning = w.Meaning,
            Phonetic = w.Phonetic,
            Example = w.Example,
            DifficultyLevel = w.DifficultyLevel,
            IsLearned = learnedSet.Contains(w.Id),
            QuizCorrectCount = quizCorrectCounts.GetValueOrDefault(w.Id, 0)
        }).ToList();
    }

    public async Task RecordQuizAnswerAsync(int wordId, bool isCorrect)
    {
        _db.Add(new LearningRecord
        {
            WordId = wordId,
            ActivityType = LearningActivityType.Quiz,
            IsCorrect = isCorrect,
            PracticedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<int[]> GetDifficultyFilterAsync()
    {
        var setting = await _db.Set<UserSetting>()
            .FirstOrDefaultAsync(s => s.Key == "DifficultyFilter");
        if (setting is not null)
        {
            try
            {
                return setting.Value.Split(',').Select(int.Parse).ToArray();
            }
            catch { }
        }
        return [2, 3]; // default: B1+B2 (雅思基础)
    }

    public async Task SetDifficultyFilterAsync(int[] filter)
    {
        var setting = await _db.Set<UserSetting>()
            .FirstOrDefaultAsync(s => s.Key == "DifficultyFilter");

        var value = string.Join(",", filter);

        if (setting is null)
        {
            _db.Add(new UserSetting { Key = "DifficultyFilter", Value = value });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    private static string GetGroupIndexKey(int[]? difficultyFilter)
    {
        if (difficultyFilter is null || difficultyFilter.Length == 0)
            return "CurrentLearningGroup";
        return $"CurrentLearningGroup_{string.Join("_", difficultyFilter.OrderBy(x => x))}";
    }

    private async Task<List<int>> GetMasteredWordIdsAsync()
    {
        return await _db.Set<LearningRecord>()
            .Where(r => r.ActivityType == LearningActivityType.Quiz && r.IsCorrect)
            .GroupBy(r => r.WordId)
            .Where(g => g.Count() >= 10)
            .Select(g => g.Key)
            .ToListAsync();
    }
}
