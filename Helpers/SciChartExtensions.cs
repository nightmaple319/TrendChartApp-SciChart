using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public static Color ToColor(this System.Windows.Media.Color wpfColor)
        {
            return new Color(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
        }

        /// <summary>
        /// 將SolidColorBrush轉換為SciChart顏色
        /// </summary>
        public static Color ToColor(this SolidColorBrush brush)
        {
            return new Color(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
        }
    }
}
