using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace KG_Zapret.Services {
    public class SystemTrayService : ISystemTrayService {
        private NotifyIcon? _notifyIcon;
        private System.Windows.Window? _mainWindow;
        private bool _shownHint = false;
        private bool _closingCompletely = false;
        private bool _allowClose = false;

        public bool IsInitialized => _notifyIcon != null;

        public void Initialize(System.Windows.Window mainWindow) {
            _mainWindow = mainWindow;

            Icon? appIcon = null;
            try {
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "icon.ico");
                if (System.IO.File.Exists(iconPath)) {
                    appIcon = new Icon(iconPath);
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Failed to load tray icon: {ex.Message}");
            }

            _notifyIcon = new NotifyIcon {
                Icon = appIcon ?? SystemIcons.Application,
                Text = "KG-Zapret",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            
            var showItem = new ToolStripMenuItem("Показать");
            showItem.Click += (s, e) => ShowWindow();
            contextMenu.Items.Add(showItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            var exitItem = new ToolStripMenuItem("Выход");
            exitItem.Click += (s, e) => ExitOnly();
            contextMenu.Items.Add(exitItem);
            
            var exitStopItem = new ToolStripMenuItem("Выход и остановить DPI");
            exitStopItem.Click += (s, e) => ExitAndStop();
            contextMenu.Items.Add(exitStopItem);
            
            _notifyIcon.ContextMenuStrip = contextMenu;

            _notifyIcon.DoubleClick += (s, e) => {
                if (_mainWindow != null) {
                    if (_mainWindow.IsVisible) {
                        _mainWindow.Hide();
                    }
                    else {
                        ShowWindow();
                    }
                }
            };

            if (_mainWindow != null) {
                _mainWindow.Closing += MainWindow_Closing;
                _mainWindow.StateChanged += MainWindow_StateChanged;
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
            if (_closingCompletely || _allowClose) {
                return;
            }

            e.Cancel = true;
            if (_mainWindow != null) {
                _mainWindow.Hide();
                
                if (!_shownHint) {
                    ShowNotification(
                        "KG-Zapret продолжает работать",
                        "Свернуто в трей. Кликните по иконке, чтобы открыть окно."
                    );
                    _shownHint = true;
                }
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e) {
            if (_mainWindow != null && _mainWindow.WindowState == WindowState.Minimized) {
                _mainWindow.Hide();
                
                if (!_shownHint) {
                    ShowNotification(
                        "KG-Zapret продолжает работать",
                        "Свернуто в трей. Кликните по иконке, чтобы открыть окно."
                    );
                    _shownHint = true;
                }
            }
        }

        public void ShowWindow() {
            if (_mainWindow != null) {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            }
        }

        private void ExitOnly() {
            _allowClose = true;
            _closingCompletely = true;
            Hide();
            if (_mainWindow != null) {
                _mainWindow.Close();
            }
            System.Windows.Application.Current.Shutdown();
        }

        private void ExitAndStop() {
            // TODO: остановить DPI перед выходом
            ExitOnly();
        }

        public void Show() {
            _notifyIcon?.ShowBalloonTip(5000, "KG-Zapret", "Приложение работает в фоновом режиме", ToolTipIcon.Info);
        }

        public void Hide() {
            _notifyIcon?.Dispose();
            _notifyIcon = null;
        }

        public void ShowNotification(string title, string message) {
            _notifyIcon?.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
        }
    }
}

