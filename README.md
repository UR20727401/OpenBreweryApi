# Open Brewery API
Features: IMemoryCache (10 min), DI, Interfaces, SOLID-friendly layering, search, sorting(name/city/distance), data transformation, error handling.
GET /api/breweries?search=dog&sortBy=name


### Cache key composition
- Default list calls use a cache key equal to the configured `CacheKeyPrefix` (for example `brewery_data`).
- Distance requests that include a lat/lon pair use a per-location cache key so nearby queries do not collide:
  - `"{CacheKeyPrefix}|bydist:{lat},{lon}"`
  - Example: `brewery_data|bydist:32.88313237,-117.1649842`

This ensures separate cache entries for different location queries.

### Cache population flow (thundering?herd safe)
The service uses `IMemoryCache` plus a per-key async lock to prevent multiple requests from fetching the upstream API simultaneously when the cache is cold.

Flow:
1. On request, compute `cacheKey` and `url` (append `?by_dist=lat,lon` when appropriate).
2. Quick cache check:
   - If cache contains the key, return the cached `List<BreweryModel>` (then apply in-memory filtering/sorting).
3. If cache miss, obtain a per-key semaphore via `AsyncLockProvider.GetLock(cacheKey)` and `await sem.WaitAsync()`:
   - Double-check cache after acquiring lock (another request may have populated it).
   - If still missing:
     - Fetch the upstream JSON via `IHttpClientFactory` client.
     - Deserialize to typed DTOs (`UpstreamBreweryDto`).
     - Map DTOs to internal `BreweryModel` list.
     - Set cache with configured TTL: `_cache.Set(cacheKey, list, TimeSpan.FromMinutes(_settings.CacheMinutes))`.
   - Release the semaphore (`sem.Release()`).
4. Apply filtering and sorting in-memory to the cached list and return.

Notes:
- The double-check under the lock prevents duplicate upstream fetches when many concurrent requests occur.
- `AsyncLockProvider` is a small helper that returns a `SemaphoreSlim` per cache key using a `ConcurrentDictionary<string, SemaphoreSlim>`.
- `AsyncLockProvider` is registered as a singleton in DI.

### Why `IMemoryCache` + per-key async lock?
- `IMemoryCache` is simple and fast for a single-instance app.
- The per-key async lock prevents a "thundering herd" where many requests concurrently trigger the same expensive fetch.
- For multi-instance deployments, replace `IMemoryCache` with a distributed cache (Redis) and consider a distributed lock (e.g., RedLock) to provide the same protection cluster-wide.

### By-distance (`by_dist`) behavior
- When the request includes `by_dist=lat,lon`, the controller:
  - Validates and parses the lat/lon pair using invariant culture.
  - Overrides the sort type to `by_dist` and composes the upstream URL:
    - `https://api.openbrewerydb.org/v1/breweries?by_dist={lat},{lon}`
  - Uses the location-specific cache key described above.

- Sorting:
  - If `by_dist` is used and the client provided `lat`/`lon`, the service computes the great?circle distance from each brewery to the requested point and sorts according to the requested direction (`asc` or `desc`).
  - If `by_dist` was forwarded upstream without lat/lon present locally, the upstream ordering may already be distance-ordered, but the service only computes distances and enforces ordering when it has the lat/lon.

### Distance calculation (Haversine)
- The service computes distances using the Haversine formula to get an accurate great-circle distance in kilometers.
- Implementation notes:
  - Input: brewery latitude/longitude and request `lat`/`lon`.
  - If either the brewery lat/lon is missing, the computed distance is treated as `null` and those entries are ordered after entries with valid distances (implementation uses `double.MaxValue` for ordering when `null`).
  - Earth's radius used: `6371.0088 km` (mean Earth radius).

C# reference implementation (same algorithm used by service):