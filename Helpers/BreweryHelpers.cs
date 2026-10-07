using System;
using System.Collections.Generic;
using System.Linq;
using OpenBreweryApi.Models;
using OpenBreweryApi.Models.Requests;

namespace OpenBreweryApi.Helpers
{
    public static class BreweryHelpers
    {
        // Apply search, sorting and paging on the in-memory collection.
        public static IEnumerable<BreweryModel> ApplyFiltersAndSorting(IEnumerable<BreweryModel> data, BrewerySearchRequest? request)
        {
            IEnumerable<BreweryModel> query = data;

            if (!string.IsNullOrWhiteSpace(request?.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x =>
                    Contains(x.Name, search) ||
                    Contains(x.BreweryType, search) ||
                    Contains(x.Address1, search) ||
                    Contains(x.Address2, search) ||
                    Contains(x.Address3, search) ||
                    Contains(x.City, search) ||
                    Contains(x.StateProvince, search) ||
                    Contains(x.PostalCode, search) ||
                    Contains(x.Country, search) ||
                    Contains(x.Phone, search) ||
                    Contains(x.State, search) ||
                    Contains(x.Street, search));
            }

            bool desc = request?.SortDir == SortDir.desc;
            var sortType = request?.SortType;

            if (sortType == SortType.by_dist &&
                request?.Lat.HasValue == true &&
                request?.Lon.HasValue == true)
            {
                double reqLat = request.Lat.Value;
                double reqLon = request.Lon.Value;

                query = desc
                    ? query.OrderByDescending(
                        b => ComputeDistanceKm(
                            b.Latitude,
                            b.Longitude,
                            reqLat,
                            reqLon) ?? double.MaxValue)
                    : query.OrderBy(
                        b => ComputeDistanceKm(
                            b.Latitude,
                            b.Longitude,
                            reqLat,
                            reqLon) ?? double.MaxValue);
            }
            else if (sortType.HasValue)
            {
                query = sortType.Value switch
                {
                    SortType.city => desc
                        ? query.OrderByDescending(x => x.City)
                        : query.OrderBy(x => x.City),

                    SortType.name => desc
                        ? query.OrderByDescending(x => x.Name)
                        : query.OrderBy(x => x.Name),

                    _ => query
                };
            }

            // Paging
            int perPage = request?.PerPage ?? 50;
            if (perPage < 1) perPage = 50;
            if (perPage > 200) perPage = 200;

            int page = request?.Page ?? 1;
            if (page < 1) page = 1;

            int skip = (page - 1) * perPage;
            return query.Skip(skip).Take(perPage);
        }

        // Haversine formula to compute distance (in kilometers) between two lat/lon points.
        public static double? ComputeDistanceKm(double? lat1, double? lon1, double lat2, double lon2)
        {
            if (!lat1.HasValue || !lon1.HasValue)
                return null;

            const double R = 6371.0088; // Earth's radius in km

            double dLat = ToRadians(lat2 - lat1.Value);
            double dLon = ToRadians(lon2 - lon1.Value);

            double a = Math.Pow(Math.Sin(dLat / 2), 2) +
                       Math.Cos(ToRadians(lat1.Value)) * Math.Cos(ToRadians(lat2)) *
                       Math.Pow(Math.Sin(dLon / 2), 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);

        private static bool Contains(
            string? value,
            string search)
        {
            return value?.Contains(
                search,
                StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}