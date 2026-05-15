using System.Globalization;
using System.Speech.Synthesis;
using EnglishLearner.Core.Interfaces;
using Serilog;

namespace EnglishLearner.Infrastructure.Services;

public sealed class SystemSpeechService : ISpeechService
{
    private readonly SpeechSynthesizer _synth;
    private bool _disposed;
    private volatile bool _warmedUp;

    private string? _preloadedText;
    private PromptBuilder? _preloadedBuilder;

    public SystemSpeechService()
    {
        _synth = new SpeechSynthesizer();
        var voice = _synth.GetInstalledVoices(new CultureInfo("en-US"))
            .FirstOrDefault(v => v.Enabled);
        if (voice != null)
            _synth.SelectVoice(voice.VoiceInfo.Name);
        _synth.Rate = -1;

        Log.Information("System.Speech TTS created (voice: {Voice})",
            voice?.VoiceInfo.Name ?? "default");
    }

    public int Rate
    {
        get => _synth.Rate;
        set => _synth.Rate = value;
    }

    public Task InitializeAsync()
    {
        if (_warmedUp) return Task.CompletedTask;

        // Warm up the TTS engine so first real Speak is instant
        _ = Task.Run(() =>
        {
            try
            {
                var builder = new PromptBuilder(new CultureInfo("en-US"));
                _synth.Speak(builder);
                _warmedUp = true;
                Log.Information("System.Speech TTS warmed up");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "System.Speech warm-up failed");
            }
        });

        return Task.CompletedTask;
    }

    public void Preload(string text)
    {
        if (_disposed) return;
        if (_preloadedText == text) return;

        _preloadedText = text;
        _preloadedBuilder = new PromptBuilder(new CultureInfo("en-US"));
        _preloadedBuilder.AppendText(text);
    }

    public void Speak(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _synth.SpeakAsyncCancelAll();

        if (_preloadedText == text && _preloadedBuilder is not null)
        {
            _synth.SpeakAsync(_preloadedBuilder);
            _preloadedBuilder = null;
            _preloadedText = null;
        }
        else
        {
            _synth.SpeakAsync(text);
        }
    }

    public void Stop()
    {
        _synth.SpeakAsyncCancelAll();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _synth.SpeakAsyncCancelAll();
        _synth.Dispose();
    }
}
