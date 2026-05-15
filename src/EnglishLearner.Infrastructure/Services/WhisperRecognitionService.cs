using EnglishLearner.Core.Interfaces;
using Whisper.net;

namespace EnglishLearner.Infrastructure.Services;

public sealed class WhisperRecognitionService : ISpeechRecognitionService
{
    private readonly string _modelPath;
    private WhisperFactory? _factory;
    private bool _disposed;

    public WhisperRecognitionService(string modelPath)
    {
        _modelPath = modelPath;
    }

    public Task<bool> IsModelReadyAsync()
    {
        return Task.FromResult(File.Exists(_modelPath));
    }

    public async Task<string> RecognizeAsync(string audioFilePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _factory ??= WhisperFactory.FromPath(_modelPath);

        await using var processor = _factory.CreateBuilder()
            .WithLanguage("en")
            .Build();

        await using var fileStream = File.OpenRead(audioFilePath);

        var segments = new List<string>();
        await foreach (var segment in processor.ProcessAsync(fileStream))
        {
            segments.Add(segment.Text);
        }

        return string.Join("", segments).Trim();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _factory?.Dispose();
        _factory = null;
    }
}
