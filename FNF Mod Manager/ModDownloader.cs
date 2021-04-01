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

namespace FNF_Mod_Manager
{
    /*
     * TODO:
     * Maybe change to static class by passing data through ref 
     * Organize code
     * Separate extraction code in another class
     * Add progress bar for extraction
     * Figure out why progress bar doesn't show up when already opened
     */
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
        public async void Download(string line)
        {
            ParseProtocol(line);
            await GetData();
            DownloadWindow downloadWindow = new DownloadWindow(response.Name);
            downloadWindow.ShowDialog();
            if (downloadWindow.YesNo)
            {
                await DownloadFile(URL_TO_ARCHIVE, fileName, new Progress<DownloadProgress>(ReportUpdateProgress), 
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
                await ExtractFile(fileName);
            }
        }

        private async Task GetData()
        {
            string responseString = await client.GetStringAsync(URL);
            response = JsonSerializer.Deserialize<GameBananaItem>(responseString);
            fileName = response.Files[DL_ID].FileName;
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
            progressBox.progressText.Text = $"{Math.Round(progress.Percentage * 100, 2)}% ({StringConverters.FormatSize(progress.DownloadedBytes)} of {StringConverters.FormatSize(progress.TotalBytes)})";
        }

        private void ParseProtocol(string line)
        {
            line = line.Replace("fnfmm:", "");
            string[] data = line.Split(',');
            URL_TO_ARCHIVE = data[0];
            DL_ID = URL_TO_ARCHIVE.Replace("https://gamebanana.com/mmdl/", "");
            // Do something with these later
            string MOD_TYPE = data[1];
            string MOD_ID = data[2];
            URL = $"https://api.gamebanana.com/Core/Item/Data?itemtype={MOD_TYPE}&itemid={MOD_ID}&fields=name,Files().aFiles()&return_keys=1";
        }

        private async Task ExtractFile(string fileName)
        {
            string _ArchiveSource = $@"{assemblyLocation}\Downloads\{fileName}";
            string _ArchiveType = Path.GetExtension(fileName);
            string ArchiveDestination = $@"{assemblyLocation}\Mods\{Path.ChangeExtension(fileName, null)}";
            if (File.Exists(_ArchiveSource))
            {
                switch (_ArchiveType)
                {
                    case ".rar":
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
                        break;
                    case ".zip":
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
                        break;
                    case ".7z":
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
                        break;
                    default:

                        break;
                }
                File.Delete(_ArchiveSource);
            }
            
        }
        private async Task DownloadFile(string uri, string fileName, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
        {
            try
            {
                // Create the downloads folder if necessary
                Directory.CreateDirectory($@"{assemblyLocation}\Downloads");
                // Download the file if it doesn't already exist
                if (!File.Exists($@"{assemblyLocation}\Downloads\{fileName}"))
                {
                    progressBox = new ProgressBox(cancellationToken);
                    progressBox.progressBar.Value = 0;
                    progressBox.progressText.Text = $"Downloading...";
                    progressBox.finished = false;
                    progressBox.Title = $"Update Progress";
                    progressBox.Show();
                    progressBox.Activate();
                    // Write and download the file
                    using (var fs = new FileStream(
                        $@"{assemblyLocation}\Downloads\{fileName}", FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                    }
                    progressBox.Close();
                }
                else
                {
                    Console.WriteLine($"[INFO] {fileName} already exists in downloads, using this instead");
                }
                //ExtractFile(fileName);
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                File.Delete($@"{assemblyLocation}\Downloads\{fileName}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error whilst downloading {fileName}: {e.Message}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
            }
        }

    }
}
