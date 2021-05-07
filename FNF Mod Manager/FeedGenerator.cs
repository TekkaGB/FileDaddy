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
    public static class FeedGenerator
    {
        private static Dictionary<string, GameBananaModList> feed;
        public static async Task<ObservableCollection<GameBananaRecord>> GetFeed(int page, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, bool pending, int perPage)
        {
            if (feed == null)
                feed = new Dictionary<string, GameBananaModList>();
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "FileDaddy");
                var requestUrl = GenerateUrl(page, filter, category, subcategory, pending, perPage);               
                if (feed.ContainsKey(requestUrl) && feed[requestUrl].IsValid)
                    return feed[requestUrl].Records;
                var responseString = await httpClient.GetStringAsync(requestUrl);
                var response = JsonSerializer.Deserialize<GameBananaModList>(responseString);
                if (!feed.ContainsKey(requestUrl))
                    feed.Add(requestUrl, response);
                else
                    feed[requestUrl] = response;
                return response.Records;
            }
        }
        private static string GenerateUrl(int page, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, bool pending, int perPage)
        {
            // Base
            var url = "https://gamebanana.com/apiv3/Mod/";
            // Different starting endpoint if requesting all mods instead of specific category
            if (category.ID != null)
                url += "ByCategory?";
            else
                url += "ByGame? _aGameRowIds[] = 8694&";
            // Consistent args
            url += $"&_aArgs[]=_sbIsNsfw = false&_sRecordSchema=FileDaddy&_bReturnMetadata=true&_nPerpage={perPage}";
            // Sorting filter
            switch (filter)
            {
                case FeedFilter.Recent:
                    url += "&_sOrderBy=_tsDateUpdated,DESC";
                    break;
                case FeedFilter.Featured:
                    url += "&_aArgs[]=_sbWasFeatured = true& _sOrderBy=_tsDateAdded,DESC";
                    break;
                case FeedFilter.Popular:
                    url += "&_sOrderBy=_nDownloadCount,DESC";
                    break;
            }
            // Choose subcategory or category
            if (subcategory.ID != null)
                url += $"&_aCategoryRowIds[]={subcategory.ID}";
            else if (category.ID != null)
                url += $"&_aCategoryRowIds[]={category.ID}";
            
            // Include pending submissions
            if (pending)
                url += "&_bIncludePending=true";
            // Get page number
            url += $"&_nPage={page}";
            return url;
        }
        public static GameBananaMetadata GetMetadata(int page, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, bool pending, int perPage)
        {
            var url = GenerateUrl(page, filter, category, subcategory, pending, perPage);
            if (feed.ContainsKey(url))
                return feed[url].Metadata;
            else
                return null;
        }
    }
}
