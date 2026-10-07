using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenBreweryApi.Interfaces;
using OpenBreweryApi.Models;
using OpenBreweryApi.Models.Settings;

namespace OpenBreweryApi.Services
{
    public class OpenBreweryClient : IOpenBreweryClient
    {
        private const int PageSize = 200;
        private const int MaximumPages = 1000;

        private readonly IHttpClientFactory _factory;
        private readonly OpenBrewerySettings _settings;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public OpenBreweryClient(
            IHttpClientFactory factory,
            IOptions<OpenBrewerySettings> settings)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _settings = settings?.Value ??
                throw new ArgumentNullException(nameof(settings));
        }

        public async Task<List<Response>> GetBreweriesAsync(
            string? byDist = null,
            string? search = null,
            string? sort = null,
            int? page = null,
            int? perPage = null,
            CancellationToken cancellationToken = default)
        {
            var client = _factory.CreateClient();
            var baseUrl = _settings.BaseUrl?.TrimEnd('/')
                ?? throw new InvalidOperationException(
                    "OpenBrewery BaseUrl is not configured.");

            var requestPage = page ?? 1;
            var requestPageSize = perPage ?? PageSize;

            var requestUrl = BuildPageUrl(
                baseUrl,
                byDist,
                search,
                sort,
                requestPage,
                requestPageSize);

            return await GetPageAsync(
                client,
                requestUrl,
                cancellationToken);
        }

        private async Task<List<Response>> GetPageAsync(
            HttpClient client,
            string pageUrl,
            CancellationToken cancellationToken)
        {
            using var response = await client.GetAsync(
                pageUrl,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new UpstreamServiceException(
                    $"The brewery service returned HTTP {(int)response.StatusCode}.");
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken).ConfigureAwait(false);

            try
            {
                var pageItems =
                    await JsonSerializer.DeserializeAsync<List<Response>>(
                        stream,
                        _jsonOptions,
                        cancellationToken).ConfigureAwait(false);

                if (pageItems is null)
                {
                    throw new UpstreamServiceException(
                        "The brewery service returned an empty response.");
                }

                return pageItems;
            }
            catch (JsonException)
            {
                throw new UpstreamServiceException(
                    "The brewery service returned an invalid brewery response.");
            }
        }

        private static string BuildPageUrl(
            string baseUrl,
            string? byDist,
            string? search,
            string? sort,
            int page,
            int perPage)
        {
            var endpoint = string.IsNullOrWhiteSpace(search)
                ? baseUrl
                : $"{baseUrl}/search";

            var query = new List<string>
            {
                $"page={page}",
                $"per_page={perPage}"
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                query.Add($"query={Uri.EscapeDataString(search.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(sort) &&
                string.IsNullOrWhiteSpace(byDist))
            {
                query.Add($"sort={Uri.EscapeDataString(sort)}");
            }

            if (!string.IsNullOrWhiteSpace(byDist))
            {
                query.Add($"by_dist={Uri.EscapeDataString(byDist)}");
            }

            return $"{endpoint}?{string.Join("&", query)}";
        }
    }
}