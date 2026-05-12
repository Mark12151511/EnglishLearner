namespace EnglishLearner.Core.Interfaces;

public interface ISpeechService : IDisposable
{
    Task InitializeAsync();
    void Speak(string text);
    void Preload(string text);
    void Stop();
}
