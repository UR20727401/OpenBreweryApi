namespace OpenBreweryApi.Dtos
{
    public sealed class BreweryDto
    {
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? Phone { get; set; }

        // Return explicit lat/lon in the response JSON (match upstream names)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}