namespace EnglishLearner.Core.Models;

public class PronunciationResult
{
    public string OriginalText   { get; init; } = "";
    public string RecognizedText { get; init; } = "";
    public double Score          { get; init; }
    public IReadOnlyList<WordMatchResult> WordMatches { get; init; } = [];
}

public class WordMatchResult
{
    public string Word      { get; init; } = "";
    public bool   IsCorrect { get; init; }
}
