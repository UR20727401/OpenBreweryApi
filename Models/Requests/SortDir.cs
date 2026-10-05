using System.Text.Json.Serialization;

namespace OpenBreweryApi.Models.Requests
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SortDir
    {
        asc,
        desc
    }
}