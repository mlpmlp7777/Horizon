using Microsoft.Win32;

namespace Horizon.App.Services;

public sealed class WindowsStartupService : IStartupRegistrationService
{
    internal const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string ValueName = "Horizon";

    public bool TrySetEnabled(bool enabled, out string? errorMessage)
    {
        try
        {
            if (enabled)
            {
                var executablePath = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    errorMessage = "无法确定 Horizon 程序路径。";
                    return false;
                }

                using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
                if (key is null)
                {
                    errorMessage = "无法打开 Windows 启动项。";
                    return false;
                }

                key.SetValue(ValueName, BuildCommand(executablePath), RegistryValueKind.String);
            }
            else
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                key?.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            errorMessage = null;
            return true;
        }
        catch (Exception)
        {
            errorMessage = "无法更新 Windows 开机启动设置。";
            return false;
        }
    }

    internal static string BuildCommand(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        return $"\"{executablePath.Trim().Trim('\"')}\"";
    }
}
