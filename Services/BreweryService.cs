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

        public async Task<IEnumerable<BreweryModel>> GetBreweriesAsync(
            BrewerySearchRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var search = request.Search?.Trim();
            var sort = request.SortType?.ToString();
            string? byDist = null;

            if (request.SortType == SortType.by_dist &&
                request.Lat.HasValue &&
                request.Lon.HasValue)
            {
                byDist =
                    $"{request.Lat.Value.ToString(CultureInfo.InvariantCulture)}," +
                    $"{request.Lon.Value.ToString(CultureInfo.InvariantCulture)}";
            }

            var cacheKey = BuildCacheKey(
                search,
                byDist,
                sort,
                request.Page,
                request.PerPage);

            if (_cache.TryGetValue(
                    cacheKey,
                    out List<BreweryModel>? cached) &&
                cached is not null)
            {
                return cached;
            }

            using (await _lockProvider.AcquireAsync(cacheKey))
            {
                if (_cache.TryGetValue(
                        cacheKey,
                        out cached) &&
                    cached is not null)
                {
                    return cached;
                }

                var upstream = await _openBreweryClient.GetBreweriesAsync(
                    byDist: byDist,
                    search: search,
                    sort: sort,
                    page: request.Page,
                    perPage: request.PerPage);

                var list = MapBreweries(upstream);

                _cache.Set(
                    cacheKey,
                    list,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromMinutes(_settings.CacheMinutes),
                        Size = 1
                    });

                return list;
            }
        }

        private string BuildCacheKey(
            string? search,
            string? byDist,
            string? sort,
            int page,
            int perPage)
        {
            return string.Join(
                "|",
                _settings.CacheKeyPrefix,
                $"search:{search ?? string.Empty}",
                $"bydist:{byDist ?? string.Empty}",
                $"sort:{sort ?? string.Empty}",
                $"page:{page}",
                $"per_page:{perPage}");
        }

        private static List<BreweryModel> MapBreweries(
            IEnumerable<Response> breweries)
        {
            return breweries.Select(u => new BreweryModel
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
        }
    }
}