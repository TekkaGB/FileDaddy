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
        Popular
    }
    public enum CategoryFilter
    {
        All,
        CustomSongs,
        CustomSongsSkins,
        Executables,
        RemixesRecharts,
        RemixesRechartsSkins,
        Stages,
        Translations,
        UI
    }
    public static class FeedGenerator
    {
        private static Dictionary<string, GameBananaModList> feed;
        public static async Task<ObservableCollection<GameBananaRecord>> GetFeed(int page, FeedFilter filter, CategoryFilter category)
        {
            if (feed == null)
                feed = new Dictionary<string, GameBananaModList>();
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
                var requestUrl = GenerateUrl(page, filter, category);                
                if (feed.ContainsKey(requestUrl))
                    return feed[requestUrl].Records;
                var responseString = await httpClient.GetStringAsync(requestUrl);
                var response = JsonSerializer.Deserialize<GameBananaModList>(responseString);
                feed.Add(requestUrl, response);
                return response.Records;
            }
        }
        private static string GenerateUrl(int page, FeedFilter filter, CategoryFilter category)
        {
            var args = "";
            switch (filter)
            {
                case FeedFilter.Recent:
                    args = "";
                    break;
                case FeedFilter.Featured:
                    args = "&_aArgs[]=_sbWasFeatured = true";
                    break;
                case FeedFilter.Popular:
                    args = "&_sOrderBy=_nDownloadCount,DESC";
                    break;
            }
            switch (category)
            {
                case CategoryFilter.CustomSongs:
                    args += "&_aArgs[]=_aCategory._idRow = 3819";
                    break;
                case CategoryFilter.CustomSongsSkins:
                    args += "&_aArgs[]=_aCategory._idRow = 3821";
                    break;
                case CategoryFilter.Executables:
                    args += "&_aArgs[]=_aCategory._idRow = 3827";
                    break;
                case CategoryFilter.RemixesRecharts:
                    args += "&_aArgs[]=_aCategory._idRow = 3825";
                    break;
                case CategoryFilter.RemixesRechartsSkins:
                    args += "&_aArgs[]=_aCategory._idRow = 3826";
                    break;
                case CategoryFilter.Stages:
                    args += "&_aArgs[]=_aCategory._idRow = 5064";
                    break;
                case CategoryFilter.Translations:
                    args += "&_aArgs[]=_aCategory._idRow = 3828";
                    break;
                case CategoryFilter.UI:
                    args += "&_aArgs[]=_aCategory._idRow = 1931";
                    break;
            }
            args += $"&_nPage={page}";
            return $"https://gamebanana.com/apiv3/Mod/Index?_aArgs[]=_aGame._idRow = 8694&_aArgs[]=_sbIsNsfw = false&_sRecordSchema=FileDaddy" +
                $"&_nPerpage=20&_bReturnMetadata=true{args}";
        }
        public static GameBananaMetadata GetMetadata(int page, FeedFilter filter, CategoryFilter category)
        {
            return feed[GenerateUrl(page, filter, category)].Metadata;
        }
    }
}
