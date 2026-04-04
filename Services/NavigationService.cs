using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using fufu_toolbox.Pages;

namespace fufu_toolbox.Services;

public interface INavigationService
{
    bool CanGoBack { get; }

    void Initialize(Frame frame);

    bool NavigateTo(string key, object? parameter = null);

    void GoBack();
}

public static class NavigationKeys
{
    public const string Home = "home";
    public const string MergeTxt = "merge-txt";
    public const string Settings = "settings";
}

public sealed class NavigationService : INavigationService
{
    private readonly Dictionary<string, Type> _pages = new()
    {
        [NavigationKeys.Home] = typeof(HomePage),
        [NavigationKeys.MergeTxt] = typeof(MergeTxtPage),
        [NavigationKeys.Settings] = typeof(SettingsPage)
    };

    private Frame? _frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    // 在窗口初始化后绑定导航容器。
    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    // 根据标识跳转到指定页面。
    public bool NavigateTo(string key, object? parameter = null)
    {
        if (_frame is null || !_pages.TryGetValue(key, out Type? pageType))
        {
            return false;
        }

        if (_frame.Content?.GetType() == pageType)
        {
            return false;
        }

        return _frame.Navigate(pageType, parameter);
    }

    // 返回上一页。
    public void GoBack()
    {
        if (CanGoBack)
        {
            _frame!.GoBack();
        }
    }
}
