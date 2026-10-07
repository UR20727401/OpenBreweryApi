using System.Globalization;
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
        private readonly IOpenBreweryClient _openBreweryClient;

        public BreweryService(
            IHttpClientFactory factory,
            IMemoryCache cache,
            AsyncLockProvider lockProvider,
            IOptions<OpenBrewerySettings> settings,
            IOpenBreweryClient openBreweryClient)
        {
            _factory = factory;
            _cache = cache;
            _lockProvider = lockProvider;
            _settings = settings.Value;
            _openBreweryClient = openBreweryClient;
        }

        public async Task<IEnumerable<BreweryModel>> GetBreweriesAsync(BrewerySearchRequest request)
        {
            var sortType = request?.SortType ?? SortType.name;
            string cacheKey = _settings.CacheKeyPrefix;
            string url = _settings.BaseUrl;
            string? byDist = null;

            if (sortType == SortType.by_dist && request?.Lat.HasValue == true && request?.Lon.HasValue == true)
            {
                string latlon =
                    $"{request.Lat.Value.ToString(CultureInfo.InvariantCulture)}," +
                    $"{request.Lon.Value.ToString(CultureInfo.InvariantCulture)}";

                byDist = latlon;
                cacheKey = $"{_settings.CacheKeyPrefix}|bydist:{latlon}";
                var encoded = Uri.EscapeDataString(latlon);
                url = $"{_settings.BaseUrl}?by_dist={encoded}";
            }

            // Quick cache check
            if (_cache.TryGetValue(cacheKey, out List<BreweryModel>? cached) && cached is not null)
            {
                return BreweryHelpers.ApplyFiltersAndSorting(cached, request);
            }

            using (await _lockProvider.AcquireAsync(cacheKey))
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
                {
                    return BreweryHelpers.ApplyFiltersAndSorting(cached, request);
                }

                var upstream = await _openBreweryClient.GetBreweriesAsync(byDist);

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
                _cache.Set(
                    cacheKey,
                    list,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_settings.CacheMinutes),
                        Size = 1
                    });

                cached = list;
            }

            return BreweryHelpers.ApplyFiltersAndSorting(cached!, request);
        }
    }
}