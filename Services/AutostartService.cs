using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KG_Zapret.Config;

namespace KG_Zapret.Services {
    public class AutostartService : IAutostartService {
        private readonly ILoggerService _loggerService;
        private readonly IRegistryService _registryService;
        private const string TASK_NAME = "KG-Zapret-Autostart";
        
        public AutostartService(ILoggerService loggerService, IRegistryService registryService) {
            _loggerService = loggerService;
            _registryService = registryService;
        }
        
        public bool IsAutostartEnabled() {
            try {
                var startInfo = new ProcessStartInfo {
                    FileName = "schtasks",
                    Arguments = $"/Query /TN \"{TASK_NAME}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using var process = Process.Start(startInfo);
                if (process == null) return false;
                
                process.WaitForExit();
                
                return process.ExitCode == 0;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error checking autostart status: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> EnableAutostartAsync(string strategyId) {
            try {
                _loggerService.LogInfo($"Enabling autostart with strategy: {strategyId}");
                
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) {
                    _loggerService.LogError("Cannot get executable path");
                    return false;
                }
                
                await DisableAutostartAsync();
                
                var arguments = $"/Create /TN \"{TASK_NAME}\" /TR \"\\\"{exePath}\\\" --autostart {strategyId}\" /SC ONLOGON /RL HIGHEST /F";
                
                var startInfo = new ProcessStartInfo {
                    FileName = "schtasks",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using var process = Process.Start(startInfo);
                if (process == null) {
                    _loggerService.LogError("Failed to start schtasks process");
                    return false;
                }
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0) {
                    _loggerService.LogInfo("Autostart enabled successfully");
                    
                    _registryService.SetValue("AutostartStrategy", strategyId);
                    _registryService.SetValue("AutostartEnabled", "1");
                    
                    return true;
                }
                else {
                    var error = await process.StandardError.ReadToEndAsync();
                    _loggerService.LogError($"Failed to enable autostart: {error}");
                    return false;
                }
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error enabling autostart: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> DisableAutostartAsync() {
            try {
                _loggerService.LogInfo("Disabling autostart");
                
                var startInfo = new ProcessStartInfo {
                    FileName = "schtasks",
                    Arguments = $"/Delete /TN \"{TASK_NAME}\" /F",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using var process = Process.Start(startInfo);
                if (process == null) {
                    _loggerService.LogError("Failed to start schtasks process");
                    return false;
                }
                
                await process.WaitForExitAsync();
                
                _registryService.DeleteValue("AutostartStrategy");
                _registryService.SetValue("AutostartEnabled", "0");
                
                _loggerService.LogInfo("Autostart disabled successfully");
                return true;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error disabling autostart: {ex.Message}");
                return false;
            }
        }
    }
}
