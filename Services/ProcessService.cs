using System;
 using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace KG_Zapret.Services {
    public class ProcessService : IProcessService {
        private ManagementEventWatcher? _processWatcher;
        private readonly ILoggerService _loggerService;
        private CancellationTokenSource? _monitoringCts;
        
        public event EventHandler<ProcessStatusChangedEventArgs>? ProcessStatusChanged;
        
        public ProcessService(ILoggerService loggerService) {
            _loggerService = loggerService;
        }
        
        public bool IsProcessRunning(string processName) {
            try {
                var processes = Process.GetProcessesByName(processName.Replace(".exe", ""));
                return processes.Length > 0;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error checking process: {ex.Message}");
                return false;
            }
        }
        
        public bool IsProcessRunningWmi(string processName, bool silent = false) {
            try {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Process WHERE Name = '{processName}'");
                using var results = searcher.Get();
                
                var count = results.Count;
                if (!silent) {
                    _loggerService.LogDebug($"WMI found {count} instance(s) of {processName}");
                }
                
                return count > 0;
            }
            catch (Exception ex) {
                if (!silent) {
                    _loggerService.LogError($"Error checking process via WMI: {ex.Message}");
                }
                return IsProcessRunning(processName);
            }
        }

        public void KillProcess(string processName) {
            try {
                var processes = Process.GetProcessesByName(processName.Replace(".exe", ""));
                foreach (var process in processes) {
                    try {
                        _loggerService.LogInfo($"Killing process {processName} (PID: {process.Id})");
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch (Exception ex) {
                        _loggerService.LogWarning($"Failed to kill process: {ex.Message}");
                    }
                }
                
                try {
                    var startInfo = new ProcessStartInfo {
                        FileName = "taskkill",
                        Arguments = $"/F /IM {processName} /T",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };
                    
                    using var proc = Process.Start(startInfo);
                    proc?.WaitForExit(5000);
                }
                catch {
                }
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error killing process: {ex.Message}");
            }
        }
        
        public void StartMonitoring(string processName, int intervalMs = 2000) {
            StopMonitoring();
            
            _monitoringCts = new CancellationTokenSource();
            var token = _monitoringCts.Token;
            
            Task.Run(async () => {
                bool wasRunning = IsProcessRunningWmi(processName, true);
                _loggerService.LogInfo($"Started monitoring {processName}, initial state: {(wasRunning ? "running" : "stopped")}");
                
                while (!token.IsCancellationRequested) {
                    try {
                        await Task.Delay(intervalMs, token);
                        
                        bool isRunning = IsProcessRunningWmi(processName, true);
                        
                        if (isRunning != wasRunning) {
                            _loggerService.LogInfo($"Process {processName} status changed: {(isRunning ? "started" : "stopped")}");
                            ProcessStatusChanged?.Invoke(this, new ProcessStatusChangedEventArgs {
                                ProcessName = processName,
                                IsRunning = isRunning
                            });
                            wasRunning = isRunning;
                        }
                    }
                    catch (OperationCanceledException) {
                        break;
                    }
                    catch (Exception ex) {
                        _loggerService.LogError($"Error in process monitoring: {ex.Message}");
                    }
                }
                
                _loggerService.LogInfo($"Stopped monitoring {processName}");
            }, token);
        }
        
        public void StopMonitoring() {
            try {
                _monitoringCts?.Cancel();
                _monitoringCts?.Dispose();
                _monitoringCts = null;
                
                _processWatcher?.Stop();
                _processWatcher?.Dispose();
                _processWatcher = null;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error stopping monitoring: {ex.Message}");
            }
        }
        
        public int GetProcessId(string processName) {
            try {
                var processes = Process.GetProcessesByName(processName.Replace(".exe", ""));
                return processes.Length > 0 ? processes[0].Id : -1;
            }
            catch {
                return -1;
            }
        }
    }
    
    public class ProcessStatusChangedEventArgs : EventArgs {
        public string ProcessName { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
    }
}

