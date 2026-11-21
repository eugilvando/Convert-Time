namespace FileConverter
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Win32;

    internal static class ConvertTheme
    {
        private const string Key = @"HKEY_CURRENT_USER\Software\ConvertTime";
        public static int Mode { get; private set; }
        public static void Start()
        {
            Mode = RegistryValue("Theme", 0);
            if (Mode < 0 || Mode > 2) Mode = 0;
            Apply();
            SystemEvents.UserPreferenceChanged += PreferenceChanged;
        }

        private static int RegistryValue(string name, int fallback) =>
            Microsoft.Win32.Registry.GetValue(Key, name, fallback) is int stored ? stored : fallback;

        private static void PreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            var app = System.Windows.Application.Current;
            if (Mode == 0 && app != null && !app.Dispatcher.HasShutdownStarted) app.Dispatcher.BeginInvoke((Action)Apply);
        }
        public static void SetMode(int mode)
        {
            if (mode < 0 || mode > 2 || mode == Mode) return;
            Microsoft.Win32.Registry.SetValue(Key, "Theme", mode);
            Mode = mode;
            Apply();
        }

        private static void Apply()
        {
            bool dark = Mode == 2 || (Mode == 0 && Equals(Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1), 0));
            var resources = System.Windows.Application.Current.Resources;
            string[,] colors = {
                { "Window_BackgroundColor", "#F6F6F8", "#191A1E" }, { "BackgroundColor", "#FFFFFF", "#25262B" },
                { "DarkBackgroundColor", "#F1F1F3", "#303137" }, { "Control_BorderColor", "#C5C6CC", "#55565F" },
                { "Button_BackgroundColor", "#FFFFFF", "#303137" }, { "Button_MouseOver_BackgroundColor", "#F7E5E2", "#443637" },
                { "TileButton_MouseOver_BackgroundColor", "#F7E5E2", "#443637" }, { "TileButton_MouseOver_BorderColor", "#B85145", "#FF897B" },
                { "DarkForegroundColor", "#202126", "#F2F2F5" }, { "LightForegroundColor", "#494A54", "#CFD0D7" },
                { "LighterForegroundColor", "#62636A", "#B5B6BF" }, { "AccentForegroundColor", "#A63630", "#FF958B" },
                { "MainColor", "#AE4037", "#FF9185" }, { "MainLighterColor", "#FBDAD5", "#553936" },
                { "MainLightColor", "#DB6054", "#FFAB9E" }, { "MainDarkColor", "#892F28", "#FF9B90" }, { "MainDarkerColor", "#69231F", "#FFB4A9" },
                { "AccentBackgroundColor", "#FFE3DF", "#493231" }, { "PendingBackgroundColor", "#FFF1CC", "#44391F" },
                { "SuccessBackgroundColor", "#DCF3E8", "#233C34" }, { "ErrorBackgroundColor", "#FFE1E1", "#492E32" },
                { "PendingForegroundColor", "#7D5700", "#FFCF69" }, { "SuccessForegroundColor", "#206544", "#92E0BB" },
                { "ErrorForegroundColor", "#A02135", "#FFACB6" }
            };
            for (int i = 0; i < colors.GetLength(0); i++)
                resources[colors[i, 0]] = (Color)ColorConverter.ConvertFromString(colors[i, dark ? 2 : 1]);
            foreach (var key in new object[] { SystemColors.WindowBrushKey, SystemColors.ControlBrushKey, SystemColors.MenuBrushKey })
                Set(resources, key, dark ? "#25262B" : "#FFFFFF");
            foreach (var key in new object[] { SystemColors.WindowTextBrushKey, SystemColors.ControlTextBrushKey, SystemColors.MenuTextBrushKey })
                Set(resources, key, dark ? "#F2F2F5" : "#202126");
            Set(resources, SystemColors.HighlightBrushKey, dark ? "#653F3B" : "#FBDAD5");
            Set(resources, SystemColors.HighlightTextBrushKey, dark ? "#FFFFFF" : "#202126");
            Set(resources, SystemColors.GrayTextBrushKey, dark ? "#AAAAAE" : "#62636A");
        }
        private static void Set(ResourceDictionary resources, object key, string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            resources[key] = brush;
        }
    }
}