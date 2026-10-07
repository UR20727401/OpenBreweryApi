using System;
using OpenBreweryApi.Models.Requests;

namespace OpenBreweryApi.Helpers;

public static class SortHelpers
{
    public static bool TryParse(
        string? sort,
        out SortType? sortType,
        out SortDir sortDir)
    {
        sortType = null;
        sortDir = SortDir.asc;

        if (string.IsNullOrWhiteSpace(sort))
        {
            return true;
        }

        var tokens = sort.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
        {
            return false;
        }

        var primaryParts = tokens[0].Split(
            ':',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        var field = primaryParts[0].ToLowerInvariant();

        sortType = field switch
        {
            "name" or "by_name" => SortType.name,
            "city" or "by_city" => SortType.city,
            "distance" or "dist" or "by_dist" => SortType.by_dist,
            _ => null
        };

        if (!sortType.HasValue)
        {
            return false;
        }

        if (primaryParts.Length > 1)
        {
            sortDir = primaryParts[1].ToLowerInvariant() switch
            {
                "asc" => SortDir.asc,
                "desc" => SortDir.desc,
                _ => (SortDir)(-1)
            };

            if ((int)sortDir == -1)
            {
                return false;
            }
        }

        return true;
    }
}