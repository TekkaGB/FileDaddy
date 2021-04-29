﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace FNF_Mod_Manager
{
    public static class FeedGenerator
    {
        private static ObservableCollection<RssFeed> RecentFeed;
        public async static Task<ObservableCollection<RssFeed>> GetRecentFeed()
        {
            if (RecentFeed != null)
                return RecentFeed;
            IEnumerable<GBMod> modsEnum = Enumerable.Empty<GBMod>();
            // Grab multiple pages at once
            // TODO: split up large requestUrls
            for (int i = 1; i <= 3; i++)
            {
                XDocument feedXML = XDocument.Load($"https://api.gamebanana.com/Core/List/New?format=xml&gameid=8694&itemtype=Mod,Skin,Sound,Wip&include_updated=1&page={i}");

                var modPage = from feed in feedXML.Descendants("valueset")
                               select new GBMod
                               {
                                   MOD_TYPE = feed.Elements("value").ToList()[0].Value,
                                   MOD_ID = Int32.Parse(feed.Elements("value").ToList()[1].Value)
                               };
                modsEnum = modsEnum.Concat(modPage);
            }
            var mods = modsEnum.ToList();
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "FileDaddy");
                var requestUrl = $"https://api.gamebanana.com/Core/Item/Data?";
                foreach (var mod in mods)
                {
                    requestUrl += $"itemtype[]={mod.MOD_TYPE}&itemid[]={mod.MOD_ID}&fields[]=name,Owner().name,description," +
                        $"views,downloads,likes,Preview().sStructuredDataFullsizeUrl()&";
                }
                requestUrl += "return_keys=1";

                var responseString = await httpClient.GetStringAsync(requestUrl);
                var response = JsonSerializer.Deserialize<GameBananaItem[]>(responseString);
                var feedList = new List<RssFeed>();
                for (int i = 0; i < mods.Count; i++)
                {
                    requestUrl = $"https://gamebanana.com/apiv3/{mods[i].MOD_TYPE}/{mods[i].MOD_ID}";
                    var dataString = await httpClient.GetStringAsync(requestUrl);
                    GameBananaAPIV3 data = new GameBananaAPIV3();
                    try
                    {
                        data = JsonSerializer.Deserialize<GameBananaAPIV3>(dataString);
                    }
                    catch
                    {
                        continue;
                    }
                    // Files are null if trashed/withheld/not a downloadable mod
                    if (data.Files == null)
                    {
                        continue;
                    }
                    var feed = new RssFeed();
                    feed.Title = response[i].Name;
                    feed.Link = new Uri($"https://gamebanana.com/{mods[i].MOD_TYPE.ToLower()}s/{mods[i].MOD_ID}");
                    feed.Image = response[i].EmbedImage;
                    feed.Description = response[i].Description;
                    feed.Stats = $"{response[i].Downloads} downloads • {response[i].Likes} likes • {response[i].Views} views";
                    feed.Submitter = response[i].Owner;
                    feed.Files = data.Files.Where(x => !x.ContainsExe).ToList();
                    feed.Compatible = !data.Files.All(x => x.ContainsExe);
                    feedList.Add(feed);
                }
                RecentFeed = new ObservableCollection<RssFeed>(feedList.OrderByDescending(x => x.Compatible));
                return RecentFeed;
            }
        }
        
        public async static Task<ObservableCollection<RssFeed>> GetFeed()
        {
            XDocument feedXML = XDocument.Load("https://api.gamebanana.com/Rss/Featured?gameid=8694");

            var feeds = from feed in feedXML.Descendants("item")
                        select new RssFeed
                        {
                            Title = feed.Element("title").Value,
                            Link = new Uri(feed.Element("link").Value),
                            Image = new Uri(feed.Element("image").Value)
                        };
            var feedList = feeds.ToList();

            GameBananaAPIV3 data = new GameBananaAPIV3();
            GameBananaItem response = new GameBananaItem();
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "FileDaddy");
                for (int i = feedList.Count - 1; i >= 0; i--)
                {
                    var MOD_TYPE = char.ToUpper(feedList[i].Link.Segments[1][0]) + feedList[i].Link.Segments[1].Substring(1, feedList[i].Link.Segments[1].Length - 3);
                    var MOD_ID = feedList[i].Link.Segments[2];
                    var requestUrl = $"https://api.gamebanana.com/Core/Item/Data?itemtype={MOD_TYPE}&itemid={MOD_ID}&fields=" +
                        $"Owner().name,description," +
                        $"views,downloads,likes,RootCategory().name&return_keys=1";
                    var responseString = await httpClient.GetStringAsync(requestUrl);
                    response = JsonSerializer.Deserialize<GameBananaItem>(responseString);
                    requestUrl = $"https://gamebanana.com/apiv3/{MOD_TYPE}/{MOD_ID}";
                    var dataString = await httpClient.GetStringAsync(requestUrl);
                    try
                    {
                        data = JsonSerializer.Deserialize<GameBananaAPIV3>(dataString);
                    }
                    catch
                    {
                        continue;
                    }
                    // Files are null if trashed/withheld/not a downloadable mod
                    if (data.Files == null)
                    {
                        feedList.RemoveAt(i);
                        continue;
                    }
                    feedList[i].Compatible = !data.Files.All(x => x.ContainsExe);
                    feedList[i].Stats = $"{response.Downloads} downloads • {response.Likes} likes • {response.Views} views";
                    feedList[i].Submitter = $"Submitter: {data.Member.Name}";
                    feedList[i].Description = response.Description;
                    feedList[i].Files = data.Files.Where(x => !x.ContainsExe).ToList();
                }
                // Show compatible files first
                return new ObservableCollection<RssFeed>(feedList.OrderByDescending(x => x.Compatible));
            }
        }
    }
}