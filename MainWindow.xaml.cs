using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using TrendChartApp.Models;
using TrendChartApp.Helpers;
using TrendChartApp.Views;
using TrendChartApp.Services;
using System.Windows.Threading;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Data.Model;
using SciChart.Charting.Visuals;
using SciChart.Charting.ChartModifiers;
using SciChart.Charting.Visuals.PointMarkers;
using SciChart.Core.Extensions;
using SciChart.Drawing.VisualXcceleratorRasterizer;
using SciChart.Charting;
using SciChart.Drawing.Common;

namespace TrendChartApp
{
    /// <summary>
    /// MainWindow class for the main trend chart application interface
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region 參數宣告

        // Services
        private DatabaseHelper _dbHelper;
        private ConfigurationService _configService;
        private ValidationService _validationService;
        private DataCacheService _cacheService;
        private ProgressService _progressService;

        // SciChart Surface
        private SciChartSurface _sciChartSurface;

        // Selected tags for charting
        private ObservableCollection<TagInfo> _selectedTags;
        public ObservableCollection<TagInfo> SelectedTags
        {
            get => _selectedTags;
            set
            {
                _selectedTags = value;
                OnPropertyChanged();
            }
        }

        // SciChart系列集合
        private ObservableCollection<BaseRenderableSeries> _renderableSeries;
        public ObservableCollection<BaseRenderableSeries> RenderableSeries
        {
            get => _renderableSeries;
            set
            {
                _renderableSeries = value;
                OnPropertyChanged();
            }
        }

        // 數據系列集合，用於存儲每個標籤的數據
        private Dictionary<int, IXyDataSeries<DateTime, double>> _dataSeries;

        // Time range properties
        private DateTime _startTime;
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
            }
        }

        // 開始日期
        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                UpdateStartTime();
                OnPropertyChanged();
            }
        }

        // 開始時間文本
        private string _startTimeText = "00:00:00";
        public string StartTimeText
        {
            get => _startTimeText;
            set
            {
                _startTimeText = value;
                UpdateStartTime();
                OnPropertyChanged();
            }
        }

        private DateTime _endTime;
        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
            }
        }

        // 結束日期
        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                UpdateEndTime();
                OnPropertyChanged();
            }
        }

        // 結束時間文本
        private string _endTimeText = "23:59:59";
        public string EndTimeText
        {
            get => _endTimeText;
            set
            {
                _endTimeText = value;
                UpdateEndTime();
                OnPropertyChanged();
            }
        }

        // SciChart可見範圍
        private DateRange _visibleRange;
        public DateRange VisibleRange
        {
            get => _visibleRange;
            set
            {
                _visibleRange = value;
                OnPropertyChanged();
            }
        }

        // Y軸範圍
        private DoubleRange _yAxisRange;
        public DoubleRange YAxisRange
        {
            get => _yAxisRange;
            set
            {
                _yAxisRange = value;
                OnPropertyChanged();
            }
        }

        // Y-axis range properties
        private double _yAxisMin;
        public double YAxisMin
        {
            get => _yAxisMin;
            set
            {
                _yAxisMin = value;
                UpdateYAxisRange();
                OnPropertyChanged();
            }
        }

        private double _yAxisMax;
        public double YAxisMax
        {
            get => _yAxisMax;
            set
            {
                _yAxisMax = value;
                UpdateYAxisRange();
                OnPropertyChanged();
            }
        }

        // 縮放狀態
        private bool _isZooming = false;
        private readonly DispatcherTimer _zoomTimer = new DispatcherTimer();

        // 時間格式跟踪
        private string _currentTimeFormat = "yyyy-MM-dd HH:mm:ss";

        // X-axis labels formatter
        public string DateTimeFormatter { get; set; }

        // 添加一個IsUiEnabled屬性來控制UI元素的啟用狀態
        private bool _isUiEnabled = true;
        public bool IsUiEnabled
        {
            get => _isUiEnabled;
            set
            {
                _isUiEnabled = value;
                OnPropertyChanged();
            }
        }

        // 資料縮減係數 (重新取樣比例)
        private int _samplingFactor = 1;  // 初始值為1，表示不縮減

        #endregion

        #region 初始化

        /// <summary>
        /// 建構函數
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            InitializeControls();
            InitializeSciChart();
        }

        /// <summary>
        /// 初始化服務
        /// </summary>
        private void InitializeServices()
        {
            // 初始化配置服務
            _configService = new ConfigurationService();

            // 初始化資料庫助手
            _dbHelper = new DatabaseHelper(_configService.Settings.Database.ConnectionString);

            // 初始化其他服務
            _validationService = new ValidationService();
            _cacheService = new DataCacheService();
            _progressService = new ProgressService(Dispatcher);

            // 初始化全域異常處理
            GlobalExceptionHandler.Initialize();

            // 記錄應用程式啟動
            LoggingService.Instance.LogInfo("應用程式啟動", "MainWindow");
        }

        /// <summary>
        /// 初始化控件
        /// </summary>
        private void InitializeControls()
        {
            // 初始化集合
            _renderableSeries = new ObservableCollection<BaseRenderableSeries>();
            _selectedTags = new ObservableCollection<TagInfo>();
            _dataSeries = new Dictionary<int, IXyDataSeries<DateTime, double>>();

            // 設置時間範圍
            InitializeTimeRange();

            // 設置Y軸範圍
            YAxisMin = 0;
            YAxisMax = 100;
            UpdateYAxisRange();

            // 初始化縮放計時器
            _zoomTimer.Interval = TimeSpan.FromMilliseconds(500);
            _zoomTimer.Tick += ZoomTimer_Tick;

            // 設置資料上下文
            DataContext = this;

            // 設置日期時間格式
            SetDateTimeFormatter(TimeSpan.FromHours(1));
        }

        /// <summary>
        /// 初始化時間範圍
        /// </summary>
        private void InitializeTimeRange()
        {
            // 設置默認時間範圍 (最近1小時)
            DateTime currentTime = DateTime.Now;
            DateTime startTime = currentTime.AddHours(-_configService.Settings.Chart.DefaultTimeRangeHours);

            // 設置開始和結束時間
            EndDate = currentTime.Date;
            EndTimeText = currentTime.ToString("HH:mm:ss");
            EndTime = currentTime;

            StartDate = startTime.Date;
            StartTimeText = startTime.ToString("HH:mm:ss");
            StartTime = startTime;

            // 設置可見範圍
            VisibleRange = new DateRange(StartTime, EndTime);
        }

        /// <summary>
        /// 初始化 SciChart 控件
        /// </summary>
        private void InitializeSciChart()
        {
            try
            {
                // 創建新的 SciChartSurface
                _sciChartSurface = new SciChartSurface();

                // 創建X軸 (DateTimeAxis)
                var xAxis = new DateTimeAxis
                {
                    AxisTitle = "時間",
                    TextFormatting = DateTimeFormatter,
                    DrawMajorBands = false,
                    AutoRange = AutoRange.Never,
                    Foreground = new SolidColorBrush(Colors.White)
                };

                // 繫結 VisibleRange
                var xAxisBinding = new Binding("VisibleRange")
                {
                    Source = this,
                    Mode = BindingMode.TwoWay
                };
                xAxis.SetBinding(DateTimeAxis.VisibleRangeProperty, xAxisBinding);

                // 繫結 TextFormatting
                var formatterBinding = new Binding("DateTimeFormatter")
                {
                    Source = this,
                    Mode = BindingMode.OneWay
                };
                xAxis.SetBinding(DateTimeAxis.TextFormattingProperty, formatterBinding);

                // 創建Y軸 (NumericAxis)
                var yAxis = new NumericAxis
                {
                    AxisTitle = "數值",
                    AutoRange = AutoRange.Never,
                    Foreground = new SolidColorBrush(Colors.White)
                };

                // 繫結 VisibleRange
                var yAxisBinding = new Binding("YAxisRange")
                {
                    Source = this,
                    Mode = BindingMode.TwoWay
                };
                yAxis.SetBinding(NumericAxis.VisibleRangeProperty, yAxisBinding);

                // 加入軸到圖表
                _sciChartSurface.XAxes.Add(xAxis);
                _sciChartSurface.YAxes.Add(yAxis);

                // 添加修飾器
                var modifierGroup = new ModifierGroup();

                // 添加縮放和平移功能
                modifierGroup.ChildModifiers.Add(new RubberBandXyZoomModifier
                {
                    IsXAxisOnly = true,
                    ExecuteOn = ExecuteOn.MouseLeftButton,
                    ZoomExtentsY = false
                });

                modifierGroup.ChildModifiers.Add(new ZoomPanModifier
                {
                    ExecuteOn = ExecuteOn.MouseRightButton,
                    ClipModeX = ClipMode.None
                });

                modifierGroup.ChildModifiers.Add(new ZoomExtentsModifier
                {
                    ExecuteOn = ExecuteOn.MouseDoubleClick
                });

                modifierGroup.ChildModifiers.Add(new MouseWheelZoomModifier());

                // 添加遊標修飾器
                var cursorModifier = new CursorModifier
                {
                    ShowTooltip = true,
                    ShowAxisLabels = true,
                    SourceMode = SourceMode.AllSeries
                };
                modifierGroup.ChildModifiers.Add(cursorModifier);

                // 添加圖例修飾器
                var legendModifier = new LegendModifier
                {
                    Orientation = Orientation.Vertical,
                    LegendPlacement = LegendPlacement.Right,
                    ShowLegend = true
                };
                modifierGroup.ChildModifiers.Add(legendModifier);

                // 應用修飾器到圖表
                _sciChartSurface.ChartModifier = modifierGroup;

                // 設置背景色
                _sciChartSurface.Background = (SolidColorBrush)FindResource("PanelBrush");

                // 使用高性能渲染器
                _sciChartSurface.RenderableSeries = new ObservableCollection<IRenderableSeries>();

                // 嘗試設置渲染表面 (如果版本支持)
                try
                {
                    // 使用反射來設置渲染表面，以處理不同版本的API
                    var renderSurfaceProperty = typeof(SciChartSurface).GetProperty("RenderSurface");
                    if (renderSurfaceProperty != null)
                    {
                        renderSurfaceProperty.SetValue(_sciChartSurface, 2); // 2 通常對應 DirectX 或 VisualXccelerator 
                    }
                }
                catch
                {
                    // 忽略錯誤，如果不支援此功能
                }

                // 將圖表添加到容器
                chartContainer.Content = _sciChartSurface;

                LoggingService.Instance.LogInfo("SciChart 初始化完成", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("SciChart 初始化失敗", ex, "MainWindow");
                MessageBox.Show($"圖表初始化失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 事件處理程序

        /// <summary>
        /// 打開標籤選擇視窗按鈕點擊事件
        /// </summary>
        private async void OpenTagSelectionWindow(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectionWindow = new TagSelectionWindow(SelectedTags);
                selectionWindow.Owner = this;

                if (selectionWindow.ShowDialog() == true)
                {
                    // 更新選中的標籤
                    SelectedTags = new ObservableCollection<TagInfo>(selectionWindow.SelectedTags);

                    // 驗證標籤選擇
                    var validation = _validationService.ValidateTagSelection(SelectedTags, _configService.Settings.Chart.MaxSelectedTags);
                    if (!validation.IsValid)
                    {
                        MessageBox.Show(validation.GetErrorMessage(), "標籤選擇錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else if (validation.HasWarnings)
                    {
                        MessageBox.Show(validation.GetWarningMessage(), "標籤選擇警告", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    LoggingService.Instance.LogInfo($"已選擇 {SelectedTags.Count} 個標籤", "MainWindow");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("打開標籤選擇視窗失敗", ex, "MainWindow");
                MessageBox.Show($"打開標籤選擇視窗時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新圖表按鈕點擊事件
        /// </summary>
        private async void UpdateChart(object sender, RoutedEventArgs e)
        {
            using var perfTimer = new PerformanceTimer("UpdateChart");

            try
            {
                // 驗證圖表設定
                var validation = _validationService.ValidateChartSettings(StartTime, EndTime, YAxisMin, YAxisMax, SelectedTags);
                if (!validation.IsValid)
                {
                    MessageBox.Show(validation.GetErrorMessage(), "設定錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (validation.HasWarnings)
                {
                    var result = MessageBox.Show($"{validation.GetWarningMessage()}\n\n是否繼續？",
                        "設定警告", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                        return;
                }

                // 檢查是否有選中的標籤
                if (SelectedTags.Count == 0)
                {
                    MessageBox.Show("請先選擇標籤。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 使用進度服務執行更新
                await _progressService.ExecuteWithProgressAsync(async (progress, cancellationToken) =>
                {
                    // 刪除現有系列
                    progress.Report(new ProgressInfo("清理現有圖表..."));
                    ClearExistingSeries();

                    // 批次載入資料
                    var tagDataDict = await _dbHelper.GetMultipleTagDataWithProgressAsync(
                        SelectedTags, StartTime, EndTime, progress, cancellationToken);

                    // 為每個選中標籤添加新系列
                    int processedTags = 0;
                    foreach (var tag in SelectedTags)
                    {
                        progress.Report(new ProgressInfo($"建立圖表系列: {tag.TagName}",
                            (double)processedTags / SelectedTags.Count * 100));

                        if (tagDataDict.TryGetValue(tag.Index, out var data) && data.Count > 0)
                        {
                            await AddSeries(tag, data);
                        }

                        processedTags++;
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    // 計算合適的時間格式和採樣係數
                    var timeSpan = EndTime - StartTime;
                    SetDateTimeFormatter(timeSpan);
                    CalculateSamplingFactor(timeSpan);

                    // 設置X軸範圍
                    VisibleRange = new DateRange(StartTime, EndTime);

                    // 自動調整Y軸範圍
                    if (_configService.Settings.Chart.AutoYAxisScale)
                    {
                        AutoAdjustYAxisRange();
                    }

                    return true;
                }, "正在更新趨勢圖...");

                LoggingService.Instance.LogInfo($"圖表更新完成，共載入 {SelectedTags.Count} 個標籤的資料", "MainWindow");
            }
            catch (OperationCanceledException)
            {
                LoggingService.Instance.LogInfo("圖表更新被用戶取消", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("更新圖表失敗", ex, "MainWindow");
                MessageBox.Show($"更新圖表時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重置縮放按鈕點擊事件
        /// </summary>
        private void ResetZoom(object sender, RoutedEventArgs e)
        {
            try
            {
                VisibleRange = new DateRange(StartTime, EndTime);
                LoggingService.Instance.LogInfo("重置圖表縮放", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("重置縮放失敗", ex, "MainWindow");
            }
        }

        /// <summary>
        /// 打開資料庫連線設定視窗
        /// </summary>
        private void OpenDatabaseConnectionWindow(object sender, RoutedEventArgs e)
        {
            try
            {
                // 創建資料庫連線設定視窗的實例，並傳入當前的連線字串
                var connectionWindow = new Views.DatabaseConnectionWindow(_configService.Settings.Database.ConnectionString);
                connectionWindow.Owner = this;

                // 顯示視窗並等待結果
                if (connectionWindow.ShowDialog() == true)
                {
                    // 如果用戶按了「儲存」，則更新連線字串並重新初始化資料庫連接
                    string newConnectionString = AppConfig.ConnectionString;

                    // 更新配置服務中的連線字串
                    _configService.UpdateConnectionString(newConnectionString);

                    // 重新創建資料庫助手
                    _dbHelper = new DatabaseHelper(newConnectionString);

                    // 清空緩存
                    _cacheService.ClearCache();

                    // 提示用戶重新載入資料
                    MessageBox.Show("資料庫連線設定已更新。請重新載入資料以應用新的設定。",
                                    "連線設定已更新", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoggingService.Instance.LogInfo("資料庫連線設定已更新", "MainWindow");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("打開資料庫連線設定視窗失敗", ex, "MainWindow");
                MessageBox.Show($"打開資料庫連線設定時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 匯出趨勢圖為圖片
        /// </summary>
        private void ExportChartImage(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp",
                    DefaultExt = ".png",
                    Title = "Save Chart as Image"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 驗證檔案路徑
                    var validation = _validationService.ValidateFilePath(saveFileDialog.FileName, new[] { ".png", ".jpg", ".jpeg", ".bmp" });
                    if (!validation.IsValid)
                    {
                        MessageBox.Show(validation.GetErrorMessage(), "檔案路徑錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Utils.SaveElementAsImage(_sciChartSurface, saveFileDialog.FileName);
                    MessageBox.Show("圖片已成功保存。", "匯出成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoggingService.Instance.LogInfo($"圖表已匯出至: {saveFileDialog.FileName}", "MainWindow");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("匯出圖片失敗", ex, "MainWindow");
                MessageBox.Show($"保存圖片時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 匯出趨勢數據為CSV
        /// </summary>
        private async void ExportTrendData(object sender, RoutedEventArgs e)
        {
            try
            {
                // 檢查是否有數據
                if (_dataSeries == null || _dataSeries.Count == 0)
                {
                    MessageBox.Show("沒有可匯出的數據。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    Title = "Export Trend Data"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 驗證檔案路徑
                    var validation = _validationService.ValidateFilePath(saveFileDialog.FileName, new[] { ".csv" });
                    if (!validation.IsValid)
                    {
                        MessageBox.Show(validation.GetErrorMessage(), "檔案路徑錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await _progressService.ExecuteWithProgressAsync(async (progress, cancellationToken) =>
                    {
                        progress.Report(new ProgressInfo("正在匯出資料..."));

                        using (var writer = new StreamWriter(saveFileDialog.FileName))
                        {
                            // 寫入標題行
                            var headerLine = "DateTime";
                            foreach (var tag in SelectedTags)
                            {
                                headerLine += $",{tag.TagName}";
                            }
                            await writer.WriteLineAsync(headerLine);

                            // 獲取所有時間點
                            var allTimePoints = new HashSet<DateTime>();
                            foreach (var series in _dataSeries.Values)
                            {
                                for (int i = 0; i < series.Count; i++)
                                {
                                    allTimePoints.Add(series.XValues[i]);
                                }
                            }

                            // 排序時間點
                            var sortedTimePoints = allTimePoints.OrderBy(t => t).ToList();
                            int totalPoints = sortedTimePoints.Count;
                            int processedPoints = 0;

                            // 寫入資料行
                            foreach (var timePoint in sortedTimePoints)
                            {
                                var line = timePoint.ToString("yyyy-MM-dd HH:mm:ss");

                                foreach (var tag in SelectedTags)
                                {
                                    if (_dataSeries.TryGetValue(tag.Index, out var series))
                                    {
                                        // 查找最接近的時間點
                                        int index = -1;
                                        for (int i = 0; i < series.Count; i++)
                                        {
                                            if (series.XValues[i] == timePoint)
                                            {
                                                index = i;
                                                break;
                                            }
                                        }

                                        if (index != -1)
                                        {
                                            line += $",{series.YValues[index]:F2}";
                                        }
                                        else
                                        {
                                            line += ",";
                                        }
                                    }
                                    else
                                    {
                                        line += ",";
                                    }
                                }

                                await writer.WriteLineAsync(line);

                                processedPoints++;
                                if (processedPoints % 1000 == 0) // 每1000筆更新一次進度
                                {
                                    var percentage = (double)processedPoints / totalPoints * 100;
                                    progress.Report(new ProgressInfo($"已處理 {processedPoints}/{totalPoints} 筆資料", percentage));
                                }

                                cancellationToken.ThrowIfCancellationRequested();
                            }
                        }

                        return true;
                    }, "正在匯出趨勢資料...");

                    MessageBox.Show("資料已成功匯出。", "匯出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoggingService.Instance.LogInfo($"趨勢資料已匯出至: {saveFileDialog.FileName}", "MainWindow");
                }
            }
            catch (OperationCanceledException)
            {
                LoggingService.Instance.LogInfo("資料匯出被用戶取消", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("匯出資料失敗", ex, "MainWindow");
                MessageBox.Show($"匯出資料時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 退出應用程式
        /// </summary>
        private void ExitApplication(object sender, RoutedEventArgs e)
        {
            try
            {
                // 保存視窗設定
                _configService.UpdateWindowSettings(Width, Height, WindowState.ToString(), Left, Top);

                LoggingService.Instance.LogInfo("應用程式正常關閉", "MainWindow");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("應用程式關閉時發生錯誤", ex, "MainWindow");
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// 縮放計時器事件
        /// </summary>
        private void ZoomTimer_Tick(object sender, EventArgs e)
        {
            _zoomTimer.Stop();
            _isZooming = false;
            zoomingIndicator.Visibility = Visibility.Collapsed;

            // 如果視圖範圍變更幅度大，重新採樣時間點
            var visibleTimeSpan = VisibleRange.Max - VisibleRange.Min;
            SetDateTimeFormatter(visibleTimeSpan);
        }

        #endregion

        #region 輔助方法

        /// <summary>
        /// 根據時間範圍設置日期時間格式
        /// </summary>
        private void SetDateTimeFormatter(TimeSpan timeSpan)
        {
            // 根據時間範圍選擇合適的時間格式
            if (timeSpan.TotalDays > 365)
            {
                _currentTimeFormat = "yyyy-MM";
                DateTimeFormatter = "yyyy-MM";
            }
            else if (timeSpan.TotalDays > 30)
            {
                _currentTimeFormat = "MM-dd";
                DateTimeFormatter = "MM-dd";
            }
            else if (timeSpan.TotalDays > 1)
            {
                _currentTimeFormat = "MM-dd HH:mm";
                DateTimeFormatter = "MM-dd HH:mm";
            }
            else if (timeSpan.TotalHours > 1)
            {
                _currentTimeFormat = "HH:mm";
                DateTimeFormatter = "HH:mm";
            }
            else
            {
                _currentTimeFormat = "HH:mm:ss";
                DateTimeFormatter = "HH:mm:ss";
            }

            OnPropertyChanged(nameof(DateTimeFormatter));

            // 如果X軸已經創建，更新其格式
            if (_sciChartSurface != null && _sciChartSurface.XAxes.Count > 0 && _sciChartSurface.XAxes[0] is DateTimeAxis dateTimeAxis)
            {
                dateTimeAxis.TextFormatting = DateTimeFormatter;
            }
        }

        /// <summary>
        /// 計算數據採樣係數，用於減少大量數據的點
        /// </summary>
        private void CalculateSamplingFactor(TimeSpan timeSpan)
        {
            // 根據時間範圍和估計的數據點數來計算採樣係數
            var threshold = _configService.Settings.Performance.DataSamplingThreshold;

            if (timeSpan.TotalDays > 30)
            {
                _samplingFactor = 60; // 每60個點採樣1個
            }
            else if (timeSpan.TotalDays > 7)
            {
                _samplingFactor = 20; // 每20個點採樣1個
            }
            else if (timeSpan.TotalDays > 1)
            {
                _samplingFactor = 5; // 每5個點採樣1個
            }
            else
            {
                _samplingFactor = 1; // 不採樣，使用所有點
            }
        }

        /// <summary>
        /// 清除現有系列
        /// </summary>
        private void ClearExistingSeries()
        {
            if (_sciChartSurface != null)
            {
                _sciChartSurface.RenderableSeries.Clear();
            }
            _renderableSeries.Clear();
            _dataSeries.Clear();
        }

        /// <summary>
        /// 為標籤添加數據系列（使用預載入的資料）
        /// </summary>
        private async Task AddSeries(TagInfo tag, List<TrendDataPoint> data)
        {
            try
            {
                if (data.Count == 0)
                {
                    return; // 如果沒有數據則跳過
                }

                // 驗證數據
                var validation = _validationService.ValidateTrendData(data, tag.TagName);
                if (!validation.IsValid)
                {
                    LoggingService.Instance.LogWarning($"標籤 {tag.TagName} 的數據驗證失敗: {validation.GetErrorMessage()}", "MainWindow");
                }

                // 根據採樣係數縮減數據
                var sampledData = DatabaseHelper.SampleDataIntelligently(data, _configService.Settings.Chart.MaxDataPoints);

                // 創建數據系列
                var dataSeries = new XyDataSeries<DateTime, double> { SeriesName = tag.TagNo };

                // 添加數據點
                foreach (var point in sampledData)
                {
                    dataSeries.Append(point.DateTime, point.Value);
                }

                // 存儲數據系列
                _dataSeries[tag.Index] = dataSeries;

                // 獲取顏色索引
                int colorIndex = _renderableSeries.Count % _configService.Settings.Chart.DefaultColors.Count;
                var colorString = _configService.Settings.Chart.DefaultColors[colorIndex];
                var seriesColor = (Color)ColorConverter.ConvertFromString(colorString);

                // 創建 FastLineRenderableSeries
                var lineSeries = new FastLineRenderableSeries
                {
                    DataSeries = dataSeries,
                    Stroke = seriesColor,
                    StrokeThickness = _configService.Settings.Chart.LineThickness,
                    AntiAliasing = true
                };

                // 設置點標記
                var pointMarker = new EllipsePointMarker
                {
                    Width = 5,
                    Height = 5,
                    Fill = seriesColor,
                    Stroke = seriesColor
                };
                lineSeries.PointMarker = pointMarker;

                // 添加到渲染系列集合
                _renderableSeries.Add(lineSeries);

                // 添加到SciChart
                _sciChartSurface.RenderableSeries.Add(lineSeries);

                LoggingService.Instance.LogInfo($"已添加標籤 {tag.TagName} 的圖表系列，資料點數: {sampledData.Count}", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"添加標籤 {tag.TagName} 的圖表系列失敗", ex, "MainWindow");
            }
        }

        /// <summary>
        /// 自動調整Y軸範圍
        /// </summary>
        private void AutoAdjustYAxisRange()
        {
            if (_dataSeries.Count == 0)
            {
                return;
            }

            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            // 查找所有系列中的最小值和最大值
            foreach (var series in _dataSeries.Values)
            {
                for (int i = 0; i < series.Count; i++)
                {
                    double value = series.YValues[i];
                    minValue = Math.Min(minValue, value);
                    maxValue = Math.Max(maxValue, value);
                }
            }

            // 添加一些填充
            double padding = (maxValue - minValue) * 0.1;
            if (padding < 0.1) padding = 0.1; // 確保至少有一些填充

            YAxisMin = Math.Floor(minValue - padding);
            YAxisMax = Math.Ceiling(maxValue + padding);

            // 更新Y軸範圍
            UpdateYAxisRange();

            LoggingService.Instance.LogInfo($"Y軸範圍已自動調整為: {YAxisMin:F2} ~ {YAxisMax:F2}", "MainWindow");
        }

        /// <summary>
        /// 更新Y軸範圍
        /// </summary>
        private void UpdateYAxisRange()
        {
            YAxisRange = new DoubleRange(YAxisMin, YAxisMax);
        }

        /// <summary>
        /// 更新開始時間
        /// </summary>
        private void UpdateStartTime()
        {
            if (StartDate != DateTime.MinValue && !string.IsNullOrEmpty(StartTimeText))
            {
                try
                {
                    // 驗證時間字串
                    var timeValidation = _validationService.ValidateTimeString(StartTimeText);
                    if (timeValidation.IsValid)
                    {
                        if (TimeSpan.TryParse(StartTimeText, out TimeSpan startTime))
                        {
                            StartTime = StartDate.Date.Add(startTime);
                        }
                    }
                }
                catch (Exception)
                {
                    // 忽略格式錯誤，使用當前值
                }
            }
        }

        /// <summary>
        /// 更新結束時間
        /// </summary>
        private void UpdateEndTime()
        {
            if (EndDate != DateTime.MinValue && !string.IsNullOrEmpty(EndTimeText))
            {
                try
                {
                    // 驗證時間字串
                    var timeValidation = _validationService.ValidateTimeString(EndTimeText);
                    if (timeValidation.IsValid)
                    {
                        if (TimeSpan.TryParse(EndTimeText, out TimeSpan endTime))
                        {
                            EndTime = EndDate.Date.Add(endTime);
                        }
                    }
                }
                catch (Exception)
                {
                    // 忽略格式錯誤，使用當前值
                }
            }
        }

        /// <summary>
        /// 顯示載入覆蓋層（已被ProgressService取代，保留向後相容）
        /// </summary>
        private void ShowLoadingOverlay(string message)
        {
            loadingTextBlock.Text = message;
            loadingOverlay.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 隱藏載入覆蓋層（已被ProgressService取代，保留向後相容）
        /// </summary>
        private void HideLoadingOverlay()
        {
            loadingOverlay.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region INotifyPropertyChanged 實現

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region 清理資源

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // 清理服務資源
                _cacheService?.Dispose();
                _zoomTimer?.Stop();

                LoggingService.Instance.LogInfo("應用程式視窗已關閉", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("清理資源時發生錯誤", ex, "MainWindow");
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        #endregion
    }
}