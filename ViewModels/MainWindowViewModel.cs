using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TrendChartApp.Models;
using TrendChartApp.Helpers;
using SciChart.Data.Model;

namespace TrendChartApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Fields
        private readonly DatabaseHelper _dbHelper;
        private ObservableCollection<TagInfo> _selectedTags;
        private DateTime _startDate;
        private string _startTimeText = "00:00:00";
        private DateTime _endDate;
        private string _endTimeText = "23:59:59";
        private double _yAxisMin;
        private double _yAxisMax;
        private DateRange _visibleRange;
        private DoubleRange _yAxisRange;
        private bool _isUiEnabled = true;
        private bool _isLoading;
        #endregion

        #region Properties
        public ObservableCollection<TagInfo> SelectedTags
        {
            get => _selectedTags;
            set => SetProperty(ref _selectedTags, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                    UpdateStartTime();
            }
        }

        public string StartTimeText
        {
            get => _startTimeText;
            set
            {
                if (SetProperty(ref _startTimeText, value))
                    UpdateStartTime();
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                    UpdateEndTime();
            }
        }

        public string EndTimeText
        {
            get => _endTimeText;
            set
            {
                if (SetProperty(ref _endTimeText, value))
                    UpdateEndTime();
            }
        }

        public double YAxisMin
        {
            get => _yAxisMin;
            set
            {
                if (SetProperty(ref _yAxisMin, value))
                    UpdateYAxisRange();
            }
        }

        public double YAxisMax
        {
            get => _yAxisMax;
            set
            {
                if (SetProperty(ref _yAxisMax, value))
                    UpdateYAxisRange();
            }
        }

        public DateRange VisibleRange
        {
            get => _visibleRange;
            set => SetProperty(ref _visibleRange, value);
        }

        public DoubleRange YAxisRange
        {
            get => _yAxisRange;
            set => SetProperty(ref _yAxisRange, value);
        }

        public bool IsUiEnabled
        {
            get => _isUiEnabled;
            set => SetProperty(ref _isUiEnabled, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        #endregion

        #region Commands
        public ICommand UpdateChartCommand { get; set; }
        public ICommand ResetZoomCommand { get; set; }
        public ICommand OpenTagSelectionCommand { get; set; }
        public ICommand ExportChartCommand { get; set; }
        public ICommand ExportDataCommand { get; set; }
        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            _dbHelper = new DatabaseHelper(AppConfig.ConnectionString);
            _selectedTags = new ObservableCollection<TagInfo>();

            InitializeCommands();
            InitializeTimeRange();
            InitializeYAxisRange();
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            UpdateChartCommand = new RelayCommand(async () => await UpdateChartAsync(), () => !IsLoading);
            ResetZoomCommand = new RelayCommand(ResetZoom, () => !IsLoading);
            OpenTagSelectionCommand = new RelayCommand(OpenTagSelection, () => !IsLoading);
            ExportChartCommand = new RelayCommand(ExportChart, () => !IsLoading && SelectedTags.Count > 0);
            ExportDataCommand = new RelayCommand(ExportData, () => !IsLoading && SelectedTags.Count > 0);
        }

        private void InitializeTimeRange()
        {
            DateTime currentTime = DateTime.Now;
            DateTime startTime = currentTime.AddHours(-AppConfig.DefaultChartTimeRangeHours);

            EndDate = currentTime.Date;
            EndTimeText = currentTime.ToString("HH:mm:ss");
            StartDate = startTime.Date;
            StartTimeText = startTime.ToString("HH:mm:ss");

            UpdateVisibleRange();
        }

        private void InitializeYAxisRange()
        {
            YAxisMin = 0;
            YAxisMax = 100;
            UpdateYAxisRange();
        }

        private void UpdateStartTime()
        {
            if (StartDate != DateTime.MinValue && !string.IsNullOrEmpty(StartTimeText))
            {
                if (TimeSpan.TryParse(StartTimeText, out TimeSpan startTime))
                {
                    var newStartTime = StartDate.Date.Add(startTime);
                    UpdateVisibleRange();
                }
            }
        }

        private void UpdateEndTime()
        {
            if (EndDate != DateTime.MinValue && !string.IsNullOrEmpty(EndTimeText))
            {
                if (TimeSpan.TryParse(EndTimeText, out TimeSpan endTime))
                {
                    var newEndTime = EndDate.Date.Add(endTime);
                    UpdateVisibleRange();
                }
            }
        }

        private void UpdateVisibleRange()
        {
            var startDateTime = StartDate.Date.Add(TimeSpan.TryParse(StartTimeText, out var st) ? st : TimeSpan.Zero);
            var endDateTime = EndDate.Date.Add(TimeSpan.TryParse(EndTimeText, out var et) ? et : TimeSpan.Zero);
            VisibleRange = new DateRange(startDateTime, endDateTime);
        }

        private void UpdateYAxisRange()
        {
            YAxisRange = new DoubleRange(YAxisMin, YAxisMax);
        }

        private async Task UpdateChartAsync()
        {
            if (SelectedTags.Count == 0) return;

            IsLoading = true;
            IsUiEnabled = false;

            try
            {
                // 觸發圖表更新事件
                ChartUpdateRequested?.Invoke();
            }
            finally
            {
                IsLoading = false;
                IsUiEnabled = true;
            }
        }

        private void ResetZoom()
        {
            UpdateVisibleRange();
        }

        private void OpenTagSelection()
        {
            // 觸發標籤選擇事件
            TagSelectionRequested?.Invoke();
        }

        private void ExportChart()
        {
            // 觸發圖表匯出事件
            ChartExportRequested?.Invoke();
        }

        private void ExportData()
        {
            // 觸發資料匯出事件
            DataExportRequested?.Invoke();
        }
        #endregion

        #region Events
        public event Action ChartUpdateRequested;
        public event Action TagSelectionRequested;
        public event Action ChartExportRequested;
        public event Action DataExportRequested;
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    // 簡單的RelayCommand實現
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();
    }
}