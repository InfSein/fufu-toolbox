using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using fufu_toolbox.Models;
using fufu_toolbox.ViewModels;

namespace fufu_toolbox.Pages;

public sealed partial class XivItemTranslatorPage : Page
{
    public XivItemTranslatorViewModel ViewModel { get; }

    public XivItemTranslatorPage()
    {
        ViewModel = App.GetService<XivItemTranslatorViewModel>();
        InitializeComponent();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    // 页面加载后初始化下拉项和默认值。
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        InputLanguageComboBox.ItemsSource = ViewModel.InputLanguages;
        OutputLanguageComboBox.ItemsSource = ViewModel.OutputLanguages;
        InputLanguageComboBox.SelectedItem = ViewModel.SelectedInputLanguage;
        OutputLanguageComboBox.SelectedItem = ViewModel.SelectedOutputLanguage;
        IgnoredPrefixTextBox.Text = ViewModel.IgnoredPrefix;
        KeepPrefixCheckBox.IsChecked = ViewModel.KeepPrefix;
        RefreshUiFromViewModel();
    }

    // 输入文本变化时刷新按钮可用状态。
    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.InputText = InputTextBox.Text;
        TranslateButton.IsEnabled = ViewModel.CanTranslate;
    }

    // 清空输入框内容。
    private void ClearInputButton_Click(object sender, RoutedEventArgs e)
    {
        InputTextBox.Text = string.Empty;
    }

    // 一键复制输出框内容。
    private async void CopyOutputButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(OutputTextBox.Text))
        {
            await ShowToastAsync("当前没有可复制的内容。", true);
            return;
        }

        try
        {
            DataPackage package = new();
            package.SetText(OutputTextBox.Text);
            Clipboard.SetContent(package);
            Clipboard.Flush();
            await ShowToastAsync("复制成功。", false);
        }
        catch (Exception ex)
        {
            await ShowToastAsync($"复制失败：{ex.Message}", true);
        }
    }

    // 点击按钮后执行批量翻译并给出结果提示。
    private async void TranslateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SyncInputToViewModel();
            XivTranslateBatchResult result = await ViewModel.TranslateAsync();
            OutputTextBox.Text = result.OutputText;

            if (result.FailedCount > 0)
            {
                await ShowInfoAsync("翻译结束", result.FailureSummary);
                return;
            }

            await ShowInfoAsync("翻译结束", "全部翻译成功。");
        }
        catch (Exception ex)
        {
            await ShowInfoAsync("翻译失败", ex.Message);
        }
    }

    // 把界面输入同步回 ViewModel。
    private void SyncInputToViewModel()
    {
        ViewModel.SelectedInputLanguage = InputLanguageComboBox.SelectedItem?.ToString() ?? ViewModel.SelectedInputLanguage;
        ViewModel.SelectedOutputLanguage = OutputLanguageComboBox.SelectedItem?.ToString() ?? ViewModel.SelectedOutputLanguage;
        ViewModel.IgnoredPrefix = IgnoredPrefixTextBox.Text;
        ViewModel.KeepPrefix = KeepPrefixCheckBox.IsChecked ?? true;
        ViewModel.InputText = InputTextBox.Text;
    }

    // 响应 ViewModel 状态变化并刷新界面。
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(RefreshUiFromViewModel);
    }

    // 将 ViewModel 当前状态映射到界面控件。
    private void RefreshUiFromViewModel()
    {
        TranslateButton.Content = ViewModel.TranslateButtonText;
        TranslateButton.IsEnabled = ViewModel.CanTranslate;
        ProgressTextBlock.Text = ViewModel.ProgressText;
        TranslateProgressBar.Maximum = ViewModel.ProgressMaximum;
        TranslateProgressBar.Value = ViewModel.ProgressValue;
        OutputTextBox.Text = ViewModel.OutputText;
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

    // 用轻提示条显示复制结果，并在短时间后自动关闭。
    private async Task ShowToastAsync(string message, bool isError)
    {
        ToastInfoBar.Severity = isError ? InfoBarSeverity.Error : InfoBarSeverity.Success;
        ToastInfoBar.Message = message;
        ToastInfoBar.IsOpen = true;
        await Task.Delay(1800);
        ToastInfoBar.IsOpen = false;
    }
}
