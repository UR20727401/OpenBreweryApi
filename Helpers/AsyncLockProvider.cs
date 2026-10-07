using System.Collections.Concurrent;
using System.Threading;

namespace OpenBreweryApi.Helpers
{
   
    public sealed class AsyncLockProvider
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public SemaphoreSlim GetLock(string key) =>
            _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
    }
}