using System.Windows.Media;

namespace TrendChartApp.Helpers
{
    /// <summary>
    /// SciChart相關的擴展方法
    /// </summary>
    public static class SciChartExtensions
    {
        /// <summary>
        /// 將WPF顏色轉換為SciChart可用顏色
        /// </summary>
        public static Color ToSciChartColor(this Color wpfColor)
        {
            // 在SciChart 8.7中，可以直接使用WPF的Color對象
            // 這個方法只是為了保持代碼的一致性
            return wpfColor;
        }

        /// <summary>
        /// 將SolidColorBrush轉換為SciChart可用顏色
        /// </summary>
        public static Color ToSciChartColor(this SolidColorBrush brush)
        {
            // 在SciChart 8.7中，可以直接使用WPF的Color對象
            return brush.Color;
        }
    }
}