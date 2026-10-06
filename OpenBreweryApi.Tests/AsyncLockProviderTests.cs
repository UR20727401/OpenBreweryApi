using OpenBreweryApi.Helpers;
using Xunit;

namespace OpenBreweryApi.Tests;

public sealed class AsyncLockProviderTests
{
    [Fact]
    public void GetLock_WithSameKey_ReturnsSameLock()
    {
        var provider = new AsyncLockProvider();

        var firstLock = provider.GetLock("breweries");
        var secondLock = provider.GetLock("breweries");

        Assert.Same(firstLock, secondLock);
    }

    [Fact]
    public void GetLock_WithDifferentKeys_ReturnsDifferentLocks()
    {
        var provider = new AsyncLockProvider();

        var firstLock = provider.GetLock("breweries");
        var secondLock = provider.GetLock("breweries-distance");

        Assert.NotSame(firstLock, secondLock);
    }

    [Fact]
    public async Task GetLock_AllowsOnlyOneConcurrentOperation()
    {
        var provider = new AsyncLockProvider();
        var semaphore = provider.GetLock("breweries");
        var activeOperations = 0;
        var maximumConcurrentOperations = 0;

        async Task ExecuteAsync()
        {
            await semaphore.WaitAsync();

            try
            {
                activeOperations++;
                maximumConcurrentOperations = Math.Max(
                    maximumConcurrentOperations,
                    activeOperations);

                await Task.Delay(50);

                activeOperations--;
            }
            finally
            {
                semaphore.Release();
            }
        }

        await Task.WhenAll(
            ExecuteAsync(),
            ExecuteAsync(),
            ExecuteAsync());

        Assert.Equal(1, maximumConcurrentOperations);
    }
}