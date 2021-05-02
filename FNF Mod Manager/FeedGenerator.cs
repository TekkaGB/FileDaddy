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
            public ObservableCollection<GameBananaRecord> Feed { get; set; }
        }
        private static Feeds feeds;
        public static async Task<ObservableCollection<GameBananaRecord>> GetFeed(int page, FeedFilter filter)
        {
            if (feeds != null)
            {
                switch (filter)
                {
                    case FeedFilter.Recent:
                        if (feeds.RecentFeed != null && feeds.RecentFeed.Feed.Count > 20 * (page - 1))
                            return new ObservableCollection<GameBananaRecord>(feeds.RecentFeed.Feed.Skip(20 * (page - 1)).Take(20));
                        break;
                    case FeedFilter.Featured:
                        if (feeds.FeaturedFeed != null && feeds.FeaturedFeed.Feed.Count > 20 * (page - 1))
                            return new ObservableCollection<GameBananaRecord>(feeds.FeaturedFeed.Feed.Skip(20 * (page - 1)).Take(20));
                        break;
                    case FeedFilter.Downloads:
                        if (feeds.DownloadFeed != null && feeds.DownloadFeed.Feed.Count > 20 * (page - 1))
                            return new ObservableCollection<GameBananaRecord>(feeds.DownloadFeed.Feed.Skip(20 * (page - 1)).Take(20));
                        break;
                    case FeedFilter.Likes:
                        if (feeds.LikeFeed != null && feeds.LikeFeed.Feed.Count > 20 * (page - 1))
                            return new ObservableCollection<GameBananaRecord>(feeds.LikeFeed.Feed.Skip(20 * (page - 1)).Take(20));
                        break;
                    case FeedFilter.Views:
                        if (feeds.ViewFeed != null && feeds.ViewFeed.Feed.Count > 20 * (page - 1))
                            return new ObservableCollection<GameBananaRecord>(feeds.ViewFeed.Feed.Skip(20 * (page - 1)).Take(20));
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
                feeds.RecentFeed.Feed = new ObservableCollection<GameBananaRecord>();
                feeds.FeaturedFeed.Feed = new ObservableCollection<GameBananaRecord>();
                feeds.DownloadFeed.Feed = new ObservableCollection<GameBananaRecord>();
                feeds.LikeFeed.Feed = new ObservableCollection<GameBananaRecord>();
                feeds.ViewFeed.Feed = new ObservableCollection<GameBananaRecord>();
            }
            /*
             * Featured = _aArgs[]=_sbWasFeatured = true
             * Most Downloaded = _sOrderBy=_nDownloadCount,DESC
             * Most Liked = _sOrderBy=_nLikeCount,DESC
             * Most Viewed = _sOrderBy=_nViewCount,DESC
             * Recent = <none>
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
                    feed.Compatible = feed.Files.Count > 0;
                    feed.Image = new Uri($"{feed.Media[0].Base}/{feed.Media[0].File}");
                }
                // Todo: Compare metadata and refresh cache if totalrecords change
                switch (filter)
                {
                    case FeedFilter.Recent:
                        feeds.RecentFeed.Feed = new ObservableCollection<GameBananaRecord>(feeds.RecentFeed.Feed.Concat(response.Records));
                        feeds.RecentFeed.Metadata = response.Metadata;
                        break;
                    case FeedFilter.Featured:
                        feeds.FeaturedFeed.Feed = new ObservableCollection<GameBananaRecord>(feeds.FeaturedFeed.Feed.Concat(response.Records));
                        feeds.FeaturedFeed.Metadata = response.Metadata;
                        break;
                    case FeedFilter.Downloads:
                        feeds.DownloadFeed.Feed = new ObservableCollection<GameBananaRecord>(feeds.DownloadFeed.Feed.Concat(response.Records));
                        feeds.DownloadFeed.Metadata = response.Metadata;
                        break;
                    case FeedFilter.Likes:
                        feeds.LikeFeed.Feed = new ObservableCollection<GameBananaRecord>(feeds.LikeFeed.Feed.Concat(response.Records));
                        feeds.LikeFeed.Metadata = response.Metadata;
                        break;
                    case FeedFilter.Views:
                        feeds.ViewFeed.Feed = new ObservableCollection<GameBananaRecord>(feeds.ViewFeed.Feed.Concat(response.Records));
                        feeds.ViewFeed.Metadata = response.Metadata;
                        break;
                }
                return new ObservableCollection<GameBananaRecord>(response.Records);
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
    }
}
