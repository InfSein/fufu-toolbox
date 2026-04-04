using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using fufu_toolbox.Models;

namespace fufu_toolbox.Services;

public interface ITxtMergeService
{
    Task<IReadOnlyList<string>> ScanTxtFilesAsync(string folderPath);

    Task<string> GetPreviewTextAsync(string filePath, int maxLines = 10);

    Task<string> BuildMergedContentAsync(IReadOnlyList<MergeTxtFileItem> filesInOrder);
}

public sealed class TxtMergeService : ITxtMergeService
{
    // 扫描目录及子目录中的所有 txt 文件，并按文件名自然排序。
    public Task<IReadOnlyList<string>> ScanTxtFilesAsync(string folderPath)
    {
        NaturalStringComparer comparer = NaturalStringComparer.Instance;

        IReadOnlyList<string> files = Directory
            .EnumerateFiles(folderPath, "*.txt", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetFileName(path), comparer)
            .ThenBy(path => path, comparer)
            .ToList();

        return Task.FromResult(files);
    }

    // 读取文件前若干行用于预览。
    public async Task<string> GetPreviewTextAsync(string filePath, int maxLines = 10)
    {
        try
        {
            List<string> lines = [];

            using StreamReader reader = new(filePath, Encoding.UTF8, true);
            for (int i = 0; i < maxLines; i++)
            {
                string? line = await reader.ReadLineAsync();
                if (line is null)
                {
                    break;
                }

                lines.Add(line);
            }

            bool hasMore = await reader.ReadLineAsync() is not null;
            if (lines.Count == 0)
            {
                return "(空文件)";
            }

            if (hasMore)
            {
                lines.Add("...");
            }

            return string.Join("\r\n", lines);
        }
        catch (Exception ex)
        {
            return $"(预览失败) {ex.Message}";
        }
    }

    // 按顺序拼接多个 txt 的标题与内容。
    public async Task<string> BuildMergedContentAsync(IReadOnlyList<MergeTxtFileItem> filesInOrder)
    {
        List<string> sections = new(filesInOrder.Count);

        foreach (MergeTxtFileItem item in filesInOrder)
        {
            string content = await File.ReadAllTextAsync(item.FilePath, Encoding.UTF8);
            string title = item.GetDisplayTitle();

            if (string.IsNullOrEmpty(title))
            {
                sections.Add(content);
            }
            else
            {
                sections.Add($"{title}\r\n\r\n{content}");
            }
        }

        return string.Join("\r\n\r\n\r\n", sections);
    }
}

internal sealed class NaturalStringComparer : IComparer<string>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        int i = 0;
        int j = 0;

        while (i < x.Length && j < y.Length)
        {
            char cx = x[i];
            char cy = y[j];
            bool dx = char.IsDigit(cx);
            bool dy = char.IsDigit(cy);

            if (dx && dy)
            {
                int cmp = CompareNumberRun(x, ref i, y, ref j);
                if (cmp != 0)
                {
                    return cmp;
                }

                continue;
            }

            int textEndX = i;
            while (textEndX < x.Length && !char.IsDigit(x[textEndX]))
            {
                textEndX++;
            }

            int textEndY = j;
            while (textEndY < y.Length && !char.IsDigit(y[textEndY]))
            {
                textEndY++;
            }

            string sx = x.Substring(i, textEndX - i);
            string sy = y.Substring(j, textEndY - j);
            int textCmp = string.Compare(sx, sy, StringComparison.CurrentCultureIgnoreCase);
            if (textCmp != 0)
            {
                return textCmp;
            }

            i = textEndX;
            j = textEndY;
        }

        return x.Length.CompareTo(y.Length);
    }

    // 比较同位置的数字片段。
    private static int CompareNumberRun(string x, ref int i, string y, ref int j)
    {
        int startX = i;
        while (i < x.Length && char.IsDigit(x[i]))
        {
            i++;
        }

        int startY = j;
        while (j < y.Length && char.IsDigit(y[j]))
        {
            j++;
        }

        string numX = x.Substring(startX, i - startX).TrimStart('0');
        string numY = y.Substring(startY, j - startY).TrimStart('0');
        numX = numX.Length == 0 ? "0" : numX;
        numY = numY.Length == 0 ? "0" : numY;

        if (numX.Length != numY.Length)
        {
            return numX.Length.CompareTo(numY.Length);
        }

        int cmp = string.Compare(numX, numY, StringComparison.Ordinal);
        if (cmp != 0)
        {
            return cmp;
        }

        int rawLenX = i - startX;
        int rawLenY = j - startY;
        return rawLenX.CompareTo(rawLenY);
    }
}

