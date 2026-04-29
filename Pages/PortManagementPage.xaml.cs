using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using fufu_toolbox.Models;
using fufu_toolbox.Services;
using fufu_toolbox.ViewModels;

namespace fufu_toolbox.Pages;

public sealed partial class PortManagementPage : Page
{
    private CancellationTokenSource? _filterRefreshCts;
    private bool _hasLoaded;

    public PortManagementViewModel ViewModel { get; }

    public PortManagementPage()
    {
        ViewModel = App.GetService<PortManagementViewModel>();
        InitializeComponent();
        Loaded += PortManagementPage_Loaded;
    }

    // 首次进入页面时自动刷新一次。
    private async void PortManagementPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await SafeRefreshAsync();
    }

    // 点击刷新按钮时按筛选条件重新加载。
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await SafeRefreshAsync();
    }

    // 用户修改筛选词后自动刷新（防抖）。
    private async void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _filterRefreshCts?.Cancel();
        _filterRefreshCts?.Dispose();
        _filterRefreshCts = new CancellationTokenSource();
        CancellationToken token = _filterRefreshCts.Token;

        try
        {
            await Task.Delay(350, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            await SafeRefreshAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    // 点击中止时先确认，再执行中止流程。
    private async void TerminateProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PortOccupancyItem item })
        {
            return;
        }

        bool confirmed = await ShowConfirmAsync(
            "确认中止程序",
            $"确认要中止“{item.ProcessName}”(PID: {item.ProcessId}) 吗？\n这会释放相关端口占用。",
            "确认中止");

        if (!confirmed)
        {
            return;
        }

        PortTerminateResult result = await ViewModel.TerminateProcessAsync(item.ProcessId);
        await ShowInfoAsync(result.IsSuccess ? "处理完成" : "处理中", result.Message);

        await SafeRefreshAsync();
    }

    // 统一处理刷新异常，避免页面直接崩溃。
    private async Task SafeRefreshAsync()
    {
        try
        {
            await ViewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            await ShowInfoAsync("刷新失败", ex.Message);
        }
    }

    // 弹出确认框，返回用户是否确认。
    private async Task<bool> ShowConfirmAsync(string title, string message, string confirmButtonText)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = message,
            PrimaryButtonText = confirmButtonText,
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    // 统一弹出提示对话框。
    private async Task ShowInfoAsync(string title, string message)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = "确定"
        };

        await dialog.ShowAsync();
    }
}
