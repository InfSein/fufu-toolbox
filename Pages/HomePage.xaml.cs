using Microsoft.UI.Xaml.Controls;
using fufu_toolbox.ViewModels;

namespace fufu_toolbox.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
    }
}
