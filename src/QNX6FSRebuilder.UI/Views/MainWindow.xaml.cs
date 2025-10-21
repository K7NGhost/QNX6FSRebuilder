using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using QNX6FSRebuilder.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QNX6FSRebuilder.UI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; } = App.GetService<MainWindowViewModel>();
        public MainWindow()
        {
            InitializeComponent();

        }

        private void PartitionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel is not MainWindowViewModel vm)
                return;

            foreach(PartitionViewModel added in e.AddedItems)
            {
                if (!vm.SelectedPartitions.Contains(added))
                    vm.SelectedPartitions.Add(added);
            }

            foreach (PartitionViewModel removed in e.RemovedItems)
                vm.SelectedPartitions.Remove(removed);
        }

    }
}
