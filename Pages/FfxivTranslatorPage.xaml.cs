using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using fufu_toolbox.ViewModels;

namespace fufu_toolbox.Pages;

public sealed partial class FfxivTranslatorPage : Page
{
    public FfxivTranslatorViewModel ViewModel { get; }

    public FfxivTranslatorPage()
    {
        ViewModel = App.GetService<FfxivTranslatorViewModel>();
        InitializeComponent();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        RefreshUiFromViewModel();
    }

    private void InputTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        ViewModel.InputText = InputTextBox.Text;
        TranslateButton.IsEnabled = ViewModel.CanTranslate;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(RefreshUiFromViewModel);
    }

    private void RefreshUiFromViewModel()
    {
        TranslateButton.Content = ViewModel.TranslateButtonText;
        TranslateButton.IsEnabled = ViewModel.CanTranslate;

        if (OutputTextBox.Text != ViewModel.OutputText)
        {
            OutputTextBox.Text = ViewModel.OutputText;
        }
    }

    private async void PermanentTermsButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowPermanentTermsDialogAsync();
    }

    private async void CustomTermsButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowCustomTermsDialogAsync();
    }

    private async void ExtraRequirementsButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowExtraRequirementsDialogAsync();
    }

    private async void LlmSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowLlmSettingsDialogAsync();
    }

    private async void PromptSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowPromptSettingsDialogAsync();
    }

    private async void TranslateButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.InputText = InputTextBox.Text;

        try
        {
            TranslateButton.IsEnabled = false;
            TranslateButton.Content = "翻译中…";

            List<string> unknownTerms = await ViewModel.TranslateAsync();
            OutputTextBox.Text = ViewModel.OutputText;

            if (unknownTerms.Count > 0)
            {
                await ShowUnknownTermsDialogAsync(unknownTerms);
            }
        }
        catch (Exception ex)
        {
            await ShowInfoAsync("翻译失败", ex.Message);
        }
        finally
        {
            TranslateButton.Content = "执行";
            TranslateButton.IsEnabled = ViewModel.CanTranslate;
        }
    }

    private async Task ShowUnknownTermsDialogAsync(List<string> unknownTerms)
    {
        try
        {
            Dictionary<string, string> userTerms = new();

            foreach (string term in unknownTerms)
            {
                StackPanel panel = new() { Spacing = 12 };

                TextBlock prompt = new()
                {
                    Text = $"请为以下日语术语提供中文翻译：",
                    TextWrapping = TextWrapping.Wrap
                };
                panel.Children.Add(prompt);

                TextBlock termText = new()
                {
                    Text = term,
                    FontSize = 18,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]
                };
                panel.Children.Add(termText);

                TextBox inputBox = new()
                {
                    PlaceholderText = "输入中文翻译，留空跳过"
                };
                panel.Children.Add(inputBox);

                ContentDialog dialog = new()
                {
                    XamlRoot = XamlRoot,
                    Title = $"未知术语 ({unknownTerms.IndexOf(term) + 1}/{unknownTerms.Count})",
                    Content = panel,
                    PrimaryButtonText = "确定",
                    CloseButtonText = "跳过"
                };

                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputBox.Text))
                {
                    userTerms[term] = inputBox.Text.Trim();
                }
            }

            if (userTerms.Count > 0)
            {
                await ViewModel.AddCustomTermsAsync(userTerms);
                await ShowInfoAsync("完成", $"已将 {userTerms.Count} 个术语添加到自定术语表。");
            }
        }
        catch (Exception ex)
        {
            await ShowInfoAsync("错误", $"处理未知术语失败: {ex.Message}");
        }
    }

    private async void LogButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowLogDialogAsync();
    }

    private async Task ShowLogDialogAsync()
    {
        try
        {
            var logs = ViewModel.GetTranslationLogs();

            Grid mainGrid = new() { ColumnSpacing = 16 };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280, GridUnitType.Pixel) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border leftBorder = new()
            {
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(8),
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1)
            };

            StackPanel leftPanel = new() { Spacing = 12 };

            TextBlock leftTitle = new()
            {
                Text = "对话记录",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 16
            };
            leftPanel.Children.Add(leftTitle);

            ListView logListView = new()
            {
                SelectionMode = ListViewSelectionMode.Single,
                MinHeight = 400
            };
            ScrollViewer.SetVerticalScrollBarVisibility(logListView, ScrollBarVisibility.Auto);

            if (logs.Count == 0)
            {
                TextBlock emptyText = new()
                {
                    Text = "暂无记录",
                    Opacity = 0.6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                logListView.Items.Add(emptyText);
            }
            else
            {
                for (int i = logs.Count - 1; i >= 0; i--)
                {
                    var log = logs[i];
                    Border itemBorder = new()
                    {
                        Padding = new Thickness(10, 8, 10, 8),
                        CornerRadius = new CornerRadius(6),
                        Margin = new Thickness(0, 0, 0, 4),
                        Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ControlFillColorDefaultBrush"]
                    };

                    StackPanel itemPanel = new() { Spacing = 2 };

                    TextBlock indexText = new()
                    {
                        Text = $"#{i + 1}",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontSize = 13
                    };

                    TextBlock timeText = new()
                    {
                        Text = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        FontSize = 12,
                        Opacity = 0.7
                    };

                    itemPanel.Children.Add(indexText);
                    itemPanel.Children.Add(timeText);
                    itemBorder.Child = itemPanel;

                    ListViewItem listViewItem = new()
                    {
                        Content = itemBorder,
                        Tag = i,
                        Padding = new Thickness(0),
                        Margin = new Thickness(0)
                    };
                    logListView.Items.Add(listViewItem);
                }
            }

            leftPanel.Children.Add(logListView);
            leftBorder.Child = leftPanel;
            Grid.SetColumn(leftBorder, 0);

            StackPanel rightPanel = new() { Spacing = 12 };

            TextBlock rightTitle = new()
            {
                Text = "详细内容",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 16
            };
            rightPanel.Children.Add(rightTitle);

            Border requestBorder = new()
            {
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(8),
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1)
            };

            StackPanel requestPanel = new() { Spacing = 8 };

            StackPanel requestHeader = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
            FontIcon requestIcon = new() { Glyph = "&#E724;", FontSize = 14 };
            TextBlock requestLabel = new()
            {
                Text = "发送内容",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14
            };
            requestHeader.Children.Add(requestIcon);
            requestHeader.Children.Add(requestLabel);
            requestPanel.Children.Add(requestHeader);

            TextBox requestBox = new()
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 12,
                MinHeight = 150,
                PlaceholderText = "选择记录后显示发送内容"
            };
            ScrollViewer.SetVerticalScrollBarVisibility(requestBox, ScrollBarVisibility.Auto);
            requestPanel.Children.Add(requestBox);
            requestBorder.Child = requestPanel;
            rightPanel.Children.Add(requestBorder);

            Border responseBorder = new()
            {
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(8),
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1)
            };

            StackPanel responsePanel = new() { Spacing = 8 };

            StackPanel responseHeader = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
            FontIcon responseIcon = new() { Glyph = "&#E73A;", FontSize = 14 };
            TextBlock responseLabel = new()
            {
                Text = "接收内容",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14
            };
            responseHeader.Children.Add(responseIcon);
            responseHeader.Children.Add(responseLabel);
            responsePanel.Children.Add(responseHeader);

            TextBox responseBox = new()
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 12,
                MinHeight = 150,
                PlaceholderText = "选择记录后显示接收内容"
            };
            ScrollViewer.SetVerticalScrollBarVisibility(responseBox, ScrollBarVisibility.Auto);
            responsePanel.Children.Add(responseBox);
            responseBorder.Child = responsePanel;
            rightPanel.Children.Add(responseBorder);

            Grid.SetColumn(rightPanel, 1);

            if (logs.Count > 0)
            {
                logListView.SelectionChanged += (s, args) =>
                {
                    if (logListView.SelectedItem is ListViewItem selected && selected.Tag is int index)
                    {
                        var log = logs[index];
                        requestBox.Text = FormatJsonForDisplay(log.RequestSent);
                        responseBox.Text = FormatJsonForDisplay(log.ResponseReceived);
                    }
                };
            }

            mainGrid.Children.Add(leftBorder);
            mainGrid.Children.Add(rightPanel);

            StackPanel dialogContent = new() { Spacing = 16 };
            dialogContent.Children.Add(mainGrid);

            StackPanel bottomBar = new() { Orientation = Orientation.Horizontal, Spacing = 12 };

            TextBlock countText = new()
            {
                Text = $"共 {logs.Count} 条记录",
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.7
            };
            bottomBar.Children.Add(countText);

            Button clearButton = new()
            {
                Content = "清空日志",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            clearButton.Click += (s, args) =>
            {
                ViewModel.ClearTranslationLogs();
                logListView.Items.Clear();
                requestBox.Text = string.Empty;
                responseBox.Text = string.Empty;
                countText.Text = "共 0 条记录";
            };
            bottomBar.Children.Add(clearButton);

            dialogContent.Children.Add(bottomBar);

            ContentDialog dialog = new()
            {
                XamlRoot = XamlRoot,
                Title = "翻译日志",
                Content = dialogContent,
                CloseButtonText = "关闭",
                FullSizeDesired = true
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            await ShowInfoAsync("错误", $"显示日志失败: {ex.Message}");
        }
    }

    private string FormatJsonForDisplay(string json)
    {
        if (string.IsNullOrEmpty(json)) return string.Empty;

        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(json);
            return System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch
        {
            return json;
        }
    }

    private async Task ShowPermanentTermsDialogAsync()
    {
        StackPanel panel = new() { Spacing = 8 };

        TextBlock hint = new()
        {
            Text = "从JSON文件导入常驻术语表（Dictionary<string,string>格式）"
        };
        panel.Children.Add(hint);

        TextBlock currentTerms = new()
        {
            Text = $"当前常驻术语：{ViewModel.PermanentTermsDisplay}",
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 200
        };
        panel.Children.Add(currentTerms);

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "常驻术语表",
            Content = panel,
            PrimaryButtonText = "导入",
            CloseButtonText = "关闭"
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.ImportPermanentTermsAsync();
            currentTerms.Text = $"当前常驻术语：{ViewModel.PermanentTermsDisplay}";
        }
    }

    private async Task ShowCustomTermsDialogAsync()
    {
        StackPanel panel = new() { Spacing = 8 };

        TextBlock hint = new()
        {
            Text = "输入自定术语，格式：日语术语->中文术语，一行一个"
        };
        panel.Children.Add(hint);

        TextBox termsBox = new()
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 200
        };
        ScrollViewer.SetVerticalScrollBarVisibility(termsBox, ScrollBarVisibility.Auto);

        if (ViewModel.CustomTerms.Count > 0)
        {
            termsBox.Text = string.Join("\n", ViewModel.CustomTerms.Select(t => $"{t.Key}->{t.Value}"));
        }

        panel.Children.Add(termsBox);

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "自定术语表",
            Content = panel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消"
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.SaveCustomTermsAsync(termsBox.Text);
        }
    }

    private async Task ShowExtraRequirementsDialogAsync()
    {
        StackPanel panel = new() { Spacing = 8 };

        TextBlock hint = new()
        {
            Text = "输入额外翻译要求（可换行）"
        };
        panel.Children.Add(hint);

        TextBox extraBox = new()
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 150,
            Text = ViewModel.ExtraRequirements
        };
        ScrollViewer.SetVerticalScrollBarVisibility(extraBox, ScrollBarVisibility.Auto);
        panel.Children.Add(extraBox);

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "额外要求",
            Content = panel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消"
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.SaveExtraRequirementsAsync(extraBox.Text);
        }
    }

    private async Task ShowLlmSettingsDialogAsync()
    {
        try
        {
            StackPanel panel = new() { Spacing = 8 };

            Grid endpointGrid = new() { ColumnSpacing = 8 };
            endpointGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            endpointGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock endpointLabel = new() { Text = "接口地址：", VerticalAlignment = VerticalAlignment.Center };
            TextBox endpointBox = new() { Text = ViewModel.LlmEndpoint, PlaceholderText = "https://api.example.com" };
            endpointGrid.Children.Add(endpointLabel);
            Grid.SetColumn(endpointLabel, 0);
            endpointGrid.Children.Add(endpointBox);
            Grid.SetColumn(endpointBox, 1);
            panel.Children.Add(endpointGrid);

            Grid apiKeyGrid = new() { ColumnSpacing = 8 };
            apiKeyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            apiKeyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock apiKeyLabel = new() { Text = "API Key：", VerticalAlignment = VerticalAlignment.Center };
            TextBox apiKeyBox = new() { Text = ViewModel.LlmApiKey };
            apiKeyGrid.Children.Add(apiKeyLabel);
            Grid.SetColumn(apiKeyLabel, 0);
            apiKeyGrid.Children.Add(apiKeyBox);
            Grid.SetColumn(apiKeyBox, 1);
            panel.Children.Add(apiKeyGrid);

            Grid modelGrid = new() { ColumnSpacing = 8 };
            modelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            modelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock modelLabel = new() { Text = "模型名称：", VerticalAlignment = VerticalAlignment.Center };
            TextBox modelNameBox = new() { Text = ViewModel.LlmModelName, PlaceholderText = "输入或选择模型" };
            Button fetchModelsButton = new() { Content = "获取列表" };
            modelGrid.Children.Add(modelLabel);
            Grid.SetColumn(modelLabel, 0);
            modelGrid.Children.Add(modelNameBox);
            Grid.SetColumn(modelNameBox, 1);
            modelGrid.Children.Add(fetchModelsButton);
            Grid.SetColumn(fetchModelsButton, 2);
            panel.Children.Add(modelGrid);

            StackPanel modelListPanel = new() { Spacing = 4, Visibility = Visibility.Collapsed };
            ScrollViewer modelListScrollViewer = new()
            {
                Content = modelListPanel,
                MaxHeight = 200,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Visibility = Visibility.Collapsed
            };
            panel.Children.Add(modelListScrollViewer);

            TextBlock statusText = new() { Visibility = Visibility.Collapsed, TextWrapping = TextWrapping.Wrap };
            panel.Children.Add(statusText);

            StackPanel buttonPanel = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
            Button testButton = new() { Content = "测试连接" };
            TextBlock testResultText = new() { VerticalAlignment = VerticalAlignment.Center };
            buttonPanel.Children.Add(testButton);
            buttonPanel.Children.Add(testResultText);
            panel.Children.Add(buttonPanel);

            XamlRoot? currentXamlRoot = XamlRoot;
            string savedEndpoint = ViewModel.LlmEndpoint;
            string savedApiKey = ViewModel.LlmApiKey;
            string savedModelName = ViewModel.LlmModelName;

            fetchModelsButton.Click += async (s, args) =>
            {
                try
                {
                    fetchModelsButton.IsEnabled = false;
                    fetchModelsButton.Content = "获取中…";
                    modelListPanel.Children.Clear();
                    modelListPanel.Visibility = Visibility.Collapsed;
                    modelListScrollViewer.Visibility = Visibility.Collapsed;
                    statusText.Visibility = Visibility.Collapsed;

                    string endpoint = endpointBox.Text ?? string.Empty;
                    string apiKey = apiKeyBox.Text ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
                    {
                        statusText.Text = "请先输入接口地址和API Key";
                        statusText.Visibility = Visibility.Visible;
                        return;
                    }

                    List<string> models = await ViewModel.GetModelListAsync(endpoint, apiKey);

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            if (models.Count > 0)
                            {
                                foreach (string model in models)
                                {
                                    RadioButton radio = new() { Content = model, Tag = model };
                                    if (model == modelNameBox.Text)
                                    {
                                        radio.IsChecked = true;
                                    }
                                    radio.Checked += (sender, e) =>
                                    {
                                        modelNameBox.Text = radio.Tag?.ToString() ?? string.Empty;
                                    };
                                    modelListPanel.Children.Add(radio);
                                }

                                modelListPanel.Visibility = Visibility.Visible;
                                modelListScrollViewer.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                statusText.Text = "未获取到可用模型";
                                statusText.Visibility = Visibility.Visible;
                            }
                        }
                        catch (Exception ex)
                        {
                            statusText.Text = $"显示模型列表失败: {ex.Message}";
                            statusText.Visibility = Visibility.Visible;
                        }
                    });
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        statusText.Text = $"获取失败: {ex.Message}";
                        statusText.Visibility = Visibility.Visible;
                    });
                }
                finally
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        fetchModelsButton.IsEnabled = true;
                        fetchModelsButton.Content = "获取列表";
                    });
                }
            };

            testButton.Click += async (s, args) =>
            {
                try
                {
                    testButton.IsEnabled = false;
                    testButton.Content = "测试中…";
                    testResultText.Text = "";

                    string endpoint = endpointBox.Text ?? string.Empty;
                    string apiKey = apiKeyBox.Text ?? string.Empty;
                    string modelName = modelNameBox.Text ?? string.Empty;

                    bool success = await ViewModel.TestConnectionAsync(endpoint, apiKey, modelName);

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        testResultText.Text = success ? "连接成功" : "连接失败";
                    });
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        testResultText.Text = $"错误：{ex.Message}";
                    });
                }
                finally
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        testButton.IsEnabled = true;
                        testButton.Content = "测试连接";
                    });
                }
            };

            ContentDialog dialog = new()
            {
                XamlRoot = currentXamlRoot,
                Title = "大模型接口设置",
                Content = panel,
                PrimaryButtonText = "保存",
                CloseButtonText = "取消"
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.SaveLlmSettingsAsync(endpointBox.Text ?? string.Empty, apiKeyBox.Text ?? string.Empty, modelNameBox.Text ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            try
            {
                ContentDialog errorDialog = new()
                {
                    XamlRoot = XamlRoot,
                    Title = "错误",
                    Content = $"打开设置对话框失败: {ex.Message}",
                    CloseButtonText = "确定"
                };
                await errorDialog.ShowAsync();
            }
            catch
            {
            }
        }
    }

    private async Task ShowPromptSettingsDialogAsync()
    {
        StackPanel panel = new() { Spacing = 8 };

        TextBlock hint = new()
        {
            Text = "自定义发送翻译请求时的系统提示词",
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(hint);

        TextBox promptBox = new()
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Text = ViewModel.SystemPrompt,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
        };
        ScrollViewer.SetVerticalScrollBarVisibility(promptBox, ScrollBarVisibility.Auto);

        ScrollViewer scrollViewer = new()
        {
            Content = promptBox,
            MaxHeight = 400,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        panel.Children.Add(scrollViewer);

        Button resetButton = new()
        {
            Content = "还原初始提示词",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        resetButton.Click += (s, args) =>
        {
            ViewModel.ResetSystemPrompt();
            promptBox.Text = ViewModel.SystemPrompt;
        };
        panel.Children.Add(resetButton);

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "提示词设置",
            Content = panel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消"
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.SaveSystemPromptAsync(promptBox.Text);
        }
    }

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

    private async Task ShowToastAsync(string message, bool isError)
    {
        ToastInfoBar.Severity = isError ? InfoBarSeverity.Error : InfoBarSeverity.Success;
        ToastInfoBar.Message = message;
        ToastInfoBar.IsOpen = true;
        await Task.Delay(1800);
        ToastInfoBar.IsOpen = false;
    }
}
