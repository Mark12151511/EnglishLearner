using System.Text.RegularExpressions;
using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;

namespace EnglishLearner.Infrastructure.Services;

public sealed class PronunciationScoringService : IPronunciationScoringService
{
    public PronunciationResult Score(string originalText, string recognizedText)
    {
        var originalWords = Tokenize(originalText);
        var recognizedWords = Tokenize(recognizedText);

        if (originalWords.Length == 0 && recognizedWords.Length == 0)
            return new PronunciationResult { OriginalText = originalText, RecognizedText = recognizedText, Score = 1.0 };

        var lcsLength = LcsLength(originalWords, recognizedWords);
        var maxLen = Math.Max(originalWords.Length, recognizedWords.Length);
        var score = maxLen == 0 ? 1.0 : (double)lcsLength / maxLen;

        var matches = new List<WordMatchResult>();
        for (var i = 0; i < originalWords.Length; i++)
        {
            matches.Add(new WordMatchResult
            {
                Word = originalWords[i],
                IsCorrect = i < recognizedWords.Length && originalWords[i] == recognizedWords[i]
            });
        }

        return new PronunciationResult
        {
            OriginalText = originalText,
            RecognizedText = recognizedText,
            Score = score,
            WordMatches = matches
        };
    }

    public DictationResult ScoreDictation(string originalText, string userInput)
    {
        var originalWords = Tokenize(originalText);
        var inputWords = Tokenize(userInput);
        var totalWords = originalWords.Length;

        if (totalWords == 0)
            return new DictationResult { OriginalText = originalText, UserInput = userInput, Score = 1.0 };

        var correctWords = 0;
        var matches = new List<WordMatchResult>();

        for (var i = 0; i < originalWords.Length; i++)
        {
            var isCorrect = i < inputWords.Length && originalWords[i] == inputWords[i];
            if (isCorrect) correctWords++;
            matches.Add(new WordMatchResult { Word = originalWords[i], IsCorrect = isCorrect });
        }

        return new DictationResult
        {
            OriginalText = originalText,
            UserInput = userInput,
            Score = (double)correctWords / totalWords,
            CorrectWords = correctWords,
            TotalWords = totalWords,
            WordMatches = matches
        };
    }

    private static string[] Tokenize(string text)
    {
        var lower = text.ToLowerInvariant();
        var cleaned = Regex.Replace(lower, @"[^a-z\s]", "");
        return cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static int LcsLength(string[] a, string[] b)
    {
        var m = a.Length;
        var n = b.Length;
        var dp = new int[m + 1, n + 1];

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1] + 1
                    : Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        return dp[m, n];
    }
}
