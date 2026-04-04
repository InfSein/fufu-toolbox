using Microsoft.UI.Xaml.Controls;
using fufu_toolbox.ViewModels;

namespace fufu_toolbox.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }
}
