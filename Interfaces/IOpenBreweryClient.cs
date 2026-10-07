using OpenBreweryApi.Models;

namespace OpenBreweryApi.Interfaces
{
    public interface IOpenBreweryClient
    {
        Task<List<Response>> GetBreweriesAsync(
            string? byDist = null,
            string? search = null,
            string? sort = null,
            int? page = null,
            int? perPage = null,
            CancellationToken cancellationToken = default);
    }
}