using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using System.Net.Http;
using System.Threading;
using System.Text.Json;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace FNF_Mod_Manager
{
    public class ModDownloader
    {
        private string URL_TO_ARCHIVE;
        private string URL;
        private string DL_ID;
        private string fileName;
        private string assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private HttpClient client = new HttpClient();
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private GameBananaItem response = new GameBananaItem();
        private ProgressBox progressBox;
        public async void Download(string line, bool running)
        {
            if (ParseProtocol(line))
            {
                if (await GetData())
                {
                    DownloadWindow downloadWindow = new DownloadWindow(response);
                    downloadWindow.ShowDialog();
                    if (downloadWindow.YesNo)
                    {
                        await DownloadFile(URL_TO_ARCHIVE, fileName, new Progress<DownloadProgress>(ReportUpdateProgress),
                            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
                        await ExtractFile(fileName);
                    }
                }
            }
            if (running)
                Environment.Exit(0);
        }

        private async Task<bool> GetData()
        {
            try
            {
                string responseString = await client.GetStringAsync(URL);
                response = JsonSerializer.Deserialize<GameBananaItem>(responseString);
                fileName = response.Files[DL_ID].FileName;
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while fetching data: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        private void ReportUpdateProgress(DownloadProgress progress)
        {
            if (progress.Percentage == 1)
            {
                progressBox.finished = true;
            }
            progressBox.progressBar.Value = progress.Percentage * 100;
            progressBox.taskBarItem.ProgressValue = progress.Percentage;
            progressBox.progressTitle.Text = $"Downloading {progress.FileName}...";
            progressBox.progressText.Text = $"{Math.Round(progress.Percentage * 100, 2)}% " +
                $"({StringConverters.FormatSize(progress.DownloadedBytes)} of {StringConverters.FormatSize(progress.TotalBytes)})";
        }

        private bool ParseProtocol(string line)
        {
            try
            {
                line = line.Replace("filedaddy:", "");
                string[] data = line.Split(',');
                URL_TO_ARCHIVE = data[0];
                // Used to grab file info from dictionary
                var match = Regex.Match(URL_TO_ARCHIVE, @"\d*$");
                DL_ID = match.Value;
                string MOD_TYPE = data[1];
                string MOD_ID = data[2];
                URL = $"https://api.gamebanana.com/Core/Item/Data?itemtype={MOD_TYPE}&itemid={MOD_ID}&fields=name,Files().aFiles(),Preview().sStructuredDataFullsizeUrl(),Preview().sSubFeedImageUrl()&return_keys=1";
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while parsing {line}: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        // Extract download to Mods directory
        private async Task ExtractFile(string fileName)
        {
            await Task.Run(() =>
            {
                string _ArchiveSource = $@"{assemblyLocation}/Downloads/{fileName}";
                string _ArchiveType = Path.GetExtension(fileName);
                string ArchiveDestination = $@"{assemblyLocation}/Mods/{response.Name}";
                // Find a unique destination if it already exists
                var counter = 2;
                while (Directory.Exists(ArchiveDestination))
                {
                    ArchiveDestination = $@"{assemblyLocation}/Mods/{response.Name} ({counter})";
                    ++counter;
                }
                if (File.Exists(_ArchiveSource))
                {
                    switch (_ArchiveType)
                    {
                        case ".rar":
                            try
                            {
                                using (var archive = RarArchive.Open(_ArchiveSource))
                                {
                                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                    {
                                        entry.WriteToDirectory(ArchiveDestination, new ExtractionOptions()
                                        {
                                            ExtractFullPath = true,
                                            Overwrite = true
                                        });
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show($"Couldn't extract {fileName}: {e.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            break;
                        case ".zip":
                            try
                            {
                                using (var archive = ZipArchive.Open(_ArchiveSource))
                                {
                                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                    {
                                        entry.WriteToDirectory(ArchiveDestination, new ExtractionOptions()
                                        {
                                            ExtractFullPath = true,
                                            Overwrite = true
                                        });
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show($"Couldn't extract {fileName}: {e.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            break;
                        case ".7z":
                            try
                            {
                                using (var archive = SevenZipArchive.Open(_ArchiveSource))
                                {
                                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                    {
                                        entry.WriteToDirectory(ArchiveDestination, new ExtractionOptions()
                                        {
                                            ExtractFullPath = true,
                                            Overwrite = true
                                        });
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show($"Couldn't extract {fileName}: {e.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            break;
                        default:

                            break;
                    }
                    // Check if folder output folder exists, if not nothing had an assets folder
                    if (!Directory.Exists(ArchiveDestination))
                    {
                        MessageBox.Show($"Didn't extract {fileName} due to improper format", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        // Only delete if successfully extracted
                        File.Delete(_ArchiveSource);
                    }
                }
            });
            
        }
        private async Task DownloadFile(string uri, string fileName, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
        {
            try
            {
                // Create the downloads folder if necessary
                Directory.CreateDirectory($@"{assemblyLocation}/Downloads");
                // Download the file if it doesn't already exist
                if (File.Exists($@"{assemblyLocation}/Downloads/{fileName}"))
                {
                    try
                    {
                        File.Delete($@"{assemblyLocation}/Downloads/{fileName}");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Couldn't delete the already existing {assemblyLocation}/Downloads/{fileName} ({e.Message})", 
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                progressBox = new ProgressBox(cancellationToken);
                progressBox.progressBar.Value = 0;
                progressBox.finished = false;
                progressBox.Title = $"Update Progress";
                progressBox.Show();
                progressBox.Activate();
                // Write and download the file
                using (var fs = new FileStream(
                    $@"{assemblyLocation}/Downloads/{fileName}", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                }
                progressBox.Close();
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                File.Delete($@"{assemblyLocation}/Downloads/{fileName}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                return;
            }
            catch (Exception e)
            {
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                MessageBox.Show($"Error whilst downloading {fileName}: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

    }
}
