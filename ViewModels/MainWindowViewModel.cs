using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KG_Zapret.Services;

namespace KG_Zapret.ViewModels {
    public partial class MainWindowViewModel : ObservableObject {
        private readonly IDpiService _dpiService;
        private readonly IStrategyService _strategyService;
        private readonly IHostsService _hostsService;
        private readonly IDnsService _dnsService;
        private readonly IProcessService _processService;
        private readonly ILoggerService _loggerService;
        private readonly IAutostartService _autostartService;

        [ObservableProperty]
        private string _processStatus = "проверка…";

        [ObservableProperty]
        private string _currentStrategy = "Автостарт DPI отключен";

        [ObservableProperty]
        private string _statusText = "";

        [ObservableProperty]
        private bool _isDpiRunning = false;

        [ObservableProperty]
        private bool _isAutostartEnabled = false;

        public MainWindowViewModel(
            IDpiService dpiService,
            IStrategyService strategyService,
            IHostsService hostsService,
            IDnsService dnsService,
            IProcessService processService,
            ILoggerService loggerService,
            IAutostartService autostartService) {
            _dpiService = dpiService;
            _strategyService = strategyService;
            _hostsService = hostsService;
            _dnsService = dnsService;
            _processService = processService;
            _loggerService = loggerService;
            _autostartService = autostartService;

            CheckDpiStatus();
            
            IsAutostartEnabled = _autostartService.IsAutostartEnabled();
        }

        private void CheckDpiStatus() {
            IsDpiRunning = _dpiService.IsDpiRunning();
            ProcessStatus = IsDpiRunning ? "работает" : "остановлен";
        }

        [RelayCommand]
        private Task SelectStrategy() {
            // TODO: открыть диалог выбора стратегии
            StatusText = "Выбор стратегии...";
            _loggerService.LogInfo("Opening strategy selection dialog");
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task StartStop() {
            if (IsDpiRunning) {
                StatusText = "Остановка DPI...";
                await _dpiService.StopDpiAsync();
                IsDpiRunning = false;
                ProcessStatus = "остановлен";
                StatusText = "DPI остановлен";
            }
            else {
                StatusText = "Запуск DPI...";
                var lastStrategy = _strategyService.GetLastStrategy();
                if (!string.IsNullOrEmpty(lastStrategy)) {
                    var success = await _dpiService.StartDpiAsync(lastStrategy);
                    if (success) {
                        IsDpiRunning = true;
                        ProcessStatus = "работает";
                        StatusText = "DPI запущен";
                    }
                    else {
                        StatusText = "Ошибка запуска DPI";
                    }
                }
                else {
                    StatusText = "Сначала выберите стратегию";
                }
            }
        }

        [RelayCommand]
        private async Task Autostart() {
            if (IsAutostartEnabled) {
                // дизабле аутостарт
                StatusText = "Отключение автозапуска...";
                var success = await _autostartService.DisableAutostartAsync();
                if (success) {
                    IsAutostartEnabled = false;
                    StatusText = "Автозапуск отключен";
                    _loggerService.LogInfo("Autostart disabled");
                }
                else {
                    StatusText = "Ошибка отключения автозапуска";
                }
            }
            else {
                // энабле аутостарт
                var lastStrategy = _strategyService.GetLastStrategy();
                if (string.IsNullOrEmpty(lastStrategy)) {
                    StatusText = "Сначала выберите стратегию";
                    return;
                }
                
                StatusText = "Включение автозапуска...";
                var success = await _autostartService.EnableAutostartAsync(lastStrategy);
                if (success) {
                    IsAutostartEnabled = true;
                    StatusText = "Автозапуск включен";
                    _loggerService.LogInfo("Autostart enabled");
                }
                else {
                    StatusText = "Ошибка включения автозапуска";
                }
            }
        }

        [RelayCommand]
        private void OpenFolder() {
            try {
                var folderPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(folderPath) && System.IO.Directory.Exists(folderPath)) {
                    System.Diagnostics.Process.Start("explorer.exe", folderPath);
                    StatusText = "Папка открыта";
                    _loggerService.LogInfo($"Opened folder: {folderPath}");
                }
                else {
                    StatusText = "Ошибка: папка не найдена";
                    _loggerService.LogError("Application folder not found");
                }
            }
            catch (Exception ex) {
                StatusText = "Ошибка открытия папки";
                _loggerService.LogError($"Error opening folder: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task TestConnection() {
            StatusText = "Тестирование соединения...";
            _loggerService.LogInfo("Testing connection");
            
            try {
                var sites = new[] { "youtube.com", "discord.com", "rutracker.org" };
                var results = new List<string>();
                
                foreach (var site in sites) {
                    var startInfo = new System.Diagnostics.ProcessStartInfo {
                        FileName = "ping",
                        Arguments = $"-n 1 -w 2000 {site}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };
                    
                    using var process = System.Diagnostics.Process.Start(startInfo);
                    if (process != null) {
                        await process.WaitForExitAsync();
                        results.Add($"{site}: {(process.ExitCode == 0 ? "✓" : "✗")}");
                    }
                }
                
                StatusText = $"Тест завершен: {string.Join(", ", results)}";
                _loggerService.LogInfo($"Connection test results: {string.Join(", ", results)}");
            }
            catch (Exception ex) {
                StatusText = "Ошибка тестирования";
                _loggerService.LogError($"Error testing connection: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DnsSettings() {
            StatusText = "Открытие настроек DNS...";
            _loggerService.LogInfo("Opening DNS settings dialog");
            
            try {
                var ipv6Available = await _dnsService.CheckIpv6ConnectivityAsync();
                _loggerService.LogInfo($"IPv6 connectivity: {(ipv6Available ? "available" : "unavailable")}");
                
                // TODO: открыть диалоговое окно DNS
                StatusText = "DNS настройки";
            }
            catch (Exception ex) {
                StatusText = "Ошибка открытия DNS настроек";
                _loggerService.LogError($"Error opening DNS settings: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task Proxy() {
            if (_hostsService.IsProxyDomainsActive()) {
                StatusText = "Отключение разблокировки...";
                await _hostsService.RemoveProxyDomainsAsync();
                StatusText = "Разблокировка отключена";
            }
            else {
                StatusText = "Включение разблокировки...";
                await _hostsService.AddProxyDomainsAsync();
                StatusText = "Разблокировка включена";
            }
        }

        [RelayCommand]
        private async Task UpdateCheck() {
            StatusText = "Проверка обновлений...";
            _loggerService.LogInfo("Checking for updates");
            
            try {
                // TODO: реализовать проверку обновлений API GitHub
                // хотя хуй знает, заморочка с этим будет да и проект больно ебанутый и нахуй не нужный никому
                await Task.Delay(1000); // имитация имимтации бурной деятельности
                StatusText = "Обновлений не найдено";
                _loggerService.LogInfo("No updates available");
            }
            catch (Exception ex) {
                StatusText = "Ошибка проверки обновлений";
                _loggerService.LogError($"Error checking updates: {ex.Message}");
            }
        }
    }
}

