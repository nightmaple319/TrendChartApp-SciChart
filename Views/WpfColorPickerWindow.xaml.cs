using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TrendChartApp.Services;

namespace TrendChartApp.Views
{
    public partial class WpfColorPickerWindow : Window
    {
        public Color SelectedColor { get; private set; }
        private readonly Color _initialColor;

        // 預定義的顏色調色板
        private readonly Color[] _predefinedColors = new Color[]
        {
            // 基本顏色
            Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow,
            Colors.Orange, Colors.Purple, Colors.Cyan, Colors.Magenta,
            Colors.Pink, Colors.Lime, Colors.Teal, Colors.Indigo,
            
            // 深色調
            Colors.DarkRed, Colors.DarkGreen, Colors.DarkBlue, Colors.DarkOrange,
            Colors.DarkViolet, Colors.DarkCyan, Colors.DarkMagenta, Colors.DarkGoldenrod,
            
            // 淺色調
            Colors.LightCoral, Colors.LightGreen, Colors.LightBlue, Colors.LightYellow,
            Colors.LightPink, Colors.LightCyan, Colors.LightGray, Colors.LightSalmon,
            
            // 特殊顏色
            Colors.Gold, Colors.Silver, Colors.Brown, Colors.Maroon,
            Colors.Navy, Colors.Olive, Colors.Coral, Colors.Crimson,
            
            // 更多顏色選擇
            Color.FromRgb(255, 99, 71),   // Tomato
            Color.FromRgb(255, 140, 0),   // DarkOrange
            Color.FromRgb(255, 215, 0),   // Gold
            Color.FromRgb(154, 205, 50),  // YellowGreen
            Color.FromRgb(0, 255, 127),   // SpringGreen
            Color.FromRgb(0, 191, 255),   // DeepSkyBlue
            Color.FromRgb(138, 43, 226),  // BlueViolet
            Color.FromRgb(255, 20, 147),  // DeepPink
            
            // 灰階
            Colors.Black, Colors.DimGray, Colors.Gray, Colors.DarkGray,
            Colors.Silver, Colors.LightGray, Colors.Gainsboro, Colors.White
        };

        public WpfColorPickerWindow(Color initialColor)
        {
            InitializeComponent();
            _initialColor = initialColor;
            SelectedColor = initialColor;

            InitializeColorPalette();
            UpdateSelectedColor();
        }

        private void InitializeColorPalette()
        {
            colorWrapPanel.Children.Clear();

            foreach (var color in _predefinedColors)
            {
                var colorButton = CreateColorButton(color);
                colorWrapPanel.Children.Add(colorButton);
            }
        }

        private Border CreateColorButton(Color color)
        {
            var border = new Border
            {
                Width = 40,
                Height = 40,
                Margin = new Thickness(2),
                Background = new SolidColorBrush(color),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = $"RGB({color.R}, {color.G}, {color.B})\n#{color.R:X2}{color.G:X2}{color.B:X2}"
            };

            // 如果是當前選中的顏色，添加邊框
            if (color == _initialColor)
            {
                border.BorderBrush = Brushes.White;
                border.BorderThickness = new Thickness(3);
            }

            border.MouseLeftButtonUp += (sender, e) =>
            {
                SelectedColor = color;
                UpdateSelectedColor();
                UpdateColorButtonSelection();
            };

            border.MouseEnter += (sender, e) =>
            {
                if (border.BorderThickness.Left == 1) // 只對未選中的進行懸停效果
                {
                    border.BorderBrush = Brushes.White;
                    border.BorderThickness = new Thickness(2);
                }
            };

            border.MouseLeave += (sender, e) =>
            {
                if (border.BorderThickness.Left == 2) // 只對懸停狀態的進行恢復
                {
                    border.BorderBrush = Brushes.Gray;
                    border.BorderThickness = new Thickness(1);
                }
            };

            return border;
        }

        private void UpdateColorButtonSelection()
        {
            foreach (Border border in colorWrapPanel.Children)
            {
                var brush = border.Background as SolidColorBrush;
                if (brush != null)
                {
                    if (brush.Color == SelectedColor)
                    {
                        border.BorderBrush = Brushes.White;
                        border.BorderThickness = new Thickness(3);
                    }
                    else
                    {
                        border.BorderBrush = Brushes.Gray;
                        border.BorderThickness = new Thickness(1);
                    }
                }
            }
        }

        private void UpdateSelectedColor()
        {
            selectedColorPreview.Fill = new SolidColorBrush(SelectedColor);
            selectedColorText.Text = $"RGB({SelectedColor.R}, {SelectedColor.G}, {SelectedColor.B}) - #{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
            hexColorTextBox.Text = $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
        }

        private void HexColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = hexColorTextBox.Text.Trim();
            if (text.StartsWith("#") && text.Length == 7)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(text);
                    // 不立即更新選中顏色，等用戶點擊套用按鈕
                }
                catch
                {
                    // 忽略無效的顏色格式
                }
            }
        }

        private void ApplyHexColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = hexColorTextBox.Text.Trim();
                if (!text.StartsWith("#"))
                {
                    text = "#" + text;
                }

                if (text.Length == 7)
                {
                    var color = (Color)ColorConverter.ConvertFromString(text);
                    SelectedColor = color;
                    UpdateSelectedColor();
                    UpdateColorButtonSelection();

                    LoggingService.Instance.LogInfo($"套用自訂顏色: {text}", "WpfColorPickerWindow");
                }
                else
                {
                    MessageBox.Show("請輸入有效的顏色格式，例如: #FF0000", "無效格式",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("套用自訂顏色失敗", ex, "WpfColorPickerWindow");
                MessageBox.Show("無效的顏色格式。請使用 #RRGGBB 格式，例如: #FF0000", "錯誤",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedColor = _initialColor; // 恢復原始顏色
            DialogResult = false;
            Close();
        }
    }
}