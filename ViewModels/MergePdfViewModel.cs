using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using fufu_toolbox.Models;
using fufu_toolbox.Services;

namespace fufu_toolbox.ViewModels;

public sealed class MergePdfViewModel : INotifyPropertyChanged
{
    private readonly IPdfMergeService _pdfMergeService;
    private string _selectedFolderPath = "未选择目录";
    private string _scanSummary = "请选择目录后开始扫描 pdf 和图片文件。";

    public MergePdfViewModel(IPdfMergeService pdfMergeService)
    {
        _pdfMergeService = pdfMergeService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MergePdfFileItem> Files { get; } = [];

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

    // 扫描指定目录并更新可合并文件列表。
    public async Task ScanFolderAsync(string folderPath)
    {
        IReadOnlyList<string> filePaths = await _pdfMergeService.ScanSupportedFilesAsync(folderPath);

        foreach (MergePdfFileItem oldItem in Files)
        {
            oldItem.PropertyChanged -= FileItem_PropertyChanged;
        }

        Files.Clear();

        foreach (string path in filePaths)
        {
            MergePdfFileItem item = new(path)
            {
                IsSelected = true
            };

            item.PropertyChanged += FileItem_PropertyChanged;
            Files.Add(item);
        }

        SelectedFolderPath = folderPath;
        RefreshSelectionState();
    }

    // 将文件项向上移动一位。
    public void MoveUp(MergePdfFileItem item)
    {
        int index = Files.IndexOf(item);
        if (index <= 0)
        {
            return;
        }

        Files.Move(index, index - 1);
    }

    // 将文件项向下移动一位。
    public void MoveDown(MergePdfFileItem item)
    {
        int index = Files.IndexOf(item);
        if (index < 0 || index >= Files.Count - 1)
        {
            return;
        }

        Files.Move(index, index + 1);
    }

    // 一键全选当前列表文件。
    public void SelectAll()
    {
        foreach (MergePdfFileItem item in Files)
        {
            item.IsSelected = true;
        }

        RefreshSelectionState();
    }

    // 反转当前选择状态。
    public void InvertSelection()
    {
        foreach (MergePdfFileItem item in Files)
        {
            item.IsSelected = !item.IsSelected;
        }

        RefreshSelectionState();
    }

    // 按当前顺序将已选文件合并为新 PDF。
    public Task MergeSelectedFilesAsync(string outputPath)
    {
        List<MergePdfFileItem> selected = Files.Where(item => item.IsSelected).ToList();
        return _pdfMergeService.MergeFilesAsync(selected, outputPath);
    }

    // 响应文件项选择变化并更新可合并状态。
    private void FileItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MergePdfFileItem.IsSelected))
        {
            RefreshSelectionState();
        }
    }

    // 刷新统计文案和选择相关状态。
    private void RefreshSelectionState()
    {
        ScanSummary = $"共扫描到 {Files.Count} 个文件，已选择 {SelectedCount} 个。";
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CanMerge));
    }

    // 抛出属性变化通知。
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
