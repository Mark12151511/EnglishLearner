namespace EnglishLearner.Core.Models;

public class DictationResult
{
    public string OriginalText { get; init; } = "";
    public string UserInput    { get; init; } = "";
    public double Score        { get; init; }
    public int    CorrectWords { get; init; }
    public int    TotalWords   { get; init; }
    public IReadOnlyList<WordMatchResult> WordMatches { get; init; } = [];
}
