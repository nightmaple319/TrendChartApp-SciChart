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

        // Database helper
        private readonly DatabaseHelper _dbHelper;

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

            // 初始化資料庫助手
            _dbHelper = new DatabaseHelper(AppConfig.ConnectionString);

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

            // 初始化 SciChart
            InitializeSciChart();
        }

        /// <summary>
        /// 初始化時間範圍
        /// </summary>
        private void InitializeTimeRange()
        {
            // 設置默認時間範圍 (最近1小時)
            DateTime currentTime = DateTime.Now;
            DateTime startTime = currentTime.AddHours(-AppConfig.DefaultChartTimeRangeHours);

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
            // 創建新的 SciChartSurface
            _sciChartSurface = new SciChartSurface();

            // 創建X軸 (DateTimeAxis)
            var xAxis = new DateTimeAxis
            {
                AxisTitle = "時間",
                TextFormatting = DateTimeFormatter,
                DrawMajorBands = false,
                AutoRange = AutoRange.Never,
                TitleStyle = new Style(typeof(TextBlock))
                {
                    Setters = { new Setter(TextBlock.MarginProperty, new Thickness(0, 10, 0, 0)) }
                },
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
                TitleStyle = new Style(typeof(TextBlock))
                {
                    Setters = { new Setter(TextBlock.MarginProperty, new Thickness(0, 10, 0, 0)) }
                },
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
        }

        #endregion

        #region 事件處理程序

        /// <summary>
        /// 打開標籤選擇視窗按鈕點擊事件
        /// </summary>
        private void OpenTagSelectionWindow(object sender, RoutedEventArgs e)
        {
            var selectionWindow = new TagSelectionWindow(SelectedTags);
            selectionWindow.Owner = this;

            if (selectionWindow.ShowDialog() == true)
            {
                // 更新選中的標籤
                SelectedTags = new ObservableCollection<TagInfo>(selectionWindow.SelectedTags);
            }
        }

        /// <summary>
        /// 更新圖表按鈕點擊事件
        /// </summary>
        private async void UpdateChart(object sender, RoutedEventArgs e)
        {
            // 檢查是否有選中的標籤
            if (SelectedTags.Count == 0)
            {
                MessageBox.Show("請先選擇標籤。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 顯示載入覆蓋層
            ShowLoadingOverlay("正在載入資料...");

            // 禁用UI
            IsUiEnabled = false;

            try
            {
                // 刪除現有系列
                ClearExistingSeries();

                // 為每個選中標籤添加新系列
                foreach (var tag in SelectedTags)
                {
                    await AddSeries(tag);
                }

                // 計算合適的時間格式和採樣係數
                var timeSpan = EndTime - StartTime;
                SetDateTimeFormatter(timeSpan);
                CalculateSamplingFactor(timeSpan);

                // 設置X軸範圍
                VisibleRange = new DateRange(StartTime, EndTime);

                // 自動調整Y軸範圍
                AutoAdjustYAxisRange();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新圖表時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 啟用UI
                IsUiEnabled = true;

                // 隱藏載入覆蓋層
                HideLoadingOverlay();
            }
        }

        /// <summary>
        /// 重置縮放按鈕點擊事件
        /// </summary>
        private void ResetZoom(object sender, RoutedEventArgs e)
        {
            VisibleRange = new DateRange(StartTime, EndTime);
        }

        /// <summary>
        /// 匯出趨勢圖為圖片
        /// </summary>
        private void ExportChartImage(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp",
                DefaultExt = ".png",
                Title = "Save Chart as Image"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    Utils.SaveElementAsImage(_sciChartSurface, saveFileDialog.FileName);
                    MessageBox.Show("圖片已成功保存。", "匯出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存圖片時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 匯出趨勢數據為CSV
        /// </summary>
        private void ExportTrendData(object sender, RoutedEventArgs e)
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
                try
                {
                    ShowLoadingOverlay("正在匯出資料...");

                    using (var writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        // 寫入標題行
                        var headerLine = "DateTime";
                        foreach (var tag in SelectedTags)
                        {
                            headerLine += $",{tag.TagName}";
                        }
                        writer.WriteLine(headerLine);

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

                            writer.WriteLine(line);
                        }
                    }

                    MessageBox.Show("資料已成功匯出。", "匯出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"匯出資料時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    HideLoadingOverlay();
                }
            }
        }

        /// <summary>
        /// 退出應用程式
        /// </summary>
        private void ExitApplication(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
            // 這是一個簡單的啟發式方法，可以根據實際需求調整
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
        /// 為標籤添加數據系列
        /// </summary>
        private async Task AddSeries(TagInfo tag)
        {
            // 獲取標籤數據
            var data = await _dbHelper.GetTagDataAsync(tag, StartTime, EndTime);

            if (data.Count == 0)
            {
                return; // 如果沒有數據則跳過
            }

            // 根據採樣係數縮減數據
            var sampledData = SampleData(data, _samplingFactor);

            // 創建數據系列
            var dataSeries = new XyDataSeries<DateTime, double> { SeriesName = tag.TagName };

            // 添加數據點
            foreach (var point in sampledData)
            {
                dataSeries.Append(point.DateTime, point.Value);
            }

            // 存儲數據系列
            _dataSeries[tag.Index] = dataSeries;

            // 獲取顏色索引
            int colorIndex = _renderableSeries.Count % AppConfig.ChartColors.Count;
            var seriesColor = AppConfig.ChartColors[colorIndex];

            // 創建 FastLineRenderableSeries
            var lineSeries = new FastLineRenderableSeries
            {
                DataSeries = dataSeries,
                Stroke = seriesColor.ToSciChartColor(), // 這裡實際上只是返回原始的seriesColor
                StrokeThickness = 2,
                AntiAliasing = true
            };

            // 設置點標記
            var pointMarker = new EllipsePointMarker
            {
                Width = 5,
                Height = 5,
                Fill = seriesColor.ToSciChartColor(),
                Stroke = seriesColor.ToSciChartColor()
            };
            lineSeries.PointMarker = pointMarker;

            // 添加到渲染系列集合
            _renderableSeries.Add(lineSeries);

            // 添加到SciChart
            _sciChartSurface.RenderableSeries.Add(lineSeries);
        }

        /// <summary>
        /// 對數據進行採樣以減少點數
        /// </summary>
        private List<TrendDataPoint> SampleData(List<TrendDataPoint> data, int factor)
        {
            if (factor <= 1 || data.Count <= 100)
            {
                return data; // 如果係數小於等於1或資料點少於100個，則不採樣
            }

            var result = new List<TrendDataPoint>();

            // 始終包含第一個和最後一個點
            if (data.Count > 0)
                result.Add(data[0]);

            // 添加採樣點
            for (int i = 1; i < data.Count - 1; i += factor)
            {
                result.Add(data[i]);
            }

            // 添加最後一個點
            if (data.Count > 1)
                result.Add(data[data.Count - 1]);

            return result;
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
                    // 嘗試解析時間字串
                    if (TimeSpan.TryParse(StartTimeText, out TimeSpan startTime))
                    {
                        StartTime = StartDate.Date.Add(startTime);
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
                    // 嘗試解析時間字串
                    if (TimeSpan.TryParse(EndTimeText, out TimeSpan endTime))
                    {
                        EndTime = EndDate.Date.Add(endTime);
                    }
                }
                catch (Exception)
                {
                    // 忽略格式錯誤，使用當前值
                }
            }
        }

        /// <summary>
        /// 顯示載入覆蓋層
        /// </summary>
        private void ShowLoadingOverlay(string message)
        {
            loadingTextBlock.Text = message;
            loadingOverlay.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 隱藏載入覆蓋層
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
    }
}
