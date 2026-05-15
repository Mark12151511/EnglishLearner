using EnglishLearner.Core.Interfaces;

namespace EnglishLearner.Infrastructure.Services;

public sealed class AzureSpeechService : ISpeechService
{
    private bool _disposed;

    public int Rate { get; set; } = -1;
    public Task InitializeAsync() => Task.CompletedTask;
    public void Speak(string text) { }
    public void Preload(string text) { }
    public void Stop() { }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
