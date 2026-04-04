using CommunityToolkit.Mvvm.ComponentModel;

namespace fufu_toolbox.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private bool _isBackButtonVisible;

    public string AppTitle => "浮浮工具箱";

    public bool IsBackButtonVisible
    {
        get => _isBackButtonVisible;
        set => SetProperty(ref _isBackButtonVisible, value);
    }

    // 根据导航栈状态更新返回按钮可见性。
    public void UpdateBackButtonState(bool canGoBack)
    {
        IsBackButtonVisible = canGoBack;
    }
}


