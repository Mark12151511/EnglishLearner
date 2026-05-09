using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using Serilog;

namespace EnglishLearner.App.Services;

public sealed class TrayIconService : IDisposable
{
    private TaskbarIcon? _notifyIcon;
    private readonly Action _onShowWindow;
    private readonly Action _onExit;

    public TrayIconService(Action onShowWindow, Action onExit)
    {
        _onShowWindow = onShowWindow;
        _onExit = onExit;
    }

    public void Initialize()
    {
        _notifyIcon = new TaskbarIcon
        {
            ToolTipText = "EnglishLearner",
            Icon = System.Drawing.SystemIcons.Information,
            Visibility = Visibility.Visible,
            ContextMenu = CreateContextMenu()
        };

        _notifyIcon.TrayLeftMouseDown += (_, _) => _onShowWindow();
        Log.Information("Tray icon initialized");
    }

    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        var showItem = new MenuItem { Header = "显示主窗口" };
        showItem.Click += (_, _) => _onShowWindow();
        menu.Items.Add(showItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "退出" };
        exitItem.Click += (_, _) => _onExit();
        menu.Items.Add(exitItem);

        return menu;
    }

    public void ShowBalloonTip(string title, string message)
    {
        _notifyIcon?.ShowBalloonTip(title, message, BalloonIcon.Info);
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }
}
