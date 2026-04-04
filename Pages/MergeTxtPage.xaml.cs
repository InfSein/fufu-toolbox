using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using fufu_toolbox.Models;
using fufu_toolbox.ViewModels;

namespace fufu_toolbox.Pages;

public sealed partial class MergeTxtPage : Page
{
    public MergeTxtViewModel ViewModel { get; }

    public MergeTxtPage()
    {
        ViewModel = App.GetService<MergeTxtViewModel>();
        InitializeComponent();
    }

    // 选择目录并扫描其中所有 txt 文件。
    private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
    {
        FolderPicker picker = new();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, GetMainWindowHandle());

        StorageFolder? folder = await picker.PickSingleFolderAsync();
        if (folder is null)
        {
            return;
        }

        try
        {
            await ViewModel.ScanFolderAsync(folder.Path);
            await ShowInfoAsync("扫描完成", ViewModel.ScanSummary);
        }
        catch (Exception ex)
        {
            await ShowInfoAsync("扫描失败", ex.Message);
        }
    }

    // 全选当前列表文件。
    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAll();
    }

    // 反选当前列表文件。
    private void InvertSelectButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.InvertSelection();
    }

    // 将当前行文件上移。
    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MergeTxtFileItem item })
        {
            ViewModel.MoveUp(item);
        }
    }

    // 将当前行文件下移。
    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MergeTxtFileItem item })
        {
            ViewModel.MoveDown(item);
        }
    }

    // 合并所选 txt 并弹出另存为窗口。
    private async void MergeAndSaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanMerge)
        {
            await ShowInfoAsync("无法合并", "请至少选择一个 txt 文件。");
            return;
        }

        FileSavePicker picker = new();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = "merged";
        picker.FileTypeChoices.Add("文本文件", new List<string> { ".txt" });
        InitializeWithWindow.Initialize(picker, GetMainWindowHandle());

        StorageFile? outputFile = await picker.PickSaveFileAsync();
        if (outputFile is null)
        {
            return;
        }

        try
        {
            string mergedContent = await ViewModel.BuildMergedContentAsync();
            await File.WriteAllTextAsync(outputFile.Path, mergedContent, new UTF8Encoding(false));
            await ShowInfoAsync("合并完成", $"已输出到：{outputFile.Path}");
        }
        catch (Exception ex)
        {
            await ShowInfoAsync("合并失败", ex.Message);
        }
    }

    // 获取主窗口句柄给系统文件选择器使用。
    private static IntPtr GetMainWindowHandle()
    {
        return WindowNative.GetWindowHandle(App.GetService<MainWindow>());
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
