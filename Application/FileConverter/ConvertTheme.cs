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
            Set(resources, "Window_BackgroundBrush", dark ? "#202124" : "#F8F8F9");
            Set(resources, "ReadyForegroundBrush", dark ? "#F2F2F2" : "#242428");
            Set(resources, "MainBrush", "#FF7466");
            foreach (var key in new object[] {SystemColors.WindowBrushKey, SystemColors.ControlBrushKey, "Button_BackgroundBrush", "TextBox_BackgroundBrush", "TabControl_BackgroundBrush", "TreeView_BackgroundBrush"})
                Set(resources, key, dark ? "#303136" : "#FFFFFF");
            foreach (var key in new object[] {SystemColors.WindowTextBrushKey, SystemColors.ControlTextBrushKey, "Markdown_ForegroundBrush"})
                Set(resources, key, dark ? "#F4F4F4" : "#252529");
        }
        private static void Set(ResourceDictionary resources, object key, string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            resources[key] = brush;
        }
    }
}