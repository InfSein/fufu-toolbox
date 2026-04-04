using fufu_toolbox.Services;

namespace fufu_toolbox.Models;

public sealed class ThemeOptionItem
{
    public required AppThemeMode Mode { get; init; }

    public required string DisplayName { get; init; }
}
