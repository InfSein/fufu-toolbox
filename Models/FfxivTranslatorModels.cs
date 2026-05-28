using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fufu_toolbox.Models;

public sealed class FfxivLlmSettings
{
    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;
}

public sealed class FfxivTranslationRequest
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("terms")]
    public Dictionary<string, string> Terms { get; set; } = new();

    [JsonPropertyName("extra")]
    public string Extra { get; set; } = string.Empty;
}

public sealed class FfxivTranslationResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("unknown_terms")]
    public List<string> UnknownTerms { get; set; } = new();
}

public sealed class FfxivTranslatorSettings
{
    public FfxivLlmSettings LlmSettings { get; set; } = new();

    public Dictionary<string, string> PermanentTerms { get; set; } = new();

    public Dictionary<string, string> CustomTerms { get; set; } = new();

    public string ExtraRequirements { get; set; } = string.Empty;

    public string SystemPrompt { get; set; } = string.Empty;
}

public sealed class FfxivTranslationLogEntry
{
    public DateTime Timestamp { get; set; }

    public string RequestSent { get; set; } = string.Empty;

    public string ResponseReceived { get; set; } = string.Empty;
}
