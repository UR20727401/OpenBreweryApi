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
        private readonly IHttpClientFactory _factory;
        private readonly OpenBrewerySettings _settings;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public OpenBreweryClient(IHttpClientFactory factory, IOptions<OpenBrewerySettings> settings)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<List<Response>> GetBreweriesAsync(string? byDist = null, CancellationToken cancellationToken = default)
        {
            var client = _factory.CreateClient();
            var url = _settings.BaseUrl?.TrimEnd('/') ?? throw new InvalidOperationException("OpenBrewery BaseUrl is not configured.");

            if (!string.IsNullOrWhiteSpace(byDist))
            {
                var encoded = Uri.EscapeDataString(byDist);
                url = $"{url}?by_dist={encoded}";
            }

            using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var items = await JsonSerializer.DeserializeAsync<List<Response>>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false)
                        ?? new List<Response>();

            return items;
        }
    }
}