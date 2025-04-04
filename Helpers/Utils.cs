using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrendChartApp.Models;

namespace TrendChartApp.Helpers
{
    public static class Utils
    {
        /// <summary>
        /// Saves a FrameworkElement as an image file
        /// </summary>
        public static void SaveElementAsImage(FrameworkElement element, string filePath)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            // Create a render bitmap
            var renderBitmap = new RenderTargetBitmap(
                (int)element.ActualWidth,
                (int)element.ActualHeight,
                96, 96, PixelFormats.Pbgra32);

            renderBitmap.Render(element);

            // Create appropriate encoder based on file extension
            BitmapEncoder encoder;
            string extension = Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                case ".png":
                default:
                    encoder = new PngBitmapEncoder();
                    break;
            }

            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            // Save to file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        /// <summary>
        /// Formats a tooltip for a data point
        /// </summary>
        public static string FormatTooltip(TagInfo tag, DateTime dateTime, double value)
        {
            return $"{dateTime:yyyy-MM-dd HH:mm:ss}\n{tag.TagName}: {value:F2}";
        }

        /// <summary>
        /// Exception class for application-specific errors
        /// </summary>
        public class TrendChartException : Exception
        {
            public TrendChartException() : base() { }

            public TrendChartException(string message) : base(message) { }

            public TrendChartException(string message, Exception innerException)
                : base(message, innerException) { }
        }
    }
}