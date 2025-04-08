using System.Collections.Generic;
using System.Windows.Media;

namespace TrendChartApp
{
    public static class AppConfig
    {
        // Database connection string
        public static string ConnectionString { get; set; } = "Server=127.0.0.1;Database=TrendDB;User Id=ViewDev;Password=ViewDev;";

        // Default chart colors
        public static List<Color> ChartColors { get; } = new List<Color>
        {
            Colors.Red,
            Colors.Blue,
            Colors.Green,
            Colors.Orange,
            Colors.Purple,
            Colors.Teal,
            Colors.Brown,
            Colors.Magenta
        };

        // Application settings
        public static int MaxSelectedTags { get; } = 8;
        public static int DefaultChartTimeRangeHours { get; } = 1;
    }
}