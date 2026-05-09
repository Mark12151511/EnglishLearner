namespace EnglishLearner.Core.Models;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string DifficultyLevel { get; set; } = "Intermediate";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LearningRecord> LearningRecords { get; set; } = [];
}
