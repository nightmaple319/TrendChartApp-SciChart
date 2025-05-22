using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrendChartApp.Models;

namespace TrendChartApp.Services
{
    public class DataCacheService
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly Timer _cleanupTimer;
        private readonly object _lockObject = new object();
        private const int MaxCacheSize = 100; // 最大緩存項目數
        private const int CacheExpirationMinutes = 10; // 緩存過期時間（分鐘）

        public DataCacheService()
        {
            // 每5分鐘清理一次過期緩存
            _cleanupTimer = new Timer(CleanupExpiredEntries, null,
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// 生成緩存鍵
        /// </summary>
        private string GenerateCacheKey(int tagIndex, DateTime startTime, DateTime endTime)
        {
            return $"tag_{tagIndex}_{startTime:yyyyMMddHHmmss}_{endTime:yyyyMMddHHmmss}";
        }

        /// <summary>
        /// 檢查是否存在有效緩存
        /// </summary>
        public bool TryGetCachedData(int tagIndex, DateTime startTime, DateTime endTime,
            out List<TrendDataPoint> data)
        {
            data = null;
            var key = GenerateCacheKey(tagIndex, startTime, endTime);

            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.IsExpired)
                {
                    _cache.TryRemove(key, out _);
                    return false;
                }

                data = entry.Data.ToList(); // 返回副本以避免修改原始緩存
                entry.LastAccessed = DateTime.Now;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 將資料加入緩存
        /// </summary>
        public void CacheData(int tagIndex, DateTime startTime, DateTime endTime,
            List<TrendDataPoint> data)
        {
            var key = GenerateCacheKey(tagIndex, startTime, endTime);
            var entry = new CacheEntry
            {
                Data = data.ToList(), // 存儲副本
                CreatedAt = DateTime.Now,
                LastAccessed = DateTime.Now
            };

            _cache.AddOrUpdate(key, entry, (k, oldEntry) => entry);

            // 檢查緩存大小，如果超過限制則清理最舊的項目
            if (_cache.Count > MaxCacheSize)
            {
                CleanupOldestEntries();
            }
        }

        /// <summary>
        /// 檢查是否可以使用部分緩存資料
        /// </summary>
        public bool TryGetPartialCachedData(int tagIndex, DateTime startTime, DateTime endTime,
            out List<TrendDataPoint> cachedData, out DateTime missingStart, out DateTime missingEnd)
        {
            cachedData = new List<TrendDataPoint>();
            missingStart = startTime;
            missingEnd = endTime;

            // 查找重疊的緩存項目
            var overlappingEntries = _cache.Values
                .Where(entry => !entry.IsExpired && entry.HasOverlap(startTime, endTime))
                .OrderBy(entry => entry.StartTime)
                .ToList();

            if (!overlappingEntries.Any())
                return false;

            // 合併重疊的資料
            foreach (var entry in overlappingEntries)
            {
                var relevantData = entry.Data
                    .Where(point => point.DateTime >= startTime && point.DateTime <= endTime)
                    .ToList();

                cachedData.AddRange(relevantData);
                entry.LastAccessed = DateTime.Now;
            }

            if (cachedData.Any())
            {
                cachedData = cachedData.OrderBy(p => p.DateTime).ToList();

                // 計算仍需從資料庫載入的時間範圍
                var firstCachedTime = cachedData.First().DateTime;
                var lastCachedTime = cachedData.Last().DateTime;

                if (firstCachedTime > startTime)
                {
                    missingEnd = firstCachedTime.AddSeconds(-1);
                }
                else if (lastCachedTime < endTime)
                {
                    missingStart = lastCachedTime.AddSeconds(1);
                }
                else
                {
                    // 完全覆蓋，不需要額外載入
                    return true;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 清理過期的緩存項目
        /// </summary>
        private void CleanupExpiredEntries(object state)
        {
            lock (_lockObject)
            {
                var expiredKeys = _cache
                    .Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// 清理最舊的緩存項目
        /// </summary>
        private void CleanupOldestEntries()
        {
            lock (_lockObject)
            {
                var itemsToRemove = _cache.Count - MaxCacheSize + 10; // 多清理10個
                if (itemsToRemove <= 0) return;

                var oldestEntries = _cache
                    .OrderBy(kvp => kvp.Value.LastAccessed)
                    .Take(itemsToRemove)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in oldestEntries)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// 清空所有緩存
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// 獲取緩存統計信息
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                ExpiredEntries = _cache.Values.Count(e => e.IsExpired),
                TotalMemoryUsage = _cache.Values.Sum(e => e.EstimatedMemoryUsage)
            };
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _cache.Clear();
        }

        private class CacheEntry
        {
            public List<TrendDataPoint> Data { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastAccessed { get; set; }

            public DateTime StartTime => Data?.FirstOrDefault()?.DateTime ?? DateTime.MinValue;
            public DateTime EndTime => Data?.LastOrDefault()?.DateTime ?? DateTime.MaxValue;

            public bool IsExpired => DateTime.Now - CreatedAt > TimeSpan.FromMinutes(CacheExpirationMinutes);

            public bool HasOverlap(DateTime start, DateTime end)
            {
                return StartTime <= end && EndTime >= start;
            }

            public long EstimatedMemoryUsage => (Data?.Count ?? 0) * 32; // 估算每個點佔用32字節
        }
    }

    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int ExpiredEntries { get; set; }
        public long TotalMemoryUsage { get; set; }
    }
}