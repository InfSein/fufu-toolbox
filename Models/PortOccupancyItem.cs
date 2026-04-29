using Microsoft.UI.Xaml.Media;

namespace fufu_toolbox.Models;

public sealed class PortOccupancyItem
{
    public string Protocol { get; init; } = string.Empty;

    public int Port { get; init; }

    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = "未知进程";

    public string ExecutablePath { get; init; } = "路径不可访问";

    public ImageSource? ProcessIcon { get; init; }
}
