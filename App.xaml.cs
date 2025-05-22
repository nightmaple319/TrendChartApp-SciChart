using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using MaterialDesignColors;
using System.Windows.Media;
using SciChart.Charting.Visuals;
using TrendChartApp.Services;

namespace TrendChartApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ConfigurationService _configService;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                // 初始化全域異常處理
                GlobalExceptionHandler.Initialize();

                // 初始化配置服務
                _configService = new ConfigurationService();

                // 記錄應用程式啟動
                LoggingService.Instance.LogInfo("TrendChartApp 啟動", "App");

                // 設置SciChart授權碼 - 更新至v8版本的授權方法
                string key = "XJXa4eSai1Cde/dOSh+RdrWoX6q4cbemeViJNWzAR+nthJH7QYw5t5EZKWVCfH8AqhHCaVGVbW5p1ke3F8tVWzm/c4fiYAm5rVlWy5R5obLer2a9U+bli9XAcN5OLdFUb9ZGQWxqpENAR94U+d6KR992+POYRMLfB1rwegwrjo+C6PuT7aOBy2IqXoxg1wZIOsC585Bov7uIRKtU/v25465K2P6nPsFCR7dY+tlu9UqqTTObyUjM8fUdB7DtGM88sm/8ADkzDG5jCf7HH1Q1kMRUkP0VSQF7sKIDdkdQ5kl/X9mZgZyYXm6SJfBNPPhkMmSSQ0lKnUjZOS/OhNZY9/hP3GEmzq9bufFwbXru8Qgzy7lYwIPjoF/FuGAMxuo5tUEQtWWaI7OSsxAIzMnUCgKOV8+cVX7maHeMoiCzPOFTYfDSmT9muMKE8rvj/ib9EAMCKIFtZxBRNKmEUaYfZbAcG6WEyoaN4kxAJDbFko0CHR/6bzuKvJpTKLIcZg==";
                SciChart.Charting.Visuals.SciChartSurface.SetRuntimeLicenseKey(key);

                // 配置MaterialDesign主題顏色
                ConfigureMaterialDesignTheme();

                LoggingService.Instance.LogInfo("應用程式初始化完成", "App");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogCritical("應用程式啟動失敗", ex, "App");
                MessageBox.Show($"應用程式啟動失敗: {ex.Message}", "啟動錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private void ConfigureMaterialDesignTheme()
        {
            try
            {
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();

                // 根據配置設定主題
                var themeType = _configService.Settings.UI.Theme;
                if (themeType.Equals("Dark", StringComparison.OrdinalIgnoreCase))
                {
                    theme.SetBaseTheme(Theme.Dark);
                }
                else
                {
                    theme.SetBaseTheme(Theme.Light);
                }

                // 設置主色和強調色，與應用程式的深色主題配色匹配
                theme.PrimaryDark = new ColorPair(Color.FromRgb(0, 122, 204), Color.FromRgb(230, 230, 230)); // #007ACC
                theme.PrimaryMid = new ColorPair(Color.FromRgb(0, 122, 204), Color.FromRgb(230, 230, 230));
                theme.PrimaryLight = new ColorPair(Color.FromRgb(0, 122, 204), Color.FromRgb(230, 230, 230));

                // 設置強調色
                theme.SecondaryDark = new ColorPair(Color.FromRgb(62, 62, 66), Color.FromRgb(230, 230, 230)); // #3E3E42
                theme.SecondaryMid = new ColorPair(Color.FromRgb(62, 62, 66), Color.FromRgb(230, 230, 230));
                theme.SecondaryLight = new ColorPair(Color.FromRgb(62, 62, 66), Color.FromRgb(230, 230, 230));

                // 設置視窗和選單元素顏色
                theme.Paper = Color.FromRgb(45, 45, 48); // #2D2D30 - 視窗背景色
                theme.CardBackground = Color.FromRgb(37, 37, 38); // #252526 - 面板背景色

                paletteHelper.SetTheme(theme);

                LoggingService.Instance.LogInfo($"MaterialDesign 主題已設定為: {themeType}", "App");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("配置MaterialDesign主題失敗", ex, "App");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo("應用程式正在關閉", "App");
                base.OnExit(e);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("應用程式關閉時發生錯誤", ex, "App");
            }
        }
    }
}