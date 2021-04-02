using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var buildWarnings = 0;
            var buildErrors = 0;
            foreach (var mod in mods)
            {
                foreach (var file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
                {
                    if (Directory.Exists($@"{mod}/assets") && file.Contains("assets"))
                    {
                        string filePath;
                        try
                        {
                            //Create list of modified assets
                            string[] split = file.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                            int index = split.ToList().IndexOf("assets") + 1;
                            filePath = string.Join('/', split[index..]);
                        }
                        catch (Exception e)
                        {
                            logger.WriteLine($"Couldn't parse path after assets ({e.Message})", LoggerType.Error);
                            ++buildErrors;
                            continue;
                        }
                        if (!File.Exists($@"{path}/{filePath}.backup") && File.Exists($@"{path}/{filePath}"))
                        {
                            logger.WriteLine($@"Backing up {path}/{filePath}...", LoggerType.Info);
                            try
                            {
                                //Create backup of unmodded file
                                File.Copy($@"{path}/{filePath}", $@"{path}/{filePath}.backup");
                            }
                            catch (Exception e)
                            {
                                logger.WriteLine($"Couldn't create backup ({e.Message})", LoggerType.Error);
                                ++buildErrors;
                                continue;
                            }
                        }
                        else if (!File.Exists($@"{path}/{filePath}.backup") && !File.Exists($@"{path}/{filePath}"))
                        {
                            logger.WriteLine($@"Skipping {path}/{filePath}, couldn't find the original asset (Check if it's misnamed or has the wrong path)", LoggerType.Warning);
                            ++buildWarnings;
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
                            ++buildErrors;
                        }
                    }
                }
            }

            logger.WriteLine("Finished building!", LoggerType.Info);
            if (buildErrors > 0 || buildWarnings > 0)
                logger.WriteLine(buildErrors + " errors and " + buildWarnings + " warnings occurred during building. Please double-check before launching the game.", LoggerType.Warning);
        }
    }
}
