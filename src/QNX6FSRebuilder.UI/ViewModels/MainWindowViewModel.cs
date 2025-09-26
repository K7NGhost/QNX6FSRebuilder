using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Microsoft.Windows.Storage.Pickers;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.Logging;
using System;

namespace QNX6FSRebuilder.UI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly string testFilePath = @"D:\qnx6_realImage.img";

        [ObservableProperty]
        private string selectedFilePath;


        public MainWindowViewModel(ILogger<MainWindowViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("MainWindowViewModel initialized");
        }

        [RelayCommand]
        private async Task BrowseFileAsync(XamlRoot xamlRoot)
        {
            _logger.LogInformation("BrowseFileAsync called");
        }
    }
}
