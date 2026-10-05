using System;
using OpenBreweryApi.Models.Requests;

namespace OpenBreweryApi.Helpers
{
    public static class SortHelpers
    {
        // Parse primary field from "sort" query. Examples:
        // "name", "city:desc", "name,city:asc" -> returns corresponding SortType (defaults to by_name)
        public static SortType ParseSortField(string? sort)
        {
            if (string.IsNullOrWhiteSpace(sort))
            {
                return GetDefaultSortType();
            }

            // Use the first token before comma as primary field
            var tokens = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var first = tokens.Length > 0 ? tokens[0] : string.Empty;

            // If first contains colon, strip direction
            var field = first.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0]
                             .Trim().ToLowerInvariant();

            // Map common synonyms to enum names
            return field switch
            {
                "name" or "by_name" => SortType.name,
                "city" or "by_city" => SortType.city,
                "distance" or "dist" or "by_dist" => SortType.by_dist,
                _ => GetDefaultSortType()
            };
        }

        // Determine sort direction by looking for any :asc/:desc token in the comma-separated list.
        // Defaults to asc.
        public static SortDir ParseSortDir(string? sort)
        {
            if (string.IsNullOrWhiteSpace(sort))
            {
                return GetDefaultSortDir();
            }

            var tokens = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var token in tokens)
            {
                var parts = token.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    var dir = parts[1].Trim().ToLowerInvariant();
                    if (dir == "desc") return SortDir.desc;
                    if (dir == "asc") return SortDir.asc;
                }
            }

            // If no explicit direction found, default to asc
            return GetDefaultSortDir();
        }

        public static SortType GetDefaultSortType() => SortType.name;
        public static SortDir GetDefaultSortDir() => SortDir.asc;
    }
}