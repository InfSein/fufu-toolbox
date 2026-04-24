using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using fufu_toolbox.Models;
using fufu_toolbox.Services;

namespace fufu_toolbox.ViewModels;

public sealed class XivItemTranslatorViewModel : INotifyPropertyChanged
{
    private readonly IXivItemTranslationService _translationService;
    private readonly IReadOnlyDictionary<string, string> _languageCodeMap = new Dictionary<string, string>
    {
        ["中文"] = "zh",
        ["英文"] = "en",
        ["日文"] = "ja"
    };

    private string _selectedInputLanguage = "日文";
    private string _selectedOutputLanguage = "中文";
    private string _ignoredPrefix = "[*]";
    private string _inputText = string.Empty;
    private string _outputText = string.Empty;
    private bool _keepPrefix = true;
    private bool _isTranslating;
    private int _progressValue;
    private int _progressMaximum = 1;
    private string _progressText = "0 / 0";

    public XivItemTranslatorViewModel(IXivItemTranslationService translationService)
    {
        _translationService = translationService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> InputLanguages { get; } = ["日文", "英文"];

    public IReadOnlyList<string> OutputLanguages { get; } = ["中文"];

    public string SelectedInputLanguage
    {
        get => _selectedInputLanguage;
        set
        {
            if (_selectedInputLanguage == value)
            {
                return;
            }

            _selectedInputLanguage = value;
            OnPropertyChanged(nameof(SelectedInputLanguage));
        }
    }

    public string SelectedOutputLanguage
    {
        get => _selectedOutputLanguage;
        set
        {
            if (_selectedOutputLanguage == value)
            {
                return;
            }

            _selectedOutputLanguage = value;
            OnPropertyChanged(nameof(SelectedOutputLanguage));
        }
    }

    public string IgnoredPrefix
    {
        get => _ignoredPrefix;
        set
        {
            if (_ignoredPrefix == value)
            {
                return;
            }

            _ignoredPrefix = value;
            OnPropertyChanged(nameof(IgnoredPrefix));
        }
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (_inputText == value)
            {
                return;
            }

            _inputText = value;
            OnPropertyChanged(nameof(InputText));
            OnPropertyChanged(nameof(CanTranslate));
        }
    }

    public string OutputText
    {
        get => _outputText;
        private set
        {
            if (_outputText == value)
            {
                return;
            }

            _outputText = value;
            OnPropertyChanged(nameof(OutputText));
        }
    }

    public bool KeepPrefix
    {
        get => _keepPrefix;
        set
        {
            if (_keepPrefix == value)
            {
                return;
            }

            _keepPrefix = value;
            OnPropertyChanged(nameof(KeepPrefix));
        }
    }

    public bool IsTranslating
    {
        get => _isTranslating;
        private set
        {
            if (_isTranslating == value)
            {
                return;
            }

            _isTranslating = value;
            OnPropertyChanged(nameof(IsTranslating));
            OnPropertyChanged(nameof(CanTranslate));
            OnPropertyChanged(nameof(TranslateButtonText));
        }
    }

    public int ProgressValue
    {
        get => _progressValue;
        private set
        {
            if (_progressValue == value)
            {
                return;
            }

            _progressValue = value;
            OnPropertyChanged(nameof(ProgressValue));
        }
    }

    public int ProgressMaximum
    {
        get => _progressMaximum;
        private set
        {
            if (_progressMaximum == value)
            {
                return;
            }

            _progressMaximum = value;
            OnPropertyChanged(nameof(ProgressMaximum));
        }
    }

    public string ProgressText
    {
        get => _progressText;
        private set
        {
            if (_progressText == value)
            {
                return;
            }

            _progressText = value;
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public bool CanTranslate => !IsTranslating && !string.IsNullOrWhiteSpace(InputText);

    public string TranslateButtonText => IsTranslating ? "翻译中…" : "开始翻译";

    // 按输入内容逐行翻译并返回执行结果。
    public async Task<XivTranslateBatchResult> TranslateAsync()
    {
        string inputLanguageCode = ParseLanguageCode(SelectedInputLanguage);
        string outputLanguageCode = ParseLanguageCode(SelectedOutputLanguage);

        string[] lines = SplitLines(InputText);
        ProgressMaximum = Math.Max(1, lines.Length);
        ProgressValue = 0;
        ProgressText = $"0 / {lines.Length}";

        List<string> outputLines = new(lines.Length);
        Dictionary<string, List<string>> failedItems = [];
        int successCount = 0;
        int failedCount = 0;

        IsTranslating = true;

        try
        {
            foreach (string rawLine in lines)
            {
                (string translatedLine, bool translatedSuccess, bool translatedFailed) = await TranslateLineAsync(rawLine, inputLanguageCode, outputLanguageCode, failedItems);
                if (translatedSuccess)
                {
                    successCount++;
                }
                if (translatedFailed)
                {
                    failedCount++;
                }

                outputLines.Add(translatedLine);
                ProgressValue++;
                ProgressText = $"{ProgressValue} / {lines.Length}";
            }
        }
        finally
        {
            IsTranslating = false;
        }

        string outputText = string.Join(Environment.NewLine, outputLines);
        OutputText = outputText;

        return new XivTranslateBatchResult
        {
            OutputText = outputText,
            TotalCount = lines.Length,
            SuccessCount = successCount,
            FailedCount = failedCount,
            FailureSummary = BuildFailureSummary(failedItems)
        };
    }

    // 按规则处理单行并翻译物品名。
    private async Task<(string OutputLine, bool TranslatedSuccess, bool TranslatedFailed)> TranslateLineAsync(string rawLine, string inputLanguageCode, string outputLanguageCode, Dictionary<string, List<string>> failedItems)
    {
        string line = rawLine ?? string.Empty;
        if (string.IsNullOrWhiteSpace(line))
        {
            return (line, false, false);
        }

        string linePrefix = string.Empty;
        string itemName = line.Trim();
        if (!string.IsNullOrEmpty(IgnoredPrefix) && line.StartsWith(IgnoredPrefix, StringComparison.Ordinal))
        {
            linePrefix = line[..IgnoredPrefix.Length];
            itemName = line[IgnoredPrefix.Length..].Trim();
        }

        if (string.IsNullOrWhiteSpace(itemName))
        {
            return (line, false, false);
        }

        XivTranslateLineResult result = await _translationService.TranslateItemNameAsync(itemName, inputLanguageCode, outputLanguageCode);
        if (!result.Success)
        {
            AddFailedItem(failedItems, result.ErrorMessage, itemName);
            return (line, false, true);
        }

        if (KeepPrefix && !string.IsNullOrEmpty(linePrefix))
        {
            return ($"{linePrefix}{result.TranslatedName}", true, false);
        }

        return (result.TranslatedName, true, false);
    }

    // 将文本拆分为逐行数组，保留空行位置。
    private static string[] SplitLines(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n');
    }

    // 记录失败原因与对应物品名。
    private static void AddFailedItem(Dictionary<string, List<string>> failedItems, string errorMessage, string itemName)
    {
        string key = string.IsNullOrWhiteSpace(errorMessage) ? "未知错误" : errorMessage;
        if (!failedItems.TryGetValue(key, out List<string>? names))
        {
            names = [];
            failedItems[key] = names;
        }

        names.Add(itemName);
    }

    // 把失败信息拼接成可读提示文本。
    private static string BuildFailureSummary(Dictionary<string, List<string>> failedItems)
    {
        if (failedItems.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        builder.AppendLine("以下物品翻译失败：");
        builder.AppendLine();

        foreach ((string reason, List<string> items) in failedItems)
        {
            builder.Append("■ ");
            builder.Append(items.Count);
            builder.Append(" 个，因为 ");
            builder.Append(reason);
            builder.Append("：");
            builder.AppendLine(string.Join("、", items));
        }

        return builder.ToString().TrimEnd();
    }

    // 将界面语言名映射为接口语言代码。
    private string ParseLanguageCode(string languageText)
    {
        if (_languageCodeMap.TryGetValue(languageText, out string? code))
        {
            return code;
        }

        return "unknown";
    }

    // 抛出属性变化通知。
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
