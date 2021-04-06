using Onova.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Common;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;

namespace FNF_Mod_Manager
{
    public class ZipExtractor : IPackageExtractor
    {
        public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath,
            IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            using (var archive = ZipArchive.Open(sourceFilePath))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(destDirPath, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
            File.Delete(@$"{sourceFilePath}");
            // Move the folders to the right place
            string parentPath = Directory.GetParent(destDirPath).FullName;
            Directory.Move(Directory.GetDirectories(destDirPath)[0], $@"{parentPath}/FileDaddy");
            Directory.Delete(destDirPath);
            Directory.Move($@"{parentPath}/FileDaddy", destDirPath);
        }

    }
}
