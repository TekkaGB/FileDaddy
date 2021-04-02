using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Text.Json;
using Microsoft.Win32;

namespace FNF_Mod_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Config config;
        public Logger logger;
        public string absolutePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        // Separated from config so that order is updated when datagrid is modified
        public ObservableCollection<Mod> ModList;
        private FileSystemWatcher ModsWatcher;

        public MainWindow()
        {
            InitializeComponent();
            logger = new Logger(ConsoleWindow);
            config = new Config();
            // Get config if it exists
            if (File.Exists($@"{absolutePath}\Config.json"))
            {
                try
                {
                    string configString = File.ReadAllText($@"{absolutePath}\Config.json");
                    config = JsonSerializer.Deserialize<Config>(configString);
                }
                catch (Exception e)
                {
                    logger.WriteLine(e.Message, LoggerType.Error);
                }
            }

            if (config.ModList == null)
                config.ModList = new ObservableCollection<Mod>();

            ModList = config.ModList;

            if (config.exe == null || !File.Exists(config.exe))
                logger.WriteLine("Please select your Funkin.exe in config.", LoggerType.Warning);

            // Create Mods Directory if it doesn't exist
            Directory.CreateDirectory($@"{absolutePath}\Mods");
            Refresh();

            ModsWatcher = new FileSystemWatcher($@"{absolutePath}\Mods");
            ModsWatcher.Created += OnCreated;
            ModsWatcher.Deleted += OnCreated;
            ModsWatcher.Renamed += OnCreated;

            ModsWatcher.EnableRaisingEvents = true;
        }
        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            // Add new folders found in Mods to the ModList
            foreach (var mod in Directory.GetDirectories($@"{absolutePath}\Mods"))
            {
                if (ModList.ToList().Where(x => x.name == Path.GetFileName(mod)).Count() == 0)
                {
                    logger.WriteLine($"Adding {Path.GetFileName(mod)}", LoggerType.Info);
                    Mod m = new Mod();
                    m.name = Path.GetFileName(mod);
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ModList.Add(m);
                    });
                }
            }
            // Remove deleted folders that are still in the ModList
            foreach (var mod in ModList.ToList())
            {
                if (!Directory.GetDirectories($@"{absolutePath}\Mods").ToList().Contains($@"{absolutePath}\Mods\{mod.name}"))
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ModList.Remove(mod);
                    });
                    logger.WriteLine($"Deleted {mod.name}", LoggerType.Warning);
                }
            }
            // Move all enabled mods to top
            ModList = new ObservableCollection<Mod>(ModList.ToList().OrderByDescending(x => x.enabled).ToList());

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                ModGrid.ItemsSource = ModList;
            });
            config.ModList = ModList;
            logger.WriteLine("Refreshed!", LoggerType.Info);
        }

        // Events for Enabled checkboxes
        private void OnChecked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.OriginalSource as CheckBox;

            Mod mod = checkBox?.DataContext as Mod;

            if (mod != null)
            {
                mod.enabled = true;
                List<Mod> temp = config.ModList.ToList();
                foreach (var m in temp)
                {
                    if (m.name == mod.name)
                        m.enabled = true;
                }
                config.ModList = new ObservableCollection<Mod>(temp);
                UpdateConfig();
            }
        }
        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.OriginalSource as CheckBox;

            Mod mod = checkBox?.DataContext as Mod;

            if (mod != null)
            {
                mod.enabled = false;
                List<Mod> temp = config.ModList.ToList();
                foreach (var m in temp)
                {
                    if (m.name == mod.name)
                        m.enabled = false;
                }
                config.ModList = new ObservableCollection<Mod>(temp);
                UpdateConfig();
            }
        }
        // Triggered when priority is switched on drag and dropped
        private void ModGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            UpdateConfig();
        }

        private void Config_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow configWindow = new ConfigWindow(this);
            configWindow.ShowDialog();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
            UpdateConfig();
        }
        private void ScrollToBottom(object sender, TextChangedEventArgs args)
        {
            ConsoleWindow.ScrollToEnd();
        }

        public void UpdateConfig()
        {
            config.ModList = ModList;
            string configString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            try
            {
                File.WriteAllText($@"{absolutePath}\Config.json", configString);
            }
            catch (Exception e)
            {
                logger.WriteLine($"Couldn't write Config.json ({e.Message})", LoggerType.Error);
            }
        }

        private async void Build_Click(object sender, RoutedEventArgs e)
        {
            if (config.exe != null && Directory.Exists($@"{Path.GetDirectoryName(config.exe)}\Assets"))
            {
                Refresh();
                await Build($@"{Path.GetDirectoryName(config.exe)}\Assets");
            }
            else
                logger.WriteLine("Please set up correct Game Path in Config", LoggerType.Error);
        }

        private async Task Build(string path)
        {
            await Task.Run(() =>
            { 
                ModLoader.Restart(path, logger);
                List<string> mods = config.ModList.Where(x => x.enabled).Select(y => $@"{absolutePath}\Mods\{y.name}").ToList();
                mods.Reverse();
                ModLoader.Build(path, mods, logger);
            });
            logger.WriteLine("Finished Building!", LoggerType.Info);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
