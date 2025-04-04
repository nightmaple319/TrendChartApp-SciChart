using System.Windows.Media;
using SciChart.Drawing.Common;

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
        public static SciChart.Drawing.Common.Color ToColor(this System.Windows.Media.Color wpfColor)
        {
            // 使用正確的 SciChart 顏色建立方式
            return SciChart.Drawing.Common.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
        }

        /// <summary>
        /// 將SolidColorBrush轉換為SciChart顏色
        /// </summary>
        public static SciChart.Drawing.Common.Color ToColor(this SolidColorBrush brush)
        {
            // 使用正確的 SciChart 顏色建立方式
            return SciChart.Drawing.Common.Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
        }
    }
}