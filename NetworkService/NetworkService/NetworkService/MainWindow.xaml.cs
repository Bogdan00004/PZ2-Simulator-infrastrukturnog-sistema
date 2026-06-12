using NetworkService.ViewModel;
using System;
using System.Windows;

namespace NetworkService
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                foreach (var process in
                    System.Diagnostics.Process.GetProcessesByName("MeteringSimulator"))
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Shutdown Cleanup Error] {ex.Message}");
            }
        }
    }
}