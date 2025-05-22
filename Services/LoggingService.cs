using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TrendChartApp.Services
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public class LoggingService
    {
        private static readonly Lazy<LoggingService> _instance = new(() => new LoggingService());
        public static LoggingService Instance => _instance.Value;

        private readonly string _logDirectory;
        private string _logFileName;
        private readonly ConcurrentQueue<LogEntry> _logQueue = new();
        private readonly object _fileLock = new object();
        private bool _isLoggingEnabled = true;

        private LoggingService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDirectory = Path.Combine(appDataPath, "TrendChartApp", "Logs");
            Directory.CreateDirectory(_logDirectory);

            _logFileName = Path.Combine(_logDirectory, $"TrendChart_{DateTime.Now:yyyyMMdd}.log");

            // 啟動後台日誌寫入任務
            Task.Run(ProcessLogQueue);
        }

        /// <summary>
        /// 記錄除錯訊息
        /// </summary>
        public void LogDebug(string message, string source = null)
        {
            Log(LogLevel.Debug, message, source);
        }

        /// <summary>
        /// 記錄資訊訊息
        /// </summary>
        public void LogInfo(string message, string source = null)
        {
            Log(LogLevel.Info, message, source);
        }

        /// <summary>
        /// 記錄警告訊息
        /// </summary>
        public void LogWarning(string message, string source = null)
        {
            Log(LogLevel.Warning, message, source);
        }

        /// <summary>
        /// 記錄錯誤訊息
        /// </summary>
        public void LogError(string message, Exception exception = null, string source = null)
        {
            var fullMessage = exception != null
                ? $"{message} - Exception: {exception}"
                : message;
            Log(LogLevel.Error, fullMessage, source);
        }

        /// <summary>
        /// 記錄嚴重錯誤訊息
        /// </summary>
        public void LogCritical(string message, Exception exception = null, string source = null)
        {
            var fullMessage = exception != null
                ? $"{message} - Exception: {exception}"
                : message;
            Log(LogLevel.Critical, fullMessage, source);
        }

        /// <summary>
        /// 記錄效能指標
        /// </summary>
        public void LogPerformance(string operation, TimeSpan duration, string additionalInfo = null)
        {
            var message = $"Performance - {operation}: {duration.TotalMilliseconds:F2}ms";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                message += $" - {additionalInfo}";
            }
            Log(LogLevel.Info, message, "Performance");
        }

        /// <summary>
        /// 記錄資料庫操作
        /// </summary>
        public void LogDatabaseOperation(string operation, TimeSpan duration, int recordCount = 0)
        {
            var message = $"DB Operation - {operation}: {duration.TotalMilliseconds:F2}ms";
            if (recordCount > 0)
            {
                message += $", Records: {recordCount}";
            }
            Log(LogLevel.Info, message, "Database");
        }

        /// <summary>
        /// 核心日誌方法
        /// </summary>
        private void Log(LogLevel level, string message, string source)
        {
            if (!_isLoggingEnabled) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Source = source ?? "Unknown",
                ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId
            };

            _logQueue.Enqueue(entry);

            // 對於嚴重錯誤，立即輸出到控制台
            if (level >= LogLevel.Error)
            {
                Console.WriteLine($"[{level}] {entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} - {message}");
            }
        }

        /// <summary>
        /// 後台處理日誌佇列
        /// </summary>
        private async Task ProcessLogQueue()
        {
            while (true)
            {
                try
                {
                    if (_logQueue.TryDequeue(out var entry))
                    {
                        await WriteLogEntryToFile(entry);
                    }
                    else
                    {
                        await Task.Delay(100); // 沒有日誌時稍作等待
                    }
                }
                catch (Exception ex)
                {
                    // 避免日誌系統本身的錯誤影響應用程式
                    Console.WriteLine($"Logging error: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
        }

        /// <summary>
        /// 將日誌項目寫入檔案
        /// </summary>
        private async Task WriteLogEntryToFile(LogEntry entry)
        {
            var logLine = FormatLogEntry(entry);

            lock (_fileLock)
            {
                try
                {
                    File.AppendAllText(_logFileName, logLine, Encoding.UTF8);
                }
                catch (Exception)
                {
                    // 如果寫入失敗，嘗試重新創建日誌檔案
                    try
                    {
                        _logFileName = Path.Combine(_logDirectory, $"TrendChart_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                        File.WriteAllText(_logFileName, logLine, Encoding.UTF8);
                    }
                    catch
                    {
                        // 最後手段：輸出到控制台
                        Console.WriteLine(logLine);
                    }
                }
            }

            // 清理舊日誌檔案（保留最近7天）
            await CleanupOldLogFiles();
        }

        /// <summary>
        /// 格式化日誌項目
        /// </summary>
        private string FormatLogEntry(LogEntry entry)
        {
            return $"[{entry.Level,-8}] {entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} " +
                   $"[{entry.ThreadId,2}] [{entry.Source,-12}] {entry.Message}{Environment.NewLine}";
        }

        /// <summary>
        /// 清理舊日誌檔案
        /// </summary>
        private async Task CleanupOldLogFiles()
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-7);
                var logFiles = Directory.GetFiles(_logDirectory, "TrendChart_*.log");

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // 忽略清理錯誤
            }
        }

        /// <summary>
        /// 啟用或停用日誌記錄
        /// </summary>
        public void SetLoggingEnabled(bool enabled)
        {
            _isLoggingEnabled = enabled;
        }

        /// <summary>
        /// 獲取當前日誌檔案路徑
        /// </summary>
        public string GetCurrentLogFilePath()
        {
            return _logFileName;
        }

        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Message { get; set; }
            public string Source { get; set; }
            public int ThreadId { get; set; }
        }
    }

    /// <summary>
    /// 效能測量輔助類
    /// </summary>
    public class PerformanceTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly string _additionalInfo;
        private readonly DateTime _startTime;
        private bool _disposed = false;

        public PerformanceTimer(string operationName, string additionalInfo = null)
        {
            _operationName = operationName;
            _additionalInfo = additionalInfo;
            _startTime = DateTime.Now;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var duration = DateTime.Now - _startTime;
                LoggingService.Instance.LogPerformance(_operationName, duration, _additionalInfo);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 全域異常處理器
    /// </summary>
    public static class GlobalExceptionHandler
    {
        public static void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                LoggingService.Instance.LogCritical("Unhandled exception occurred", exception, "GlobalHandler");
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LoggingService.Instance.LogCritical("Unobserved task exception occurred", e.Exception, "TaskScheduler");
                e.SetObserved(); // 防止應用程式崩潰
            };
        }
    }
}