using System;
using System.Runtime.Caching;

namespace DatwiseSafetyDemo.Infrastructure
{
    /// <summary>
    /// Simple in-memory rate limiter for login attempts.
    /// Intended to reduce brute force risk. For multi-node deployments, replace with distributed store.
    /// </summary>
    public sealed class LoginRateLimiter
    {
        private static readonly MemoryCache Cache = MemoryCache.Default;

        private readonly int _maxFailures;
        private readonly TimeSpan _window;
        private readonly TimeSpan _blockDuration;

        public LoginRateLimiter(int maxFailures = 5, int windowMinutes = 10, int blockMinutes = 5)
        {
            if (maxFailures <= 0) throw new ArgumentOutOfRangeException(nameof(maxFailures));
            _maxFailures = maxFailures;
            _window = TimeSpan.FromMinutes(windowMinutes);
            _blockDuration = TimeSpan.FromMinutes(blockMinutes);
        }

        public bool IsBlocked(string key, out TimeSpan retryAfter)
        {
            retryAfter = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(key)) return false;

            var blockedUntil = Cache.Get(key + ":blocked") as DateTime?;
            if (blockedUntil.HasValue && blockedUntil.Value > DateTime.UtcNow)
            {
                retryAfter = blockedUntil.Value - DateTime.UtcNow;
                return true;
            }

            return false;
        }

        public void RegisterFailure(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            var counterKey = key + ":failures";
            var now = DateTime.UtcNow;

            var existing = Cache.Get(counterKey) as FailureWindow;
            if (existing == null || now - existing.WindowStart > _window)
            {
                existing = new FailureWindow { WindowStart = now, Count = 0 };
            }

            existing.Count++;

            Cache.Set(counterKey, existing, new CacheItemPolicy { AbsoluteExpiration = now.Add(_window) });

            if (existing.Count >= _maxFailures)
            {
                Cache.Set(key + ":blocked", now.Add(_blockDuration), new CacheItemPolicy { AbsoluteExpiration = now.Add(_blockDuration) });
            }
        }

        public void RegisterSuccess(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            Cache.Remove(key + ":failures");
            Cache.Remove(key + ":blocked");
        }

        private sealed class FailureWindow
        {
            public DateTime WindowStart { get; set; }
            public int Count { get; set; }
        }
    }
}
