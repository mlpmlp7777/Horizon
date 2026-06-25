using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Horizon.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Horizon",
                "logs");

            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "ui-crash.log");
            var content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}\r\n\r\n";
            File.AppendAllText(logPath, content);
        }
        catch
        {
            // ignore logging failures and keep the app alive
        }

        MessageBox.Show(
            "界面刚刚触发了一个异常，程序已拦截，未直接退出。\n如果问题重复出现，我已经把异常写入本地日志。",
            "Horizon",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        e.Handled = true;
    }
}
