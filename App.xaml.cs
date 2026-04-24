using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using fufu_toolbox.Services;
using fufu_toolbox.ViewModels;

namespace fufu_toolbox;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    public static IHost Host { get; } = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<MergeTxtViewModel>();
            services.AddSingleton<MergePdfViewModel>();
            services.AddSingleton<XivItemTranslatorViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<ITxtMergeService, TxtMergeService>();
            services.AddSingleton<IPdfMergeService, PdfMergeService>();
            services.AddSingleton<IXivItemTranslationService, XivItemTranslationService>();
        })
        .Build();

    // 统一从依赖注入容器获取对象。
    public static T GetService<T>() where T : class
    {
        return Host.Services.GetRequiredService<T>();
    }

    // 应用启动时创建并激活主窗口。
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = GetService<MainWindow>();
        _window.Activate();
    }
}
