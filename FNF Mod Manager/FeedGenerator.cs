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
    public enum TypeFilter
    {
        Mods,
        WiPs,
        Sounds
    }
    public static class FeedGenerator
    {
        private static Dictionary<string, GameBananaModList> feed;
        public static bool error;
        public static Exception exception;
        public static GameBananaModList CurrentFeed;
        public static double GetHeader(this HttpResponseMessage request, string key)
        {
            IEnumerable<string> keys = null;
            if (!request.Headers.TryGetValues(key, out keys))
                return -1;
            return Double.Parse(keys.First());
        }
        public static async Task GetFeed(int page, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, bool pending, int perPage)
        {
            error = false;
            if (feed == null)
                feed = new Dictionary<string, GameBananaModList>();
            using (var httpClient = new HttpClient())
            {
                var requestUrl = GenerateUrl(page, type, filter, category, subcategory, pending, perPage);
                if (feed.ContainsKey(requestUrl) && feed[requestUrl].IsValid)
                {
                    CurrentFeed = feed[requestUrl];
                    return;
                }
                GameBananaModList modList = new();
                try
                {
                    var response = await httpClient.GetAsync(requestUrl);
                    var records = JsonSerializer.Deserialize<ObservableCollection<GameBananaRecord>>(await response.Content.ReadAsStringAsync());
                    modList.Records = records;
                    var numRecords = response.GetHeader("X-GbApi-Metadata_nRecordCount");
                    if (numRecords != -1)
                    {
                        var totalPages = Math.Ceiling(numRecords / Convert.ToDouble(perPage));
                        if (totalPages == 0)
                            totalPages = 1;
                        modList.TotalPages = totalPages;
                    }
                }
                catch (Exception e)
                {
                    error = true;
                    exception = e;
                    return;
                }
                if (!feed.ContainsKey(requestUrl))
                    feed.Add(requestUrl, modList);
                else
                    feed[requestUrl] = modList;
                CurrentFeed = modList;
            }
        }
        private static string GenerateUrl(int page, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, bool pending, int perPage)
        {
            // Base
            var url = "https://gamebanana.com/apiv4/";
            switch (type)
            {
                case TypeFilter.Mods:
                    url += "Mod/";
                    break;
                case TypeFilter.Sounds:
                    url += "Sound/";
                    break;
                case TypeFilter.WiPs:
                    url += "Wip/";
                    break;
            }
            // Different starting endpoint if requesting all mods instead of specific category
            if (category.ID != null)
                url += "ByCategory?";
            else
                url += "ByGame?_aGameRowIds[]=8694&";
            // Consistent args
            url += $"&_aArgs[]=_sbIsNsfw = false&_sRecordSchema=FileDaddy&_nPerpage={perPage}";
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
                url += "&_bIncludeUpcoming=true";
            // Get page number
            url += $"&_nPage={page}";
            return url;
        }
    }
}
