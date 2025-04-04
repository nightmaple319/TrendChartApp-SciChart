using MaterialDesignThemes.Wpf;
using System.Windows;
using MaterialDesignColors;
using System.Windows.Media;
using SciChart.Charting.Visuals;

namespace TrendChartApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 設置SciChart授權碼 - 更新至v8版本的授權方法
            // 注意：SciChart v8的授權可能需要使用新的API
            string key = "/uhv+ls+c5yjshio9YKpfzFsMGdlElTmQAcHDAn4/0cUqFqHatToI+Xdzu1JP0KBMzwRGejuU+4D16XIdqgQ52hA5hr4O9wM+hl7ptPphqNxLergd+9794B5kAZzGtAaCJoRTlbriFBOfJ/LBaqm8xsScXmruFn257SVWvVwPrBoX9+9oEH1LmqmxMFdV5JgyFWFReqe8C1dMRJLl1DKoQfLzAAweIs0BSKIooF01Ov9Fh0Ml9MtVfukQnAXo2wFFXuNlwHBYBpA53/Fp+d/F7l7p76yoK+Nb930kScFwpInane/yM8juILRNbTuf9Tpslj7YuSSSpKWrRUXVKD9Dpwp2SsPXeHM96pFVb3xKWF66m/SdOO1scXZB7rbIyCUyho81jWGRLlbogoP0oqYir+O5NPgP9PT1T3GTmBQCAB2Ut4vbPtZauXG0sS9kR4uyCDFkY6Nfn/ktfe+kZMANvLLHKv09jRTa8G34EBwbL/VQ//WG+uNdB/k7TDCE1AJpJsUpQ75jdyBlqhb4Bvo/TLjaS2jn9SoG5ezoj1NGLI62PnK53CCXhhaiyvAbWAdn1zEriKD0g==";
            SciChart.Charting.Visuals.SciChartSurface.SetRuntimeLicenseKey(key);

            // 配置MaterialDesign主題顏色
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            // 設置為深色主題
            theme.SetBaseTheme(Theme.Dark);

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
        }
    }
}