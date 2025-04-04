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