using Microsoft.AspNetCore.Mvc;

namespace OpenBreweryApi.Models.Requests
{
    public sealed class BrewerySearchRequest
    {
        /// <summary>
        /// Free-text search applied to name and city (optional).
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Supported values (query text): "by_name", "by_city", "by_dist".
        /// Bound via custom model binder.
        /// </summary>
        [FromQuery(Name = "sortType")]
        public SortType SortType { get; set; } = SortType.name;

        /// <summary>
        /// Supported values: "asc", "desc".
        /// Bound via custom model binder.
        /// </summary>
        [FromQuery(Name = "sortDir")]
        public SortDir SortDir { get; set; } = SortDir.asc;

        /// <summary>
        /// Latitude when requesting by distance (required when SortType == ByDist).
        /// Query name: lat
        /// </summary>
        [FromQuery(Name = "lat")]
        public double? Lat { get; set; }

        /// <summary>
        /// Longitude when requesting by distance (required when SortType == ByDist).
        /// Query name: lon
        /// </summary>
        [FromQuery(Name = "lon")]
        public double? Lon { get; set; }

        // Paging: page number (1-based). Default = 1.
        public int Page { get; set; } = 1;

        // Items per page. Default = 50. Max = 200 (enforced by controller/service).
        public int PerPage { get; set; } = 50;
    }
}