using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using TrendChartApp.Models;

namespace TrendChartApp.Helpers
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;
        private static readonly SemaphoreSlim _connectionSemaphore = new(10, 10); // 限制並發連接數

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 異步獲取所有標籤，支援取消操作
        /// </summary>
        public async Task<List<TagInfo>> GetAllTagsAsync(CancellationToken cancellationToken = default)
        {
            var tags = new List<TagInfo>();

            await _connectionSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                const string query = "SELECT iIndex, GroupNo, GroupName, TAGNo, TAGName, TableNo, ItemPos FROM TrendTAGNoTable";

                using var command = new SqlCommand(query, connection);
                command.CommandTimeout = 30; // 設定合理的超時時間

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var tag = new TagInfo
                    {
                        Index = reader.GetInt32("iIndex"),
                        GroupNo = reader.GetInt32("GroupNo"),
                        GroupName = reader.GetString("GroupName"),
                        TagNo = reader.GetString("TAGNo"),
                        TagName = reader.GetString("TAGName"),
                        TableNo = reader.GetInt32("TableNo"),
                        ItemPos = reader.GetInt32("ItemPos"),
                        IsSelected = false
                    };

                    tags.Add(tag);
                }
            }
            finally
            {
                _connectionSemaphore.Release();
            }

            return tags;
        }

        /// <summary>
        /// 改進的批次資料獲取方法
        /// </summary>
        public async Task<Dictionary<int, List<TrendDataPoint>>> GetMultipleTagDataAsync(
            IEnumerable<TagInfo> tags,
            DateTime startTime,
            DateTime endTime,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<int, List<TrendDataPoint>>();
            var tagList = tags.ToList();

            // 按表分組以優化查詢
            var tagsByTable = tagList.GroupBy(t => t.TableNo);

            int processedTables = 0;
            int totalTables = tagsByTable.Count();

            foreach (var tableGroup in tagsByTable)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tableNo = tableGroup.Key;
                var tableTags = tableGroup.ToList();

                progress?.Report($"正在載入表 Trend{tableNo:D5}Data 的資料...");

                await _connectionSemaphore.WaitAsync(cancellationToken);
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync(cancellationToken);

                    // 建構動態查詢以一次獲取多個欄位
                    var columns = string.Join(", ", tableTags.Select(t => $"Item{t.ItemPos:D2}"));
                    var tableName = $"Trend{tableNo:D5}Data";

                    var query = $@"
                        SELECT DateTime, {columns} 
                        FROM {tableName} 
                        WHERE DateTime BETWEEN @StartTime AND @EndTime 
                        ORDER BY DateTime";

                    using var command = new SqlCommand(query, connection);
                    command.Parameters.Add("@StartTime", SqlDbType.DateTime2).Value = startTime;
                    command.Parameters.Add("@EndTime", SqlDbType.DateTime2).Value = endTime;
                    command.CommandTimeout = 60;

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);

                    // 初始化結果字典
                    foreach (var tag in tableTags)
                    {
                        result[tag.Index] = new List<TrendDataPoint>();
                    }

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var dateTime = reader.GetDateTime("DateTime");

                        foreach (var tag in tableTags)
                        {
                            var columnName = $"Item{tag.ItemPos:D2}";
                            if (!reader.IsDBNull(columnName))
                            {
                                var value = Math.Round(reader.GetDouble(columnName), 2);
                                result[tag.Index].Add(new TrendDataPoint
                                {
                                    DateTime = dateTime,
                                    Value = value
                                });
                            }
                        }
                    }
                }
                finally
                {
                    _connectionSemaphore.Release();
                }

                processedTables++;
                progress?.Report($"已完成 {processedTables}/{totalTables} 個資料表");
            }

            return result;
        }

        /// <summary>
        /// 智能數據採樣方法
        /// </summary>
        public static List<TrendDataPoint> SampleDataIntelligently(
            List<TrendDataPoint> data,
            int maxPoints = 10000)
        {
            if (data.Count <= maxPoints)
                return data;

            var result = new List<TrendDataPoint>();

            // 確保包含第一個和最後一個點
            result.Add(data[0]);

            // 計算採樣間隔
            int interval = data.Count / (maxPoints - 2);

            // 使用最大最小值保留重要特徵
            for (int i = 1; i < data.Count - 1; i += interval)
            {
                var segment = data.Skip(i).Take(interval);
                if (segment.Any())
                {
                    // 添加最大值和最小值點
                    var maxPoint = segment.OrderByDescending(p => p.Value).First();
                    var minPoint = segment.OrderBy(p => p.Value).First();

                    if (maxPoint.DateTime != minPoint.DateTime)
                    {
                        if (maxPoint.DateTime < minPoint.DateTime)
                        {
                            result.Add(maxPoint);
                            result.Add(minPoint);
                        }
                        else
                        {
                            result.Add(minPoint);
                            result.Add(maxPoint);
                        }
                    }
                    else
                    {
                        result.Add(maxPoint);
                    }
                }
            }

            result.Add(data[data.Count - 1]);

            return result.OrderBy(p => p.DateTime).ToList();
        }

        /// <summary>
        /// 檢查表是否存在（異步版本）
        /// </summary>
        public async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken = default)
        {
            await _connectionSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                const string query = @"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_NAME = @TableName
                    ) THEN 1 ELSE 0 END";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.CommandTimeout = 10;

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt32(result) == 1;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// 測試資料庫連接
        /// </summary>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 獲取資料庫統計信息
        /// </summary>
        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new DatabaseStatistics();

            await _connectionSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // 獲取標籤總數
                const string tagCountQuery = "SELECT COUNT(*) FROM TrendTAGNoTable";
                using var tagCommand = new SqlCommand(tagCountQuery, connection);
                stats.TotalTags = (int)await tagCommand.ExecuteScalarAsync(cancellationToken);

                // 獲取趨勢表數量
                const string tableCountQuery = @"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME LIKE 'Trend%Data'";
                using var tableCommand = new SqlCommand(tableCountQuery, connection);
                stats.TrendTableCount = (int)await tableCommand.ExecuteScalarAsync(cancellationToken);
            }
            finally
            {
                _connectionSemaphore.Release();
            }

            return stats;
        }
    }

    public class DatabaseStatistics
    {
        public int TotalTags { get; set; }
        public int TrendTableCount { get; set; }
    }
}