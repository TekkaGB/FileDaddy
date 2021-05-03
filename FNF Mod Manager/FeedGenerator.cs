using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FNF_Mod_Manager
{
    public enum FeedFilter
    {
        Featured,
        Recent,
        Downloads,
        Likes,
        Views
    }
    public static class FeedGenerator
    {
        public class Feeds
        {
            public FeedObject RecentFeed { get; set; }
            public FeedObject FeaturedFeed { get; set; }
            public FeedObject DownloadFeed { get; set; }
            public FeedObject LikeFeed { get; set; }
            public FeedObject ViewFeed { get; set; }
        }
        public class FeedObject
        {
            public GameBananaMetadata Metadata { get; set; }
            public Dictionary<int, ObservableCollection<GameBananaRecord>> Feed { get; set; }
            // If caching categories go with public Dictionary<int, Dictionary<int, ObservableCollection<GameBananaRecord>>> Feed { get; set; }
        }
        private static Feeds feeds;
        public static async Task<ObservableCollection<GameBananaRecord>> GetFeed(int page, FeedFilter filter)
        {
            if (feeds != null)
            {
                switch (filter)
                {
                    case FeedFilter.Recent:
                        if (feeds.RecentFeed.Feed.ContainsKey(page))
                            return feeds.RecentFeed.Feed[page];
                        break;
                    case FeedFilter.Featured:
                        if (feeds.FeaturedFeed.Feed.ContainsKey(page))
                            return feeds.FeaturedFeed.Feed[page];
                        break;
                    case FeedFilter.Downloads:
                        if (feeds.DownloadFeed.Feed.ContainsKey(page))
                            return feeds.DownloadFeed.Feed[page];
                        break;
                    case FeedFilter.Likes:
                        if (feeds.LikeFeed.Feed.ContainsKey(page))
                            return feeds.LikeFeed.Feed[page];
                        break;
                    case FeedFilter.Views:
                        if (feeds.ViewFeed.Feed.ContainsKey(page))
                            return feeds.ViewFeed.Feed[page];
                        break;
                }
            }
            else
            {
                feeds = new Feeds();
                feeds.RecentFeed = new FeedObject();
                feeds.FeaturedFeed = new FeedObject();
                feeds.DownloadFeed = new FeedObject();
                feeds.LikeFeed = new FeedObject();
                feeds.ViewFeed = new FeedObject();
                feeds.RecentFeed.Feed = new Dictionary<int, ObservableCollection<GameBananaRecord>>();
                feeds.FeaturedFeed.Feed = new Dictionary<int, ObservableCollection<GameBananaRecord>>();
                feeds.DownloadFeed.Feed = new Dictionary<int, ObservableCollection<GameBananaRecord>>();
                feeds.LikeFeed.Feed = new Dictionary<int, ObservableCollection<GameBananaRecord>>();
                feeds.ViewFeed.Feed = new Dictionary<int, ObservableCollection<GameBananaRecord>>();
            }
            /*
             * Featured = _aArgs[]=_sbWasFeatured = true
             * Most Downloaded = _sOrderBy=_nDownloadCount,DESC
             * Most Liked = _sOrderBy=_nLikeCount,DESC
             * Most Viewed = _sOrderBy=_nViewCount,DESC
             * Recent = <none>
             * Custom Songs = 3819
             * Custom Songs + Skins = 3821
             * Executables = 3827
             * Remixes/Recharts = 3825
             * Remixes/Recharts + Skins = 3826
             * Stages = 5064
             * Translations = 3828
             * UI = 1931
             */
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "FileDaddy");
                string args = "";
                switch (filter)
                {
                    case FeedFilter.Recent:
                        args = "";
                        break;
                    case FeedFilter.Featured:
                        args = "&_aArgs[]=_sbWasFeatured = true";
                        break;
                    case FeedFilter.Downloads:
                        args = "&_sOrderBy=_nDownloadCount,DESC";
                        break;
                    case FeedFilter.Likes:
                        args = "&_sOrderBy=_nLikeCount,DESC";
                        break;
                    case FeedFilter.Views:
                        args = "&_sOrderBy=_nViewCount,DESC";
                        break;
                }
                args += $"&_nPage={page}";
                var requestUrl = $"https://gamebanana.com/apiv3/Mod/Index?_aArgs[]=_aGame._idRow = 8694&_aArgs[]=_sbIsNsfw = false&_sRecordSchema=FileDaddy" +
                    $"&_nPerpage=20&_bReturnMetadata=true{args}";
                var responseString = await httpClient.GetStringAsync(requestUrl);
                var response = JsonSerializer.Deserialize<GameBananaModList>(responseString);

                foreach (var feed in response.Records)
                {
                    if (feed.Description.Length == 0)
                        feed.Description = null;
                    feed.DownloadString = StringConverters.FormatNumber(feed.Downloads);
                    feed.LikeString = StringConverters.FormatNumber(feed.Likes);
                    feed.ViewString = StringConverters.FormatNumber(feed.Views);
                    feed.Submitter = feed.Owner.Name;
                    feed.Files = feed.Files.Where(x => !x.ContainsExe).ToList();
                    feed.Compatible = feed.Files.Count > 0 && feed.Category.ID != 3827;
                    feed.Image = new Uri($"{feed.Media[0].Base}/{feed.Media[0].File}");
                }
                // Todo: Compare metadata and refresh cache if totalrecords change
                var records = new ObservableCollection<GameBananaRecord>(response.Records);
                switch (filter)
                {
                    case FeedFilter.Recent:
                        feeds.RecentFeed.Feed.Add(page, records);
                        feeds.RecentFeed.Metadata = response.Metadata;
                        break;
                    case FeedFilter.Featured:
                        feeds.FeaturedFeed.Feed.Add(page, records);
                        feeds.FeaturedFeed.Metadata = response.Metadata;
                        break;
                    case FeedFilter.Downloads:
                        feeds.DownloadFeed.Feed.Add(page, records);
                        feeds.DownloadFeed.Metadata = response.Metadata;
                        break;
                    case FeedFilter.Likes:
                        feeds.LikeFeed.Feed.Add(page, records);
                        feeds.LikeFeed.Metadata = response.Metadata;
                        break;
                    case FeedFilter.Views:
                        feeds.ViewFeed.Feed.Add(page, records);
                        feeds.ViewFeed.Metadata = response.Metadata;
                        break;
                }
                return records;
            }
        }
        public static int GetSize(FeedFilter filter)
        {
            switch (filter)
            {
                case FeedFilter.Recent:
                    if (feeds.RecentFeed != null)
                        return feeds.RecentFeed.Metadata.TotalRecords;
                    else
                        return -1;
                case FeedFilter.Featured:
                    if (feeds.FeaturedFeed != null)
                        return feeds.FeaturedFeed.Metadata.TotalRecords;
                    else
                        return -1;
                case FeedFilter.Downloads:
                    if (feeds.DownloadFeed != null)
                        return feeds.DownloadFeed.Metadata.TotalRecords;
                    else
                        return -1;
                case FeedFilter.Likes:
                    if (feeds.LikeFeed != null)
                        return feeds.LikeFeed.Metadata.TotalRecords;
                    else
                        return -1;
                case FeedFilter.Views:
                    if (feeds.ViewFeed != null)
                        return feeds.ViewFeed.Metadata.TotalRecords;
                    else
                        return -1;
            }
            return -1;
        }
        public static int GetMaxPage(FeedFilter filter)
        {
            switch (filter)
            {
                case FeedFilter.Recent:
                    if (feeds.RecentFeed != null)
                        return feeds.RecentFeed.Metadata.TotalPages;
                    else
                        return -1;
                case FeedFilter.Featured:
                    if (feeds.FeaturedFeed != null)
                        return feeds.FeaturedFeed.Metadata.TotalPages;
                    else
                        return -1;
                case FeedFilter.Downloads:
                    if (feeds.DownloadFeed != null)
                        return feeds.DownloadFeed.Metadata.TotalPages;
                    else
                        return -1;
                case FeedFilter.Likes:
                    if (feeds.LikeFeed != null)
                        return feeds.LikeFeed.Metadata.TotalPages;
                    else
                        return -1;
                case FeedFilter.Views:
                    if (feeds.ViewFeed != null)
                        return feeds.ViewFeed.Metadata.TotalPages;
                    else
                        return -1;
            }
            return -1;
        }
    }
}
