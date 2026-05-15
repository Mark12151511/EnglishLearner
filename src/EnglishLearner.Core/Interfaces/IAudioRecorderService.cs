namespace EnglishLearner.Core.Interfaces;

public interface IAudioRecorderService : IDisposable
{
    bool IsRecording { get; }
    Task StartRecordingAsync(string outputPath);
    Task<string> StopRecordingAsync();
}
