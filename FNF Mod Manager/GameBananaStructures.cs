using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FNF_Mod_Manager
{
    public class GameBananaItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("Files().aFiles()")]
        public Dictionary<string, GameBananaItemFile> Files { get; set; }

    }
    public class GameBananaItemFile
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        [JsonPropertyName("_sFile")]
        public string FileName { get; set; }

        [JsonPropertyName("_nFilesize")]
        public long Filesize { get; set; }

        [JsonPropertyName("_sDownloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("_sDescription")]
        public string Description { get; set; }

        [JsonPropertyName("_tsDateAdded")]
        public long DateAddedLong { get; set; }
    }
}
