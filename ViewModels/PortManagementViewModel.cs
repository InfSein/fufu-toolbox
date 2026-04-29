using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using fufu_toolbox.Models;
using fufu_toolbox.Services;

namespace fufu_toolbox.ViewModels;

public sealed class PortManagementViewModel : INotifyPropertyChanged
{
    private readonly IPortManagementService _portManagementService;
    private string _searchKeyword = string.Empty;
    private string _summary = "点击刷新查看当前端口占用情况。";
    private bool _isRefreshing;

    public PortManagementViewModel(IPortManagementService portManagementService)
    {
        _portManagementService = portManagementService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PortOccupancyItem> PortItems { get; } = [];

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (_searchKeyword == value)
            {
                return;
            }

            _searchKeyword = value;
            OnPropertyChanged(nameof(SearchKeyword));
        }
    }

    public string Summary
    {
        get => _summary;
        private set
        {
            if (_summary == value)
            {
                return;
            }

            _summary = value;
            OnPropertyChanged(nameof(Summary));
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (_isRefreshing == value)
            {
                return;
            }

            _isRefreshing = value;
            OnPropertyChanged(nameof(IsRefreshing));
            OnPropertyChanged(nameof(RefreshMaskVisibility));
        }
    }

    public Visibility RefreshMaskVisibility => IsRefreshing ? Visibility.Visible : Visibility.Collapsed;

    // 按当前筛选词刷新端口占用列表。
    public async Task RefreshAsync()
    {
        IsRefreshing = true;

        try
        {
            IReadOnlyList<PortOccupancyItem> items = await _portManagementService.GetPortOccupancyAsync(SearchKeyword);

            PortItems.Clear();
            foreach (PortOccupancyItem item in items)
            {
                PortItems.Add(item);
            }

            Summary = $"共 {PortItems.Count} 条占用记录。";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    // 中止指定进程并返回处理结果。
    public Task<PortTerminateResult> TerminateProcessAsync(int processId)
    {
        return _portManagementService.TerminateProcessAsync(processId);
    }

    // 抛出属性变化通知。
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
