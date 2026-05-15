namespace EnglishLearner.Core.Interfaces;

public interface ISpeechService : IDisposable
{
    int Rate { get; set; }
    Task InitializeAsync();
    void Speak(string text);
    void Preload(string text);
    void Stop();
}
