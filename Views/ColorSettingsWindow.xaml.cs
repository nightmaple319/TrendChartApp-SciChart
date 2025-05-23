using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrendChartApp.Models;
using TrendChartApp.Services;

namespace TrendChartApp.Views
{
    public partial class ColorSettingsWindow : Window
    {
        public ObservableCollection<TagColorSetting> TagColorSettings { get; set; }
        private readonly ConfigurationService _configService;
        public event EventHandler<ColorChangedEventArgs> ColorChanged;

        public ColorSettingsWindow(IEnumerable<TagInfo> selectedTags, ConfigurationService configService)
        {
            InitializeComponent();
            _configService = configService;

            InitializeTagColorSettings(selectedTags);
            tagColorItemsControl.ItemsSource = TagColorSettings;

            DataContext = this;
        }

        private void InitializeTagColorSettings(IEnumerable<TagInfo> selectedTags)
        {
            TagColorSettings = new ObservableCollection<TagColorSetting>();
            var defaultColors = _configService.Settings.Chart.DefaultColors;

            int colorIndex = 0;
            foreach (var tag in selectedTags)
            {
                var colorString = defaultColors[colorIndex % defaultColors.Count];
                var color = (Color)ColorConverter.ConvertFromString(colorString);

                TagColorSettings.Add(new TagColorSetting
                {
                    TagIndex = tag.Index,
                    TagNo = tag.TagNo,
                    TagName = tag.TagName,
                    CurrentColor = color,
                    OriginalColor = color
                });

                colorIndex++;
            }
        }

        private void CustomColorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var tagColorSetting = button?.Tag as TagColorSetting;

                if (tagColorSetting != null)
                {
                    var colorDialog = new System.Windows.Forms.ColorDialog
                    {
                        Color = System.Drawing.Color.FromArgb(
                            tagColorSetting.CurrentColor.A,
                            tagColorSetting.CurrentColor.R,
                            tagColorSetting.CurrentColor.G,
                            tagColorSetting.CurrentColor.B),
                        FullOpen = true
                    };

                    if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        var selectedColor = Color.FromArgb(
                            colorDialog.Color.A,
                            colorDialog.Color.R,
                            colorDialog.Color.G,
                            colorDialog.Color.B);

                        tagColorSetting.CurrentColor = selectedColor;

                        LoggingService.Instance.LogInfo($"標籤 {tagColorSetting.TagName} 顏色已更改為自訂顏色", "ColorSettingsWindow");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("開啟顏色選擇器失敗", ex, "ColorSettingsWindow");
                MessageBox.Show($"開啟顏色選擇器時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetDefaultColors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var defaultColors = _configService.Settings.Chart.DefaultColors;

                for (int i = 0; i < TagColorSettings.Count; i++)
                {
                    var colorString = defaultColors[i % defaultColors.Count];
                    var color = (Color)ColorConverter.ConvertFromString(colorString);
                    TagColorSettings[i].CurrentColor = color;
                }

                LoggingService.Instance.LogInfo("已重置所有標籤顏色為預設值", "ColorSettingsWindow");
                MessageBox.Show("已重置所有標籤顏色為預設值。", "重置完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("重置預設顏色失敗", ex, "ColorSettingsWindow");
                MessageBox.Show($"重置顏色時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RandomColors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var random = new Random();
                var availableColors = GetAvailableColors();

                foreach (var setting in TagColorSettings)
                {
                    var randomColorItem = availableColors[random.Next(availableColors.Count)];
                    setting.CurrentColor = randomColorItem.Color;
                }

                LoggingService.Instance.LogInfo("已為所有標籤設定隨機顏色", "ColorSettingsWindow");
                MessageBox.Show("已為所有標籤設定隨機顏色。", "隨機顏色", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("設定隨機顏色失敗", ex, "ColorSettingsWindow");
                MessageBox.Show($"設定隨機顏色時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyColorChanges();
                MessageBox.Show("顏色設定已套用到圖表。", "套用成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("套用顏色設定失敗", ex, "ColorSettingsWindow");
                MessageBox.Show($"套用顏色設定時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyColorChanges();
                SaveColorSettings();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("確認顏色設定失敗", ex, "ColorSettingsWindow");
                MessageBox.Show($"儲存顏色設定時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ApplyColorChanges()
        {
            var colorChanges = TagColorSettings.ToDictionary(
                setting => setting.TagIndex,
                setting => setting.CurrentColor
            );

            ColorChanged?.Invoke(this, new ColorChangedEventArgs(colorChanges));
            LoggingService.Instance.LogInfo($"已套用 {colorChanges.Count} 個標籤的顏色變更", "ColorSettingsWindow");
        }

        private void SaveColorSettings()
        {
            // 可以選擇性地將顏色設定儲存到配置檔案中
            // 這裡只記錄操作
            LoggingService.Instance.LogInfo("顏色設定已儲存", "ColorSettingsWindow");
        }

        private List<ColorItem> GetAvailableColors()
        {
            return new List<ColorItem>
            {
                new ColorItem("紅色", Colors.Red),
                new ColorItem("藍色", Colors.Blue),
                new ColorItem("綠色", Colors.Green),
                new ColorItem("橙色", Colors.Orange),
                new ColorItem("紫色", Colors.Purple),
                new ColorItem("青色", Colors.Teal),
                new ColorItem("棕色", Colors.Brown),
                new ColorItem("洋紅", Colors.Magenta),
                new ColorItem("黃色", Colors.Yellow),
                new ColorItem("粉紅", Colors.Pink),
                new ColorItem("淺藍", Colors.LightBlue),
                new ColorItem("淺綠", Colors.LightGreen),
                new ColorItem("深紅", Colors.DarkRed),
                new ColorItem("深藍", Colors.DarkBlue),
                new ColorItem("深綠", Colors.DarkGreen),
                new ColorItem("金色", Colors.Gold),
                new ColorItem("銀色", Colors.Silver),
                new ColorItem("深紫", Colors.DarkViolet)
            };
        }
    }

    public class TagColorSetting : INotifyPropertyChanged
    {
        public int TagIndex { get; set; }
        public string TagNo { get; set; }
        public string TagName { get; set; }
        public Color OriginalColor { get; set; }

        private Color _currentColor;
        public Color CurrentColor
        {
            get => _currentColor;
            set
            {
                _currentColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ColorBrush));
            }
        }

        public SolidColorBrush ColorBrush => new SolidColorBrush(CurrentColor);

        private ColorItem _selectedColor;
        public ColorItem SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    if (value != null)
                    {
                        CurrentColor = value.Color;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public List<ColorItem> AvailableColors { get; set; } = new List<ColorItem>
        {
            new ColorItem("紅色", Colors.Red),
            new ColorItem("藍色", Colors.Blue),
            new ColorItem("綠色", Colors.Green),
            new ColorItem("橙色", Colors.Orange),
            new ColorItem("紫色", Colors.Purple),
            new ColorItem("青色", Colors.Teal),
            new ColorItem("棕色", Colors.Brown),
            new ColorItem("洋紅", Colors.Magenta),
            new ColorItem("黃色", Colors.Yellow),
            new ColorItem("粉紅", Colors.Pink)
        };

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ColorItem
    {
        public string Name { get; set; }
        public Color Color { get; set; }
        public SolidColorBrush Brush => new SolidColorBrush(Color);

        public ColorItem(string name, Color color)
        {
            Name = name;
            Color = color;
        }
    }

    public class ColorChangedEventArgs : EventArgs
    {
        public Dictionary<int, Color> ColorChanges { get; }

        public ColorChangedEventArgs(Dictionary<int, Color> colorChanges)
        {
            ColorChanges = colorChanges;
        }
    }
}