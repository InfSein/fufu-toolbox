using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using fufu_toolbox.Models;

namespace fufu_toolbox.Services;

public interface IPortManagementService
{
    Task<IReadOnlyList<PortOccupancyItem>> GetPortOccupancyAsync(string keyword);

    Task<PortTerminateResult> TerminateProcessAsync(int processId);
}

public sealed class PortTerminateResult
{
    public bool IsSuccess { get; init; }

    public bool IsElevationTriggered { get; init; }

    public string Message { get; init; } = string.Empty;
}

public sealed class PortManagementService : IPortManagementService
{
    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_SMALLICON = 0x000000001;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);
    private static readonly ImageSource DefaultExeIcon = CreateDefaultExeIcon();

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        out ShFileInfo psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    // 获取本机当前被占用端口与对应进程信息。
    public async Task<IReadOnlyList<PortOccupancyItem>> GetPortOccupancyAsync(string keyword)
    {
        List<(string Protocol, int Port, int ProcessId)> rows = new();
        rows.AddRange(await ReadNetstatAsync("tcp"));
        rows.AddRange(await ReadNetstatAsync("udp"));

        IEnumerable<(string Protocol, int Port, int ProcessId)> distinctRows = rows
            .Where(row => row.Port > 0 && row.ProcessId > 0)
            .DistinctBy(row => $"{row.Protocol}:{row.Port}:{row.ProcessId}");

        List<PortOccupancyItem> items = new();

        foreach ((string protocol, int port, int processId) in distinctRows)
        {
            (string processName, string executablePath) = GetProcessInfo(processId);
            ImageSource icon = await TryGetProcessIconAsync(executablePath) ?? DefaultExeIcon;

            items.Add(new PortOccupancyItem
            {
                Protocol = protocol,
                Port = port,
                ProcessId = processId,
                ProcessName = processName,
                ExecutablePath = executablePath,
                ProcessIcon = icon
            });
        }

        IEnumerable<PortOccupancyItem> filtered = FilterItems(items, keyword);

        return filtered
            .OrderBy(item => item.Port)
            .ThenBy(item => item.ProcessName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<PortTerminateResult> TerminateProcessAsync(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            process.Kill(true);
            await process.WaitForExitAsync();

            return new PortTerminateResult
            {
                IsSuccess = true,
                Message = $"已中止 PID {processId}。"
            };
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
        {
            bool triggered = TryTriggerElevatedTermination(processId);
            return new PortTerminateResult
            {
                IsSuccess = false,
                IsElevationTriggered = triggered,
                Message = triggered
                    ? "当前权限不足，已尝试触发管理员权限来中止该进程。"
                    : "当前权限不足，未能触发管理员提权。"
            };
        }
        catch (InvalidOperationException)
        {
            return new PortTerminateResult
            {
                IsSuccess = true,
                Message = "进程已退出。"
            };
        }
        catch (Exception ex)
        {
            return new PortTerminateResult
            {
                IsSuccess = false,
                Message = $"中止失败：{ex.Message}"
            };
        }
    }

    private static async Task<IReadOnlyList<(string Protocol, int Port, int ProcessId)>> ReadNetstatAsync(string protocol)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "netstat",
            Arguments = $"-ano -p {protocol}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using Process process = new() { StartInfo = startInfo };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        List<(string Protocol, int Port, int ProcessId)> result = new();

        foreach (string line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            string normalized = WhitespaceRegex.Replace(line.Trim(), " ");
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            string[] columns = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (protocol.Equals("tcp", StringComparison.OrdinalIgnoreCase))
            {
                if (columns.Length < 5 || !columns[0].Equals("TCP", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!columns[3].Equals("LISTENING", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int port = ParsePort(columns[1]);
                if (!int.TryParse(columns[4], out int processId))
                {
                    continue;
                }

                result.Add(("TCP", port, processId));
                continue;
            }

            if (columns.Length < 4 || !columns[0].Equals("UDP", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int udpPort = ParsePort(columns[1]);
            if (!int.TryParse(columns[3], out int udpProcessId))
            {
                continue;
            }

            result.Add(("UDP", udpPort, udpProcessId));
        }

        return result;
    }

    private static int ParsePort(string endpoint)
    {
        int separatorIndex = endpoint.LastIndexOf(':');
        if (separatorIndex < 0 || separatorIndex >= endpoint.Length - 1)
        {
            return -1;
        }

        string portText = endpoint[(separatorIndex + 1)..];
        return int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port) ? port : -1;
    }

    private static (string ProcessName, string ExecutablePath) GetProcessInfo(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            string processName = process.ProcessName;

            try
            {
                string path = process.MainModule?.FileName ?? "路径不可访问";
                return (processName, path);
            }
            catch
            {
                return (processName, "路径不可访问");
            }
        }
        catch
        {
            return ("未知进程", "路径不可访问");
        }
    }

    private static async Task<ImageSource?> TryGetProcessIconAsync(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || executablePath == "路径不可访问" || !File.Exists(executablePath))
        {
            return null;
        }

        try
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(executablePath);
            using StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 32, ThumbnailOptions.UseCurrentScale);
            if (thumbnail is null)
            {
                return null;
            }

            BitmapImage image = new();
            await image.SetSourceAsync(thumbnail);
            return image;
        }
        catch
        {
            return null;
        }
    }

    // 获取 Windows 对 .exe 文件类型的默认图标。
    private static ImageSource CreateDefaultExeIcon()
    {
        try
        {
            SHGetFileInfo(
                ".exe",
                FILE_ATTRIBUTE_NORMAL,
                out ShFileInfo fileInfo,
                (uint)Marshal.SizeOf<ShFileInfo>(),
                SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

            if (fileInfo.hIcon == IntPtr.Zero)
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Square44x44Logo.scale-200.png"));
            }

            using Icon icon = (Icon)Icon.FromHandle(fileInfo.hIcon).Clone();
            DestroyIcon(fileInfo.hIcon);

            using MemoryStream stream = new();
            using Bitmap bitmap = icon.ToBitmap();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Position = 0;

            using InMemoryRandomAccessStream ras = new();
            using Stream outStream = ras.AsStreamForWrite();
            stream.CopyTo(outStream);
            outStream.Flush();
            ras.Seek(0);

            BitmapImage image = new();
            image.SetSource(ras);
            return image;
        }
        catch
        {
            return new BitmapImage(new Uri("ms-appx:///Assets/Square44x44Logo.scale-200.png"));
        }
    }

    private static IEnumerable<PortOccupancyItem> FilterItems(IEnumerable<PortOccupancyItem> items, string keyword)
    {
        string text = keyword?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return items;
        }

        return items.Where(item =>
            item.Port.ToString(CultureInfo.InvariantCulture).Contains(text, StringComparison.OrdinalIgnoreCase) ||
            item.ProcessName.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryTriggerElevatedTermination(int processId)
    {
        try
        {
            string killCommand = $"Stop-Process -Id {processId} -Force";
            ProcessStartInfo startInfo = new()
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{killCommand}\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
