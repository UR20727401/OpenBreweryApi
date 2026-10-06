
namespace OpenBreweryApi.Security
{
    public interface IApiKeyProvider
    {
        string? GetApiKey();
    }
}