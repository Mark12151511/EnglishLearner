namespace EnglishLearner.Core.Models;

public class LearningRecord
{
    public int Id { get; set; }
    public int WordId { get; set; }
    public int? ArticleId { get; set; }
    public LearningActivityType ActivityType { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime PracticedAt { get; set; } = DateTime.UtcNow;

    public Word Word { get; set; } = null!;
    public Article? Article { get; set; }
}

public enum LearningActivityType
{
    Flashcard,
    Spelling,
    Listening,
    Reading,
}
