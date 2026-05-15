namespace EnglishLearner.Core.Models;

public class Sentence
{
    public int    Id              { get; set; }
    public string Text            { get; set; } = "";
    public string? Translation    { get; set; }
    public int    DifficultyLevel { get; set; }
    public string Source          { get; set; } = "";
    public int    WordCount       { get; set; }
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
}
