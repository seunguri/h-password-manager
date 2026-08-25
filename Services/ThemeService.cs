using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace PasswordProtector.Services
{
    public static class ThemeService
    {
        public const string Dark = "Dark";
        public const string Light = "Light";
        public const string Hanwha = "Hanwha";

        private static readonly HashSet<string> ThemeNames = new(StringComparer.Ordinal)
        {
            Dark,
            Light,
            Hanwha
        };

        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PasswordProtector");

        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "theme.txt");

        public static string CurrentTheme { get; private set; } = Hanwha;

        public static void LoadSavedTheme()
        {
            var themeName = Hanwha;

            try
            {
                if (File.Exists(SettingsPath))
                    themeName = File.ReadAllText(SettingsPath).Trim();
            }
            catch
            {
                // Keep the default when the saved preference cannot be read.
            }

            Apply(themeName, save: false);
        }

        public static void Apply(string? themeName, bool save = true)
        {
            var selectedTheme = ThemeNames.Contains(themeName ?? string.Empty)
                ? themeName!
                : Hanwha;

            var palette = new ResourceDictionary
            {
                Source = new Uri($"/PasswordProtector;component/Themes/{selectedTheme}.xaml", UriKind.Relative)
            };

            var application = Application.Current;
            if (application is null)
                return;

            foreach (DictionaryEntry entry in palette)
            {
                if (entry.Key is null)
                    continue;

                if (entry.Value is SolidColorBrush sourceBrush)
                {
                    if (application.Resources[entry.Key] is SolidColorBrush targetBrush && !targetBrush.IsFrozen)
                    {
                        targetBrush.Color = sourceBrush.Color;
                    }
                    else
                    {
                        application.Resources[entry.Key] = sourceBrush.Clone();
                    }
                }
                else
                {
                    application.Resources[entry.Key] = entry.Value;
                }
            }

            CurrentTheme = selectedTheme;

            if (save)
                Save(selectedTheme);
        }

        public static SolidColorBrush GetBrush(string resourceKey) =>
            Application.Current?.TryFindResource(resourceKey) as SolidColorBrush ?? Brushes.Transparent;

        private static void Save(string themeName)
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(SettingsPath, themeName);
            }
            catch
            {
                // The active theme remains usable if its preference cannot be saved.
            }
        }
    }
}
