namespace EnglishLearner.Core.Models;

public class WordExplanation
{
    public string Word { get; init; } = string.Empty;
    public string Phonetic { get; init; } = string.Empty;
    public IReadOnlyList<string> Definitions { get; init; } = [];
    public IReadOnlyList<string> Synonyms { get; init; } = [];
    public string ContextualMeaning { get; init; } = string.Empty;
    public string ExampleSentence { get; init; } = string.Empty;
}
