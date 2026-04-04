using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using fufu_toolbox.Models;
using fufu_toolbox.Services;

namespace fufu_toolbox.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;

    private ThemeOptionItem? _selectedTheme;

    public SettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;

        ThemeOptions =
        [
            new ThemeOptionItem { Mode = AppThemeMode.FollowSystem, DisplayName = "跟随系统" },
            new ThemeOptionItem { Mode = AppThemeMode.Light, DisplayName = "浅色" },
            new ThemeOptionItem { Mode = AppThemeMode.Dark, DisplayName = "深色" }
        ];

        SelectedTheme = ThemeOptions.Find(item => item.Mode == _themeService.CurrentTheme) ?? ThemeOptions[0];
    }

    public List<ThemeOptionItem> ThemeOptions { get; }

    public ThemeOptionItem? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (!SetProperty(ref _selectedTheme, value) || value is null)
            {
                return;
            }

            // 用户选择后立即应用主题。
            _themeService.ApplyTheme(value.Mode);
        }
    }
}
