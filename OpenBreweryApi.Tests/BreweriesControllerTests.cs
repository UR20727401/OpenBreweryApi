using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpenBreweryApi.Controllers;
using OpenBreweryApi.Interfaces;
using OpenBreweryApi.Models;
using OpenBreweryApi.Models.Requests;
using Xunit;

namespace OpenBreweryApi.Tests;

public sealed class BreweriesControllerTests
{
    [Fact]
    public async Task Get_WithPageLessThanOne_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Get(
            search: null,
            sort: null,
            byDist: null,
            page: 0,
            perPage: null);

        var response = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(
            "page",
            response.Value?.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_WithInvalidPerPage_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Get(
            search: null,
            sort: null,
            byDist: null,
            page: null,
            perPage: 201);

        var response = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Contains(
            "per_page",
            response.Value?.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_WithInvalidDistance_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Get(
            search: null,
            sort: null,
            byDist: "invalid-distance",
            page: null,
            perPage: null);

        var response = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(
            "by_dist",
            response.Value?.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_WithValidSearchRequest_ReturnsOk()
    {
        var controller = CreateController();

        var result = await controller.Get(
            search: "brew",
            sort: "name",
            byDist: null,
            page: 1,
            perPage: 20);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(response.Value);
    }

    [Fact]
    public void Controller_RequiresApiKeyAuthentication()
    {
        var attribute = typeof(BreweriesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal("ApiKey", attribute.AuthenticationSchemes);
    }

    private static BreweriesController CreateController()
    {
        return new BreweriesController(
            new FakeBreweryService(),
            NullLogger<BreweriesController>.Instance);
    }

    private sealed class FakeBreweryService : IBreweryService
    {
        public Task<IEnumerable<BreweryModel>> GetBreweriesAsync(
            BrewerySearchRequest request)
        {
            return Task.FromResult<IEnumerable<BreweryModel>>(
                CreateBreweries());
        }

        private static IEnumerable<BreweryModel> CreateBreweries()
        {
            return
            [
                new BreweryModel
                {
                    Name = "Example Brewery",
                    City = "San Diego"
                }
            ];
        }
    }
}