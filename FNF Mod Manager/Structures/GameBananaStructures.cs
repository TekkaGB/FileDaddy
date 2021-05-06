using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FNF_Mod_Manager
{
    public class GameBananaItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("views")]
        public int Views { get; set; }
        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }
        [JsonPropertyName("likes")]
        public int Likes { get; set; }
        [JsonPropertyName("Owner().name")]
        public string Owner { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("RootCategory().name")]
        public string RootCat { get; set; }
        [JsonPropertyName("Preview().sSubFeedImageUrl()")]
        public Uri SubFeedImage { get; set; }
        [JsonPropertyName("Preview().sStructuredDataFullsizeUrl()")]
        public Uri EmbedImage { get; set; }
        [JsonPropertyName("Updates().bSubmissionHasUpdates()")]
        public bool HasUpdates { get; set; }

        [JsonPropertyName("Updates().aGetLatestUpdates()")]
        public GameBananaItemUpdate[] Updates { get; set; }
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
        [JsonIgnore]
        public string ConvertedFileSize => StringConverters.FormatSize(Filesize);

        [JsonPropertyName("_sDownloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("_sDescription")]
        public string Description { get; set; }
        [JsonPropertyName("_bContainsExe")]
        public bool ContainsExe { get; set; }
        [JsonPropertyName("_nDownloadCount")]
        public int Downloads { get; set; }
        [JsonIgnore]
        public string DownloadString => StringConverters.FormatNumber(Downloads);

        [JsonPropertyName("_tsDateAdded")]
        public long DateAddedLong { get; set; }

        [JsonIgnore]
        public DateTime DateAdded => Epoch.AddSeconds(DateAddedLong);

        [JsonIgnore]
        public string TimeSinceUpload => StringConverters.FormatTimeAgo(DateTime.UtcNow - DateAdded);
    }
    public class GameBananaAPIV3
    {
        [JsonPropertyName("_aSubmitter")]
        public GameBananaMember Member { get; set; }
        [JsonPropertyName("_aCategory")]
        public GameBananaCategory Category { get; set; }
        [JsonPropertyName("_aFiles")]
        public List<GameBananaItemFile> Files { get; set; }
    }
    public class GameBananaInstallerIntegration
    {
        [JsonPropertyName("_sDownloadUrl")]
        public string Download { get; set; }
    }
    public class GameBananaCategory
    {
        [JsonPropertyName("_idRow")]
        public int ID { get; set; }
        [JsonPropertyName("_idParentCategoryRow")]
        public int RootID { get; set; }
        [JsonPropertyName("_sModelName")]
        public string Model { get; set; }
        [JsonPropertyName("_sName")]
        public string Name { get; set; }
        [JsonPropertyName("_sIconUrl")]
        public Uri Icon { get; set; }
    }
    public class GameBananaMember
    {
        [JsonPropertyName("_sName")]
        public string Name { get; set; }
        [JsonPropertyName("_sAvatarUrl")]
        public Uri Avatar { get; set; }
        [JsonPropertyName("_sUpicUrl")]
        public Uri Upic { get; set; }
        [JsonIgnore]
        public bool HasUpic => Upic.OriginalString.Length > 0;
    }
    public class GameBananaItemUpdate
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);
        [JsonPropertyName("_sTitle")]
        public string Title { get; set; }

        [JsonPropertyName("_aChangeLog")]
        public GameBananaItemUpdateChange[] Changes { get; set; }

        [JsonPropertyName("_sText")]
        public string Text { get; set; }

        [JsonPropertyName("_tsDateAdded")]
        public long DateAddedLong { get; set; }

        [JsonIgnore]
        public DateTime DateAdded => Epoch.AddSeconds(DateAddedLong);
    }
    public class GameBananaItemUpdateChange
    {
        [JsonPropertyName("cat")]
        public string Category { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
    public class GameBananaRecord
    {
        [JsonPropertyName("_sName")]
        public string Title { get; set; }
        [JsonPropertyName("_sProfileUrl")]
        public Uri Link { get; set; }
        [JsonIgnore]
        public Uri Image => new Uri($"{Media[0].Base}/{Media[0].File}");
        [JsonPropertyName("_aPreviewMedia")]
        public List<GameBananaImage> Media { get; set; }
        [JsonPropertyName("_sDescription")]
        public string Description { get; set; }
        [JsonPropertyName("_nViewCount")]
        public int Views { get; set; }
        [JsonPropertyName("_nLikeCount")]
        public int Likes { get; set; }
        [JsonPropertyName("_nDownloadCount")]
        public int Downloads { get; set; }
        [JsonIgnore]
        public string DownloadString => StringConverters.FormatNumber(Downloads);
        [JsonIgnore]
        public string ViewString => StringConverters.FormatNumber(Views);
        [JsonIgnore]
        public string LikeString => StringConverters.FormatNumber(Likes);
        [JsonPropertyName("_aSubmitter")]
        public GameBananaMember Owner { get; set; }
        [JsonPropertyName("_aFiles")]
        public List<GameBananaItemFile> AllFiles { get; set; }
        [JsonIgnore]
        public List<GameBananaItemFile> Files => AllFiles.Where(x => !x.ContainsExe).ToList();
        [JsonPropertyName("_aCategory")]
        public GameBananaCategory Category { get; set; }
        [JsonPropertyName("_aRootCategory")]
        public GameBananaCategory RootCategory { get; set; }
        [JsonIgnore]
        public string CategoryName => StringConverters.FormatSingular(RootCategory.Name, Category.Name);
        [JsonIgnore]
        public bool Compatible => Files.Count > 0 && Category.ID != 3827;

        [JsonPropertyName("_tsDateUpdated")]
        public long DateUpdatedLong { get; set; }
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        [JsonIgnore]
        public DateTime DateUpdated => Epoch.AddSeconds(DateUpdatedLong);
        [JsonPropertyName("_tsDateAdded")]
        public long DateAddedLong { get; set; }

        [JsonIgnore]
        public DateTime DateAdded => Epoch.AddSeconds(DateAddedLong);
        [JsonIgnore]
        public string DateAddedFormatted => $"Added {StringConverters.FormatTimeAgo(DateTime.UtcNow - DateAdded)}";//{DateAdded.ToString("M/d/yyyy")}";
        [JsonIgnore]
        public bool HasUpdates => DateAdded.CompareTo(DateUpdated) != 0;
        [JsonIgnore]
        public string DateUpdatedAgo => $"Updated {StringConverters.FormatTimeAgo(DateTime.UtcNow - DateUpdated)}";
    }
    public class GameBananaModList
    {
        [JsonPropertyName("_aMetadata")]
        public GameBananaMetadata Metadata { get; set; }
        [JsonPropertyName("_aRecords")]
        public ObservableCollection<GameBananaRecord> Records { get; set; }
    }
    public class GameBananaMetadata
    {
        [JsonPropertyName("_nRecordCount")]
        public int Records { get; set; }
        [JsonPropertyName("_nTotalRecordCount")]
        public int TotalRecords { get; set; }
        [JsonPropertyName("_nPageCount")]
        public int TotalPages { get; set; }
    }
    public class GameBananaImage
    {
        [JsonPropertyName("_sBaseUrl")]
        public Uri Base { get; set; }
        [JsonPropertyName("_sFile")]
        public Uri File { get; set; }
    }
}
