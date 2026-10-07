using Microsoft.AspNetCore.Mvc;

namespace OpenBreweryApi.Models.Requests
{
    public sealed class BrewerySearchRequest
    {
        /// <summary>
        /// Free-text search applied to name and city (optional).
        /// </summary>
        public string? Search { get; set; }

        // Preserves the complete public value, for example "name:desc".
        public string? Sort { get; set; }

        /// <summary>
        /// Optional sort field. When null, the original upstream order is preserved.
        /// </summary>
        [FromQuery(Name = "sortType")]
        public SortType? SortType { get; set; }

        /// <summary>
        /// Supported values: "asc", "desc".
        /// </summary>
        [FromQuery(Name = "sortDir")]
        public SortDir SortDir { get; set; } = SortDir.asc;

        [FromQuery(Name = "lat")]
        public double? Lat { get; set; }

        [FromQuery(Name = "lon")]
        public double? Lon { get; set; }

        public int Page { get; set; } = 1;

        public int PerPage { get; set; } = 50;
    }
}