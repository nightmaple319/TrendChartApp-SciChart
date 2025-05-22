using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TrendChartApp.Helpers;
using TrendChartApp.Models;

namespace TrendChartApp.Services
{
    public interface IProgressService
    {
        void ShowProgress(string message, bool indeterminate = true);
        void UpdateProgress(string message, double? percentage = null);
        void HideProgress();
        Task<T> ExecuteWithProgressAsync<T>(Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> operation,
            string initialMessage, CancellationToken cancellationToken = default);
    }

    public class ProgressService : INotifyPropertyChanged, IProgressService
    {
        private readonly Dispatcher _dispatcher;
        private bool _isVisible;
        private string _message;
        private double _percentage;
        private bool _isIndeterminate;
        private CancellationTokenSource _cancellationTokenSource;

        public ProgressService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        #region Properties

        public bool IsVisible
        {
            get => _isVisible;
            private set => SetProperty(ref _isVisible, value);
        }

        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public double Percentage
        {
            get => _percentage;
            private set => SetProperty(ref _percentage, value);
        }

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            private set => SetProperty(ref _isIndeterminate, value);
        }

        public bool CanCancel => _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;

        #endregion

        #region Public Methods

        public void ShowProgress(string message, bool indeterminate = true)
        {
            _dispatcher.Invoke(() =>
            {
                Message = message;
                IsIndeterminate = indeterminate;
                Percentage = 0;
                IsVisible = true;
                _cancellationTokenSource = new CancellationTokenSource();
                OnPropertyChanged(nameof(CanCancel));
            });
        }

        public void UpdateProgress(string message, double? percentage = null)
        {
            _dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(message))
                    Message = message;

                if (percentage.HasValue)
                {
                    Percentage = Math.Max(0, Math.Min(100, percentage.Value));
                    IsIndeterminate = false;
                }
            });
        }

        public void HideProgress()
        {
            _dispatcher.Invoke(() =>
            {
                IsVisible = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                OnPropertyChanged(nameof(CanCancel));
            });
        }

        public void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
        }

        public async Task<T> ExecuteWithProgressAsync<T>(
            Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> operation,
            string initialMessage,
            CancellationToken cancellationToken = default)
        {
            var progress = new Progress<ProgressInfo>(info =>
            {
                UpdateProgress(info.Message, info.Percentage);
            });

            ShowProgress(initialMessage);

            try
            {
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, _cancellationTokenSource?.Token ?? CancellationToken.None);

                return await operation(progress, combinedCts.Token);
            }
            finally
            {
                HideProgress();
            }
        }

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

    public class ProgressInfo
    {
        public string Message { get; set; }
        public double? Percentage { get; set; }
        public object Data { get; set; }

        public ProgressInfo(string message, double? percentage = null, object data = null)
        {
            Message = message;
            Percentage = percentage;
            Data = data;
        }
    }

    /// <summary>
    /// 帶進度報告的資料庫操作包裝器
    /// </summary>
    public static class ProgressExtensions
    {
        public static async Task<List<TrendDataPoint>> GetTagDataWithProgressAsync(
            this DatabaseHelper dbHelper,
            TagInfo tag,
            DateTime startTime,
            DateTime endTime,
            IProgress<ProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressInfo($"正在載入標籤 {tag.TagName} 的資料..."));

            try
            {
                var data = await dbHelper.GetTagDataAsync(tag, startTime, endTime);

                progress?.Report(new ProgressInfo($"已載入 {data.Count} 筆資料", 100));

                return data;
            }
            catch (OperationCanceledException)
            {
                progress?.Report(new ProgressInfo("操作已取消"));
                throw;
            }
            catch (Exception ex)
            {
                progress?.Report(new ProgressInfo($"載入失敗: {ex.Message}"));
                throw;
            }
        }

        public static async Task<Dictionary<int, List<TrendDataPoint>>> GetMultipleTagDataWithProgressAsync(
            this DatabaseHelper dbHelper,
            IEnumerable<TagInfo> tags,
            DateTime startTime,
            DateTime endTime,
            IProgress<ProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            var tagList = tags.ToList();
            var totalTags = tagList.Count;
            var processedTags = 0;

            progress?.Report(new ProgressInfo($"開始載入 {totalTags} 個標籤的資料...", 0));

            var tagProgress = new Progress<string>(message =>
            {
                processedTags++;
                var percentage = (double)processedTags / totalTags * 100;
                progress?.Report(new ProgressInfo(message, percentage));
            });

            return await dbHelper.GetMultipleTagDataAsync(tagList, startTime, endTime, tagProgress, cancellationToken);
        }
    }
}