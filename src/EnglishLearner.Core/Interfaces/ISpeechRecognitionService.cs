namespace EnglishLearner.Core.Interfaces;

public interface ISpeechRecognitionService
{
    /// <summary>识别音频文件，返回识别出的文本</summary>
    Task<string> RecognizeAsync(string audioFilePath);
    Task<bool> IsModelReadyAsync();
}
