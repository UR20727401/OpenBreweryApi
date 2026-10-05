using System;

namespace OpenBreweryApi.Models.Settings
{
    public sealed class OpenBrewerySettings
    {
        public string BaseUrl { get; set; } = "https://api.openbrewerydb.org/v1/breweries";
        public int CacheMinutes { get; set; } = 10;
        public string CacheKeyPrefix { get; set; } = "brewery_data";
    }
}