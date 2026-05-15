using System.IO;
using System.Windows;
using EnglishLearner.App.Services;
using EnglishLearner.App.ViewModels;
using EnglishLearner.App.Views;
using EnglishLearner.Core.Interfaces;
using EnglishLearner.Core.Models;
using EnglishLearner.Infrastructure.Data;
using EnglishLearner.Infrastructure.Repositories;
using EnglishLearner.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;

namespace EnglishLearner.App;

public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;
    private TrayIconService _trayIcon = null!;
    private DailyReviewScheduler _scheduler = null!;

    public IServiceProvider ServiceProvider => _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/englishlearner-.log", rollingInterval: RollingInterval.Day)
            .WriteTo.Debug()
            .CreateLogger();

        Log.Information("Application starting");

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        await EnsureDatabaseAsync(_serviceProvider);

        // ★ 词库导入（仅首次运行执行，之后自动跳过）
        using (var seedScope = _serviceProvider.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var seeder = new WordSeeder(db);
            var csvPath = Path.Combine(AppContext.BaseDirectory,
                                       "Assets", "WordLists", "words_enriched.csv");
            await seeder.SeedAsync(csvPath);

            var sentenceSeeder = new SentenceSeeder(db);
            await sentenceSeeder.SeedFromWordExamplesAsync();
        }

        // Toast 点击事件
        ToastNotificationManagerCompat.OnActivated += OnToastActivated;

        // 系统托盘
        _trayIcon = new TrayIconService(ShowMainWindow, ExitApp);
        _trayIcon.Initialize();

        // 每日提醒调度
        _scheduler = _serviceProvider.GetRequiredService<DailyReviewScheduler>();
        _scheduler.Start();

        // 后台初始化 TTS 引擎（模型较大，不阻塞 UI）
        _ = _serviceProvider.GetRequiredService<ISpeechService>().InitializeAsync();

        // 显示主窗口
        ShowMainWindow();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var dbPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "englishlearner.db");
            options.UseSqlite($"Data Source={dbPath}", sqlite =>
                sqlite.MigrationsAssembly("EnglishLearner.Infrastructure"));
        });

        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainWindow>();

        services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));
        services.AddSingleton<ISentenceRepository, SentenceRepository>();
        services.AddSingleton<ISm2Service, Sm2Service>();
        services.AddSingleton<IAiService, ClaudeAiService>();
        services.AddSingleton<ILearningService, LearningService>();
        services.AddSingleton<IQuizService, QuizService>();
        services.AddSingleton<ISpeechService, SystemSpeechService>();

        // Whisper 语音识别
        var whisperModelPath = Path.Combine(AppContext.BaseDirectory,
            "Assets", "WhisperModels", "ggml-base.en.bin");
        services.AddSingleton<IAudioRecorderService, AudioRecorderService>();
        services.AddSingleton<ISpeechRecognitionService>(
            _ => new WhisperRecognitionService(whisperModelPath));
        services.AddSingleton<IPronunciationScoringService, PronunciationScoringService>();

        services.AddSingleton<ToastNotificationService>();
        services.AddSingleton<DailyReviewScheduler>();
    }

    private static async Task EnsureDatabaseAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();
        Log.Information("Database initialized");
    }

    private void ShowMainWindow()
    {
        var window = Current.MainWindow;
        if (window == null)
        {
            window = _serviceProvider.GetRequiredService<MainWindow>();
            Current.MainWindow = window;
        }

        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }

    private void ExitApp()
    {
        _serviceProvider.GetRequiredService<ISpeechService>().Dispose();
        _scheduler.Dispose();
        _trayIcon.Dispose();
        Shutdown();
    }

    private void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        Current.Dispatcher.Invoke(() =>
        {
            var args = e.Argument;
            if (args.Contains("action=review") || string.IsNullOrEmpty(args))
            {
                ShowMainWindow();
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exiting");
        Log.CloseAndFlush();
        _serviceProvider.Dispose();
        ToastNotificationManagerCompat.History.Clear();
        base.OnExit(e);
    }
}
