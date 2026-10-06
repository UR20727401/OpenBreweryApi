# Open Brewery API

A .NET 8 Web API for retrieving brewery data from the Open Brewery DB service. The application includes API-key authentication, in-memory caching, concurrency control, centralized exception handling, Swagger/OpenAPI documentation, and configurable Open Brewery settings.


The application prints the HTTP and HTTPS URLs in the console. Open the displayed Swagger URL, usually one of the following:

- `https://localhost:7xxx/swagger`
- `http://localhost:5xxx/swagger`

The exact ports are defined by the local launch profile and may differ between environments.

## Configuration

The application reads configuration from the standard ASP.NET Core configuration providers, including `appsettings.json`, environment-specific settings, environment variables, and user secrets.

Example configuration:

```json
{
  "OpenBrewery": {
    "BaseUrl": "https://api.openbrewerydb.org/"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Use the property names defined by `OpenBrewerySettings` when adding or changing Open Brewery configuration values.

### API key configuration

The API key is read by the configured API-key provider. Configure it using the mechanism expected by `EnvApiKeyProvider` in the deployment environment. 

For a local session, set the environment variable expected by the provider before starting the application. For example, if the provider uses `OPENBREWERY_API_KEY`:

**PowerShell**

```powershell
$env:OPENBREWERY_API_KEY = "your-api-key"
dotnet run
```
is the one implemented by `EnvApiKeyProvider`.

## Authentication

Protected requests must include the API key in the `X-API-KEY` header:


Unauthenticated requests to protected endpoints return `401 Unauthorized`. Requests with an invalid key return an authentication failure according to the configured authentication handler.


## Caching

The application registers ASP.NET Core's in-memory cache through 
`AddMemoryCache()`. 
`BreweryService` uses this cache to retain suitable brewery responses and avoid unnecessary calls to the external API.

### Cache key composition

- Default list calls use a cache key equal to the configured `CacheKeyPrefix` (for example `brewery_data`).
- Distance requests that include a lat/lon pair use a per-location cache key so nearby queries do not collide:
  - `"{CacheKeyPrefix}|bydist:{lat},{lon}"`
  - Example: `brewery_data|bydist:32.88313237,-117.1649842`

This ensures separate cache entries for different location queries.

### Cache population flow (thundering herd safe)

The service uses `IMemoryCache` plus a per-key async lock to prevent
multiple requests from fetching the upstream API simultaneously when the cache is cold.

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

## Concurrency control

`AsyncLockProvider` is registered as a singleton and provides asynchronous locking for coordinated operations. The service layer can use it to prevent multiple concurrent requests from performing the same cache-miss operation at the same time.

This reduces duplicate upstream calls while preserving asynchronous request processing. Locks should be held only around the smallest required operation and should never be used as a substitute for a distributed lock when the application runs across multiple instances.

## Logging

ASP.NET Core logging is enabled through the standard hosting configuration. Configure log levels in `appsettings.json`, environment-specific configuration, or environment variables.

Recommended production settings:

When adding or reviewing logs:

- Use `ILogger<T>` rather than writing directly to the console.


## Exception handling

`ErrorHandlingMiddleware` is registered near the beginning of the request pipeline. It catches unhandled exceptions from downstream middleware and controllers, logs the failure, and returns a controlled error response.

. Unexpected failures should be allowed to reach the centralized middleware.

## Request pipeline

The application configures the following major pipeline components:

1. Exception handling middleware.
2. API-key authentication.
3. Authorization.
4. Swagger and Swagger UI.
5. Controller endpoint mapping.

Authentication must run before authorization, and middleware ordering should be preserved when adding new components.


## Development workflow

```bash
dotnet restore
dotnet build
dotnet run
```

## Testing

covered test cases around negative scanrios, api key, semaphores


## Troubleshooting

### Requests return 401

- Confirm that the `X-API-KEY` header is present.
- Verify that the key matches the value available to the configured API-key provider.
- Confirm that the endpoint requires the expected authentication scheme.

### Requests return an upstream error

- Verify network access to Open Brewery DB.
- Check the configured `OpenBrewery` base URL.
- Review application logs for the upstream status code and exception details.

