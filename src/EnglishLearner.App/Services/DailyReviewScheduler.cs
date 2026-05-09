using System.Windows.Threading;
using EnglishLearner.Core.Interfaces;
using Serilog;

namespace EnglishLearner.App.Services;

public sealed class DailyReviewScheduler : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly ToastNotificationService _toastService;
    private DateTime _lastReminderDate = DateTime.MinValue;

    // 默认每天 9:00 提醒
    public TimeSpan ReminderTime { get; set; } = new(9, 0, 0);

    public DailyReviewScheduler(ToastNotificationService toastService)
    {
        _toastService = toastService;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        _timer.Start();
        Log.Information("Daily review scheduler started, reminder at {Time}", ReminderTime);
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        if (now.Date == _lastReminderDate.Date) return;
        if (now.TimeOfDay < ReminderTime) return;

        _lastReminderDate = now.Date;
        await _toastService.ShowDailyReminderAsync();
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}
