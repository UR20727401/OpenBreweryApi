using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenBreweryApi.Dtos;
using OpenBreweryApi.Helpers;
using OpenBreweryApi.Interfaces;
using OpenBreweryApi.Models.Requests;

namespace OpenBreweryApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class BreweriesController : ControllerBase
{
    private readonly IBreweryService _service;
    private readonly ILogger<BreweriesController> _logger;

    public BreweriesController(IBreweryService service, ILogger<BreweriesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Unified endpoint supporting two query formats:
    /// - Sort style: ?sort=name,city:asc
    /// - Distance style: ?by_dist=lat,lon
    /// Pagination: ?page=1&per_page=50
    /// (defaults: page=1, per_page=50, maximum per_page=200)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery(Name = "search")] string? search,
        [FromQuery(Name = "sort")] string? sort,
        [FromQuery(Name = "by_dist")] string? byDist,
        [FromQuery(Name = "page")] int? page,
        [FromQuery(Name = "per_page")] int? perPage)
    {
        if (page.HasValue && page.Value < 1)
        {
            return BadRequest("Query parameter 'page' must be >= 1.");
        }

        if (perPage.HasValue && (perPage.Value < 1 || perPage.Value > 200))
        {
            return BadRequest(
                "Query parameter 'per_page' must be between 1 and 200 (inclusive).");
        }

        if (!SortHelpers.TryParse(
                sort,
                out var sortType,
                out var sortDir))
        {
            return BadRequest(
                "Invalid sort value. Supported fields are name, city, and by_dist. " +
                "The direction must be asc or desc.");
        }

        var request = new BrewerySearchRequest
        {
            Search = search,
            Sort = string.IsNullOrWhiteSpace(sort)
                ? null
                : sort.Trim(),
            SortType = sortType,
            SortDir = sortDir,
            Page = page ?? 1,
            PerPage = perPage ?? 50
        };

        if ((long)(request.Page - 1) * request.PerPage > int.MaxValue)
        {
            return BadRequest(
                "The requested page is too large for the selected page size.");
        }

        if (!string.IsNullOrWhiteSpace(byDist))
        {
            var parts = byDist.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            if (parts.Length == 2 &&
                double.TryParse(
                    parts[0],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var lat) &&
                double.TryParse(
                    parts[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var lon) &&
                lat is >= -90 and <= 90 &&
                lon is >= -180 and <= 180)
            {
                request.Lat = lat;
                request.Lon = lon;
                request.SortType = SortType.by_dist;
            }
            else
            {
                return BadRequest(
                    "Invalid 'by_dist' value. Latitude must be between -90 and 90, " +
                    "and longitude must be between -180 and 180.");
            }
        }

        _logger.LogInformation(
            "Handling GET /breweries request " +
            "(query='{Query}', sort='{Sort}', by_dist='{ByDist}', " +
            "page={Page}, per_page={PerPage})",
            request.Search,
            sort,
            byDist,
            request.Page,
            request.PerPage);

        var result = await _service.GetBreweriesAsync(request);

        var dto = result.Select(x => new BreweryDto
        {
            Name = x.Name,
            City = x.City,
            Phone = x.Phone,
            Latitude = x.Latitude,
            Longitude = x.Longitude
        });

        return Ok(dto);
    }
}