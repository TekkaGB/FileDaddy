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
                foreach (var file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories)
                    .Where(x => x.IndexOf("readme", 0, StringComparison.CurrentCultureIgnoreCase) == -1 // Ignore readmes
                    && x.IndexOf("read me", 0, StringComparison.CurrentCultureIgnoreCase) == -1))
                {
                    var fileKey = Path.GetFileName(file);
                    var filesFound = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                            .Where(a => string.Equals(Path.GetFileName(a), fileKey,
                            StringComparison.InvariantCultureIgnoreCase));
                    // Check if the file isn't unique (Week 7 structure)
                    if (filesFound.Count() > 1)
                    {
                        logger.WriteLine($"More than one {fileKey} found in {path}, now searching for {Path.GetFileName(Path.GetDirectoryName(file))}/{Path.GetFileName(file)}...", LoggerType.Info);
                        fileKey = $"{Path.GetFileName(Path.GetDirectoryName(file))}/{Path.GetFileName(file)}";
                        filesFound = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                            .Where(a => string.Equals($"{Path.GetFileName(Path.GetDirectoryName(a))}/{Path.GetFileName(a)}", 
                            fileKey, StringComparison.InvariantCultureIgnoreCase));
                    }

                    // Inst and Voice are named and organized differently pre/post week 7 update
                    if (filesFound.Count() == 0 && (Path.GetFileName(file).Contains("inst", StringComparison.InvariantCultureIgnoreCase)
                        || Path.GetFileName(file).Contains("voice", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        // Pre Week 7
                        if (!Directory.Exists($"{path}/songs"))
                        {
                            // Look for pre week 7 <folder>_Inst.ogg instead of post week 7 <folder>/Inst.ogg
                            fileKey = $"{Path.GetFileName(Path.GetDirectoryName(file))}_{Path.GetFileName(file)}";
                            filesFound = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                                .Where(a => string.Equals(Path.GetFileName(a),
                                fileKey, StringComparison.InvariantCultureIgnoreCase));
                        }
                        // Post Week 7
                        else
                        {
                            // Look for post week 7 <folder>/Inst.ogg instead of pre week 7 <folder>_Inst.ogg
                            fileKey = fileKey.Replace("_", "/");
                            filesFound = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                                .Where(a => string.Equals($"{Path.GetFileName(Path.GetDirectoryName(a))}/{Path.GetFileName(a)}",
                                fileKey, StringComparison.InvariantCultureIgnoreCase));
                        }
                    }

                    if (fileLookup.ContainsKey(fileKey))
                    {
                        var asset = fileLookup[fileKey];
                        // .backups should already be created if in dictionary so only copy over
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
                    else if (filesFound.Count() == 1)
                    {
                        foreach (var asset in filesFound)
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
                                    if (!fileLookup.ContainsKey(fileKey))
                                        fileLookup.Add(fileKey, asset);
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
                    }
                    else
                    {
                        if (filesFound.Count() == 0)
                            logger.WriteLine($"Couldn't find {fileKey} in {path}, skipping...", LoggerType.Warning);
                        else
                            logger.WriteLine($"Couldn't find unique {fileKey} in {path}, skipping...", LoggerType.Warning);
                        ++buildWarnings;
                    }
                }
            }
            logger.WriteLine("Finished building!", LoggerType.Info);
            if (buildErrors > 0 || buildWarnings > 0)
                logger.WriteLine(buildErrors + " errors and " + buildWarnings + " warnings occurred during building. Please double-check before launching the game.", LoggerType.Warning);
        }
    }
}
