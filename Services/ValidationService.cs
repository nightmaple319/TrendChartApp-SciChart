using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using TrendChartApp.Models;

namespace TrendChartApp.Services
{
    public class ValidationService
    {
        /// <summary>
        /// 驗證時間範圍
        /// </summary>
        public ValidationResult ValidateTimeRange(DateTime startTime, DateTime endTime)
        {
            var errors = new List<string>();

            if (startTime >= endTime)
            {
                errors.Add("開始時間必須早於結束時間");
            }

            var timeSpan = endTime - startTime;
            if (timeSpan.TotalDays > 365)
            {
                errors.Add("時間範圍不能超過365天");
            }

            if (timeSpan.TotalSeconds < 1)
            {
                errors.Add("時間範圍至少需要1秒");
            }

            if (startTime > DateTime.Now)
            {
                errors.Add("開始時間不能是未來時間");
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// 驗證Y軸範圍
        /// </summary>
        public ValidationResult ValidateYAxisRange(double minValue, double maxValue)
        {
            var errors = new List<string>();

            if (minValue >= maxValue)
            {
                errors.Add("Y軸最小值必須小於最大值");
            }

            if (double.IsNaN(minValue) || double.IsInfinity(minValue))
            {
                errors.Add("Y軸最小值必須是有效數字");
            }

            if (double.IsNaN(maxValue) || double.IsInfinity(maxValue))
            {
                errors.Add("Y軸最大值必須是有效數字");
            }

            var range = maxValue - minValue;
            if (range > 1e10)
            {
                errors.Add("Y軸範圍過大，可能影響顯示效果");
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// 驗證資料庫連線字串
        /// </summary>
        public ValidationResult ValidateConnectionString(string connectionString)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                errors.Add("連線字串不能為空");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            // 檢查必要的連線字串參數
            var requiredParams = new[] { "Server", "Database" };
            foreach (var param in requiredParams)
            {
                if (!connectionString.Contains($"{param}="))
                {
                    errors.Add($"連線字串缺少必要參數: {param}");
                }
            }

            // 檢查Server參數格式
            var serverMatch = Regex.Match(connectionString, @"Server=([^;]+);");
            if (serverMatch.Success)
            {
                var server = serverMatch.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(server))
                {
                    errors.Add("伺服器位址不能為空");
                }
                else if (!IsValidServerAddress(server))
                {
                    errors.Add("伺服器位址格式不正確");
                }
            }

            // 檢查Database參數
            var dbMatch = Regex.Match(connectionString, @"Database=([^;]+);");
            if (dbMatch.Success)
            {
                var database = dbMatch.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(database))
                {
                    errors.Add("資料庫名稱不能為空");
                }
                else if (!IsValidDatabaseName(database))
                {
                    errors.Add("資料庫名稱包含無效字元");
                }
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// 驗證標籤選擇
        /// </summary>
        public ValidationResult ValidateTagSelection(IEnumerable<TagInfo> selectedTags, int maxTags = 8)
        {
            var errors = new List<string>();
            var tagList = selectedTags?.ToList() ?? new List<TagInfo>();

            if (!tagList.Any())
            {
                errors.Add("至少需要選擇一個標籤");
            }

            if (tagList.Count > maxTags)
            {
                errors.Add($"最多只能選擇 {maxTags} 個標籤");
            }

            // 檢查是否有重複的標籤
            var duplicates = tagList.GroupBy(t => t.Index)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            if (duplicates.Any())
            {
                errors.Add("選擇的標籤中存在重複項目");
            }

            // 檢查標籤資料的完整性
            foreach (var tag in tagList)
            {
                if (string.IsNullOrWhiteSpace(tag.TagNo))
                {
                    errors.Add($"標籤 {tag.Index} 的編號無效");
                }

                if (string.IsNullOrWhiteSpace(tag.TagName))
                {
                    errors.Add($"標籤 {tag.TagNo} 的名稱無效");
                }

                if (tag.TableNo <= 0)
                {
                    errors.Add($"標籤 {tag.TagNo} 的表格編號無效");
                }

                if (tag.ItemPos <= 0)
                {
                    errors.Add($"標籤 {tag.TagNo} 的欄位位置無效");
                }
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// 驗證趨勢數據
        /// </summary>
        public ValidationResult ValidateTrendData(List<TrendDataPoint> data, string tagName = null)
        {
            var errors = new List<string>();
            var tagIdentifier = string.IsNullOrEmpty(tagName) ? "Unknown" : tagName;

            if (data == null)
            {
                errors.Add($"標籤 {tagIdentifier} 的資料為空");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            if (!data.Any())
            {
                errors.Add($"標籤 {tagIdentifier} 沒有資料點");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            // 檢查數據點的有效性
            var invalidPoints = data.Where(p =>
                double.IsNaN(p.Value) ||
                double.IsInfinity(p.Value) ||
                p.DateTime == DateTime.MinValue ||
                p.DateTime == DateTime.MaxValue
            ).ToList();

            if (invalidPoints.Any())
            {
                errors.Add($"標籤 {tagIdentifier} 包含 {invalidPoints.Count} 個無效數據點");
            }

            // 檢查時間序列是否單調遞增
            for (int i = 1; i < data.Count; i++)
            {
                if (data[i].DateTime <= data[i - 1].DateTime)
                {
                    errors.Add($"標籤 {tagIdentifier} 的時間序列不是單調遞增的");
                    break;
                }
            }

            // 檢查數據範圍
            if (data.Any())
            {
                var values = data.Select(p => p.Value).Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToList();

                if (values.Any())
                {
                    var range = values.Max() - values.Min();
                    if (range > 1e15)
                    {
                        errors.Add($"標籤 {tagIdentifier} 的數值範圍過大，可能影響圖表顯示");
                    }
                }
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors,
                Warnings = GenerateDataWarnings(data, tagIdentifier)
            };
        }

        /// <summary>
        /// 驗證檔案路徑
        /// </summary>
        public ValidationResult ValidateFilePath(string filePath, string[] allowedExtensions = null)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                errors.Add("檔案路徑不能為空");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            try
            {
                var directory = System.IO.Path.GetDirectoryName(filePath);
                if (!System.IO.Directory.Exists(directory))
                {
                    errors.Add("指定的目錄不存在");
                }

                var fileName = System.IO.Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    errors.Add("檔案名稱無效");
                }

                if (allowedExtensions != null && allowedExtensions.Length > 0)
                {
                    var extension = System.IO.Path.GetExtension(filePath).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        errors.Add($"不支援的檔案格式。允許的格式: {string.Join(", ", allowedExtensions)}");
                    }
                }

                // 檢查檔案名稱是否包含無效字元
                var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                if (fileName.Any(c => invalidChars.Contains(c)))
                {
                    errors.Add("檔案名稱包含無效字元");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"檔案路徑格式錯誤: {ex.Message}");
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// 驗證時間字串格式
        /// </summary>
        public ValidationResult ValidateTimeString(string timeString)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(timeString))
            {
                errors.Add("時間字串不能為空");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            if (!TimeSpan.TryParse(timeString, out var timeSpan))
            {
                errors.Add("時間格式不正確，請使用 HH:mm:ss 格式");
            }
            else
            {
                if (timeSpan.TotalDays >= 1)
                {
                    errors.Add("時間不能超過24小時");
                }

                if (timeSpan < TimeSpan.Zero)
                {
                    errors.Add("時間不能為負值");
                }
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// 綜合驗證圖表設定
        /// </summary>
        public ValidationResult ValidateChartSettings(
            DateTime startTime,
            DateTime endTime,
            double yMin,
            double yMax,
            IEnumerable<TagInfo> selectedTags)
        {
            var allErrors = new List<string>();
            var allWarnings = new List<string>();

            // 驗證時間範圍
            var timeResult = ValidateTimeRange(startTime, endTime);
            allErrors.AddRange(timeResult.Errors);
            allWarnings.AddRange(timeResult.Warnings);

            // 驗證Y軸範圍
            var yAxisResult = ValidateYAxisRange(yMin, yMax);
            allErrors.AddRange(yAxisResult.Errors);
            allWarnings.AddRange(yAxisResult.Warnings);

            // 驗證標籤選擇
            var tagResult = ValidateTagSelection(selectedTags);
            allErrors.AddRange(tagResult.Errors);
            allWarnings.AddRange(tagResult.Warnings);

            // 額外的綜合驗證
            var timeSpan = endTime - startTime;
            var tagCount = selectedTags?.Count() ?? 0;

            if (timeSpan.TotalDays > 30 && tagCount > 4)
            {
                allWarnings.Add("長時間範圍配合多個標籤可能導致載入時間較長");
            }

            if (timeSpan.TotalHours < 1 && tagCount > 6)
            {
                allWarnings.Add("短時間範圍內顯示太多標籤可能影響圖表可讀性");
            }

            return new ValidationResult
            {
                IsValid = !allErrors.Any(),
                Errors = allErrors,
                Warnings = allWarnings
            };
        }

        #region Private Helper Methods

        private bool IsValidServerAddress(string server)
        {
            // 檢查IP地址格式
            if (System.Net.IPAddress.TryParse(server, out _))
                return true;

            // 檢查域名格式
            var domainPattern = @"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$";
            if (Regex.IsMatch(server, domainPattern))
                return true;

            // 檢查本地主機名稱
            if (server.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                server.Equals(".", StringComparison.OrdinalIgnoreCase) ||
                server.StartsWith(".\\"))
                return true;

            return false;
        }

        private bool IsValidDatabaseName(string databaseName)
        {
            // SQL Server資料庫名稱規則
            var invalidChars = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
            return !databaseName.Any(c => invalidChars.Contains(c)) &&
                   databaseName.Length <= 128 &&
                   !databaseName.StartsWith(" ") &&
                   !databaseName.EndsWith(" ");
        }

        private List<string> GenerateDataWarnings(List<TrendDataPoint> data, string tagIdentifier)
        {
            var warnings = new List<string>();

            if (data.Count > 100000)
            {
                warnings.Add($"標籤 {tagIdentifier} 包含大量數據點({data.Count})，可能影響效能");
            }

            // 檢查數據間隙
            var timeGaps = new List<TimeSpan>();
            for (int i = 1; i < Math.Min(data.Count, 1000); i++) // 只檢查前1000個點以提高效能
            {
                timeGaps.Add(data[i].DateTime - data[i - 1].DateTime);
            }

            if (timeGaps.Any())
            {
                var avgGap = TimeSpan.FromTicks((long)timeGaps.Average(t => t.Ticks));
                var maxGap = timeGaps.Max();

                if (maxGap > TimeSpan.FromTicks(avgGap.Ticks * 10))
                {
                    warnings.Add($"標籤 {tagIdentifier} 的數據存在較大時間間隙");
                }
            }

            return warnings;
        }

        #endregion
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public string GetErrorMessage()
        {
            return string.Join(Environment.NewLine, Errors);
        }

        public string GetWarningMessage()
        {
            return string.Join(Environment.NewLine, Warnings);
        }

        public bool HasWarnings => Warnings.Any();
    }
}