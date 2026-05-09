namespace EnglishLearner.Core.Models;

public class WritingCorrection
{
    public string CorrectedText { get; init; } = string.Empty;
    public IReadOnlyList<GrammarError> Errors { get; init; } = [];
    public string Suggestions { get; init; } = string.Empty;
    public int Score { get; init; }
}

public class GrammarError
{
    public string Original { get; init; } = string.Empty;
    public string Corrected { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
}
