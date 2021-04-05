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
            // Used so that entire iteration through directory isn't needed for redundant files across mod list
            var fileLookup = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var mod in mods)
            {
                logger.WriteLine($@"Beginning to inject files from {Path.GetFileName(mod)}...", LoggerType.Info);
                foreach (var file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
                {
                    if (fileLookup.ContainsKey(Path.GetFileName(file)))
                    {
                        var asset = fileLookup[Path.GetFileName(file)];
                        // .backups should already be created if in dictionary
                        try
                        {
                            logger.WriteLine($@"Copying over {file} to {asset}...", LoggerType.Info);
                            File.Copy(file, asset, true);
                        }
                        catch (Exception e)
                        {
                            logger.WriteLine($"Couldn't copy over modded file ({e.Message})", LoggerType.Error);
                            ++buildErrors;
                        }
                    }
                    else
                    {
                        foreach (var asset in Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                            .Where(a => string.Equals(Path.GetFileName(a), Path.GetFileName(file), StringComparison.InvariantCultureIgnoreCase)))
                        {
                            // Just in case it somehow already exists and wasn't added to file lookup dictionary
                            if (!File.Exists($"{asset}.backup"))
                            {
                                logger.WriteLine($@"Backing up {asset}...", LoggerType.Info);
                                try
                                {
                                    //Create backup of unmodded file
                                    File.Copy($@"{asset}", $@"{asset}.backup");
                                    // Add to file lookup dictionary
                                    if (!fileLookup.ContainsKey(Path.GetFileName(file)))
                                        fileLookup.Add(Path.GetFileName(file), asset);
                                }
                                catch (Exception e)
                                {
                                    logger.WriteLine($"Couldn't create backup ({e.Message})", LoggerType.Error);
                                    ++buildErrors;
                                    continue;
                                }
                            }
                            try
                            {
                                logger.WriteLine($@"Copying over {file} to {asset}...", LoggerType.Info);
                                File.Copy(file, asset, true);
                            }
                            catch (Exception e)
                            {
                                logger.WriteLine($"Couldn't copy over modded file ({e.Message})", LoggerType.Error);
                                ++buildErrors;
                            }
                        }
                        // Used to check if file was found
                        if (!fileLookup.ContainsKey(Path.GetFileName(file)))
                        {
                            logger.WriteLine($"Couldn't find {Path.GetFileName(file)} in {path}, skipping...)", LoggerType.Warning);
                            ++buildWarnings;
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
