using System.Text.Json.Serialization;

namespace OpenBreweryApi.Models.Requests
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SortType
    {
        name,
        city,
        by_dist
    }
}