using OpenBreweryApi.Helpers;
using Xunit;

namespace OpenBreweryApi.Tests;

public sealed class AsyncLockProviderTests
{
    [Fact]
    public async Task AcquireAsync_WithSameKey_AllowsOnlyOneConcurrentOperation()
    {
        var provider = new AsyncLockProvider();
        var activeOperations = 0;
        var maximumConcurrentOperations = 0;

        async Task ExecuteAsync()
        {
            using (await provider.AcquireAsync("breweries"))
            {
                var active = Interlocked.Increment(ref activeOperations);

                InterlockedMax(
                    ref maximumConcurrentOperations,
                    active);

                await Task.Delay(50);

                Interlocked.Decrement(ref activeOperations);
            }
        }

        await Task.WhenAll(
            ExecuteAsync(),
            ExecuteAsync(),
            ExecuteAsync());

        Assert.Equal(1, maximumConcurrentOperations);
    }

    [Fact]
    public async Task AcquireAsync_WithDifferentKeys_AllowsConcurrentOperations()
    {
        var provider = new AsyncLockProvider();
        var firstEntered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task ExecuteAsync(
            string key,
            TaskCompletionSource<bool> entered)
        {
            using (await provider.AcquireAsync(key))
            {
                entered.SetResult(true);
                await Task.Delay(50);
            }
        }

        var first = ExecuteAsync("breweries", firstEntered);
        var second = ExecuteAsync("breweries-distance", secondEntered);

        await Task.WhenAll(
            firstEntered.Task,
            secondEntered.Task,
            first,
            second);
    }

    [Fact]
    public async Task AcquireAsync_AllowsReacquiringKeyAfterLeaseIsDisposed()
    {
        var provider = new AsyncLockProvider();

        using (await provider.AcquireAsync("breweries"))
        {
        }

        using (await provider.AcquireAsync("breweries"))
        {
        }
    }

    private static void InterlockedMax(
        ref int location,
        int value)
    {
        int current;

        do
        {
            current = Volatile.Read(ref location);

            if (current >= value)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(
                   ref location,
                   value,
                   current) != current);
    }
}