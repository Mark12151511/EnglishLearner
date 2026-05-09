namespace EnglishLearner.Core.Models;

public class Sm2Profile
{
    public int Id { get; set; }
    public int WordId { get; set; }
    public int Repetition { get; set; }
    public double EasinessFactor { get; set; } = 2.5;
    public int IntervalDays { get; set; }
    public DateTime NextReviewAt { get; set; } = DateTime.UtcNow;
    public DateTime LastReviewedAt { get; set; } = DateTime.UtcNow;

    public Word Word { get; set; } = null!;
}
