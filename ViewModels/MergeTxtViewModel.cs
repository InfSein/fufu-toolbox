using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using fufu_toolbox.Models;
using fufu_toolbox.Services;

namespace fufu_toolbox.ViewModels;

public sealed class MergeTxtViewModel : INotifyPropertyChanged
{
    private readonly ITxtMergeService _txtMergeService;
    private string _selectedFolderPath = "未选择目录";
    private string _scanSummary = "请选择目录后开始扫描 txt 文件。";

    public MergeTxtViewModel(ITxtMergeService txtMergeService)
    {
        _txtMergeService = txtMergeService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MergeTxtFileItem> Files { get; } = [];

    public string SelectedFolderPath
    {
        get => _selectedFolderPath;
        private set
        {
            if (_selectedFolderPath == value)
            {
                return;
            }

            _selectedFolderPath = value;
            OnPropertyChanged(nameof(SelectedFolderPath));
        }
    }

    public string ScanSummary
    {
        get => _scanSummary;
        private set
        {
            if (_scanSummary == value)
            {
                return;
            }

            _scanSummary = value;
            OnPropertyChanged(nameof(ScanSummary));
        }
    }

    public int SelectedCount => Files.Count(item => item.IsSelected);

    public bool CanMerge => SelectedCount > 0;

    // 扫描指定目录并更新文件列表。
    public async Task ScanFolderAsync(string folderPath)
    {
        IReadOnlyList<string> filePaths = await _txtMergeService.ScanTxtFilesAsync(folderPath);

        foreach (MergeTxtFileItem oldItem in Files)
        {
            oldItem.PropertyChanged -= FileItem_PropertyChanged;
        }

        Files.Clear();

        foreach (string path in filePaths)
        {
            MergeTxtFileItem item = new(path)
            {
                IsSelected = true,
                PreviewText = await _txtMergeService.GetPreviewTextAsync(path, 10)
            };

            item.PropertyChanged += FileItem_PropertyChanged;
            Files.Add(item);
        }

        SelectedFolderPath = folderPath;
        RefreshSelectionState();
    }

    // 将文件项向上移动一位。
    public void MoveUp(MergeTxtFileItem item)
    {
        int index = Files.IndexOf(item);
        if (index <= 0)
        {
            return;
        }

        Files.Move(index, index - 1);
    }

    // 将文件项向下移动一位。
    public void MoveDown(MergeTxtFileItem item)
    {
        int index = Files.IndexOf(item);
        if (index < 0 || index >= Files.Count - 1)
        {
            return;
        }

        Files.Move(index, index + 1);
    }

    // 一键全选当前文件。
    public void SelectAll()
    {
        foreach (MergeTxtFileItem item in Files)
        {
            item.IsSelected = true;
        }

        RefreshSelectionState();
    }

    // 反转当前选择状态。
    public void InvertSelection()
    {
        foreach (MergeTxtFileItem item in Files)
        {
            item.IsSelected = !item.IsSelected;
        }

        RefreshSelectionState();
    }

    // 生成按当前顺序和选择状态拼接的文本内容。
    public Task<string> BuildMergedContentAsync()
    {
        List<MergeTxtFileItem> selected = Files.Where(item => item.IsSelected).ToList();
        return _txtMergeService.BuildMergedContentAsync(selected);
    }

    // 响应文件项选择变化并更新可合并状态。
    private void FileItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MergeTxtFileItem.IsSelected))
        {
            RefreshSelectionState();
        }
    }

    // 刷新统计文案和选择相关状态。
    private void RefreshSelectionState()
    {
        ScanSummary = $"共扫描到 {Files.Count} 个 txt 文件，已选择 {SelectedCount} 个。";
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CanMerge));
    }

    // 抛出属性变化通知。
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
