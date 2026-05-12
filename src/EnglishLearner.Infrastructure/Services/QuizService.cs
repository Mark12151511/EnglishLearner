using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;

namespace EnglishLearner.Infrastructure.Services;

public class QuizService : IQuizService
{
    private readonly ILearningService _learningService;
    private readonly Random _rng = new();

    public QuizService(ILearningService learningService)
    {
        _learningService = learningService;
    }

    public async Task<Quiz> GenerateQuizAsync(int count = 10, int[]? difficultyFilter = null)
    {
        var all = (await _learningService.GetLearnedWordsAsync(difficultyFilter)).ToList();

        if (all.Count < 4)
            throw new InvalidOperationException("已学单词量不足以生成测验（至少需要4个）");

        count = Math.Min(count, all.Count / 4);

        var shuffled = all.OrderBy(_ => Random.Shared.Next()).ToList();
        var questionWords = shuffled.Take(count).ToList();
        var pool = shuffled.Skip(count).ToList();

        var questions = new List<QuizQuestion>();

        for (int i = 0; i < questionWords.Count; i++)
        {
            var word = questionWords[i];

            var distractors = pool
                .Where(w => w.Id != word.Id)
                .Select(w => w.Meaning)
                .Distinct()
                .OrderBy(_ => _rng.Next())
                .Take(3)
                .ToList();

            var options = new List<string> { word.Meaning };
            options.AddRange(distractors);
            Shuffle(options);

            questions.Add(new QuizQuestion
            {
                Number = i + 1,
                Question = $"「{word.Text}」的含义是？",
                Options = options,
                CorrectIndex = options.IndexOf(word.Meaning),
                Explanation = word.Example ?? word.Meaning,
                WordId = word.Id,
                WordText = word.Text
            });
        }

        return new Quiz { Questions = questions };
    }

    public Task<QuizResult> SubmitQuizAsync(IReadOnlyList<int> userAnswers, Quiz quiz)
    {
        var wrong = new List<QuizQuestion>();

        for (int i = 0; i < quiz.Questions.Count; i++)
        {
            if (i < userAnswers.Count && userAnswers[i] != quiz.Questions[i].CorrectIndex)
                wrong.Add(quiz.Questions[i]);
        }

        var correctCount = quiz.Questions.Count - wrong.Count;

        return Task.FromResult(new QuizResult
        {
            TotalCount = quiz.Questions.Count,
            CorrectCount = correctCount,
            WrongQuestions = wrong
        });
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
