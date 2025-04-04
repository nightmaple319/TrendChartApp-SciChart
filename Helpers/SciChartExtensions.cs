using System.Windows.Media;

namespace TrendChartApp.Helpers
{
    /// <summary>
    /// SciChart相關的擴展方法
    /// </summary>
    public static class SciChartExtensions
    {
        /// <summary>
        /// 將WPF顏色轉換為SciChart顏色
        /// </summary>
        public static uint ToSciChartColor(this System.Windows.Media.Color wpfColor)
        {
            // 使用 SciChart 8.7 的顏色表示方式（ARGB uint）
            return (uint)((wpfColor.A << 24) | (wpfColor.R << 16) | (wpfColor.G << 8) | wpfColor.B);
        }

        /// <summary>
        /// 將SolidColorBrush轉換為SciChart顏色
        /// </summary>
        public static uint ToSciChartColor(this SolidColorBrush brush)
        {
            var wpfColor = brush.Color;
            // 使用 SciChart 8.7 的顏色表示方式（ARGB uint）
            return (uint)((wpfColor.A << 24) | (wpfColor.R << 16) | (wpfColor.G << 8) | wpfColor.B);
        }
    }
}