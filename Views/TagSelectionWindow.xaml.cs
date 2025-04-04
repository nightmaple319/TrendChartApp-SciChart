using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TrendChartApp.Helpers;
using TrendChartApp.Models;

namespace TrendChartApp.Views
{
    /// <summary>
    /// Tag selection window to choose which tags to display in the chart
    /// </summary>
    public partial class TagSelectionWindow : Window
    {
        private readonly string _connectionString = AppConfig.ConnectionString;
        private List<TagInfo> _allTags = new List<TagInfo>();
        public ObservableCollection<TagInfo> SelectedTags { get; private set; }
        private readonly DatabaseHelper _dbHelper;
        private ICollectionView _filteredView;

        public TagSelectionWindow(ObservableCollection<TagInfo> currentSelectedTags)
        {
            InitializeComponent();

            // 確保視窗載入後立即應用深色主題
            this.Background = (SolidColorBrush)FindResource("BackgroundBrush");

            // 初始化資料庫助手
            _dbHelper = new DatabaseHelper(AppConfig.ConnectionString);

            // 初始化選定標籤集合
            SelectedTags = new ObservableCollection<TagInfo>(currentSelectedTags);

            // 載入所有標籤
            LoadTags();

            // 設置已選標籤列表資料來源
            selectedTagsListView.ItemsSource = SelectedTags;

            // 設置上下文
            DataContext = this;
        }

        /// <summary>
        /// 載入標籤資料並設置篩選視圖
        /// </summary>
        private void LoadTags()
        {
            try
            {
                // 獲取所有標籤
                _allTags = _dbHelper.GetAllTags();

                // 更新已選狀態
                UpdateSelectedStatus();

                // 建立集合視圖並應用篩選器
                _filteredView = CollectionViewSource.GetDefaultView(_allTags);
                _filteredView.Filter = FilterTags;

                // 將篩選視圖設置為ListView的資料來源
                tagsListView.ItemsSource = _filteredView;

                // 確保ListView立即應用深色主題
                tagsListView.Background = (SolidColorBrush)FindResource("ControlBrush");
                tagsListView.Foreground = (SolidColorBrush)FindResource("ControlForegroundBrush");
                tagsListView.BorderBrush = (SolidColorBrush)FindResource("BorderBrush");

                // 確保ListView重新整理
                tagsListView.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入標籤資料出錯: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新標籤的選中狀態
        /// </summary>
        private void UpdateSelectedStatus()
        {
            foreach (var tag in _allTags)
            {
                tag.IsSelected = SelectedTags.Any(t => t.Index == tag.Index);
            }
        }

        /// <summary>
        /// 標籤篩選函數
        /// </summary>
        private bool FilterTags(object item)
        {
            if (string.IsNullOrEmpty(filterTextBox.Text))
                return true;

            var tag = (TagInfo)item;
            var filter = filterTextBox.Text.ToLower();

            return tag.TagNo?.ToLower().Contains(filter) == true ||
                   tag.TagName?.ToLower().Contains(filter) == true ||
                   tag.Index.ToString().Contains(filter);
        }

        /// <summary>
        /// 篩選文字方塊文字變更事件處理
        /// </summary>
        private void FilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_filteredView != null)
            {
                _filteredView.Refresh();
            }
        }

        /// <summary>
        /// Handles tag selection
        /// </summary>
        private void SelectTag(object sender, RoutedEventArgs e)
        {
            var checkbox = (CheckBox)sender;
            var tag = (TagInfo)checkbox.DataContext;

            if (checkbox.IsChecked == true)
            {
                // Check if we've reached the maximum of 8 selected tags
                if (SelectedTags.Count >= 8)
                {
                    MessageBox.Show("您最多可以選擇8個標籤。", "選擇限制", MessageBoxButton.OK, MessageBoxImage.Warning);
                    checkbox.IsChecked = false;
                    tag.IsSelected = false;
                    return;
                }

                // Add to selected tags if not already there
                if (!SelectedTags.Any(t => t.Index == tag.Index))
                {
                    SelectedTags.Add(tag);
                }
            }
            else
            {
                // Remove from selected tags
                var tagToRemove = SelectedTags.FirstOrDefault(t => t.Index == tag.Index);
                if (tagToRemove != null)
                {
                    SelectedTags.Remove(tagToRemove);
                }
            }
        }

        /// <summary>
        /// Handles OK button click
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Handles Cancel button click
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}