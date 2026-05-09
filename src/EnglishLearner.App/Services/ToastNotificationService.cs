using EnglishLearner.Core.Interfaces;
using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;

namespace EnglishLearner.App.Services;

public sealed class ToastNotificationService
{
    private readonly ISm2Service _sm2Service;

    public ToastNotificationService(ISm2Service sm2Service)
    {
        _sm2Service = sm2Service;
    }

    public async Task ShowDailyReminderAsync()
    {
        var dueWords = await _sm2Service.GetDueWordsAsync();
        var count = dueWords.Count;

        if (count == 0)
        {
            Log.Information("No due words, skipping toast notification");
            return;
        }

        new ToastContentBuilder()
            .AddArgument("action", "review")
            .AddText("EnglishLearner 复习提醒")
            .AddText($"你有 {count} 个单词待复习，点击开始学习！")
            .AddButton(new ToastButton()
                .SetContent("开始复习")
                .AddArgument("action", "review"))
            .AddButton(new ToastButton()
                .SetContent("稍后提醒")
                .AddArgument("action", "snooze"))
            .Show();

        Log.Information("Toast notification sent: {Count} words due", count);
    }
}
