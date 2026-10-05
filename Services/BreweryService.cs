using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenBreweryApi.Helpers;
using OpenBreweryApi.Interfaces;
using OpenBreweryApi.Models;
using OpenBreweryApi.Models.Requests;
using OpenBreweryApi.Models.Settings;

namespace OpenBreweryApi.Services
{
    public class BreweryService : IBreweryService
    {
        private readonly IHttpClientFactory _factory;
        private readonly IMemoryCache _cache;
        private readonly AsyncLockProvider _lockProvider;
        private readonly OpenBrewerySettings _settings;

        public BreweryService(
            IHttpClientFactory factory,
            IMemoryCache cache,
            AsyncLockProvider lockProvider,
            IOptions<OpenBrewerySettings> settings)
        {
            _factory = factory;
            _cache = cache;
            _lockProvider = lockProvider;
            _settings = settings.Value;
        }

        // Backwards-compatible signature
        public Task<IEnumerable<BreweryModel>> GetBreweriesAsync(string? search, string? sortBy)
        {
            var request = new BrewerySearchRequest
            {
                Search = search,
                SortType = sortBy is null ? SortType.name :
                    (sortBy.Equals("distance", StringComparison.OrdinalIgnoreCase) ? SortType.by_dist : SortType.name)
            };

            return GetBreweriesAsync(request);
        }

        public async Task<IEnumerable<BreweryModel>> GetBreweriesAsync(BrewerySearchRequest request)
        {
            var sortType = request?.SortType ?? SortType.name;
            string cacheKey = _settings.CacheKeyPrefix;
            string url = _settings.BaseUrl;

            if (sortType == SortType.by_dist && request?.Lat.HasValue == true && request?.Lon.HasValue == true)
            {
                string latlon = $"{request.Lat.Value.ToString(CultureInfo.InvariantCulture)},{request.Lon.Value.ToString(CultureInfo.InvariantCulture)}";
                cacheKey = $"{_settings.CacheKeyPrefix}|bydist:{latlon}";
                var encoded = Uri.EscapeDataString(latlon);
                url = $"{_settings.BaseUrl}?by_dist={encoded}";
            }

            // Quick cache check
            if (_cache.TryGetValue(cacheKey, out List<BreweryModel>? cached) && cached is not null)
            {
                return BreweryHelpers.ApplyFiltersAndSorting(cached, request);
            }

            var sem = _lockProvider.GetLock(cacheKey);
            await sem.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
                {
                    return BreweryHelpers.ApplyFiltersAndSorting(cached, request);
                }

                var client = _factory.CreateClient();
                var json = await client.GetStringAsync(url);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var upstream = JsonSerializer.Deserialize<List<Response>>(json, options)
                             ?? new List<Response>();

                var list = upstream.Select(u => new BreweryModel
                {
                    Id = u.Id,
                    Name = u.Name,
                    BreweryType = u.BreweryType,
                    Address1 = u.Address1,
                    Address2 = u.Address2,
                    Address3 = u.Address3,
                    City = u.City,
                    StateProvince = u.StateProvince,
                    PostalCode = u.PostalCode,
                    Country = u.Country,
                    Longitude = u.Longitude,
                    Latitude = u.Latitude,
                    Phone = u.Phone,
                    WebsiteUrl = u.WebsiteUrl,
                    State = u.State,
                    Street = u.Street
                }).ToList();

                // Populate cache
                _cache.Set(cacheKey, list, TimeSpan.FromMinutes(_settings.CacheMinutes));
                cached = list;
            }
            finally
            {
                sem.Release();
            }

            return BreweryHelpers.ApplyFiltersAndSorting(cached!, request);
        }
    }
}