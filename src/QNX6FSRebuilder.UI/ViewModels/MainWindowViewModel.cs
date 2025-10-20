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
using WinRT.Interop;
using System.IO;
using Windows.Security.Cryptography.Core;
using Microsoft.UI.Dispatching;
using QNX6FSRebuilder.Core;
using System.Collections.ObjectModel;
using QNX6FSRebuilder.Core.Models;
using System.Linq;

namespace QNX6FSRebuilder.UI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private DispatcherQueue _dispatcherQueue;
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly string testFilePath = @"D:\qnx6_realImage.img";

        [ObservableProperty]
        private string selectedFilePath;

        [ObservableProperty]
        private string selectedOutputPath;

        [ObservableProperty]
        private bool isProcessing = false;

        [ObservableProperty]
        private bool preserveTimestamps = true;

        [ObservableProperty]
        private string logText = "Ready to process QNX6 disk image...";

        [ObservableProperty]
        private string statusText = "Ready";

        [ObservableProperty]
        private double progressValue = 0;

        [ObservableProperty]
        private string progressStatusText = "Processing...";

        [ObservableProperty]
        private string progressPercentageText = "0%";

        [ObservableProperty]
        private bool isProgressVisible = false;

        [ObservableProperty]
        private int fileCount = 0;

        [ObservableProperty]
        private string timestampText = string.Empty;

        [ObservableProperty]
        private bool isOptionsEnabled;

        [ObservableProperty]
        private ObservableCollection<Partition> partitions = new();


        public MainWindowViewModel(ILogger<MainWindowViewModel> logger)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("MainWindowViewModel initialized");
        }

        [RelayCommand]
        private async Task GetPartitionsAsync()
        {
            try
            {
                IsOptionsEnabled = false;
                Partitions.Clear();

                _logger.LogInformation("Reading partitions...");
                await Task.Run(() =>
                {
                    var parts = App.GetService<QNX6Parser>().GetAllPartitions();
                    foreach (var p in parts)
                        Partitions.Add(p);
                });

                _logger.LogInformation($"Found {Partitions.Count} partitions.");
                IsOptionsEnabled = Partitions.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting partitions");
            }
        }

        private bool CanParse() => IsOptionsEnabled && Partitions.Any();

        [RelayCommand]
        private async Task ParseSelectedPartitionsAsync()
        {
            foreach(var partition in Partitions)
            {
                await App.GetService<QNX6Parser>().ParsePartitionAsync(partition);
            }
        }

        [RelayCommand]
        private async Task BrowseFileAsync(object parameter)
        {
            _logger.LogInformation("BrowseFileAsync called");

            try
            {
                // Get the main window
                var mainWindow = App.MainWindow;
                if (mainWindow == null)
                {
                    _logger.LogError("Main window is null");
                    return;
                }

                // Get the AppWindow ID from the main window
                var appWindow = mainWindow.AppWindow;
                if (appWindow == null)
                {
                    _logger.LogError("AppWindow is null");
                    return;
                }

                var picker = new FileOpenPicker(appWindow.Id);

                picker.CommitButtonText = "Pick File";
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.ViewMode = PickerViewMode.List;

                // Add file type filters
                picker.FileTypeFilter.Add(".img");
                picker.FileTypeFilter.Add(".bin");
                picker.FileTypeFilter.Add("*");

                // Show the picker dialog window
                var file = await picker.PickSingleFileAsync();
                
                if (file != null)
                {
                    SelectedFilePath = file.Path;
                    StatusText = $"File selected: {Path.GetFileName(file.Path)}";
                    AppendLog($"Selected file: {file.Path}");
                    _logger.LogInformation($"File selected: {SelectedFilePath}");
                }
                else
                {
                    _logger.LogInformation("File selection cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while browsing for file");
                AppendLog($"Error selecting file: {ex.Message}");
                StatusText = "Error selecting file";
            }
        }

        [RelayCommand]
        private async Task BrowseOutputPathAsync(object parameter)
        {
            try
            {
                var mainWindow = App.MainWindow;
                if (mainWindow == null)
                {
                    _logger.LogError("Main window is null");
                    return;
                }

                var appWindow = mainWindow.AppWindow;
                if (appWindow == null)
                {
                    _logger.LogError("AppWindow is null");
                    return;
                }

                var picker = new FolderPicker(appWindow.Id);
                picker.CommitButtonText = "Select Folder";
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.ViewMode = PickerViewMode.List;

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    SelectedOutputPath = folder.Path;
                    StatusText = $"Output path selected: {Path.GetFileName(folder.Path)}";
                    AppendLog($"Selected output path: {folder.Path}");
                    _logger.LogInformation($"Output path selected: {SelectedOutputPath}");
                }
                else
                {
                    _logger.LogInformation("Output path selection cancelled");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error occurred while browsing for output path");
                AppendLog($"Error selecting output path: {ex.Message}");
                StatusText = "Error selecting output path";
            }
        }

        [RelayCommand]
        private async Task ProcessAsync()
        {
            _logger.LogInformation($"File path to process: {SelectedFilePath}");
            if (string.IsNullOrEmpty(SelectedFilePath) || !System.IO.File.Exists(SelectedFilePath))
            {
                _logger.LogInformation("Error: Please select a valid QNX6 disk image file.");
                StatusText = "No file selected";
                return;
            }
            else
            {
                _logger.LogInformation("");
                var qnx6Parser = App.GetService<QNX6Parser>();
                await Task.Run(() => qnx6Parser.ParseQNX6Async(SelectedFilePath, SelectedOutputPath)) ;
                
            }
        }

        private void AppendLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] {message}";
            LogText += string.IsNullOrEmpty(LogText) ? logEntry : $"\n{logEntry}";
        }

        [RelayCommand]
        private void ClearLog()
        {
            LogText = string.Empty;
            FileCount = 0;
            StatusText = "Ready";

        }

        private void UpdateTimestamp()
        {
            TimestampText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void SetLogSink(Action<Action<string>> sinkSetter)
        {
            sinkSetter(msg => _dispatcherQueue.TryEnqueue(() =>
            {
                LogText += msg + Environment.NewLine;
            }));
        }
    }
}
