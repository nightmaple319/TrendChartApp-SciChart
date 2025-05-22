using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace TrendChartApp.Services
{
    public class ConfigurationService
    {
        private readonly string _configFilePath;
        private AppSettings _settings;

        public ConfigurationService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "TrendChartApp");
            Directory.CreateDirectory(appFolder);
            _configFilePath = Path.Combine(appFolder, "appsettings.json");

            LoadSettings();
        }

        public AppSettings Settings => _settings;

        /// <summary>
        /// 載入設定
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    };
                    _settings = JsonSerializer.Deserialize<AppSettings>(json, options);
                }
                else
                {
                    _settings = GetDefaultSettings();
                    SaveSettings();
                }
            }
            catch (Exception)
            {
                _settings = GetDefaultSettings();
            }
        }

        /// <summary>
        /// 儲存設定
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                // 記錄錯誤但不拋出異常
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得預設設定
        /// </summary>
        private AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                Database = new DatabaseSettings
                {
                    ConnectionString = "Server=127.0.0.1;Database=TrendDB_PE3;User Id=ViewDev;Password=ViewDev;",
                    ConnectionTimeout = 30,
                    CommandTimeout = 60
                },
                Chart = new ChartSettings
                {
                    DefaultTimeRangeHours = 1,
                    MaxSelectedTags = 8,
                    MaxDataPoints = 10000,
                    DefaultColors = new List<string>
                    {
                        "#FF0000", "#0000FF", "#008000", "#FFA500",
                        "#800080", "#008080", "#A52A2A", "#FF00FF"
                    },
                    AutoYAxisScale = true,
                    ShowLegend = true,
                    ShowTooltip = true,
                    LineThickness = 2
                },
                UI = new UISettings
                {
                    Theme = "Dark",
                    Language = "zh-TW",
                    WindowState = "Normal",
                    WindowWidth = 1300,
                    WindowHeight = 700,
                    RememberWindowPosition = true
                },
                Performance = new PerformanceSettings
                {
                    EnableDataCache = true,
                    CacheExpirationMinutes = 10,
                    MaxCacheSize = 100,
                    MaxConcurrentConnections = 10,
                    DataSamplingThreshold = 50000
                }
            };
        }

        /// <summary>
        /// 更新資料庫連線字串
        /// </summary>
        public void UpdateConnectionString(string connectionString)
        {
            _settings.Database.ConnectionString = connectionString;
            SaveSettings();
        }

        /// <summary>
        /// 更新視窗設定
        /// </summary>
        public void UpdateWindowSettings(double width, double height, string state, double? left = null, double? top = null)
        {
            _settings.UI.WindowWidth = width;
            _settings.UI.WindowHeight = height;
            _settings.UI.WindowState = state;

            if (left.HasValue && top.HasValue && _settings.UI.RememberWindowPosition)
            {
                _settings.UI.WindowLeft = left.Value;
                _settings.UI.WindowTop = top.Value;
            }

            SaveSettings();
        }

        /// <summary>
        /// 重置為預設設定
        /// </summary>
        public void ResetToDefaults()
        {
            _settings = GetDefaultSettings();
            SaveSettings();
        }
    }

    public class AppSettings
    {
        public DatabaseSettings Database { get; set; }
        public ChartSettings Chart { get; set; }
        public UISettings UI { get; set; }
        public PerformanceSettings Performance { get; set; }
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; }
        public int ConnectionTimeout { get; set; }
        public int CommandTimeout { get; set; }
    }

    public class ChartSettings
    {
        public int DefaultTimeRangeHours { get; set; }
        public int MaxSelectedTags { get; set; }
        public int MaxDataPoints { get; set; }
        public List<string> DefaultColors { get; set; }
        public bool AutoYAxisScale { get; set; }
        public bool ShowLegend { get; set; }
        public bool ShowTooltip { get; set; }
        public int LineThickness { get; set; }
    }

    public class UISettings
    {
        public string Theme { get; set; }
        public string Language { get; set; }
        public string WindowState { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public double WindowLeft { get; set; }
        public double WindowTop { get; set; }
        public bool RememberWindowPosition { get; set; }
    }

    public class PerformanceSettings
    {
        public bool EnableDataCache { get; set; }
        public int CacheExpirationMinutes { get; set; }
        public int MaxCacheSize { get; set; }
        public int MaxConcurrentConnections { get; set; }
        public int DataSamplingThreshold { get; set; }
    }
}