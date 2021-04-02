using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNF_Mod_Manager
{
    public static class ModLoader
    {
        // Restore all backups created from previous build
        public static void Restart(string path, Logger logger)
        {
            foreach (var file in Directory.GetFiles(path, "*.backup", SearchOption.AllDirectories))
            {
                string actualFile = Path.ChangeExtension(file, null);
                logger.WriteLine($"Restoring {actualFile}...", LoggerType.Info);
                if (File.Exists(actualFile))
                {
                    try
                    {
                        File.Delete(actualFile);
                    }
                    catch (Exception e)
                    {
                        logger.WriteLine($@"Couldn't delete modded {actualFile} ({e.Message})", LoggerType.Error);
                        continue;
                    }
                }
                try
                {
                    File.Move(file, actualFile);
                }
                catch (Exception e)
                {
                    logger.WriteLine($@"Couldn't move {actualFile} back to {file} ({e.Message})", LoggerType.Error);
                }
            }
        }
        // Copy over mod files in order of ModList
        public static void Build(string path, List<string> mods, Logger logger)
        {
            foreach (var mod in mods)
            {
                foreach (var file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
                {
                    if (Directory.Exists($@"{mod}/assets") && file.Contains("assets"))
                    {
                        string filePath;
                        try
                        {
                            string[] split = file.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                            int index = split.ToList().IndexOf("assets") + 1;
                            filePath = string.Join('/', split[index..]);
                        }
                        catch (Exception e)
                        {
                            logger.WriteLine($"Couldn't parse path after assets ({e.Message})", LoggerType.Error);
                            continue;
                        }
                        if (!File.Exists($@"{path}/{filePath}.backup") && File.Exists($@"{path}/{filePath}"))
                        {
                            logger.WriteLine($@"Backing up {path}/{filePath}...", LoggerType.Info);
                            try
                            {
                                File.Copy($@"{path}/{filePath}", $@"{path}/{filePath}.backup");
                            }
                            catch (Exception e)
                            {
                                logger.WriteLine($"Couldn't create backup ({e.Message})", LoggerType.Error);
                                continue;
                            }
                        }
                        else if (!File.Exists($@"{path}/{filePath}.backup") && !File.Exists($@"{path}/{filePath}"))
                        {
                            logger.WriteLine($@"Skipping {path}/{filePath}, couldn't find the original asset (Check if its misnamed or has the wrong path)", LoggerType.Warning);
                            continue;
                        }
                        try
                        {
                            logger.WriteLine($@"Copying over {file} to {path}/{filePath}...", LoggerType.Info);
                            File.Copy(file, $@"{path}/{filePath}", true);
                        }
                        catch (Exception e)
                        {
                            logger.WriteLine($"Couldn't copy over modded file ({e.Message})", LoggerType.Error);
                        }
                    }
                }
            }
        }
    }
}
