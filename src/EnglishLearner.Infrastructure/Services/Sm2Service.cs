using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using EnglishLearner.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearner.Infrastructure.Services;

public class Sm2Service : ISm2Service
{
    private readonly AppDbContext _db;

    public Sm2Service(AppDbContext db)
    {
        _db = db;
    }

    public Sm2Result Calculate(int repetition, double easinessFactor, int quality)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quality, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, 5);

        var ef = Math.Max(1.3, easinessFactor + 0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));

        int newRepetition;
        int interval;

        if (quality >= 3)
        {
            newRepetition = repetition + 1;
            interval = newRepetition switch
            {
                1 => 1,
                2 => 6,
                _ => (int)Math.Ceiling(ef * PreviousInterval(repetition, easinessFactor))
            };
        }
        else
        {
            newRepetition = 0;
            interval = 1;
        }

        return new Sm2Result
        {
            Repetition = newRepetition,
            EasinessFactor = ef,
            IntervalDays = interval,
            NextReviewAt = DateTime.UtcNow.AddDays(interval),
        };
    }

    public async Task<Sm2Result> ReviewAsync(int wordId, int quality)
    {
        var profile = await _db.Set<Sm2Profile>().FindAsync(wordId);

        if (profile is null)
        {
            profile = new Sm2Profile { WordId = wordId };
            _db.Add(profile);
        }

        var result = Calculate(profile.Repetition, profile.EasinessFactor, quality);

        profile.Repetition = result.Repetition;
        profile.EasinessFactor = result.EasinessFactor;
        profile.IntervalDays = result.IntervalDays;
        profile.NextReviewAt = result.NextReviewAt;
        profile.LastReviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return result;
    }

    public async Task<IReadOnlyList<Word>> GetDueWordsAsync()
    {
        var now = DateTime.UtcNow;
        return await _db.Set<Sm2Profile>()
            .Where(p => p.NextReviewAt <= now)
            .Include(p => p.Word)
            .Select(p => p.Word)
            .ToListAsync();
    }

    private static int PreviousInterval(int repetition, double ef)
    {
        // 根据传入的 repetition（即上一轮的值）还原上一轮的 interval
        return repetition switch
        {
            0 => 1,
            1 => 1,
            2 => 6,
            _ => (int)Math.Ceiling(ef * PreviousInterval(repetition - 1, ef))
        };
    }
}
