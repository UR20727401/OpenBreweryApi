using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBreweryApi.Models;

namespace OpenBreweryApi.Interfaces
{
    public interface IOpenBreweryClient
    {
        /// <summary>
        /// Gets breweries from the upstream OpenBrewery API. If <paramref name="byDist"/> is provided
        /// it will be appended as ?by_dist={byDist}.
        /// </summary>
        Task<List<Response>> GetBreweriesAsync(string? byDist = null, CancellationToken cancellationToken = default);
    }
}