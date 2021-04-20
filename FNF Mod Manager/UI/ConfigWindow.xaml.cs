using Microsoft.Win32;
using System.Windows;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace FNF_Mod_Manager
{
    public class Game : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string FileName { get; set; }
        public ImageSource Icon { get; set; }
    }
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    public partial class ConfigWindow : Window
    {
        private MainWindow _main;
        public ObservableCollection<Game> exes;

        public Game ConvertExe(string exe)
        {
            var game = new Game();
            game.FileName = exe;
            game.Icon = IconExtractor.GetIcon(exe, false, false);
            return game;
        }
        public ConfigWindow(MainWindow main)
        {
            _main = main;
            InitializeComponent();

            exes = new ObservableCollection<Game>();
            if (_main.config.exes != null)
            {
                foreach (var exe in _main.config.exes)
                {
                    exes.Add(ConvertExe(exe));
                }
            }

            if (_main.config.exe != null)
            {
                ExeTextbox.Text = _main.config.exe;
                var selectedGame = ConvertExe(_main.config.exe);
                if (!exes.Select(x => x.FileName).Contains(_main.config.exe))
                    exes.Add(selectedGame);
                ExeBox.SelectedIndex = exes.Select(x => x.FileName).ToList().IndexOf(_main.config.exe);
            }
            var prompt = new Game();
            prompt.FileName = "Add New Game Path...";
            exes.Add(prompt);
            ExeBox.ItemsSource = exes;
            if (ExeBox.SelectedIndex == -1)
            {
                ExeBox.SelectedIndex = exes.Count - 1;
                DeleteButton.IsEnabled = false;
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ExeBox.SelectedIndex != -1 && IsLoaded)
            {
                var selectedIndex = ExeBox.SelectedIndex;
                _main.logger.WriteLine($"Deleting {exes[selectedIndex]} from Game Paths", LoggerType.Info);
                exes.RemoveAt(selectedIndex);
                ExeBox.SelectedIndex = 0;
                if (exes.Count != 1)
                {
                    ExeTextbox.Text = exes[0].FileName;
                    _main.config.exe = exes[0].FileName;
                }
                else
                {
                    ExeTextbox.Text = "";
                    _main.config.exe = null;
                    _main.logger.WriteLine($"No Game Path is set.", LoggerType.Warning);
                }
                _main.config.exes = exes.Select(x => x.FileName).ToArray()[..(exes.Count - 1)].ToList();
                _main.UpdateConfig();
            }
        }
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Executable Files (*.exe)|*.exe|Javascript Files (*.js)|*.js";
            dialog.Title = "Funkin'";
            dialog.Multiselect = false;
            if (_main.config.exe != null && File.Exists(_main.config.exe))
                dialog.InitialDirectory = Path.GetDirectoryName(_main.config.exe);
            dialog.ShowDialog();
            if (dialog.FileName != null && Directory.Exists($@"{Path.GetDirectoryName(dialog.FileName)}/assets"))
            {
                var selectedGame = ExeBox.SelectedValue.ToString();
                var selectedIndex = ExeBox.SelectedIndex;
                if (dialog.FileName != selectedGame)
                {
                    if (exes.Select(x => x.FileName).Contains(dialog.FileName))
                    {
                        _main.logger.WriteLine($"{dialog.FileName} already set as one of the Game Paths", LoggerType.Error);
                        return;
                    }
                    else
                    {
                        var oldName = exes[selectedIndex].FileName;
                        exes[selectedIndex] = ConvertExe(dialog.FileName);
                        if (selectedIndex == exes.Count - 1)
                        {
                            var prompt = new Game();
                            prompt.FileName = "Add New Game Path...";
                            exes.Add(prompt);
                            _main.logger.WriteLine($"Added {dialog.FileName} to Game Paths", LoggerType.Info);
                        }
                        else
                        {
                            ExeBox.ItemsSource = exes;
                            _main.logger.WriteLine($"Replaced {oldName} with {dialog.FileName}", LoggerType.Info);
                        }
                        ExeBox.SelectedIndex = selectedIndex;
                    }
                    _main.config.exe = dialog.FileName;
                    _main.config.exes = exes.Select(x => x.FileName).ToArray()[..(exes.Count - 1)].ToList();
                }
                else
                {
                    _main.logger.WriteLine($"Game Path didn't change", LoggerType.Info);
                }
                _main.UpdateConfig();
                ExeTextbox.Text = dialog.FileName;
            }
            else if (dialog.FileName != "")
                _main.logger.WriteLine($"Couldn't find Assets folder in same directory as {dialog.FileName}", LoggerType.Error);
        }

        private void ExeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExeBox.SelectedIndex != -1 && IsLoaded)
            {
                if (ExeBox.SelectedIndex == exes.Count - 1)
                {
                    ExeTextbox.Text = "";
                    DeleteButton.IsEnabled = false;
                }
                else
                {
                    DeleteButton.IsEnabled = true;
                    var gamePath = exes[ExeBox.SelectedIndex].FileName;
                    ExeTextbox.Text = gamePath;
                    _main.config.exe = gamePath;
                    _main.UpdateConfig();
                    _main.logger.WriteLine($"Game Path switched to {gamePath}", LoggerType.Info);
                }
            }
        }
    }
}
