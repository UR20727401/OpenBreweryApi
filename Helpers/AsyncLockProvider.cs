using System.Collections.Concurrent;
using System.Threading;

namespace OpenBreweryApi.Helpers
{
    public sealed class AsyncLockProvider
    {
        private readonly ConcurrentDictionary<string, LockEntry> _locks = new();

        public async Task<IDisposable> AcquireAsync(string key)
        {
            LockEntry entry;

            while (true)
            {
                entry = _locks.GetOrAdd(key, static _ => new LockEntry());

                lock (entry.SyncRoot)
                {
                    if (!entry.IsRemoved)
                    {
                        entry.ReferenceCount++;
                        break;
                    }
                }
            }

            try
            {
                await entry.Semaphore.WaitAsync().ConfigureAwait(false);
                return new LockLease(this, key, entry);
            }
            catch
            {
                ReleaseReference(key, entry, releaseSemaphore: false);
                throw;
            }
        }

        private void Release(string key, LockEntry entry)
        {
            entry.Semaphore.Release();
            ReleaseReference(key, entry, releaseSemaphore: false);
        }

        private void ReleaseReference(
            string key,
            LockEntry entry,
            bool releaseSemaphore)
        {
            if (releaseSemaphore)
            {
                entry.Semaphore.Release();
            }

            lock (entry.SyncRoot)
            {
                entry.ReferenceCount--;

                if (entry.ReferenceCount != 0)
                {
                    return;
                }

                if (_locks.TryGetValue(key, out var current) &&
                    ReferenceEquals(current, entry) &&
                    _locks.TryRemove(key, out _))
                {
                    entry.IsRemoved = true;
                    entry.Semaphore.Dispose();
                }
            }
        }

        private sealed class LockEntry
        {
            public object SyncRoot { get; } = new();

            public SemaphoreSlim Semaphore { get; } = new(1, 1);

            public int ReferenceCount { get; set; }

            public bool IsRemoved { get; set; }
        }

        private sealed class LockLease : IDisposable
        {
            private readonly AsyncLockProvider _provider;
            private readonly string _key;
            private readonly LockEntry _entry;
            private int _disposed;

            public LockLease(
                AsyncLockProvider provider,
                string key,
                LockEntry entry)
            {
                _provider = provider;
                _key = key;
                _entry = entry;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _provider.Release(_key, _entry);
                }
            }
        }
    }
}