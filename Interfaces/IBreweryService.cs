using System.Collections.Generic;
using System.Threading.Tasks;
using OpenBreweryApi.Models;
using OpenBreweryApi.Models.Requests;

namespace OpenBreweryApi.Interfaces
{
    public interface IBreweryService
    {
        Task<IEnumerable<BreweryModel>> GetBreweriesAsync(string? search, string? sortBy);

        // New request-driven method
        Task<IEnumerable<BreweryModel>> GetBreweriesAsync(BrewerySearchRequest request);
    }
}