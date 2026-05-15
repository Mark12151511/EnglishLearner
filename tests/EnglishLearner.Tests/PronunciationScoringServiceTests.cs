using EnglishLearner.Infrastructure.Services;
using Xunit;

namespace EnglishLearner.Tests;

public class PronunciationScoringServiceTests
{
    private readonly PronunciationScoringService _sut = new();

    // ── 口语评分 ──────────────────────────────────────────────────

    [Fact]
    public void Score_PerfectMatch_Returns1()
    {
        var result = _sut.Score("hello world", "hello world");
        Assert.Equal(1.0, result.Score);
        Assert.All(result.WordMatches, m => Assert.True(m.IsCorrect));
    }

    [Fact]
    public void Score_CompletelyWrong_Returns0()
    {
        var result = _sut.Score("apple", "banana");
        Assert.Equal(0.0, result.Score);
        Assert.All(result.WordMatches, m => Assert.False(m.IsCorrect));
    }

    [Fact]
    public void Score_PartialMatch_ReturnsHalf()
    {
        var result = _sut.Score("the cat sat on the mat", "the cat sat");
        Assert.Equal(0.5, result.Score, 2);
        Assert.True(result.WordMatches[0].IsCorrect); // the
        Assert.True(result.WordMatches[1].IsCorrect); // cat
        Assert.True(result.WordMatches[2].IsCorrect); // sat
        Assert.False(result.WordMatches[3].IsCorrect); // on
    }

    [Fact]
    public void Score_CaseInsensitive_Returns1()
    {
        var result = _sut.Score("Hello", "hello");
        Assert.Equal(1.0, result.Score);
    }

    [Fact]
    public void Score_WithPunctuation_Returns1()
    {
        var result = _sut.Score("run!", "run");
        Assert.Equal(1.0, result.Score);
    }

    // ── 听写评分 ──────────────────────────────────────────────────

    [Fact]
    public void ScoreDictation_ExactMatch_Returns1()
    {
        var result = _sut.ScoreDictation("hello world", "hello world");
        Assert.Equal(1.0, result.Score);
        Assert.Equal(2, result.CorrectWords);
        Assert.Equal(2, result.TotalWords);
    }

    [Fact]
    public void ScoreDictation_HalfCorrect_CorrectWordsMatchesHalf()
    {
        var result = _sut.ScoreDictation("the cat sat on the mat", "the cat");
        Assert.Equal(2, result.CorrectWords);
        Assert.Equal(6, result.TotalWords);
        Assert.Equal(2.0 / 6, result.Score, 4);
    }
}
