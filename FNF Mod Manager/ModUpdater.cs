using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Threading;
using FNF_Mod_Manager.UI;
using System.Reflection;
using System.Windows;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FNF_Mod_Manager
{
    public static class ModUpdater
    {
        private static ProgressBox progressBox;
        private static Logger _logger;
        private static string assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private static int counter;
        public async static void CheckForUpdates(string path, Logger logger, MainWindow main)
        {
            counter = 0;
            _logger = logger;
            if (!Directory.Exists(path))
            {
                main.ModGrid.IsHitTestVisible = true;
                main.ConfigButton.IsHitTestVisible = true;
                main.BuildButton.IsHitTestVisible = true;
                main.LaunchButton.IsHitTestVisible = true;
                main.OpenModsButton.IsHitTestVisible = true;
                main.UpdateButton.IsHitTestVisible = true;
                main.Activate();
                return;
            }
            var cancellationToken = new CancellationTokenSource();
            var requestUrl = $"https://api.gamebanana.com/Core/Item/Data?";
            var mods = Directory.GetDirectories(path).Where(x => File.Exists($"{x}/mod.json")).ToList();
            foreach (var mod in mods)
            {
                if (!File.Exists($"{mod}/mod.json"))
                    continue;
                var metadataString = File.ReadAllText($"{mod}/mod.json");
                Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataString);
                Uri url = null;
                if (metadata.homepage != null)
                    url = CreateUri(metadata.homepage.ToString());
                if (url != null)
                {
                    var MOD_TYPE = char.ToUpper(url.Segments[1][0]) + url.Segments[1].Substring(1, url.Segments[1].Length - 3);
                    var MOD_ID = url.Segments[2];
                    requestUrl += $"itemtype[]={MOD_TYPE}&itemid[]={MOD_ID}&fields[]=Updates().bSubmissionHasUpdates()," +
                        $"Updates().aGetLatestUpdates(),Files().aFiles(),Preview().sStructuredDataFullsizeUrl()&";
                }
            }
            requestUrl += "return_keys=1";
            if (requestUrl == $"https://api.gamebanana.com/Core/Item/Data?return_keys=1")
            {
                _logger.WriteLine("No updates available.", LoggerType.Info);
                main.ModGrid.IsHitTestVisible = true;
                main.ConfigButton.IsHitTestVisible = true;
                main.BuildButton.IsHitTestVisible = true;
                main.LaunchButton.IsHitTestVisible = true;
                main.OpenModsButton.IsHitTestVisible = true;
                main.UpdateButton.IsHitTestVisible = true;
                main.Activate();
                return;
            }
            var client = new HttpClient();
            var responseString = await client.GetStringAsync(requestUrl);
            var response = JsonSerializer.Deserialize<GameBananaItem[]>(responseString);
            for (int i = 0; i < mods.Count; i++)
            {
                var metadata = JsonSerializer.Deserialize<Metadata>(File.ReadAllText($"{mods[i]}/mod.json"));
                await ModUpdate(response[i], mods[i], metadata, new Progress<DownloadProgress>(ReportUpdateProgress), CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
            }
            if (counter == 0)
                _logger.WriteLine("No updates available.", LoggerType.Info);
            else
                _logger.WriteLine("Done checking for updates!", LoggerType.Info);

            main.ModGrid.IsHitTestVisible = true;
            main.ConfigButton.IsHitTestVisible = true;
            main.BuildButton.IsHitTestVisible = true;
            main.LaunchButton.IsHitTestVisible = true;
            main.OpenModsButton.IsHitTestVisible = true;
            main.UpdateButton.IsHitTestVisible = true;
            main.Activate();
        }
        private static void ReportUpdateProgress(DownloadProgress progress)
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
        private static async Task ModUpdate(GameBananaItem item, string mod, Metadata metadata, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
        {
            // If lastupdate doesn't exist, add one
            if (metadata.lastupdate == null)
            {
                if (item.HasUpdates)
                    metadata.lastupdate = item.Updates[0].DateAdded;
                else
                    metadata.lastupdate = new DateTime(1970, 1, 1);
                string metadataString = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText($@"{mod}/mod.json", metadataString);
                return;
            }
            if (item.HasUpdates)
            {
                var update = item.Updates[0];
                // Compares dates of last update to current
                if (DateTime.Compare((DateTime)metadata.lastupdate, update.DateAdded) < 0)
                {
                    ++counter;
                    // Display the changelog and confirm they want to update
                    _logger.WriteLine($"An update is available for {Path.GetFileName(mod)}!", LoggerType.Info);
                    ChangelogBox changelogBox = new ChangelogBox(update, Path.GetFileName(mod), $"A new update is available for {Path.GetFileName(mod)}", item.EmbedImage, true);
                    changelogBox.Activate();
                    changelogBox.ShowDialog();
                    if (changelogBox.Skip)
                    {
                        if (File.Exists($@"{mod}/mod.json"))
                        {
                            _logger.WriteLine($"Skipped update for {Path.GetFileName(mod)}...", LoggerType.Info);
                            metadata.lastupdate = update.DateAdded;
                            string metadataString = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText($@"{mod}/mod.json", metadataString);
                        }
                        return;
                    }
                    if (!changelogBox.YesNo)
                    {
                        _logger.WriteLine($"Declined update for {Path.GetFileName(mod)}...", LoggerType.Info);
                        return;
                    }
                    // Download the update
                    var files = item.Files;
                    string downloadUrl, fileName;

                    if (files.Count > 1)
                    {
                        UpdateFileBox fileBox = new UpdateFileBox(files, Path.GetFileName(mod));
                        fileBox.Activate();
                        fileBox.ShowDialog();
                        downloadUrl = fileBox.chosenFileUrl;
                        fileName = fileBox.chosenFileName;
                    }
                    else if (files.Count == 1)
                    {
                        downloadUrl = files.ElementAt(0).Value.DownloadUrl;
                        fileName = files.ElementAt(0).Value.FileName;
                    }
                    else
                    {
                        _logger.WriteLine($"An update is available for {Path.GetFileName(mod)} but no downloadable files are available.", LoggerType.Warning);
                        return;
                    }
                    if (downloadUrl != null && fileName != null)
                    {
                        await DownloadFile(downloadUrl, fileName, mod, update.DateAdded, progress, cancellationToken);
                    }
                    else
                    {
                        _logger.WriteLine($"Cancelled update for {Path.GetFileName(mod)}", LoggerType.Info);
                    }
                }
            }
        }
        private static async Task DownloadFile(string uri, string fileName, string mod, DateTime updateTime, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
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
                        _logger.WriteLine($"Couldn't delete the already existing {assemblyLocation}/Downloads/{fileName} ({e.Message})",
                            LoggerType.Error);
                        return;
                    }
                }
                progressBox = new ProgressBox(cancellationToken);
                progressBox.progressBar.Value = 0;
                progressBox.finished = false;
                progressBox.Title = $"Download Progress";
                progressBox.Show();
                progressBox.Activate();
                // Write and download the file
                using (var fs = new FileStream(
                    $@"{assemblyLocation}/Downloads/{fileName}", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var client = new HttpClient();
                    await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                }
                progressBox.Close();
                await ExtractFile(fileName, mod, updateTime);
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
                _logger.WriteLine($"Error whilst downloading {fileName} ({e.Message})", LoggerType.Error);
            }
        }

        private static void ClearDirectory(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);

            foreach (FileInfo fi in dir.GetFiles())
            {
                if (fi.Name != "mod.json")
                    fi.Delete();
            }

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                ClearDirectory(di.FullName);
                di.Delete();
            }
        }

        private static async Task ExtractFile(string fileName, string output, DateTime updateTime)
        {
            await Task.Run(() =>
            {
                string _ArchiveSource = $@"{assemblyLocation}/Downloads/{fileName}";
                string ArchiveDestination = output;
                if (File.Exists(_ArchiveSource))
                {
                    try
                    {
                        using (Stream stream = File.OpenRead(_ArchiveSource))
                        using (var reader = ReaderFactory.Open(stream))
                        {
                            while (reader.MoveToNextEntry())
                            {
                                if (!reader.Entry.IsDirectory)
                                {
                                    Console.WriteLine(reader.Entry.Key);
                                    reader.WriteEntryToDirectory(ArchiveDestination, new ExtractionOptions()
                                    {
                                        ExtractFullPath = true,
                                        Overwrite = true
                                    });
                                }
                            }
                        }
                        if (File.Exists($@"{ArchiveDestination}/mod.json"))
                        {
                            var metadata = JsonSerializer.Deserialize<Metadata>(File.ReadAllText($@"{ArchiveDestination}/mod.json"));
                            metadata.lastupdate = updateTime;
                            string metadataString = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText($@"{ArchiveDestination}/mod.json", metadataString);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.WriteLine($"Couldn't extract {fileName}. ({e.Message})", LoggerType.Error);
                    }
                }
                File.Delete(_ArchiveSource);
            });

        }
        private static Uri CreateUri(string url)
        {
            Uri uri;
            if ((Uri.TryCreate(url, UriKind.Absolute, out uri) || Uri.TryCreate("http://" + url, UriKind.Absolute, out uri)) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Use validated URI here
                string host = uri.DnsSafeHost;
                if (uri.Segments.Length != 3)
                    return null;
                switch (host)
                {
                    case "www.gamebanana.com":
                    case "gamebanana.com":
                        return uri;
                }
            }
            return null;
        }
    }
}
