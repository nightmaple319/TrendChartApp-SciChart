using System;

namespace TrendChartApp.Models
{
    public class TrendDataPoint
    {
        public DateTime DateTime { get; set; }    // 資料時間點
        public double Value { get; set; }         // 數值
    }
}