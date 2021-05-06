using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

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
        Skins,
        Stages,
        Translations,
        UI
    }
    public static class FeedGenerator
    {
        private static Dictionary<string, GameBananaModList> feed;
        public static async Task<ObservableCollection<GameBananaRecord>> GetFeed(int page, FeedFilter filter, CategoryFilter category, int subcategory, bool pending, int perPage)
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
                var requestUrl = GenerateUrl(page, filter, category, subcategory, pending, perPage);                
                if (feed.ContainsKey(requestUrl))
                    return feed[requestUrl].Records;
                var responseString = await httpClient.GetStringAsync(requestUrl);
                var response = JsonSerializer.Deserialize<GameBananaModList>(responseString);
                if (!feed.ContainsKey(requestUrl))
                    feed.Add(requestUrl, response);
                return response.Records;
            }
        }
        private static string GenerateUrl(int page, FeedFilter filter, CategoryFilter category, int subcategory, bool pending, int perPage)
        {
            var baseUrl = "";
            var args = "";
            switch (filter)
            {
                case FeedFilter.Recent:
                    args = "&_sOrderBy=_tsDateUpdated,DESC";
                    break;
                case FeedFilter.Featured:
                    args = "&_aArgs[]=_sbWasFeatured = true& _sOrderBy = _tsDateAdded,DESC";
                    break;
                case FeedFilter.Popular:
                    args = "&_sOrderBy=_nDownloadCount,DESC";
                    break;
            }
            switch (category)
            {
                case CategoryFilter.CustomSongs:
                    switch (subcategory)
                    {
                        case 0:
                            args += "&_aCategoryRowIds[]=3819";
                            break;
                        case 1:
                            args += "&_aCategoryRowIds[]=3830";
                            break;
                        case 2:
                            args += "&_aCategoryRowIds[]=3831";
                            break;
                        case 3:
                            args += "&_aCategoryRowIds[]=3832";
                            break;
                        case 4:
                            args += "&_aCategoryRowIds[]=3834";
                            break;
                        case 5:
                            args += "&_aCategoryRowIds[]=3835";
                            break;
                        case 6:
                            args += "&_aCategoryRowIds[]=3842";
                            break;
                        case 7:
                            args += "&_aCategoryRowIds[]=3889";
                            break;
                    }
                    break;
                case CategoryFilter.CustomSongsSkins:
                    switch (subcategory)
                    {
                        case 0:
                            args += "&_aCategoryRowIds[]=3821";
                            break;
                        case 1:
                            args += "&_aCategoryRowIds[]=3836";
                            break;
                        case 2:
                            args += "&_aCategoryRowIds[]=3837";
                            break;
                        case 3:
                            args += "&_aCategoryRowIds[]=3838";
                            break;
                        case 4:
                            args += "&_aCategoryRowIds[]=3839";
                            break;
                        case 5:
                            args += "&_aCategoryRowIds[]=3840";
                            break;
                        case 6:
                            args += "&_aCategoryRowIds[]=3843";
                            break;
                        case 7:
                            args += "&_aCategoryRowIds[]=3890";
                            break;
                    }
                    break;
                case CategoryFilter.Executables:
                    args += "&_aCategoryRowIds[]=3827";
                    break;
                case CategoryFilter.RemixesRecharts:
                    args += "&_aCategoryRowIds[]=3825";
                    break;
                case CategoryFilter.RemixesRechartsSkins:
                    args += "&_aCategoryRowIds[]=3826";
                    break;
                case CategoryFilter.Skins:
                    switch (subcategory)
                    {
                        case 0:
                            args += "&_aCategoryRowIds[]=3833";
                            break;
                        case 1:
                            args += "&_aCategoryRowIds[]=7864";
                            break;
                        case 2:
                            args += "&_aCategoryRowIds[]=7863";
                            break;
                        case 3:
                            args += "&_aCategoryRowIds[]=7866";
                            break;
                        case 4:
                            args += "&_aCategoryRowIds[]=7865";
                            break;
                        case 5:
                            args += "&_aCategoryRowIds[]=7867";
                            break;
                        case 6:
                            args += "&_aCategoryRowIds[]=7868";
                            break;
                        case 7:
                            args += "&_aCategoryRowIds[]=7870";
                            break;
                        case 8:
                            args += "&_aCategoryRowIds[]=7871";
                            break;
                        case 9:
                            args += "&_aCategoryRowIds[]=7869";
                            break;
                        case 10:
                            args += "&_aCategoryRowIds[]=7872";
                            break;
                        case 11:
                            args += "&_aCategoryRowIds[]=7874";
                            break;
                        case 12:
                            args += "&_aCategoryRowIds[]=7862";
                            break;
                    }
                    break;
                case CategoryFilter.Stages:
                    switch (subcategory)
                    {
                        case 0:
                            args += "&_aCategoryRowIds[]=5064";
                            break;
                        case 1:
                            args += "&_aCategoryRowIds[]=5066";
                            break;
                        case 2:
                            args += "&_aCategoryRowIds[]=5067";
                            break;
                        case 3:
                            args += "&_aCategoryRowIds[]=5068";
                            break;
                        case 4:
                            args += "&_aCategoryRowIds[]=5073";
                            break;
                        case 5:
                            args += "&_aCategoryRowIds[]=5072";
                            break;
                        case 6:
                            args += "&_aCategoryRowIds[]=5074";
                            break;
                        case 7:
                            args += "&_aCategoryRowIds[]=5065";
                            break;
                    }
                    break;
                case CategoryFilter.Translations:
                    args += "&_aCategoryRowIds[]=3828";
                    break;
                case CategoryFilter.UI:
                    switch (subcategory)
                    {
                        case 0:
                            args += "&_aCategoryRowIds[]=1931";
                            break;
                        case 1:
                            args += "&_aCategoryRowIds[]=1937";
                            break;
                        case 2:
                            args += "&_aCategoryRowIds[]=1934";
                            break;
                        case 3:
                            args += "&_aCategoryRowIds[]=1935";
                            break;
                        case 4:
                            args += "&_aCategoryRowIds[]=1933";
                            break;
                        case 5:
                            args += "&_aCategoryRowIds[]=1932";
                            break;
                    }
                    break;
            }
            
            if (category == CategoryFilter.All)
                baseUrl = "https://gamebanana.com/apiv3/Mod/Index?_aArgs[]=_aGame._idRow = 8694&_aArgs[]=_sbIsNsfw = false&_sRecordSchema=FileDaddy" +
                    $"&_nPerpage={perPage}&_bReturnMetadata=true";
            else
                baseUrl = "https://gamebanana.com/apiv3/Mod/ByCategory?_aArgs[]=_aGame._idRow = 8694&_aArgs[]=_sbIsNsfw = false&_sRecordSchema=FileDaddy" +
                    $"&_nPerpage={perPage}&_bReturnMetadata=true";
            
            if (pending)
                args += "&_bIncludePending=true";
            args += $"&_nPage={page}";
            return $"{baseUrl}{args}";
        }
        public static GameBananaMetadata GetMetadata(int page, FeedFilter filter, CategoryFilter category, int subcategory, bool pending, int perPage)
        {
            var url = GenerateUrl(page, filter, category, subcategory, pending, perPage);
            if (feed.ContainsKey(url))
                return feed[url].Metadata;
            else
                return null;
        }
    }
}
