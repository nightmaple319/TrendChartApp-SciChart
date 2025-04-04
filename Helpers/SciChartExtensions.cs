using System.Windows.Media;

namespace TrendChartApp.Helpers
{
    /// <summary>
    /// SciChart相關的擴展方法
    /// </summary>
    public static class SciChartExtensions
    {
        // <summary>
        /// 將WPF顏色轉換為SciChart顏色
        /// </summary>
        public static SciChart.Core.Utility.ColorUtil.Color ToSciChartColor(this System.Windows.Media.Color wpfColor)
        {
            // 使用SciChart的ColorUtil.Color類型
            return new SciChart.Core.Utility.ColorUtil.Color(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
        }

        /// <summary>
        /// 將SolidColorBrush轉換為SciChart顏色
        /// </summary>
        public static SciChart.Core.Utility.ColorUtil.Color ToSciChartColor(this SolidColorBrush brush)
        {
            var wpfColor = brush.Color;
            // 使用SciChart的ColorUtil.Color類型
            return new SciChart.Core.Utility.ColorUtil.Color(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
        }
    }
}