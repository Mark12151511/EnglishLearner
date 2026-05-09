using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using EnglishLearner.Infrastructure.Security;
using Serilog;

namespace EnglishLearner.Infrastructure.Services;

public sealed class ClaudeAiService : IAiService, IDisposable
{
    private readonly HttpClient _http;
    private readonly DpapiKeyStore _keyStore;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private const string ApiBase = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-sonnet-4-20250514";
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)];

    public ClaudeAiService(HttpClient? http = null, DpapiKeyStore? keyStore = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _keyStore = keyStore ?? new DpapiKeyStore();
    }

    public async Task<WritingCorrection> CorrectWritingAsync(string essay)
    {
        var prompt = $$"""
            You are an English writing tutor. Correct the following essay and return a JSON object with this exact structure:
            {
              "corrected_text": "the full corrected text",
              "errors": [{ "original": "...", "corrected": "...", "explanation": "..." }],
              "suggestions": "overall improvement suggestions",
              "score": 85
            }

            Essay:
            {{essay}}
            """;

        var json = await CallApiWithRetryAsync(prompt);
        return JsonSerializer.Deserialize<WritingCorrection>(json, _jsonOpts)
               ?? throw new InvalidOperationException("Failed to deserialize writing correction");
    }

    public async Task<WordExplanation> ExplainWordAsync(string word, string context)
    {
        var prompt = $$"""
            You are an English vocabulary tutor. Explain the word "{{word}}" in the given context and return a JSON object:
            {
              "word": "{{word}}",
              "phonetic": "IPA pronunciation",
              "definitions": ["definition 1", "definition 2"],
              "synonyms": ["synonym 1", "synonym 2"],
              "contextual_meaning": "meaning in this specific context",
              "example_sentence": "a new example sentence"
            }

            Context:
            {{context}}
            """;

        var json = await CallApiWithRetryAsync(prompt);
        return JsonSerializer.Deserialize<WordExplanation>(json, _jsonOpts)
               ?? throw new InvalidOperationException("Failed to deserialize word explanation");
    }

    public async Task<Quiz> GenerateQuizAsync(string article)
    {
        var prompt = $$"""
            You are an English reading comprehension tutor. Generate exactly 5 multiple-choice questions based on the article.
            Return a JSON object:
            {
              "questions": [
                {
                  "number": 1,
                  "question": "...",
                  "options": ["A) ...", "B) ...", "C) ...", "D) ..."],
                  "correct_index": 0,
                  "explanation": "why this answer is correct"
                }
              ]
            }

            Article:
            {{article}}
            """;

        var json = await CallApiWithRetryAsync(prompt);
        return JsonSerializer.Deserialize<Quiz>(json, _jsonOpts)
               ?? throw new InvalidOperationException("Failed to deserialize quiz");
    }

    private async Task<string> CallApiWithRetryAsync(string prompt)
    {
        var apiKey = _keyStore.LoadKey()
                     ?? throw new InvalidOperationException(
                         "API key not configured. Use DpapiKeyStore.SaveKey() to store your key first.");

        Exception? lastException = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await CallApiOnceAsync(apiKey, prompt);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable or HttpStatusCode.InternalServerError)
            {
                lastException = ex;
                Log.Warning("Claude API attempt {Attempt} failed with {Status}, retrying...", attempt + 1, ex.StatusCode);

                if (attempt < MaxRetries)
                    await Task.Delay(RetryDelays[attempt]);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                lastException = ex;
                Log.Warning("Claude API attempt {Attempt} timed out, retrying...", attempt + 1);

                if (attempt < MaxRetries)
                    await Task.Delay(RetryDelays[attempt]);
            }
        }

        throw lastException!;
    }

    private async Task<string> CallApiOnceAsync(string apiKey, string prompt)
    {
        var body = new
        {
            model = Model,
            max_tokens = 4096,
            messages = new[] { new { role = "user", content = prompt } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiBase);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException("Empty response from Claude API");
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
