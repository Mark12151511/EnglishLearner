using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using EnglishLearner.Infrastructure.Services;
using Moq;
using Xunit;

namespace EnglishLearner.Tests;

public class QuizServiceTests
{
    private static List<Word> CreateTestWords(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Word
            {
                Id = i,
                Text = $"word{i}",
                Meaning = $"释义{i}",
                Example = $"Example sentence {i}"
            })
            .ToList();
    }

    private static QuizService CreateService(List<Word> learnedWords)
    {
        var mockLearning = new Mock<ILearningService>();
        mockLearning.Setup(s => s.GetLearnedWordsAsync(It.IsAny<int[]?>()))
            .ReturnsAsync(learnedWords);
        return new QuizService(mockLearning.Object);
    }

    [Fact]
    public async Task GenerateQuizAsync_ReturnsCorrectCount()
    {
        var service = CreateService(CreateTestWords(20));
        var quiz = await service.GenerateQuizAsync(10);
        Assert.Equal(5, quiz.Questions.Count); // 20/4 = 5
    }

    [Fact]
    public async Task GenerateQuizAsync_EachQuestionHas4Options()
    {
        var service = CreateService(CreateTestWords(20));
        var quiz = await service.GenerateQuizAsync(10);

        foreach (var q in quiz.Questions)
        {
            Assert.Equal(4, q.Options.Count);
        }
    }

    [Fact]
    public async Task GenerateQuizAsync_CorrectIndexIsValid()
    {
        var service = CreateService(CreateTestWords(20));
        var quiz = await service.GenerateQuizAsync(10);

        foreach (var q in quiz.Questions)
        {
            Assert.InRange(q.CorrectIndex, 0, 3);
        }
    }

    [Fact]
    public async Task GenerateQuizAsync_ThrowsWhenNotEnoughWords()
    {
        var service = CreateService(CreateTestWords(3));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateQuizAsync(10));
    }

    [Fact]
    public async Task SubmitQuizAsync_AccuracyCalculation()
    {
        var service = CreateService(CreateTestWords(20));
        var quiz = await service.GenerateQuizAsync(10);

        var allCorrect = quiz.Questions.Select(q => q.CorrectIndex).ToList();
        var result1 = await service.SubmitQuizAsync(allCorrect, quiz);
        Assert.Equal(1.0, result1.Accuracy);

        var halfCorrect = quiz.Questions.Select((q, i) => i < quiz.Questions.Count / 2 ? q.CorrectIndex : (q.CorrectIndex + 1) % 4).ToList();
        var result2 = await service.SubmitQuizAsync(halfCorrect, quiz);
        Assert.True(result2.Accuracy > 0 && result2.Accuracy < 1.0);
    }

    [Fact]
    public async Task SubmitQuizAsync_WrongQuestionsCollected()
    {
        var service = CreateService(CreateTestWords(20));
        var quiz = await service.GenerateQuizAsync(10);

        var wrongIndices = new HashSet<int> { 1, 3 };
        var answers = quiz.Questions.Select((q, i) =>
            wrongIndices.Contains(i) ? (q.CorrectIndex + 1) % 4 : q.CorrectIndex).ToList();

        var result = await service.SubmitQuizAsync(answers, quiz);

        Assert.Equal(wrongIndices.Count, result.WrongQuestions.Count);
        Assert.Equal(quiz.Questions.Count - wrongIndices.Count, result.CorrectCount);
    }
}
