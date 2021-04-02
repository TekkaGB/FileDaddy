using Microsoft.Win32;
using System.Windows;
using System.IO;

namespace FNF_Mod_Manager
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        private MainWindow _main;
        public ConfigWindow(MainWindow main)
        {
            _main = main;
            InitializeComponent();
            if (_main.config.exe != null)
                ExeTextbox.Text = _main.config.exe;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Executable Files (*.exe)|*.exe";
            dialog.Title = "Funkin.exe";
            dialog.Multiselect = false;
            dialog.ShowDialog();
            if (dialog.FileName != null && Directory.Exists($@"{Path.GetDirectoryName(dialog.FileName)}/assets"))
            {
                _main.logger.WriteLine($"Set {dialog.FileName} as Game Path", LoggerType.Info);
                _main.config.exe = dialog.FileName;
                _main.UpdateConfig();
                ExeTextbox.Text = dialog.FileName;
            }
            else
                _main.logger.WriteLine($"Couldn't find Assets folder in same directory as {dialog.FileName}", LoggerType.Error);
        }
    }
}
