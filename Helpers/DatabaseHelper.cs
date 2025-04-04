using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using TrendChartApp.Models;

namespace TrendChartApp.Helpers
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Gets all tags from the TrendTAGNoTable
        /// </summary>
        public List<TagInfo> GetAllTags()
        {
            var tags = new List<TagInfo>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT iIndex, GroupNo, GroupName, TAGNo, TAGName, TableNo, ItemPos FROM TrendTAGNoTable";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var tag = new TagInfo
                                {
                                    Index = Convert.ToInt32(reader["iIndex"]),
                                    GroupNo = Convert.ToInt32(reader["GroupNo"]),
                                    GroupName = reader["GroupName"].ToString(),
                                    TagNo = reader["TAGNo"].ToString(),
                                    TagName = reader["TAGName"].ToString(),
                                    TableNo = Convert.ToInt32(reader["TableNo"]),
                                    ItemPos = Convert.ToInt32(reader["ItemPos"]),
                                    IsSelected = false
                                };

                                tags.Add(tag);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tags: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return tags;
        }

        /// <summary>
        /// Gets trend data for a specific tag from the database
        /// </summary>
        public List<TrendDataPoint> GetTagData(TagInfo tag, DateTime startTime, DateTime endTime)
        {
            var result = new List<TrendDataPoint>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Determine the table name and column name based on TableNo and ItemPos
                    string tableName = $"Trend{tag.TableNo.ToString("D5")}Data";
                    string columnName = $"Item{tag.ItemPos.ToString("D2")}";

                    // Create SQL query
                    string query = $"SELECT DateTime, {columnName} FROM {tableName} " +
                                  $"WHERE DateTime BETWEEN @StartTime AND @EndTime " +
                                  $"ORDER BY DateTime";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StartTime", startTime);
                        command.Parameters.AddWithValue("@EndTime", endTime);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new TrendDataPoint
                                {
                                    DateTime = Convert.ToDateTime(reader["DateTime"]),
                                    Value = Math.Round(Convert.ToDouble(reader[columnName]), 2) // 四捨五入到2位小數
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching tag data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }

        // GetTagData 非同步版本
        public async Task<List<TrendDataPoint>> GetTagDataAsync(TagInfo tag, DateTime startTime, DateTime endTime)
        {
            var result = new List<TrendDataPoint>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 確定表名和列名
                    string tableName = $"Trend{tag.TableNo.ToString("D5")}Data";
                    string columnName = $"Item{tag.ItemPos.ToString("D2")}";

                    // 創建SQL查詢
                    string query = $"SELECT DateTime, {columnName} FROM {tableName} " +
                                   $"WHERE DateTime BETWEEN @StartTime AND @EndTime " +
                                   $"ORDER BY DateTime";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StartTime", startTime);
                        command.Parameters.AddWithValue("@EndTime", endTime);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new TrendDataPoint
                                {
                                    DateTime = Convert.ToDateTime(reader["DateTime"]),
                                    Value = Math.Round(Convert.ToDouble(reader[columnName]), 2)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤，但不顯示訊息框（防止阻塞UI線程）
                Console.WriteLine($"Error fetching tag data: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Check if a table exists in the database
        /// </summary>
        public bool TableExists(string tableName)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName) " +
                                  "THEN 1 ELSE 0 END";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TableName", tableName);
                        return (int)command.ExecuteScalar() == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking table existence: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}