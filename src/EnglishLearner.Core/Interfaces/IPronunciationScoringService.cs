using EnglishLearner.Core.Models;

namespace EnglishLearner.Core.Interfaces;

public interface IPronunciationScoringService
{
    /// <summary>对比标准文本和识别文本，返回评分结果</summary>
    PronunciationResult Score(string originalText, string recognizedText);
    DictationResult ScoreDictation(string originalText, string userInput);
}
