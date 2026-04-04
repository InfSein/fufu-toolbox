using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using fufu_toolbox.Services;
using fufu_toolbox.ViewModels;

namespace fufu_toolbox;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;

    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel, INavigationService navigationService, IThemeService themeService)
    {
        ViewModel = viewModel;
        _navigationService = navigationService;
        _themeService = themeService;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");

        _navigationService.Initialize(NavFrame);
        NavFrame.Navigated += NavFrame_Navigated;

        _themeService.ThemeChanged += OnThemeChanged;
        ApplyTheme(_themeService.CurrentTheme);

        // 启动后默认进入工具主页。
        _navigationService.NavigateTo(NavigationKeys.Home);
        UpdateBackButtonState();
    }

    // 点击标题栏按钮时收起或展开左侧菜单。
    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    // 点击标题栏返回按钮时返回上一页。
    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        _navigationService.GoBack();
        UpdateBackButtonState();
    }

    // 处理左侧菜单切换并执行页面跳转。
    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        _navigationService.NavigateTo(tag);
    }

    // 在页面变化后同步返回按钮状态。
    private void NavFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        UpdateBackButtonState();
    }

    // 响应主题变化并更新窗口主题。
    private void OnThemeChanged(AppThemeMode mode)
    {
        ApplyTheme(mode);
    }

    // 将主题模式映射到 WinUI 的主题值。
    private void ApplyTheme(AppThemeMode mode)
    {
        RootLayout.RequestedTheme = mode switch
        {
            AppThemeMode.Light => ElementTheme.Light,
            AppThemeMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    // 刷新返回按钮可见状态。
    private void UpdateBackButtonState()
    {
        ViewModel.UpdateBackButtonState(_navigationService.CanGoBack);
    }
}
