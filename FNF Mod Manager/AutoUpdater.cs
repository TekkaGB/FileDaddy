using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Onova;
using Onova.Services;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using FNF_Mod_Manager.UI;

namespace FNF_Mod_Manager
{
    public class AutoUpdater
    {
        private static string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static ProgressBox progressBox;
        private static HttpClient client = new HttpClient();
        public static async Task<bool> CheckForFileDaddyUpdate(CancellationTokenSource cancellationToken)
        {
            // Get Version Number
            var localVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            try
            {
                var requestUrl = $"https://api.gamebanana.com/Core/Item/Data?itemtype=Tool&itemid=7015&fields=Updates().bSubmissionHasUpdates(),Updates().aGetLatestUpdates(),Files().aFiles()&return_keys=1";
                GameBananaItem response = JsonSerializer.Deserialize<GameBananaItem>(await client.GetStringAsync(requestUrl));
                if (response == null)
                {
                    Console.WriteLine("[ERROR] Error whilst checking for FileDaddy update: No response from GameBanana API");
                    return false;
                }
                if (response.HasUpdates)
                {
                    GameBananaItemUpdate[] updates = response.Updates;
                    string updateTitle = updates[0].Title;
                    Match onlineVersionMatch = Regex.Match(updateTitle, @"(?<version>([1-9]+\.?)+)[^a-zA-Z]");
                    string onlineVersion = null;
                    if (onlineVersionMatch.Success)
                    {
                        onlineVersion = onlineVersionMatch.Value;
                    }
                    if (UpdateAvailable(onlineVersion, localVersion))
                    {
                        ChangelogBox notification = new ChangelogBox(updates[0], "FileDaddy", $"A new version of FileDaddy is available (v{onlineVersion}), would you like to update now?");
                        notification.ShowDialog();
                        notification.Activate();
                        if (notification.YesNo)
                        {
                            List<GameBananaItemFile> files = response.Files;
                            string downloadUrl = files.ElementAt(0).DownloadUrl;
                            string fileName = files.ElementAt(0).FileName;
                            // Download the update
                            await DownloadFileDaddy(downloadUrl, fileName, onlineVersion, new Progress<DownloadProgress>(ReportUpdateProgress), cancellationToken);
                            // Notify that the update is about to happen
                            MessageBox.Show($"Finished downloading {fileName}!\nFileDaddy will now restart.", "Notification", MessageBoxButton.OK);
                            // Update FileDaddy
                            UpdateManager updateManager = new UpdateManager(new LocalPackageResolver($"{assemblyLocation}/Downloads/FileDaddyUpdate"), new ZipExtractor());
                            if (!Version.TryParse(onlineVersion, out Version version))
                            {
                                MessageBox.Show($"Error parsing {onlineVersion}!\nCancelling update.", "Notification", MessageBoxButton.OK);
                                return false;
                            }
                            // Updates and restarts FileDaddy
                            await updateManager.PrepareUpdateAsync(version);
                            updateManager.LaunchUpdater(version);
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error whilst checking for updates: {e.Message}\n{e.StackTrace}", "Notification", MessageBoxButton.OK);
            }
            return false;
        }
        private static async Task DownloadFileDaddy(string uri, string fileName, string version, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
        {
            try
            {
                // Create the downloads folder if necessary
                if (!Directory.Exists(@$"{assemblyLocation}/Downloads"))
                {
                    Directory.CreateDirectory(@$"{assemblyLocation}/Downloads");
                }
                // Create the downloads folder if necessary
                if (!Directory.Exists(@$"{assemblyLocation}/Downloads/FileDaddyUpdate"))
                {
                    Directory.CreateDirectory(@$"{assemblyLocation}\Downloads\FileDaddyUpdate");
                }
                progressBox = new ProgressBox(cancellationToken);
                progressBox.progressBar.Value = 0;
                progressBox.progressText.Text = $"Downloading {fileName}";
                progressBox.Title = "FileDaddy Update Progress";
                progressBox.finished = false;
                progressBox.Show();
                progressBox.Activate();
                // Write and download the file
                using (var fs = new FileStream(
                    $@"{assemblyLocation}/Downloads/FileDaddyUpdate/{fileName}", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                }
                // Rename the file
                if (!File.Exists($@"{assemblyLocation}/Downloads/FileDaddyUpdate/{version}.7z"))
                {
                    File.Move($@"{assemblyLocation}/Downloads/FileDaddyUpdate/{fileName}", $@"{assemblyLocation}/Downloads/FileDaddyUpdate/{version}.7z");
                }
                progressBox.Close();
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                File.Delete(@$"{assemblyLocation}/Downloads/FileDaddyUpdate/{fileName}");
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
        private static bool UpdateAvailable(string onlineVersion, string localVersion)
        {
            if (onlineVersion is null || localVersion is null)
            {
                return false;
            }
            string[] onlineVersionParts = onlineVersion.Split('.');
            string[] localVersionParts = localVersion.Split('.');
            // Pad the version if one has more parts than another (e.g. 1.2.1 and 1.2)
            if (onlineVersionParts.Length > localVersionParts.Length)
            {
                for (int i = localVersionParts.Length; i < onlineVersionParts.Length; i++)
                {
                    localVersionParts = localVersionParts.Append("0").ToArray();
                }
            }
            else if (localVersionParts.Length > onlineVersionParts.Length)
            {
                for (int i = onlineVersionParts.Length; i < localVersionParts.Length; i++)
                {
                    onlineVersionParts = onlineVersionParts.Append("0").ToArray();
                }
            }
            // Decide whether the online version is new than local
            for (int i = 0; i < onlineVersionParts.Length; i++)
            {
                if (!int.TryParse(onlineVersionParts[i], out _))
                {
                    Console.WriteLine($"[ERROR] Couldn't parse {onlineVersion}");
                    return false;
                }
                if (!int.TryParse(localVersionParts[i], out _))
                {
                    Console.WriteLine($"[ERROR] Couldn't parse {localVersion}");
                    return false;
                }
                if (int.Parse(onlineVersionParts[i]) > int.Parse(localVersionParts[i]))
                {
                    return true;
                }
                else if (int.Parse(onlineVersionParts[i]) != int.Parse(localVersionParts[i]))
                {
                    return false;
                }
            }
            return false;
        }

        private static Uri CreateUri(string url)
        {
            Uri uri;
            if ((Uri.TryCreate(url, UriKind.Absolute, out uri) || Uri.TryCreate("http://" + url, UriKind.Absolute, out uri)) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return uri;
            }
            return null;
        }
    }
}
