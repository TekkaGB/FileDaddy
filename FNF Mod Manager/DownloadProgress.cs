using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNF_Mod_Manager
{
    public class DownloadProgress
    {
        public DownloadProgress(float percentage, long downloadedBytes, long totalBytes, string fileName)
        {
            DownloadedBytes = downloadedBytes;
            TotalBytes = totalBytes;
            Percentage = percentage;
            FileName = fileName;
        }

        public float Percentage { get; set; }
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public string FileName { get; set; }
    }
}
