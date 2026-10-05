using System;

namespace OpenBreweryApi.Models
{
    // Internal model used by service and controller - simple and maps directly from UpstreamBreweryDto
    public sealed class BreweryModel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? BreweryType { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? City { get; set; }
        public string? StateProvince { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public string? Phone { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? State { get; set; }
        public string? Street { get; set; }
    }
}