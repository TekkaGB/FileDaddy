using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Net.Http;
using System.Windows.Media;
using FNF_Mod_Manager.UI;
using System.Windows.Controls.Primitives;

namespace FNF_Mod_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string version;
        public Config config;
        public Logger logger;
        public string assemblyLocation = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        // Separated from config so that order is updated when datagrid is modified
        public ObservableCollection<Mod> ModList;
        public List<string> exes;
        private FileSystemWatcher ModsWatcher;
        private FlowDocument defaultFlow = new FlowDocument();
        private string defaultText = "FileDaddy is here to help out with all your Friday Night Funkin Mods!\n\n" +
            "(Right Click Row > Fetch Metadata and confirm the GameBanana URL of the mod to fetch metadata to show here.)";
        public MainWindow()
        {
            InitializeComponent();
            logger = new Logger(ConsoleWindow);
            config = new Config();

            // Get Version Number
            var FNFMMVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            version = FNFMMVersion.Substring(0, FNFMMVersion.LastIndexOf('.'));
            Title = $"FileDaddy v{version}";

            logger.WriteLine($"Launched FileDaddy v{version}!", LoggerType.Info);
            // Get config if it exists
            if (File.Exists($@"{assemblyLocation}/Config.json"))
            {
                try
                {
                    string configString = File.ReadAllText($@"{assemblyLocation}/Config.json");
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
                logger.WriteLine("Please set up Game System.IO.Path in Config.", LoggerType.Warning);
            else if (config.exe != null)
                logger.WriteLine($"Current Game System.IO.Path set as {config.exe}", LoggerType.Info);

            // Create Mods Directory if it doesn't exist
            Directory.CreateDirectory($@"{assemblyLocation}/Mods");
            Refresh();

            // Watch mods folder to detect
            ModsWatcher = new FileSystemWatcher($@"{assemblyLocation}/Mods");
            ModsWatcher.Created += OnModified;
            ModsWatcher.Deleted += OnModified;
            ModsWatcher.Renamed += OnModified;

            ModsWatcher.EnableRaisingEvents = true;

            defaultFlow.Blocks.Add(ConvertToFlowDocument(defaultText));
            DescriptionWindow.Document = defaultFlow;
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream iconStream = asm.GetManifestResourceStream("FNF_Mod_Manager.Assets.fdpreview.png");
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = iconStream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            Preview.Source = bitmap;
            PreviewBG.Source = null;

        }
        private static int currentBg;
        private static List<BitmapImage> bgs = new List<BitmapImage>();
        private static void InitBgs()
        {
            var bgUrls = new string[] {
            "https://media.discordapp.net/attachments/792245872259235850/838993673722658836/5.png?width=990&height=609",
            "https://media.discordapp.net/attachments/792245872259235850/838993671361396796/4.png?width=1440&height=545",
            "https://media.discordapp.net/attachments/792245872259235850/838993668157341696/3.png?width=1440&height=545",
            "https://media.discordapp.net/attachments/792245872259235850/838993665526464512/2.png?width=1075&height=609",
            "https://media.discordapp.net/attachments/792245872259235850/838993664046399558/1.png?width=931&height=609",
            "https://media.discordapp.net/attachments/792245872259235850/838993659172487178/6.png?width=1163&height=609"};
            var bitmap = new BitmapImage();
            foreach (var bg in bgUrls)
            {
                bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(bg);
                bitmap.EndInit();
                bgs.Add(bitmap);
            }
        }
        private void OnModified(object sender, FileSystemEventArgs e)
        {
            Refresh();
            UpdateConfig();
            // Bring window to front after download is done
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                Activate();
            });
        }

        private void Refresh()
        {
            // Add new folders found in Mods to the ModList
            foreach (var mod in Directory.GetDirectories($@"{assemblyLocation}/Mods"))
            {
                if (ModList.ToList().Where(x => x.name == System.IO.Path.GetFileName(mod)).Count() == 0)
                {
                    logger.WriteLine($"Adding {System.IO.Path.GetFileName(mod)}", LoggerType.Info);
                    Mod m = new Mod();
                    m.name = System.IO.Path.GetFileName(mod);
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ModList.Add(m);
                    });
                }
            }
            // Remove deleted folders that are still in the ModList
            foreach (var mod in ModList.ToList())
            {
                if (!Directory.GetDirectories($@"{assemblyLocation}/Mods").ToList().Select(x => System.IO.Path.GetFileName(x)).Contains(mod.name))
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ModList.Remove(mod);
                    });
                    logger.WriteLine($"{mod.name} was deleted.", LoggerType.Info);
                }
            }
            // Move all enabled mods to top
            ModList = new ObservableCollection<Mod>(ModList.ToList().OrderByDescending(x => x.enabled).ToList());

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                ModGrid.ItemsSource = ModList;
                Stats.Text = $"{ModList.Count} mods • {Directory.GetFiles($@"{assemblyLocation}/Mods", "*", SearchOption.AllDirectories).Length} files • " +
                $"{StringConverters.FormatSize(new DirectoryInfo($@"{assemblyLocation}/Mods").GetDirectorySize())}";
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
        private void Launch_Click(object sender, RoutedEventArgs e)
        {
            if (config.exe != null && File.Exists(config.exe))
            {
                if (System.IO.Path.GetExtension(config.exe).Equals(".js", StringComparison.InvariantCultureIgnoreCase))
                {
                    logger.WriteLine($"Cannot launch the web version from FileDaddy...", LoggerType.Warning);
                    return;
                }
                logger.WriteLine($"Launching {config.exe}", LoggerType.Info);
                try
                {
                    var ps = new ProcessStartInfo(config.exe)
                    {
                        // Game throws error if not launched from same directory
                        WorkingDirectory = (System.IO.Path.GetDirectoryName(config.exe)),
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    Process.Start(ps);
                }
                catch (Exception ex)
                {
                    logger.WriteLine($"Couldn't launch {config.exe} ({ex.Message})", LoggerType.Error);
                }
            }
            else
                logger.WriteLine($"Please set up your Game System.IO.Path in Config!", LoggerType.Warning);
        }
        private void GameBanana_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ps = new ProcessStartInfo("https://gamebanana.com/games/8694")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                logger.WriteLine($"Couldn't open up GameBanana ({ex.Message})", LoggerType.Error);
            }
        }
        private void ScrollToBottom(object sender, TextChangedEventArgs args)
        {
            ConsoleWindow.ScrollToEnd();
        }

        private void ModGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }

            ContextMenu contextMenu = element.ContextMenu;
            if (ModGrid.SelectedItem == null)
                element.ContextMenu.Visibility = Visibility.Collapsed;
            else
                element.ContextMenu.Visibility = Visibility.Visible;
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            Mod row = (Mod)ModGrid.SelectedItem;
            if (row != null)
            {
                var dialogResult = MessageBox.Show($@"Are you sure you want to delete {row.name}?" + Environment.NewLine + "This cannot be undone.", $@"Deleting {row.name}: Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.Delete($@"{assemblyLocation}/Mods/{row.name}", true);
                        logger.WriteLine($@"Deleting {row.name}.", LoggerType.Info);
                        ShowMetadata(null);
                    }
                    catch (Exception ex)
                    {
                        logger.WriteLine($@"Couldn't delete {row.name} ({ex.Message})", LoggerType.Error);
                    }
                }
            }
        }
        public void UpdateConfig()
        {
            config.ModList = ModList;
            string configString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            try
            {
                File.WriteAllText($@"{assemblyLocation}/Config.json", configString);
            }
            catch (Exception e)
            {
                logger.WriteLine($"Couldn't write Config.json ({e.Message})", LoggerType.Error);
            }
        }

        private async void Build_Click(object sender, RoutedEventArgs e)
        {
            if (config.exe != null && Directory.Exists($@"{System.IO.Path.GetDirectoryName(config.exe)}/Assets"))
            {
                ModGrid.IsHitTestVisible = false;
                ConfigButton.IsHitTestVisible = false;
                BuildButton.IsHitTestVisible = false;
                LaunchButton.IsHitTestVisible = false;
                OpenModsButton.IsHitTestVisible = false;
                UpdateButton.IsHitTestVisible = false;
                Refresh();
                await Build($@"{System.IO.Path.GetDirectoryName(config.exe)}/assets");
                ModGrid.IsHitTestVisible = true;
                ConfigButton.IsHitTestVisible = true;
                BuildButton.IsHitTestVisible = true;
                LaunchButton.IsHitTestVisible = true;
                OpenModsButton.IsHitTestVisible = true;
                UpdateButton.IsHitTestVisible = true;
                MessageBox.Show($@"Finished building loadout and ready to launch!", "Notification", MessageBoxButton.OK);
            }
            else
                logger.WriteLine("Please set up correct Game System.IO.Path in Config!", LoggerType.Warning);
        }

        private async Task Build(string path)
        {
            await Task.Run(() =>
            {
                ModLoader.Restart(path, logger);
                List<string> mods = config.ModList.Where(x => x.enabled).Select(y => $@"{assemblyLocation}/Mods/{y.name}").ToList();
                mods.Reverse();
                ModLoader.Build(path, mods, logger);
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            Mod row = (Mod)ModGrid.SelectedItem;
            if (row != null)
            {
                var folderName = $@"{assemblyLocation}\Mods\{row.name}";
                if (Directory.Exists(folderName))
                {
                    try
                    {
                        Process process = Process.Start("explorer.exe", folderName);
                        logger.WriteLine($@"Opened {folderName}.", LoggerType.Info);
                    }
                    catch (Exception ex)
                    {
                        logger.WriteLine($@"Couldn't open {folderName}. ({ex.Message})", LoggerType.Error);
                    }
                }
            }
        }
        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            Mod row = (Mod)ModGrid.SelectedItem;
            if (row != null)
            {
                EditWindow ew = new EditWindow(row, logger);
                ew.ShowDialog();
            }
        }
        private void FetchItem_Click(object sender, RoutedEventArgs e)
        {
            Mod row = (Mod)ModGrid.SelectedItem;
            if (row != null)
            {
                FetchWindow fw = new FetchWindow(row, logger);
                fw.ShowDialog();
                if (fw.success)
                    ShowMetadata(row.name);
            }
        }
        private void ModsFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderName = $@"{assemblyLocation}\Mods";
            if (Directory.Exists(folderName))
            {
                try
                {
                    Process process = Process.Start("explorer.exe", folderName);
                    logger.WriteLine($@"Opened {folderName}.", LoggerType.Info);
                }
                catch (Exception ex)
                {
                    logger.WriteLine($@"Couldn't open {folderName}. ({ex.Message})", LoggerType.Error);
                }
            }
        }
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            logger.WriteLine("Checking for updates...", LoggerType.Info);
            ModGrid.IsHitTestVisible = false;
            ConfigButton.IsHitTestVisible = false;
            BuildButton.IsHitTestVisible = false;
            LaunchButton.IsHitTestVisible = false;
            OpenModsButton.IsHitTestVisible = false;
            UpdateButton.IsHitTestVisible = false;
            ModUpdater.CheckForUpdates($"{assemblyLocation}/Mods", logger, this);
        }
        private Paragraph ConvertToFlowDocument(string text)
        {
            var flowDocument = new FlowDocument();

            var regex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = regex.Matches(text).Cast<Match>().Select(m => m.Value).ToList();

            var paragraph = new Paragraph();
            flowDocument.Blocks.Add(paragraph);


            foreach (var segment in regex.Split(text))
            {
                if (matches.Contains(segment))
                {
                    var hyperlink = new Hyperlink(new Run(segment))
                    {
                        NavigateUri = new Uri(segment),
                    };

                    hyperlink.RequestNavigate += (sender, args) =>
                    {
                        var ps = new ProcessStartInfo(segment)
                        {
                            UseShellExecute = true,
                            Verb = "open"
                        };
                        Process.Start(ps);
                    };

                    paragraph.Inlines.Add(hyperlink);
                }
                else
                {
                    paragraph.Inlines.Add(new Run(segment));
                }
            }

            return paragraph;
        }

        private void ShowMetadata(string mod)
        {
            if (mod == null || !File.Exists($"{assemblyLocation}/Mods/{mod}/mod.json"))
            {
                DescriptionWindow.Document = defaultFlow;
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream iconStream = asm.GetManifestResourceStream("FNF_Mod_Manager.Assets.fdpreview.png");
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = iconStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                Preview.Source = bitmap;
                PreviewBG.Source = null;
            }
            else
            {
                FlowDocument descFlow = new FlowDocument();
                var metadataString = File.ReadAllText($"{assemblyLocation}/Mods/{mod}/mod.json");
                Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataString);

                var para = new Paragraph();
                if (metadata.submitter != null)
                {
                    para.Inlines.Add($"Submitter: ");
                    if (metadata.avi != null && metadata.avi.ToString().Length > 0)
                    {
                        BitmapImage bm = new BitmapImage(metadata.avi);
                        Image image = new Image();
                        image.Source = bm;
                        image.Width = 20;
                        para.Inlines.Add(image);
                        para.Inlines.Add(" ");
                    }
                    if (metadata.upic != null && metadata.upic.ToString().Length > 0)
                    {
                        BitmapImage bm = new BitmapImage(metadata.upic);
                        Image image = new Image();
                        image.Source = bm;
                        image.Width = 80;
                        para.Inlines.Add(image);
                    }
                    else
                        para.Inlines.Add(metadata.submitter);
                    descFlow.Blocks.Add(para);
                }
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = metadata.preview;
                bitmap.EndInit();
                Preview.Source = bitmap;
                PreviewBG.Source = bitmap;
                if (metadata.caticon != null)
                {
                    BitmapImage bm = new BitmapImage(metadata.caticon);
                    Image image = new Image();
                    image.Source = bm;
                    image.Width = 20;
                    para = new Paragraph();
                    para.Inlines.Add("Category: ");
                    para.Inlines.Add(image);
                    para.Inlines.Add($" {metadata.cat} {metadata.section}");
                    descFlow.Blocks.Add(para);
                }
                var text = "";
                if (metadata.description != null && metadata.description.Length > 0)
                    text += $"Description: {metadata.description}\n\n";
                if (metadata.homepage != null && metadata.homepage.ToString().Length > 0)
                    text += $"Home Page: {metadata.homepage}";
                var init = ConvertToFlowDocument(text);
                descFlow.Blocks.Add(init);
                DescriptionWindow.Document = descFlow;
                var descriptionText = new TextRange(DescriptionWindow.Document.ContentStart, DescriptionWindow.Document.ContentEnd);
                descriptionText.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Center);
            }
        }
        private void ModGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mod row = (Mod)ModGrid.SelectedItem;
            if (row != null)
                ShowMetadata(row.name);
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            new ModDownloader().BrowserDownload(item);
        }
        private void Homepage_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            try
            {
                var ps = new ProcessStartInfo(item.Link.ToString())
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                logger.WriteLine($"Couldn't open up {item.Link} ({ex.Message})", LoggerType.Error);
            }
        }
        private static bool selected = false;
        private async void OnTabSelected(object sender, RoutedEventArgs e)
        {
            if (!selected)
            {
                Left.IsEnabled = false;
                Right.IsEnabled = false;
                LoadingBar.Visibility = Visibility.Visible;
                FeedBox.Visibility = Visibility.Collapsed;
                FeedBox.ItemsSource = await FeedGenerator.GetFeed(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10);
                Right.IsEnabled = true;
                PageBox.ItemsSource = Enumerable.Range(1, FeedGenerator.GetMetadata(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10).TotalPages);
                PageBox.SelectedValue = page;
                LoadingBar.Visibility = Visibility.Collapsed;
                if (FeedBox.Items.Count > 0)
                    FeedBox.ScrollIntoView(FeedBox.Items[0]);
                FeedBox.Visibility = Visibility.Visible;
                Page.Text = $"Page {page}";
                InitBgs();
                currentBg = new Random().Next(0, 5);
                BrowserBackground.Source = bgs[currentBg];
                selected = true;
            }
        }
        private static int page = 1;
        private async void DecrementPage(object sender, RoutedEventArgs e)
        {
            if (--page == 1)
                Left.IsEnabled = false;
            Page.Text = $"Page {page}";
            LoadingBar.Visibility = Visibility.Visible;
            FeedBox.Visibility = Visibility.Collapsed;
            FeedBox.ItemsSource = await FeedGenerator.GetFeed(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10);
            if (page < FeedGenerator.GetMetadata(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10).TotalPages)
                Right.IsEnabled = true;
            LoadingBar.Visibility = Visibility.Collapsed;
            if (FeedBox.Items.Count > 0)
                FeedBox.ScrollIntoView(FeedBox.Items[0]);
            FeedBox.Visibility = Visibility.Visible;
            PageBox.ItemsSource = Enumerable.Range(1, FeedGenerator.GetMetadata(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10).TotalPages);
            PageBox.SelectedValue = page;
        }
        private async void IncrementPage(object sender, RoutedEventArgs e)
        {
            if (++page != 1)
                Left.IsEnabled = true;
            Page.Text = $"Page {page}";
            LoadingBar.Visibility = Visibility.Visible;
            FeedBox.Visibility = Visibility.Collapsed;
            FeedBox.ItemsSource = await FeedGenerator.GetFeed(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10);
            if (page >= FeedGenerator.GetMetadata(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10).TotalPages)
                Right.IsEnabled = false;
            LoadingBar.Visibility = Visibility.Collapsed;
            if (FeedBox.Items.Count > 0)
                FeedBox.ScrollIntoView(FeedBox.Items[0]);
            FeedBox.Visibility = Visibility.Visible;
            PageBox.SelectedValue = page;
        }
        private static bool filterSelect;
        private async void RefreshFilter()
        {
            BrowserMessage.Visibility = Visibility.Collapsed;
            page = 1;
            filterSelect = true;
            PageBox.SelectedValue = page;
            filterSelect = false;
            Page.Text = $"Page {page}";
            LoadingBar.Visibility = Visibility.Visible;
            FeedBox.Visibility = Visibility.Collapsed;
            Left.IsEnabled = false;
            Right.IsEnabled = false;
            FeedBox.ItemsSource = await FeedGenerator.GetFeed(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10);
            if (page < FeedGenerator.GetMetadata(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10).TotalPages)
                Right.IsEnabled = true;
            if (FeedBox.Items.Count > 0)
            {
                FeedBox.ScrollIntoView(FeedBox.Items[0]);
                FeedBox.Visibility = Visibility.Visible;
            }
            else
            {
                BrowserMessage.Visibility = Visibility.Visible;
                BrowserMessage.Text = "No mods found. Pain Peko T_T";
            }
            var totalPages = FeedGenerator.GetMetadata(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10).TotalPages;
            if (totalPages == 0)
                totalPages = 1;
            PageBox.ItemsSource = Enumerable.Range(1, totalPages);
            var range = Enumerable.Range(1, 5).Where(i => i != currentBg);
            var index = new Random().Next(0, 4);
            currentBg = range.ElementAt(index);
            BrowserBackground.Source = bgs[currentBg];
            LoadingBar.Visibility = Visibility.Collapsed;
        }
        private static readonly string[] Skins = new string[]
        {
            " All",
            " Skin Packs",
            " Boyfriend",
            " Girlfriend",
            " Daddy Dearest",
            " Skid and Pump",
            " Pico",
            " Mom",
            " Parents",
            " Monster",
            " Senpai/Spirit",
            " Tankman",
            " Other/Misc"
        };
        private static readonly string[] UI = new string[]
        {
            " All",
            " Combo/Countdown",
            " Health Bar",
            " Menus",
            " Noteskins",
            " Other/Misc"
        };
        private static readonly string[] Stages = new string[]
        {
            " All",
            " Sound Stage",
            " Spooky House",
            " Philly",
            " Highway",
            " Shopping Mall",
            " Weeb School",
            " Other/Misc"
        };
        private static readonly string[] Weeks = new string[]
        {
            " All",
            " Tutorial/Week 1",
            " Week 2",
            " Week 3",
            " Week 4",
            " Week 5",
            " Week 6",
            " Week 7"
        };
        private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                RefreshFilter();
        }
        private void MainFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                filterSelect = true;
                switch ((CategoryFilter)CatBox.SelectedIndex)
                {
                    case CategoryFilter.Skins:
                        SubCatBox.ItemsSource = Skins;
                        SubCatBox.IsEnabled = true;
                        SubCatBox.SelectedIndex = 0;
                        break;
                    case CategoryFilter.Stages:
                        SubCatBox.ItemsSource = Stages;
                        SubCatBox.IsEnabled = true;
                        SubCatBox.SelectedIndex = 0;
                        break;
                    case CategoryFilter.UI:
                        SubCatBox.ItemsSource = UI;
                        SubCatBox.IsEnabled = true;
                        SubCatBox.SelectedIndex = 0;
                        break;
                    case CategoryFilter.CustomSongs:
                    case CategoryFilter.CustomSongsSkins:
                        SubCatBox.ItemsSource = Weeks;
                        SubCatBox.IsEnabled = true;
                        SubCatBox.SelectedIndex = 0;
                        break;
                    default:
                        SubCatBox.ItemsSource = null;
                        SubCatBox.IsEnabled = false;
                        SubCatBox.SelectedIndex = -1;
                        break;
                }
                filterSelect = false;
                RefreshFilter();
            }
        }
        private void SubFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
                RefreshFilter();
        }
        private void UniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = sender as UniformGrid;
            if (grid.ActualWidth > 2000)
                grid.Columns = 6;
            else if (grid.ActualWidth > 1600) 
                grid.Columns = 5;
            else if (grid.ActualWidth > 1200) 
                grid.Columns = 4;
            else 
                grid.Columns = 3;
        }

        private async void PageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect)
            {
                page = (int)PageBox.SelectedValue;
                if (page != 1)
                    Left.IsEnabled = true;
                else
                    Left.IsEnabled = false;
                Page.Text = $"Page {page}";
                LoadingBar.Visibility = Visibility.Visible;
                FeedBox.Visibility = Visibility.Collapsed;
                FeedBox.ItemsSource = await FeedGenerator.GetFeed(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10);
                if (page >= FeedGenerator.GetMetadata(page, (FeedFilter)FilterBox.SelectedIndex, (CategoryFilter)CatBox.SelectedIndex, SubCatBox.SelectedIndex, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10).TotalPages)
                    Right.IsEnabled = false;
                else
                    Right.IsEnabled = true;
                LoadingBar.Visibility = Visibility.Collapsed;
                if (FeedBox.Items.Count > 0)
                    FeedBox.ScrollIntoView(FeedBox.Items[0]);
                FeedBox.Visibility = Visibility.Visible;
            }
        }
        private void PendingCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            RefreshFilter();
        }
    }
}
