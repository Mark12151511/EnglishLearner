namespace EnglishLearner.Core.Models;

public class Sm2Result
{
    public int Repetition { get; init; }
    public double EasinessFactor { get; init; }
    public int IntervalDays { get; init; }
    public DateTime NextReviewAt { get; init; }
}
