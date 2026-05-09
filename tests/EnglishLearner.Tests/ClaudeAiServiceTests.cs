using System.Net;
using System.Text;
using System.Text.Json;
using EnglishLearner.Core.Models;
using EnglishLearner.Infrastructure.Security;
using EnglishLearner.Infrastructure.Services;
using Xunit;

namespace EnglishLearner.Tests;

public class ClaudeAiServiceTests : IDisposable
{
    private readonly string _tempKeyFile;
    private readonly DpapiKeyStore _keyStore;

    public ClaudeAiServiceTests()
    {
        _tempKeyFile = Path.Combine(Path.GetTempPath(), $"test_key_{Guid.NewGuid()}.dat");
        _keyStore = new DpapiKeyStore(_tempKeyFile);
        _keyStore.SaveKey("sk-test-fake-key");
    }

    // ── CorrectWritingAsync ──────────────────────────────────

    [Fact]
    public async Task CorrectWritingAsync_ReturnsCorrection()
    {
        var correction = new
        {
            corrected_text = "I went to the store.",
            errors = new[] { new { original = "I go to store", corrected = "I went to the store", explanation = "Past tense needed" } },
            suggestions = "Pay attention to past tense",
            score = 80
        };

        var service = CreateService(correction);
        var result = await service.CorrectWritingAsync("I go to store");

        Assert.Equal("I went to the store.", result.CorrectedText);
        Assert.Single(result.Errors);
        Assert.Equal(80, result.Score);
    }

    // ── ExplainWordAsync ─────────────────────────────────────

    [Fact]
    public async Task ExplainWordAsync_ReturnsExplanation()
    {
        var explanation = new
        {
            word = "abandon",
            phonetic = "/əˈbændən/",
            definitions = new[] { "to leave behind", "to give up" },
            synonyms = new[] { "desert", "forsake" },
            contextual_meaning = "to leave a place",
            example_sentence = "He abandoned the ship."
        };

        var service = CreateService(explanation);
        var result = await service.ExplainWordAsync("abandon", "He abandoned the old car.");

        Assert.Equal("abandon", result.Word);
        Assert.Equal("/əˈbændən/", result.Phonetic);
        Assert.Equal(2, result.Definitions.Count);
    }

    // ── GenerateQuizAsync ────────────────────────────────────

    [Fact]
    public async Task GenerateQuizAsync_ReturnsFiveQuestions()
    {
        var quiz = new
        {
            questions = Enumerable.Range(1, 5).Select(i => new
            {
                number = i,
                question = $"Question {i}?",
                options = new[] { "A) option1", "B) option2", "C) option3", "D) option4" },
                correct_index = 0,
                explanation = $"Explanation {i}"
            }).ToArray()
        };

        var service = CreateService(quiz);
        var result = await service.GenerateQuizAsync("Some article text");

        Assert.Equal(5, result.Questions.Count);
        Assert.All(result.Questions, q => Assert.Equal(4, q.Options.Count));
    }

    // ── 重试逻辑 ─────────────────────────────────────────────

    [Fact]
    public async Task CallApi_Retries_On503()
    {
        var handler = new RetryHandler(
        [
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            CreateSuccessResponse(new { corrected_text = "ok", errors = Array.Empty<object>(), suggestions = "", score = 100 })
        ]);

        var service = new ClaudeAiService(new HttpClient(handler), _keyStore);
        var result = await service.CorrectWritingAsync("test");

        Assert.Equal(3, handler.CallCount);
        Assert.Equal("ok", result.CorrectedText);
    }

    [Fact]
    public async Task CallApi_Retries_On429()
    {
        var handler = new RetryHandler(
        [
            new HttpResponseMessage((HttpStatusCode)429),
            CreateSuccessResponse(new { corrected_text = "ok", errors = Array.Empty<object>(), suggestions = "", score = 100 })
        ]);

        var service = new ClaudeAiService(new HttpClient(handler), _keyStore);
        var result = await service.CorrectWritingAsync("test");

        Assert.Equal(2, handler.CallCount);
        Assert.Equal("ok", result.CorrectedText);
    }

    [Fact]
    public async Task CallApi_ExhaustsRetries_Throws()
    {
        var handler = new RetryHandler(
        [
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
        ]);

        var service = new ClaudeAiService(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) }, _keyStore);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.CorrectWritingAsync("test"));
        Assert.Equal(4, handler.CallCount); // 1 initial + 3 retries
    }

    // ── API Key 验证 ─────────────────────────────────────────

    [Fact]
    public async Task CallApi_NoKey_ThrowsInvalidOperation()
    {
        var noKeyStore = new DpapiKeyStore(Path.Combine(Path.GetTempPath(), $"nokey_{Guid.NewGuid()}.dat"));
        var service = new ClaudeAiService(new HttpClient(), noKeyStore);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CorrectWritingAsync("test"));
    }

    // ── 超时处理 ─────────────────────────────────────────────

    [Fact]
    public async Task CallApi_Timeout_ThrowsTaskCanceledException()
    {
        var handler = new TimeoutHandler();
        var service = new ClaudeAiService(new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) }, _keyStore);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CorrectWritingAsync("test"));
    }

    // ── 辅助方法 ─────────────────────────────────────────────

    private ClaudeAiService CreateService(object responseObject)
    {
        var handler = new StubHandler(CreateSuccessResponse(responseObject));
        return new ClaudeAiService(new HttpClient(handler), _keyStore);
    }

    private static HttpResponseMessage CreateSuccessResponse(object obj)
    {
        var contentJson = JsonSerializer.Serialize(obj);
        var apiResponse = new
        {
            content = new[] { new { type = "text", text = contentJson } }
        };

        var json = JsonSerializer.Serialize(apiResponse);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public void Dispose()
    {
        if (File.Exists(_tempKeyFile)) File.Delete(_tempKeyFile);
    }

    // ── Mock Handlers ────────────────────────────────────────

    private class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private class RetryHandler(HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private int _index;
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(responses[Math.Min(_index++, responses.Length - 1)]);
        }
    }

    private class TimeoutHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
