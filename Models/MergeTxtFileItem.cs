using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace fufu_toolbox.Models;

public sealed class MergeTxtFileItem : ObservableObject
{
    private bool _isSelected;
    private string _alias = string.Empty;
    private string _previewText = "(暂无预览)";

    // 创建一个可合并的文本文件项。
    public MergeTxtFileItem(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        FileTitle = Path.GetFileNameWithoutExtension(filePath);
    }

    public string FilePath { get; }

    public string FileName { get; }

    public string FileTitle { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Alias
    {
        get => _alias;
        set => SetProperty(ref _alias, value);
    }

    public string PreviewText
    {
        get => _previewText;
        set => SetProperty(ref _previewText, value);
    }

    // 返回合并时使用的标题。
    public string GetDisplayTitle()
    {
        if (Alias.Trim() == "-")
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(Alias) ? FileTitle : Alias.Trim();
    }
}
