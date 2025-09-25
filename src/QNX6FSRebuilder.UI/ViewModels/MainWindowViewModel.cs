using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Microsoft.Windows.Storage.Pickers;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace QNX6FSRebuilder.UI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly string testFilePath = @"D:\qnx6_realImage.img";

        [ObservableProperty]
        private string selectedFilePath;


        public MainWindowViewModel()
        {
        }

        [RelayCommand]
        private async Task BrowseFileAsync(XamlRoot xamlRoot)
        {
            
        }
    }
}
