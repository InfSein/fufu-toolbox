namespace fufu_toolbox.Models;

public sealed class XivTranslateLineResult
{
    public bool Success { get; init; }

    public string TranslatedName { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;
}
