using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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
                logger.WriteLine("Please set up Game Path in Config.", LoggerType.Warning);
            else if (config.exe != null)
                logger.WriteLine($"Current Game Path set as {config.exe}", LoggerType.Info);

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
        private static List<BitmapImage> bgs;
        private static bool bgsInit = false;
        private static void InitBgs()
        {
            bgs = new List<BitmapImage>();
            var bgUrls = new string[] {
            "https://media.discordapp.net/attachments/792245872259235850/838993673722658836/5.png?width=990&height=609",
            "https://media.discordapp.net/attachments/792245872259235850/838993671361396796/4.png?width=1440&height=545",
            "https://media.discordapp.net/attachments/792245872259235850/838993668157341696/3.png?width=1440&height=545",
            "https://media.discordapp.net/attachments/792245872259235850/838993665526464512/2.png?width=1075&height=609",
            "https://media.discordapp.net/attachments/792245872259235850/838993664046399558/1.png?width=931&height=609",
            "https://media.discordapp.net/attachments/792245872259235850/838993659172487178/6.png?width=1163&height=609"};
            foreach (var bg in bgUrls)
            {
                var bitmap = new BitmapImage();
                bitmap.DownloadFailed += delegate { bgsInit = false; };
                bitmap.DownloadCompleted += delegate { bgsInit = true; };
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
                if (!Directory.GetDirectories($@"{assemblyLocation}/Mods").ToList().Select(x => Path.GetFileName(x)).Contains(mod.name))
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
                if (Path.GetExtension(config.exe).Equals(".js", StringComparison.InvariantCultureIgnoreCase))
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
                        WorkingDirectory = (Path.GetDirectoryName(config.exe)),
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
                logger.WriteLine($"Please set up your Game Path in Config!", LoggerType.Warning);
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
            if (config.exe != null && Directory.Exists($@"{Path.GetDirectoryName(config.exe)}/Assets"))
            {
                ModGrid.IsHitTestVisible = false;
                ConfigButton.IsHitTestVisible = false;
                BuildButton.IsHitTestVisible = false;
                LaunchButton.IsHitTestVisible = false;
                OpenModsButton.IsHitTestVisible = false;
                UpdateButton.IsHitTestVisible = false;
                Refresh();
                await Build($@"{Path.GetDirectoryName(config.exe)}/assets");
                ModGrid.IsHitTestVisible = true;
                ConfigButton.IsHitTestVisible = true;
                BuildButton.IsHitTestVisible = true;
                LaunchButton.IsHitTestVisible = true;
                OpenModsButton.IsHitTestVisible = true;
                UpdateButton.IsHitTestVisible = true;
                MessageBox.Show($@"Finished building loadout and ready to launch!", "Notification", MessageBoxButton.OK);
            }
            else
                logger.WriteLine("Please set up correct Game Path in Config!", LoggerType.Warning);
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

        private void Window_Closing(object sender, CancelEventArgs e)
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
                        image.Height = 35;
                        para.Inlines.Add(image);
                        para.Inlines.Add(" ");
                    }
                    if (metadata.upic != null && metadata.upic.ToString().Length > 0)
                    {
                        BitmapImage bm = new BitmapImage(metadata.upic);
                        Image image = new Image();
                        image.Source = bm;
                        image.Height= 25;
                        para.Inlines.Add(image);
                    }
                    else
                        para.Inlines.Add(metadata.submitter);
                    descFlow.Blocks.Add(para);
                }
                if (metadata.preview != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = metadata.preview;
                    bitmap.EndInit();
                    Preview.Source = bitmap;
                    PreviewBG.Source = bitmap;
                }
                else
                {
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
        private static Dictionary<TypeFilter, List<GameBananaCategory>> cats = new Dictionary<TypeFilter, List<GameBananaCategory>>();
        private static readonly List<GameBananaCategory> All = new GameBananaCategory[]
        {
            new GameBananaCategory()
            {
                Name = "All",
                ID = null
            }
        }.ToList();
        private static readonly List<GameBananaCategory> None = new GameBananaCategory[]
        {
            new GameBananaCategory()
            {
                Name = "- - -",
                ID = null
            }
        }.ToList();
        private async void InitializeBrowser()
        {
            using (var httpClient = new HttpClient())
            {
                ErrorPanel.Visibility = Visibility.Collapsed;
                // Initialize categories
                var types = new string[] { "Mod", "Wip", "Sound" };
                var counter = 0;
                foreach (var type in types)
                {
                    var requestUrl = $"https://gamebanana.com/apiv3/{type}Category/ByGame?_aGameRowIds[]=8694&_sRecordSchema=Custom" +
                        "&_csvProperties=_idRow,_sName,_sProfileUrl,_sIconUrl,_idParentCategoryRow&_nPerpage=50&_bReturnMetadata=true";
                    string responseString = "";
                    try
                    {
                        responseString = await httpClient.GetStringAsync(requestUrl);
                        responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                    }
                    catch (HttpRequestException ex)
                    {
                        LoadingBar.Visibility = Visibility.Collapsed;
                        ErrorPanel.Visibility = Visibility.Visible;
                        BrowserRefreshButton.Visibility = Visibility.Visible;
                        switch (Regex.Match(ex.Message, @"\d+").Value)
                        {
                            case "443":
                                BrowserMessage.Text = "Your internet connection is acting freaky.";
                                break;
                            case "500":
                            case "503":
                            case "504":
                                BrowserMessage.Text = "GameBanana's servers are funkin' up.";
                                break;
                            default:
                                BrowserMessage.Text = ex.Message;
                                break;
                        }
                        return;
                    }
                    GameBananaCategories response = new GameBananaCategories();
                    try
                    {
                        response = JsonSerializer.Deserialize<GameBananaCategories>(responseString);
                    }
                    catch (Exception ex)
                    {
                        LoadingBar.Visibility = Visibility.Collapsed;
                        ErrorPanel.Visibility = Visibility.Visible;
                        BrowserRefreshButton.Visibility = Visibility.Visible;
                        BrowserMessage.Text = "Uh oh! Something went wrong while deserializing the categories...";
                        return;
                    }
                    if (!cats.ContainsKey((TypeFilter)counter))
                        cats.Add((TypeFilter)counter, response.Categories);
                    // Make more requests if needed
                    if (response.Metadata.TotalPages > 1)
                    {
                        for (int i = 2; i <= response.Metadata.TotalPages; i++)
                        {
                            var requestUrlPage = $"{requestUrl}&_nPage={i}";
                            try
                            {
                                responseString = await httpClient.GetStringAsync(requestUrlPage);
                                responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                            }
                            catch (HttpRequestException ex)
                            {
                                LoadingBar.Visibility = Visibility.Collapsed;
                                ErrorPanel.Visibility = Visibility.Visible;
                                BrowserRefreshButton.Visibility = Visibility.Visible;
                                switch (Regex.Match(ex.Message, @"\d+").Value)
                                {
                                    case "443":
                                        BrowserMessage.Text = "Your internet connection is acting freaky.";
                                        break;
                                    case "500":
                                    case "503":
                                    case "504":
                                        BrowserMessage.Text = "GameBanana's servers are funkin' up.";
                                        break;
                                    default:
                                        BrowserMessage.Text = ex.Message;
                                        break;
                                }
                                return;
                            }
                            try
                            {
                                response = JsonSerializer.Deserialize<GameBananaCategories>(responseString);
                            }
                            catch (Exception ex)
                            {
                                LoadingBar.Visibility = Visibility.Collapsed;
                                ErrorPanel.Visibility = Visibility.Visible;
                                BrowserRefreshButton.Visibility = Visibility.Visible;
                                BrowserMessage.Text = "Uh oh! Something went wrong while deserializing the categories...";
                                return;
                            }
                            cats[(TypeFilter)counter] = cats[(TypeFilter)counter].Concat(response.Categories).ToList();
                        }
                    }
                    counter++;
                }
            }
            CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0 && x.HasIcon).OrderBy(y => y.ID));
            SubCatBox.ItemsSource = None;
            filterSelect = true;
            CatBox.SelectedIndex = 0;
            SubCatBox.SelectedIndex = 0;
            filterSelect = false;
            RefreshFilter();
            selected = true;
        }
        private void OnTabSelected(object sender, RoutedEventArgs e)
        {
            if (!selected)
            {
                InitializeBrowser();
            }
        }
        private static int page = 1;
        private void DecrementPage(object sender, RoutedEventArgs e)
        {
            --page;
            RefreshFilter();
        }
        private void IncrementPage(object sender, RoutedEventArgs e)
        {
            ++page;
            RefreshFilter();
        }
        private void BrowserRefresh(object sender, RoutedEventArgs e)
        {
            if (!selected)
                InitializeBrowser();
            else
                RefreshFilter();
        }
        private static bool filterSelect;
        private static Random rand = new Random();
        private async void RefreshFilter()
        {
            FilterBox.IsEnabled = false;
            TypeBox.IsEnabled = false;
            CatBox.IsEnabled = false;
            SubCatBox.IsEnabled = false;
            Left.IsEnabled = false;
            Right.IsEnabled = false;
            PendingCheckbox.IsEnabled = false;
            PageBox.IsEnabled = false;
            PerPageBox.IsEnabled = false;
            ErrorPanel.Visibility = Visibility.Collapsed;
            filterSelect = true;
            PageBox.SelectedValue = page;
            filterSelect = false;
            Page.Text = $"Page {page}";
            LoadingBar.Visibility = Visibility.Visible;
            FeedBox.Visibility = Visibility.Collapsed;
            Left.IsEnabled = false;
            Right.IsEnabled = false;
            FeedBox.ItemsSource = await FeedGenerator.GetFeed(page, (TypeFilter)TypeBox.SelectedIndex, (FeedFilter)FilterBox.SelectedIndex, (GameBananaCategory)CatBox.SelectedItem,
                (GameBananaCategory)SubCatBox.SelectedItem, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10);
            if (FeedGenerator.error)
            {
                LoadingBar.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Visible;
                BrowserRefreshButton.Visibility = Visibility.Visible;
                switch (Regex.Match(FeedGenerator.exception.Message, @"\d+").Value)
                {
                    case "443":
                        BrowserMessage.Text = "Your internet connection is acting freaky.";
                        break;
                    case "500":
                    case "503":
                    case "504":
                        BrowserMessage.Text = "GameBanana's servers are funkin' up.";
                        break;
                    default:
                        BrowserMessage.Text = FeedGenerator.exception.Message;
                        break;
                }
                return;
            }
            if (page < FeedGenerator.GetMetadata(page, (TypeFilter)TypeBox.SelectedIndex, (FeedFilter)FilterBox.SelectedIndex, (GameBananaCategory)CatBox.SelectedItem, 
                (GameBananaCategory)SubCatBox.SelectedItem, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10).TotalPages)
                Right.IsEnabled = true;
            if (page != 1)
                Left.IsEnabled = true;
            if (FeedBox.Items.Count > 0)
            {
                FeedBox.ScrollIntoView(FeedBox.Items[0]);
                FeedBox.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorPanel.Visibility = Visibility.Visible;
                BrowserRefreshButton.Visibility = Visibility.Collapsed;
                BrowserMessage.Visibility = Visibility.Visible;
                BrowserMessage.Text = "FileDaddy couldn't find any funkin' mods.";
            }
            var totalPages = FeedGenerator.GetMetadata(page, (TypeFilter)TypeBox.SelectedIndex, (FeedFilter)FilterBox.SelectedIndex, (GameBananaCategory)CatBox.SelectedItem, 
                (GameBananaCategory)SubCatBox.SelectedItem, (bool)PendingCheckbox.IsChecked, (PerPageBox.SelectedIndex + 1) * 10).TotalPages;
            if (totalPages == 0)
                totalPages = 1;
            PageBox.ItemsSource = Enumerable.Range(1, totalPages);

            if (!bgsInit)
            {
                InitBgs();
                currentBg = new Random().Next(0, bgs.Count - 1);
            }
            else
            {
                var range = Enumerable.Range(1, bgs.Count - 1).Where(i => i != currentBg);
                var index = rand.Next(0, bgs.Count - 2);
                currentBg = range.ElementAt(index);
            }
            BrowserBackground.Source = bgs[currentBg];
            LoadingBar.Visibility = Visibility.Collapsed;
            CatBox.IsEnabled = true;
            SubCatBox.IsEnabled = true;
            TypeBox.IsEnabled = true;
            FilterBox.IsEnabled = true;
            PendingCheckbox.IsEnabled = true;
            PageBox.IsEnabled = true;
            PerPageBox.IsEnabled = true;
        }

        private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                page = 1;
                RefreshFilter();
            }
        }
        private void TypeFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                filterSelect = true;
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void MainFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                filterSelect = true;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void SubFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                page = 1;
                RefreshFilter();
            }
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

        private void PageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                page = (int)PageBox.SelectedValue;
                RefreshFilter();
            }
        }
        private void PendingCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            page = 1;
            RefreshFilter();
        }
    }
}
