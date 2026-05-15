using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using EnglishLearner.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearner.Infrastructure.Repositories;

public class SentenceRepository : ISentenceRepository
{
    private readonly AppDbContext _db;

    public SentenceRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Sentence>> GetByDifficultyAsync(int level, int count = 20)
    {
        return await _db.Sentences
            .Where(s => s.DifficultyLevel == level)
            .OrderBy(_ => EF.Functions.Random())
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Sentence>> GetMixedAsync(int countPerLevel = 10)
    {
        var result = new List<Sentence>();

        for (var level = 1; level <= 3; level++)
        {
            var items = await _db.Sentences
                .Where(s => s.DifficultyLevel == level)
                .OrderBy(_ => EF.Functions.Random())
                .Take(countPerLevel)
                .AsNoTracking()
                .ToListAsync();
            result.AddRange(items);
        }

        return result.OrderBy(_ => Random.Shared.Next()).ToList();
    }
}
