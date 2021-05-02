﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

        [JsonPropertyName("_sDownloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("_sDescription")]
        public string Description { get; set; }
        [JsonPropertyName("_bContainsExe")]
        public bool ContainsExe { get; set; }

        [JsonPropertyName("_tsDateAdded")]
        public long DateAddedLong { get; set; }

        [JsonIgnore]
        public DateTime DateAdded => Epoch.AddSeconds(DateAddedLong);

        [JsonIgnore]
        public string TimeSinceUpload => StringConverters.FormatTimeSpan(DateTime.UtcNow - DateAdded);
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
        public Uri Image { get; set; }
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
        public string DownloadString { get; set; }
        [JsonIgnore]
        public string ViewString { get; set; }
        [JsonIgnore]
        public string LikeString { get; set; }
        [JsonPropertyName("_aSubmitter")]
        public GameBananaMember Owner { get; set; }
        [JsonIgnore]
        public string Submitter { get; set; }
        [JsonPropertyName("_aFiles")]
        public List<GameBananaItemFile> Files { get; set; }
        [JsonIgnore]
        public bool Compatible { get; set; }
    }
    public class GameBananaModList
    {
        [JsonPropertyName("_aMetadata")]
        public GameBananaMetadata Metadata { get; set; }
        [JsonPropertyName("_aRecords")]
        public List<GameBananaRecord> Records { get; set; }
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
