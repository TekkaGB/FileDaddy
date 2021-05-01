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
            public ObservableCollection<RssFeed> RecentFeed { get; set; }
            public ObservableCollection<RssFeed> FeaturedFeed { get; set; }
            public ObservableCollection<RssFeed> DownloadFeed { get; set; }
            public ObservableCollection<RssFeed> LikeFeed { get; set; }
            public ObservableCollection<RssFeed> ViewFeed { get; set; }
        }
        private static Feeds feeds;
        public static async Task<ObservableCollection<RssFeed>> GetFeed(int page, FeedFilter filter)
        {
            if (feeds != null)
            {
                switch (filter)
                {
                    case FeedFilter.Recent:
                        if (feeds.RecentFeed != null)
                            return new ObservableCollection<RssFeed>(feeds.RecentFeed.Skip(10 * (page - 1)).Take(10));
                        break;
                    case FeedFilter.Featured:
                        if (feeds.FeaturedFeed != null)
                            return new ObservableCollection<RssFeed>(feeds.FeaturedFeed.Skip(10 * (page - 1)).Take(10));
                        break;
                    case FeedFilter.Downloads:
                        if (feeds.DownloadFeed != null)
                            return new ObservableCollection<RssFeed>(feeds.DownloadFeed.Skip(10 * (page - 1)).Take(10));
                        break;
                    case FeedFilter.Likes:
                        if (feeds.LikeFeed != null)
                            return new ObservableCollection<RssFeed>(feeds.LikeFeed.Skip(10 * (page - 1)).Take(10));
                        break;
                    case FeedFilter.Views:
                        if (feeds.ViewFeed != null)
                            return new ObservableCollection<RssFeed>(feeds.ViewFeed.Skip(10 * (page - 1)).Take(10));
                        break;
                }
            }
            else
            {
                feeds = new Feeds();
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
                var requestUrl = $"https://gamebanana.com/apiv3/Mod/Index?_aArgs[]=_aGame._idRow = 8694&_sRecordSchema=FileDaddy&_nPerpage=50{args}";
                var responseString = await httpClient.GetStringAsync(requestUrl);
                var response = JsonSerializer.Deserialize<List<RssFeed>>(responseString);

                foreach (var feed in response)
                {
                    feed.Stats = $"{feed.Downloads} downloads • {feed.Likes} likes • {feed.Views} views";
                    feed.Submitter = $"Submitter: {feed.Owner.Name}";
                    feed.Files = feed.Files.Where(x => !x.ContainsExe).ToList();
                    feed.Compatible = feed.Files.Count > 0;
                    feed.Image = new Uri($"{feed.Media[0].Base}/{feed.Media[0].File}");
                }
                switch (filter)
                {
                    case FeedFilter.Recent:
                        feeds.RecentFeed = new ObservableCollection<RssFeed>(response);
                        break;
                    case FeedFilter.Featured:
                        feeds.FeaturedFeed = new ObservableCollection<RssFeed>(response);
                        break;
                    case FeedFilter.Downloads:
                        feeds.DownloadFeed = new ObservableCollection<RssFeed>(response);
                        break;
                    case FeedFilter.Likes:
                        feeds.LikeFeed = new ObservableCollection<RssFeed>(response);
                        break;
                    case FeedFilter.Views:
                        feeds.ViewFeed = new ObservableCollection<RssFeed>(response);
                        break;
                }
                return new ObservableCollection<RssFeed>(response.Take(10));
            }
        }
        public static int GetSize(FeedFilter filter)
        {
            switch (filter)
            {
                case FeedFilter.Recent:
                    if (feeds.RecentFeed != null)
                        return feeds.RecentFeed.Count;
                    else
                        return -1;
                case FeedFilter.Featured:
                    if (feeds.FeaturedFeed != null)
                        return feeds.FeaturedFeed.Count;
                    else
                        return -1;
                case FeedFilter.Downloads:
                    if (feeds.DownloadFeed != null)
                        return feeds.DownloadFeed.Count;
                    else
                        return -1;
                case FeedFilter.Likes:
                    if (feeds.LikeFeed != null)
                        return feeds.LikeFeed.Count;
                    else
                        return -1;
                case FeedFilter.Views:
                    if (feeds.ViewFeed != null)
                        return feeds.ViewFeed.Count;
                    else
                        return -1;
            }
            return -1;
        }
    }
}
