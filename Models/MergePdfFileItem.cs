using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace fufu_toolbox.Models;

public sealed class MergePdfFileItem : ObservableObject
{
    private bool _isSelected;

    // 创建一个可合并的 PDF/图片文件项。
    public MergePdfFileItem(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        FileType = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
    }

    public string FilePath { get; }

    public string FileName { get; }

    public string FileType { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
