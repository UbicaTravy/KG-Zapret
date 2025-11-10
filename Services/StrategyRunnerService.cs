using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KG_Zapret.Config;

namespace KG_Zapret.Services {
    public class StrategyRunnerService {
        private readonly IFileService _fileService;
        private readonly ILoggerService _loggerService;
        private readonly IProcessService _processService;
        private Process? _winwsProcess;
        private string _winwsExePath;

        public StrategyRunnerService(IFileService fileService, ILoggerService loggerService, IProcessService processService) {
            _fileService = fileService;
            _loggerService = loggerService;
            _processService = processService;
            _winwsExePath = AppConfig.WinwsExe;
        }

        public bool StartStrategy(string strategyId, List<string> arguments, string strategyName) {
            try {
                _loggerService.LogInfo($"Starting strategy: {strategyName} (ID: {strategyId})");
                
                Stop();
                
                if (!_fileService.FileExists(_winwsExePath)) {
                    _loggerService.LogError($"winws.exe not found at {_winwsExePath}");
                    return false;
                }
                
                var processedArgs = new List<string>(arguments);
                processedArgs = ResolveFilePaths(processedArgs);
                processedArgs = ApplyAllzoneReplacement(processedArgs);
                processedArgs = ApplyGameFilter(processedArgs);
                processedArgs = ApplyIpsetLists(processedArgs);
                processedArgs = ApplyWssize(processedArgs);
                
                var argumentsString = string.Join(" ", processedArgs.Select(arg => {
                    if (arg.Contains(" ") || arg.Contains("\t")) {
                        return $"\"{arg}\"";
                    }
                    return arg;
                }));
                
                LogFullCommand(_winwsExePath, processedArgs, strategyName);
                
                var startInfo = new ProcessStartInfo {
                    FileName = _winwsExePath,
                    Arguments = argumentsString,
                    WorkingDirectory = AppConfig.ExeFolder,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                _winwsProcess = Process.Start(startInfo);
                
                if (_winwsProcess == null || _winwsProcess.HasExited) {
                    _loggerService.LogError("Failed to start winws.exe");
                    return false;
                }
                
                _loggerService.LogInfo("Strategy started successfully");
                return true;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error starting strategy: {ex.Message}");
                return false;
            }
        }

        public void Stop() {
            try {
                if (_winwsProcess != null && !_winwsProcess.HasExited) {
                    _winwsProcess.Kill();
                    _winwsProcess.WaitForExit(5000);
                    _winwsProcess.Dispose();
                    _winwsProcess = null;
                }
                
                _processService.KillProcess("winws.exe");
                
                StopWinDivertService();
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error stopping strategy: {ex.Message}");
            }
        }

        public bool IsRunning() {
            if (_winwsProcess != null && !_winwsProcess.HasExited) {
                return true;
            }
            return _processService.IsProcessRunning("winws.exe");
        }

        private void LogFullCommand(string exePath, List<string> args, string strategyName) {
            try {
                var logsFolder = AppConfig.LogsFolder;
                _fileService.CreateDirectory(logsFolder);
                
                var cmdLogFile = Path.Combine(logsFolder, "commands_full.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var separator = new string('=', 80);
                
                var fullCmd = exePath + " " + string.Join(" ", args.Select(arg => {
                    if (arg.Contains(" ") || arg.Contains("\t")) {
                        return $"\"{arg}\"";
                    }
                    return arg;
                }));
                
                var logEntry = $"\n{separator}\n" +
                    $"Timestamp: {timestamp}\n" +
                    $"Strategy: {strategyName}\n" +
                    $"Command length: {fullCmd.Length} characters\n" +
                    $"Arguments count: {args.Count}\n" +
                    $"{separator}\n" +
                    $"FULL COMMAND:\n" +
                    $"{fullCmd}\n" +
                    $"{separator}\n" +
                    $"ARGUMENTS LIST:\n";
                
                for (int i = 0; i < args.Count; i++) {
                    logEntry += $"[{i,3}]: {args[i]}\n";
                }
                
                logEntry += $"{separator}\n\n";
                
                File.AppendAllText(cmdLogFile, logEntry, Encoding.UTF8);
                
                var lastCmdFile = Path.Combine(logsFolder, "last_command.txt");
                var lastCmdContent = $"# Last command executed at {timestamp}\n" +
                    $"# Strategy: {strategyName}\n\n" +
                    $"{fullCmd}";
                File.WriteAllText(lastCmdFile, lastCmdContent, Encoding.UTF8);
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error logging command: {ex.Message}");
            }
        }

        public (string? strategyName, List<string>? arguments) GetCurrentStrategyInfo() {
            if (IsRunning() && _winwsProcess != null) {
                // TODO: палучить актуальную стратегическую информацию из процесса
                return ("Running", null);
            }
            return (null, null);
        }
        
        private void StopWinDivertService() {
            try {
                var stopInfo = new ProcessStartInfo {
                    FileName = "sc",
                    Arguments = "stop windivert",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                
                using var stopProcess = Process.Start(stopInfo);
                stopProcess?.WaitForExit(5000);
                
                System.Threading.Thread.Sleep(1000);
                
                var deleteInfo = new ProcessStartInfo {
                    FileName = "sc",
                    Arguments = "delete windivert",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                
                using var deleteProcess = Process.Start(deleteInfo);
                deleteProcess?.WaitForExit(5000);
                
                _loggerService.LogInfo("WinDivert service stopped and deleted");
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error stopping WinDivert service: {ex.Message}");
            }
        }
        
        private List<string> ResolveFilePaths(List<string> args) {
            var resolved = new List<string>();
            var listsDir = Path.Combine(AppConfig.ExeFolder, "lists");
            var binDir = Path.Combine(AppConfig.ExeFolder, "bin");
            
            foreach (var arg in args) {
                if (arg.StartsWith("--hostlist=") || arg.StartsWith("--ipset=") || 
                    arg.StartsWith("--hostlist-exclude=") || arg.StartsWith("--ipset-exclude=")) {
                    var parts = arg.Split('=', 2);
                    if (parts.Length == 2) {
                        var filename = parts[1].Trim('"');
                        if (!Path.IsPathRooted(filename)) {
                            var fullPath = Path.Combine(listsDir, filename);
                            resolved.Add($"{parts[0]}={fullPath}");
                        } else {
                            resolved.Add(arg);
                        }
                    } else {
                        resolved.Add(arg);
                    }
                } else if (arg.StartsWith("--dpi-desync-fake-tls=") || arg.StartsWith("--dpi-desync-fake-syndata=") ||
                           arg.StartsWith("--dpi-desync-fake-quic=") || arg.StartsWith("--dpi-desync-fake-unknown-udp=")) {
                    var parts = arg.Split('=', 2);
                    if (parts.Length == 2 && !parts[1].StartsWith("0x")) {
                        var filename = parts[1].Trim('"');
                        if (!Path.IsPathRooted(filename)) {
                            var fullPath = Path.Combine(binDir, filename);
                            resolved.Add($"{parts[0]}={fullPath}");
                        } else {
                            resolved.Add(arg);
                        }
                    } else {
                        resolved.Add(arg);
                    }
                } else {
                    resolved.Add(arg);
                }
            }
            
            return resolved;
        }
        
        private List<string> ApplyAllzoneReplacement(List<string> args) {
            // TODO: чтение из настрояк
            bool allzoneEnabled = false;
            
            if (!allzoneEnabled) return args;
            
            var result = new List<string>();
            int replacements = 0;
            
            foreach (var arg in args) {
                if (arg.StartsWith("--hostlist=") && arg.Contains("other.txt")) {
                    result.Add(arg.Replace("other.txt", "allzone.txt"));
                    replacements++;
                } else {
                    result.Add(arg);
                }
            }
            
            if (replacements > 0) {
                _loggerService.LogInfo($"Allzone replacement applied: {replacements} replacements");
            }
            
            return result;
        }
        
        private List<string> ApplyGameFilter(List<string> args) {
            // TODO: чтение из настрояк x2
            bool gameFilterEnabled = false;
            
            if (!gameFilterEnabled) return args;
            
            var result = new List<string>();
            int i = 0;
            bool modified = false;
            
            while (i < args.Count) {
                var arg = args[i];
                result.Add(arg);
                
                if (arg.StartsWith("--filter-tcp=")) {
                    bool hasOtherHostlist = false;
                    for (int j = i + 1; j < args.Count && args[j] != "--new"; j++) {
                        if (args[j].StartsWith("--hostlist=")) {
                            var hostlistValue = args[j].Substring("--hostlist=".Length).Trim('"');
                            var filename = Path.GetFileName(hostlistValue);
                            if (filename == "other.txt" || filename == "allzone.txt" || 
                                filename == "other2.txt" || filename == "russia-blacklist.txt") {
                                hasOtherHostlist = true;
                                break;
                            }
                        }
                    }
                    
                    if (hasOtherHostlist) {
                        var ports = arg.Substring("--filter-tcp=".Length).Split(',').ToList();
                        if (!ports.Contains("1024-65535")) {
                            ports.Add("1024-65535");
                            result[result.Count - 1] = $"--filter-tcp={string.Join(",", ports)}";
                            modified = true;
                        }
                    }
                }
                
                i++;
            }
            
            if (modified) {
                _loggerService.LogInfo("Game Filter applied (added ports 1024-65535)");
            }
            
            return result;
        }
        
        private List<string> ApplyIpsetLists(List<string> args) {
            // TODO: чтение из настрояк x3
            bool ipsetEnabled = false;
            
            if (!ipsetEnabled) return args;
            
            var listsDir = Path.Combine(AppConfig.ExeFolder, "lists");
            var ipsetPath = Path.Combine(listsDir, "ipset-all.txt");
            
            if (!_fileService.FileExists(ipsetPath)) {
                _loggerService.LogWarning($"ipset-all.txt not found: {ipsetPath}");
                return args;
            }
            
            var result = new List<string>();
            var otherGroup = new[] { "other.txt", "other2.txt", "russia-blacklist.txt", "allzone.txt" };
            var youtubeGroup = new[] { "youtube.txt", "list-general.txt" };
            int added = 0;
            
            for (int i = 0; i < args.Count; i++) {
                var arg = args[i];
                result.Add(arg);
                
                if (arg.StartsWith("--hostlist=")) {
                    var filename = Path.GetFileName(arg.Substring("--hostlist=".Length).Trim('"'));
                    if (otherGroup.Contains(filename)) {
                        if (i + 1 >= args.Count || !args[i + 1].Contains("ipset-all.txt")) {
                            result.Add($"--ipset={ipsetPath}");
                            added++;
                        }
                    }
                }
            }
            
            if (added > 0) {
                _loggerService.LogInfo($"IPset lists applied: added {added} ipset-all.txt");
            }
            
            return result;
        }
        
        private List<string> ApplyWssize(List<string> args) {
            // TODO: чтение из настрояк x4
            bool wssizeEnabled = false;
            
            if (!wssizeEnabled) return args;
            
            var result = new List<string>();
            bool added = false;
            
            for (int i = 0; i < args.Count; i++) {
                var arg = args[i];
                result.Add(arg);
                
                if (arg.StartsWith("--filter-tcp=") && arg.Contains("443")) {
                    if (i + 1 >= args.Count || args[i + 1] != "--wssize=1:6") {
                        result.Add("--wssize=1:6");
                        added = true;
                    }
                }
            }
            
            if (!added) {
                int insertPos = result.Count;
                for (int i = result.Count - 1; i >= 0; i--) {
                    if (result[i].StartsWith("--wf-tcp=") || result[i].StartsWith("--wf-udp=")) {
                        insertPos = i + 1;
                        break;
                    }
                }
                
                result.Insert(insertPos, "--filter-tcp=443");
                result.Insert(insertPos + 1, "--wssize=1:6");
                if (insertPos + 2 >= result.Count || result[insertPos + 2] != "--new") {
                    result.Insert(insertPos + 2, "--new");
                }
                
                _loggerService.LogInfo("Wssize parameter applied: --filter-tcp=443 --wssize=1:6");
            }
            
            return result;
        }
    }
}

