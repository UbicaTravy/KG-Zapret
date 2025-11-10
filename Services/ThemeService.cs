using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using KG_Zapret.Services;

namespace KG_Zapret.Services {
    public class ThemeService : IThemeService {
        private readonly IRegistryService _registryService;
        private const string RegistryPath = @"Software\KG-Zapret";
        private const string ThemeKey = "SelectedTheme";

        private readonly Dictionary<string, ThemeInfo> _themes = new() {
            { "Темная синяя", new ThemeInfo { File = "dark_blue.xml", StatusColor = "#ffffff", ButtonColor = "0, 125, 242" } },
            { "Темная бирюзовая", new ThemeInfo { File = "dark_cyan.xml", StatusColor = "#ffffff", ButtonColor = "14, 152, 211" } },
            { "Темная янтарная", new ThemeInfo { File = "dark_amber.xml", StatusColor = "#ffffff", ButtonColor = "224, 132, 0" } },
            { "Темная розовая", new ThemeInfo { File = "dark_pink.xml", StatusColor = "#ffffff", ButtonColor = "255, 93, 174" } },
            { "Светлая синяя", new ThemeInfo { File = "light_blue.xml", StatusColor = "#000000", ButtonColor = "25, 118, 210" } },
            { "Светлая бирюзовая", new ThemeInfo { File = "light_cyan.xml", StatusColor = "#000000", ButtonColor = "0, 172, 193" } },
            { "РКН Тян", new ThemeInfo { File = "dark_blue.xml", StatusColor = "#ffffff", ButtonColor = "63, 85, 182" } },
            { "AMOLED Синяя", new ThemeInfo { File = "dark_blue.xml", StatusColor = "#ffffff", ButtonColor = "0, 150, 255", IsAmoled = true } },
            { "AMOLED Зеленая", new ThemeInfo { File = "dark_teal.xml", StatusColor = "#ffffff", ButtonColor = "0, 255, 127", IsAmoled = true } },
            { "AMOLED Фиолетовая", new ThemeInfo { File = "dark_purple.xml", StatusColor = "#ffffff", ButtonColor = "187, 134, 252", IsAmoled = true } },
            { "AMOLED Красная", new ThemeInfo { File = "dark_red.xml", StatusColor = "#ffffff", ButtonColor = "255, 82, 82", IsAmoled = true } },
            { "Полностью черная", new ThemeInfo { File = "dark_blue.xml", StatusColor = "#ffffff", ButtonColor = "32, 32, 32", IsPureBlack = true } }
        };

        public ThemeService(IRegistryService registryService) {
            _registryService = registryService;
        }

        public List<string> GetAvailableThemes() {
            return _themes.Keys.ToList();
        }

        public string? GetSelectedTheme() {
            return _registryService.GetValue(RegistryPath, ThemeKey);
        }

        public void SetSelectedTheme(string themeName) {
            if (_themes.ContainsKey(themeName)) {
                _registryService.SetValue(RegistryPath, ThemeKey, themeName);
            }
        }

        public void ApplyTheme(string themeName) {
            if (!_themes.TryGetValue(themeName, out var themeInfo)) {
                return;
            }

            try {
                var resourceDictionary = new ResourceDictionary();
                
                var themePath = $"Themes/{themeInfo.File}";
                // TODO: загрузить и применить тему из XML-файла
                
                Application.Current.Resources.MergedDictionaries.Clear();
                
                if (themeName.StartsWith("Темная") || themeName.StartsWith("AMOLED") || 
                    themeName == "РКН Тян" || themeName == "Полностью черная") {
                    resourceDictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml");
                }
                else {
                    resourceDictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml");
                }
                
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
                
                SetSelectedTheme(themeName);
            }
            catch (Exception ex) {
                MessageBox.Show($"Ошибка применения темы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class ThemeInfo {
            public string File { get; set; } = string.Empty;
            public string StatusColor { get; set; } = "#ffffff";
            public string ButtonColor { get; set; } = "0, 125, 242";
            public bool IsAmoled { get; set; }
            public bool IsPureBlack { get; set; }
        }
    }
}

