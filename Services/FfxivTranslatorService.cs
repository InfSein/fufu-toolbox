using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using fufu_toolbox.Models;

namespace fufu_toolbox.Services;

public interface IFfxivTranslatorService
{
    Task<FfxivTranslatorSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);

    Task SaveSettingsAsync(FfxivTranslatorSettings settings, CancellationToken cancellationToken = default);

    Task<Dictionary<string, string>> ImportPermanentTermsAsync(CancellationToken cancellationToken = default);

    Task<FfxivTranslationResponse> TranslateAsync(string content, Dictionary<string, string> terms, string extraRequirements, string systemPrompt, CancellationToken cancellationToken = default);

    Task<List<string>> GetModelListAsync(string endpoint, string apiKey, CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(string endpoint, string apiKey, string modelName, CancellationToken cancellationToken = default);

    string GetDefaultSystemPrompt();

    List<FfxivTranslationLogEntry> GetTranslationLogs();

    void ClearTranslationLogs();
}

public sealed class FfxivTranslatorService : IFfxivTranslatorService
{
    private const string SettingsFileName = "ffxiv_translator_settings.json";
    private const string DefaultPromptResourceName = "fufu_toolbox.Resources.default_prompt.md";

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(120)
    };

    private readonly List<FfxivTranslationLogEntry> _translationLogs = new();

    private static string? _cachedDefaultPrompt;

    public string GetDefaultSystemPrompt()
    {
        if (_cachedDefaultPrompt is not null)
        {
            return _cachedDefaultPrompt;
        }

        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream(DefaultPromptResourceName);
            if (stream is not null)
            {
                using StreamReader reader = new(stream, Encoding.UTF8);
                _cachedDefaultPrompt = reader.ReadToEnd();
                return _cachedDefaultPrompt;
            }
        }
        catch
        {
        }

        _cachedDefaultPrompt = string.Empty;
        return _cachedDefaultPrompt;
    }

    public async Task<FfxivTranslatorSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile? file = await localFolder.TryGetItemAsync(SettingsFileName) as StorageFile;
            if (file is null)
            {
                return CreateDefaultSettings();
            }

            string json = await FileIO.ReadTextAsync(file);
            FfxivTranslatorSettings? settings = JsonSerializer.Deserialize<FfxivTranslatorSettings>(json);
            return settings ?? CreateDefaultSettings();
        }
        catch
        {
            return CreateDefaultSettings();
        }
    }

    public async Task SaveSettingsAsync(FfxivTranslatorSettings settings, CancellationToken cancellationToken = default)
    {
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        StorageFile file = await localFolder.CreateFileAsync(SettingsFileName, CreationCollisionOption.ReplaceExisting);

        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(settings, options);
        await FileIO.WriteTextAsync(file, json);
    }

    public async Task<Dictionary<string, string>> ImportPermanentTermsAsync(CancellationToken cancellationToken = default)
    {
        FileOpenPicker picker = new();
        picker.FileTypeFilter.Add(".json");

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.GetService<MainWindow>());
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        StorageFile file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return new Dictionary<string, string>();
        }

        string json = await FileIO.ReadTextAsync(file);
        Dictionary<string, string>? terms = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return terms ?? new Dictionary<string, string>();
    }

    public async Task<FfxivTranslationResponse> TranslateAsync(string content, Dictionary<string, string> terms, string extraRequirements, string systemPrompt, CancellationToken cancellationToken = default)
    {
        FfxivTranslationRequest request = new()
        {
            Content = content,
            Terms = terms,
            Extra = extraRequirements
        };

        FfxivTranslatorSettings settings = await LoadSettingsAsync(cancellationToken);
        FfxivLlmSettings llmSettings = settings.LlmSettings;

        if (string.IsNullOrWhiteSpace(llmSettings.Endpoint) ||
            string.IsNullOrWhiteSpace(llmSettings.ApiKey) ||
            string.IsNullOrWhiteSpace(llmSettings.ModelName))
        {
            throw new InvalidOperationException("请先配置大模型接口设置。");
        }

        JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string requestJson = JsonSerializer.Serialize(request, jsonOptions);

        List<ChatMessage> messages = new()
        {
            new ChatMessage { Role = "system", Content = systemPrompt },
            new ChatMessage { Role = "user", Content = requestJson }
        };

        ChatCompletionRequest chatRequest = new()
        {
            Model = llmSettings.ModelName,
            Messages = messages,
            Temperature = 0.3f
        };

        var ep = llmSettings.Endpoint.TrimEnd('/');
        if (ep.EndsWith("/v1"))
            ep = ep[..^3];
        string endpoint = ep;
        if (!endpoint.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            endpoint += "/v1/chat/completions";
        }

        string requestContent = JsonSerializer.Serialize(chatRequest, jsonOptions);

        FfxivTranslationLogEntry logEntry = new()
        {
            Timestamp = DateTime.Now,
            RequestSent = requestContent
        };

        HttpRequestMessage httpRequest = new(HttpMethod.Post, endpoint);
        httpRequest.Content = new StringContent(requestContent, Encoding.UTF8, "application/json");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", llmSettings.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            var responseDoc = System.Text.Json.JsonDocument.Parse(responseJson);
            logEntry.ResponseReceived = System.Text.Json.JsonSerializer.Serialize(responseDoc, jsonOptions);
        }
        catch
        {
            logEntry.ResponseReceived = responseJson;
        }

        _translationLogs.Add(logEntry);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"API请求失败 ({(int)response.StatusCode}): {responseJson}");
        }

        ChatCompletionResponse? chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);
        if (chatResponse?.Choices is null || chatResponse.Choices.Count == 0)
        {
            throw new Exception("API返回了空结果。");
        }

        string assistantMessage = chatResponse.Choices[0].Message?.Content ?? string.Empty;

        FfxivTranslationResponse translationResponse;

        try
        {
            translationResponse = JsonSerializer.Deserialize<FfxivTranslationResponse>(assistantMessage) ?? new FfxivTranslationResponse
            {
                Result = assistantMessage,
                UnknownTerms = new List<string>()
            };
        }
        catch
        {
            translationResponse = new FfxivTranslationResponse
            {
                Result = assistantMessage,
                UnknownTerms = new List<string>()
            };
        }

        return translationResponse;
    }

    public async Task<List<string>> GetModelListAsync(string endpoint, string apiKey, CancellationToken cancellationToken = default)
    {
        var ep = endpoint.TrimEnd('/');
        if (ep.EndsWith("/v1"))
            ep = ep[..^3];
        
        string url = ep + "/v1/models";

        HttpRequestMessage httpRequest = new(HttpMethod.Get, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"获取模型列表失败 ({(int)response.StatusCode}): {responseJson}");
        }

        ModelListResponse? modelListResponse = JsonSerializer.Deserialize<ModelListResponse>(responseJson);
        if (modelListResponse?.Data is null)
        {
            return new List<string>();
        }

        return modelListResponse.Data.Select(m => m.Id).Where(id => !string.IsNullOrEmpty(id)).ToList()!;
    }

    public async Task<bool> TestConnectionAsync(string endpoint, string apiKey, string modelName, CancellationToken cancellationToken = default)
    {
        try
        {
            List<ChatMessage> messages = new()
            {
                new ChatMessage { Role = "user", Content = "Hello" }
            };

            ChatCompletionRequest chatRequest = new()
            {
                Model = modelName,
                Messages = messages,
                MaxTokens = 10
            };

            var ep = endpoint.TrimEnd('/');
            if (ep.EndsWith("/v1"))
                ep = ep[..^3];
            string url = ep;
            if (!url.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                url += "/v1/chat/completions";
            }

            string requestContent = JsonSerializer.Serialize(chatRequest);
            HttpRequestMessage httpRequest = new(HttpMethod.Post, url);
            httpRequest.Content = new StringContent(requestContent, Encoding.UTF8, "application/json");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private FfxivTranslatorSettings CreateDefaultSettings()
    {
        return new FfxivTranslatorSettings
        {
            SystemPrompt = GetDefaultSystemPrompt()
        };
    }

    public List<FfxivTranslationLogEntry> GetTranslationLogs()
    {
        return new List<FfxivTranslationLogEntry>(_translationLogs);
    }

    public void ClearTranslationLogs()
    {
        _translationLogs.Clear();
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = new();

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; } = 0.3f;

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }

    private sealed class ModelListResponse
    {
        [JsonPropertyName("data")]
        public List<ModelInfo>? Data { get; set; }
    }

    private sealed class ModelInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
