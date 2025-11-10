using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using KG_Zapret.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KG_Zapret.Views {
    public partial class MainWindow : Window {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow(MainWindowViewModel viewModel) {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            SetWindowIcon();

            LoadThemes();
        }

        private void SetWindowIcon() {
            try {
                var iconUri = new Uri("pack://application:,,,/images/icon.ico", UriKind.Absolute);
                Icon = BitmapFrame.Create(iconUri);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Failed to set window icon: {ex.Message}");
            }
        }

        private void LoadThemes() {
            var themeService = App.Services.GetRequiredService<Services.IThemeService>();
            var themes = themeService.GetAvailableThemes();
            
            ThemeComboBox.Items.Clear();
            foreach (var theme in themes) {
                ThemeComboBox.Items.Add(theme);
            }

            var selectedTheme = themeService.GetSelectedTheme();
            if (!string.IsNullOrEmpty(selectedTheme)) {
                ThemeComboBox.SelectedItem = selectedTheme;
            }
            else if (ThemeComboBox.Items.Count > 0) {
                ThemeComboBox.SelectedIndex = 0;
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ThemeComboBox.SelectedItem is string themeName) {
                var themeService = App.Services.GetRequiredService<Services.IThemeService>();
                themeService.ApplyTheme(themeName);
            }
        }
    }
}

