using EnglishLearner.Core.Interfaces;
using NAudio.Wave;

namespace EnglishLearner.Infrastructure.Services;

public sealed class AudioRecorderService : IAudioRecorderService
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _outputPath;
    private bool _disposed;

    public bool IsRecording => _waveIn is not null;

    public Task StartRecordingAsync(string outputPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsRecording) throw new InvalidOperationException("Already recording");

        var dir = Path.Combine(Path.GetTempPath(), "EnglishLearner");
        Directory.CreateDirectory(dir);

        _outputPath = Path.Combine(dir, $"rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1)
        };

        _writer = new WaveFileWriter(_outputPath, _waveIn.WaveFormat);

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _waveIn.StartRecording();
        return Task.CompletedTask;
    }

    public Task<string> StopRecordingAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsRecording) throw new InvalidOperationException("Not recording");

        var path = _outputPath!;

        _waveIn!.StopRecording();

        _waveIn.DataAvailable -= OnDataAvailable;
        _waveIn.RecordingStopped -= OnRecordingStopped;
        _waveIn.Dispose();
        _waveIn = null;

        _writer!.Flush();
        _writer.Dispose();
        _writer = null;

        return Task.FromResult(path);
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        _writer?.Write(e.Buffer, 0, e.BytesRecorded);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        // Handled in StopRecordingAsync
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_waveIn is not null)
        {
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.RecordingStopped -= OnRecordingStopped;
            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;
        }

        _writer?.Flush();
        _writer?.Dispose();
        _writer = null;
    }
}
