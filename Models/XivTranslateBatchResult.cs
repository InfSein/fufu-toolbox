namespace fufu_toolbox.Models;

public sealed class XivTranslateBatchResult
{
    public string OutputText { get; init; } = string.Empty;

    public int TotalCount { get; init; }

    public int SuccessCount { get; init; }

    public int FailedCount { get; init; }

    public string FailureSummary { get; init; } = string.Empty;
}
