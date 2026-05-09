namespace EnglishLearner.Core.Models;

public class Word
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Phonetic { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string? Example { get; set; }
    public string? AudioPath { get; set; }
    public int DifficultyLevel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LearningRecord> LearningRecords { get; set; } = [];
}
