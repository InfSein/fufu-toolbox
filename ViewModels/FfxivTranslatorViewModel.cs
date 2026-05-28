using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using fufu_toolbox.Models;
using fufu_toolbox.Services;

namespace fufu_toolbox.ViewModels;

public sealed class FfxivTranslatorViewModel : INotifyPropertyChanged
{
    private readonly IFfxivTranslatorService _translatorService;

    private string _inputText = string.Empty;
    private string _outputText = string.Empty;
    private string _extraRequirements = string.Empty;
    private string _systemPrompt = string.Empty;
    private string _llmEndpoint = string.Empty;
    private string _llmApiKey = string.Empty;
    private string _llmModelName = string.Empty;
    private Dictionary<string, string> _permanentTerms = new();
    private Dictionary<string, string> _customTerms = new();
    private bool _isTranslating;
    private FfxivTranslatorSettings _settings = new();

    public FfxivTranslatorViewModel(IFfxivTranslatorService translatorService)
    {
        _translatorService = translatorService;
        _systemPrompt = translatorService.GetDefaultSystemPrompt();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string InputText
    {
        get => _inputText;
        set
        {
            if (_inputText == value) return;
            _inputText = value;
            OnPropertyChanged(nameof(InputText));
            OnPropertyChanged(nameof(CanTranslate));
        }
    }

    public string OutputText
    {
        get => _outputText;
        set
        {
            if (_outputText == value) return;
            _outputText = value;
            OnPropertyChanged(nameof(OutputText));
        }
    }

    public string ExtraRequirements
    {
        get => _extraRequirements;
        set
        {
            if (_extraRequirements == value) return;
            _extraRequirements = value;
            OnPropertyChanged(nameof(ExtraRequirements));
        }
    }

    public string SystemPrompt
    {
        get => _systemPrompt;
        set
        {
            if (_systemPrompt == value) return;
            _systemPrompt = value;
            OnPropertyChanged(nameof(SystemPrompt));
        }
    }

    public string LlmEndpoint
    {
        get => _llmEndpoint;
        set
        {
            if (_llmEndpoint == value) return;
            _llmEndpoint = value;
            OnPropertyChanged(nameof(LlmEndpoint));
        }
    }

    public string LlmApiKey
    {
        get => _llmApiKey;
        set
        {
            if (_llmApiKey == value) return;
            _llmApiKey = value;
            OnPropertyChanged(nameof(LlmApiKey));
        }
    }

    public string LlmModelName
    {
        get => _llmModelName;
        set
        {
            if (_llmModelName == value) return;
            _llmModelName = value;
            OnPropertyChanged(nameof(LlmModelName));
        }
    }

    public Dictionary<string, string> PermanentTerms
    {
        get => _permanentTerms;
        set
        {
            _permanentTerms = value;
            OnPropertyChanged(nameof(PermanentTerms));
            OnPropertyChanged(nameof(PermanentTermsDisplay));
        }
    }

    public Dictionary<string, string> CustomTerms
    {
        get => _customTerms;
        set
        {
            _customTerms = value;
            OnPropertyChanged(nameof(CustomTerms));
            OnPropertyChanged(nameof(CustomTermsDisplay));
        }
    }

    public bool IsTranslating
    {
        get => _isTranslating;
        private set
        {
            if (_isTranslating == value) return;
            _isTranslating = value;
            OnPropertyChanged(nameof(IsTranslating));
            OnPropertyChanged(nameof(CanTranslate));
            OnPropertyChanged(nameof(TranslateButtonText));
        }
    }

    public bool CanTranslate => !IsTranslating && !string.IsNullOrWhiteSpace(InputText);

    public string TranslateButtonText => IsTranslating ? "翻译中…" : "执行";

    public string PermanentTermsDisplay => FormatTermsDisplay(_permanentTerms);

    public string CustomTermsDisplay => FormatTermsDisplay(_customTerms);

    public async Task InitializeAsync()
    {
        _settings = await _translatorService.LoadSettingsAsync();

        PermanentTerms = new Dictionary<string, string>(_settings.PermanentTerms);
        CustomTerms = new Dictionary<string, string>(_settings.CustomTerms);
        ExtraRequirements = _settings.ExtraRequirements;
        SystemPrompt = string.IsNullOrEmpty(_settings.SystemPrompt)
            ? _translatorService.GetDefaultSystemPrompt()
            : _settings.SystemPrompt;

        LlmEndpoint = _settings.LlmSettings.Endpoint;
        LlmApiKey = _settings.LlmSettings.ApiKey;
        LlmModelName = _settings.LlmSettings.ModelName;
    }

    public async Task ImportPermanentTermsAsync()
    {
        Dictionary<string, string> imported = await _translatorService.ImportPermanentTermsAsync();
        if (imported.Count > 0)
        {
            PermanentTerms = imported;
            _settings.PermanentTerms = imported;
            await SaveSettingsAsync();
        }
    }

    public void UpdateCustomTerms(string termsText)
    {
        Dictionary<string, string> terms = new();
        string[] lines = termsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;

            int arrowIndex = trimmedLine.IndexOf("->", StringComparison.Ordinal);
            if (arrowIndex > 0 && arrowIndex < trimmedLine.Length - 2)
            {
                string key = trimmedLine[..arrowIndex].Trim();
                string value = trimmedLine[(arrowIndex + 2)..].Trim();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    terms[key] = value;
                }
            }
        }

        CustomTerms = terms;
        _settings.CustomTerms = terms;
    }

    public async Task SaveCustomTermsAsync(string termsText)
    {
        UpdateCustomTerms(termsText);
        await SaveSettingsAsync();
    }

    public async Task SaveExtraRequirementsAsync(string extra)
    {
        ExtraRequirements = extra;
        _settings.ExtraRequirements = extra;
        await SaveSettingsAsync();
    }

    public async Task SaveLlmSettingsAsync(string endpoint, string apiKey, string modelName)
    {
        LlmEndpoint = endpoint;
        LlmApiKey = apiKey;
        LlmModelName = modelName;

        _settings.LlmSettings = new FfxivLlmSettings
        {
            Endpoint = endpoint,
            ApiKey = apiKey,
            ModelName = modelName
        };

        await SaveSettingsAsync();
    }

    public async Task SaveSystemPromptAsync(string prompt)
    {
        SystemPrompt = prompt;
        _settings.SystemPrompt = prompt;
        await SaveSettingsAsync();
    }

    public void ResetSystemPrompt()
    {
        SystemPrompt = _translatorService.GetDefaultSystemPrompt();
    }

    public async Task<List<string>> GetModelListAsync(string endpoint, string apiKey)
    {
        return await _translatorService.GetModelListAsync(endpoint, apiKey);
    }

    public async Task<bool> TestConnectionAsync(string endpoint, string apiKey, string modelName)
    {
        return await _translatorService.TestConnectionAsync(endpoint, apiKey, modelName);
    }

    public List<FfxivTranslationLogEntry> GetTranslationLogs()
    {
        return _translatorService.GetTranslationLogs();
    }

    public void ClearTranslationLogs()
    {
        _translatorService.ClearTranslationLogs();
    }

    public async Task<List<string>> TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return new List<string>();

        IsTranslating = true;

        try
        {
            Dictionary<string, string> matchedTerms = GetMatchedTerms(InputText);

            FfxivTranslationResponse response = await _translatorService.TranslateAsync(
                InputText,
                matchedTerms,
                ExtraRequirements,
                SystemPrompt);

            OutputText = response.Result;

            return response.UnknownTerms;
        }
        catch
        {
            throw;
        }
        finally
        {
            IsTranslating = false;
        }
    }

    public async Task AddCustomTermsAsync(Dictionary<string, string> newTerms)
    {
        foreach (var term in newTerms)
        {
            CustomTerms[term.Key] = term.Value;
        }
        _settings.CustomTerms = CustomTerms;
        await SaveSettingsAsync();
    }

    private Dictionary<string, string> GetMatchedTerms(string content)
    {
        Dictionary<string, string> mergedTerms = new();

        foreach (var term in PermanentTerms)
        {
            mergedTerms[term.Key] = term.Value;
        }

        foreach (var term in CustomTerms)
        {
            mergedTerms[term.Key] = term.Value;
        }

        Dictionary<string, string> matchedTerms = new();
        foreach (var term in mergedTerms)
        {
            if (content.Contains(term.Key, StringComparison.Ordinal))
            {
                matchedTerms[term.Key] = term.Value;
            }
        }

        return matchedTerms;
    }

    private string FormatTermsDisplay(Dictionary<string, string> terms)
    {
        if (terms.Count == 0) return "（无）";
        return string.Join("\n", terms.Select(t => $"{t.Key} -> {t.Value}"));
    }

    private async Task SaveSettingsAsync()
    {
        _settings.PermanentTerms = PermanentTerms;
        _settings.CustomTerms = CustomTerms;
        _settings.ExtraRequirements = ExtraRequirements;
        _settings.SystemPrompt = SystemPrompt;
        _settings.LlmSettings = new FfxivLlmSettings
        {
            Endpoint = LlmEndpoint,
            ApiKey = LlmApiKey,
            ModelName = LlmModelName
        };

        await _translatorService.SaveSettingsAsync(_settings);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
