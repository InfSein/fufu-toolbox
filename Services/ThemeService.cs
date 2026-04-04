using System;
using System.Diagnostics;
using Windows.Storage;

namespace fufu_toolbox.Services;

public enum AppThemeMode
{
    FollowSystem,
    Light,
    Dark
}

public interface IThemeService
{
    AppThemeMode CurrentTheme { get; }

    event Action<AppThemeMode>? ThemeChanged;

    void ApplyTheme(AppThemeMode mode);
}

public sealed class ThemeService : IThemeService
{
    private const string ThemeModeKey = "AppThemeMode";

    private readonly ApplicationDataContainer? _localSettings;

    public ThemeService()
    {
        _localSettings = ApplicationData.Current?.LocalSettings;
        CurrentTheme = LoadTheme();
    }

    public AppThemeMode CurrentTheme { get; private set; }

    public event Action<AppThemeMode>? ThemeChanged;

    // 应用并广播主题变化。
    public void ApplyTheme(AppThemeMode mode)
    {
        if (CurrentTheme == mode)
        {
            return;
        }

        CurrentTheme = mode;
        SaveTheme(mode);
        ThemeChanged?.Invoke(mode);
    }

    // 启动时读取本地保存的主题。
    private AppThemeMode LoadTheme()
    {
        try
        {
            if (_localSettings?.Values.TryGetValue(ThemeModeKey, out object? savedValue) != true)
            {
                return AppThemeMode.FollowSystem;
            }

            if (savedValue is int modeValue && Enum.IsDefined(typeof(AppThemeMode), modeValue))
            {
                return (AppThemeMode)modeValue;
            }

            if (savedValue is string modeText && Enum.TryParse(modeText, out AppThemeMode parsedMode))
            {
                return parsedMode;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"读取主题设置失败: {ex.Message}");
        }

        return AppThemeMode.FollowSystem;
    }

    // 用户修改主题后写入本地存储。
    private void SaveTheme(AppThemeMode mode)
    {
        try
        {
            if (_localSettings is not null)
            {
                _localSettings.Values[ThemeModeKey] = (int)mode;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"保存主题设置失败: {ex.Message}");
        }
    }
}
