using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KG_Zapret.Config;

namespace KG_Zapret.Services {
    public class DpiService : IDpiService {
        private readonly IProcessService _processService;
        private readonly ILoggerService _loggerService;
        private readonly IStrategyService _strategyService;
        private readonly IFileService _fileService;
        private readonly StrategyRunnerService _strategyRunner;

        public DpiService(IProcessService processService, ILoggerService loggerService, IStrategyService strategyService, IFileService fileService) {
            _processService = processService;
            _loggerService = loggerService;
            _strategyService = strategyService;
            _fileService = fileService;
            _strategyRunner = new StrategyRunnerService(fileService, loggerService, processService);
        }

        public async Task<bool> StartDpiAsync(string strategyId) {
            try {
                _loggerService.LogInfo($"Starting DPI with strategy: {strategyId}");
                
                if (IsDpiRunning()) {
                    _loggerService.LogInfo("Stopping existing DPI process...");
                    await StopDpiAsync();
                    await Task.Delay(1000);
                }
                
                var strategies = await _strategyService.GetStrategiesAsync();
                if (!strategies.TryGetValue(strategyId, out var strategyInfo)) {
                    _loggerService.LogError($"Strategy {strategyId} not found");
                    return false;
                }
                
                var batPath = Path.Combine(AppConfig.BatFolder, strategyInfo.FilePath);
                
                if (!_fileService.FileExists(batPath)) {
                    var downloadedPath = await _strategyService.DownloadStrategyAsync(strategyId);
                    if (downloadedPath == null) {
                        _loggerService.LogError($"Strategy file not found: {batPath}");
                        return false;
                    }
                    batPath = downloadedPath;
                }
                
                var batContent = _fileService.ReadAllText(batPath);
                var arguments = ParseBatFile(batContent);
                
                var success = _strategyRunner.StartStrategy(strategyId, arguments, strategyInfo.Name);
                
                if (success) {
                    _loggerService.LogInfo("DPI started successfully");
                }
                
                return success;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error starting DPI: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StopDpiAsync() {
            try {
                _loggerService.LogInfo("Stopping DPI");
                _strategyRunner.Stop();
                return await Task.FromResult(true);
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error stopping DPI: {ex.Message}");
                return false;
            }
        }

        public bool IsDpiRunning() {
            return _strategyRunner.IsRunning();
        }

        private List<string> ParseBatFile(string batContent) {
            var arguments = new List<string>();
            
            try {
                var lines = batContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var processedVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var line in lines) {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("set ", StringComparison.OrdinalIgnoreCase)) {
                        var assignment = trimmedLine.Substring(4).Trim();
                        var equalsIndex = assignment.IndexOf('=');
                        if (equalsIndex > 0) {
                            var varName = assignment.Substring(0, equalsIndex).Trim();
                            var varValue = assignment.Substring(equalsIndex + 1).Trim('"');
                            processedVariables[varName] = varValue;
                            _loggerService.LogDebug($"BAT variable: {varName} = {varValue}");
                        }
                    }
                }
                
                foreach (var line in lines) {
                    var trimmedLine = line.Trim();
                    
                    if (string.IsNullOrEmpty(trimmedLine) || 
                        trimmedLine.StartsWith("rem", StringComparison.OrdinalIgnoreCase) || 
                        trimmedLine.StartsWith("::") || 
                        trimmedLine.StartsWith(":")) {
                        continue;
                    }
                    
                    if (trimmedLine.StartsWith("@")) {
                        trimmedLine = trimmedLine.Substring(1).Trim();
                    }
                    
                    if (trimmedLine.Contains("winws.exe", StringComparison.OrdinalIgnoreCase)) {
                        _loggerService.LogDebug($"Found winws.exe line: {trimmedLine.Substring(0, Math.Min(100, trimmedLine.Length))}");
                        
                        var expandedLine = ExpandBatVariables(trimmedLine, processedVariables);
                        
                        var exeIndex = expandedLine.IndexOf("winws.exe", StringComparison.OrdinalIgnoreCase);
                        if (exeIndex >= 0) {
                            var afterExe = expandedLine.Substring(exeIndex + "winws.exe".Length).Trim();
                            
                            arguments = ParseCommandLineArguments(afterExe);
                            _loggerService.LogInfo($"Parsed {arguments.Count} arguments from BAT file");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error parsing BAT file: {ex.Message}");
            }
            
            return arguments;
        }
        
        private string ExpandBatVariables(string line, Dictionary<string, string> variables) {
            var result = line;
            
            foreach (var kvp in variables) {
                result = result.Replace($"%{kvp.Key}%", kvp.Value, StringComparison.OrdinalIgnoreCase);
            }
            
            result = result.Replace("%~dp0", AppConfig.ExeFolder + "\\", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("%cd%", AppConfig.ExeFolder, StringComparison.OrdinalIgnoreCase);
            
            return result;
        }
        
        private List<string> ParseCommandLineArguments(string commandLine) {
            var arguments = new List<string>();
            var currentArg = new System.Text.StringBuilder();
            bool inQuotes = false;
            bool escaped = false;
            
            for (int i = 0; i < commandLine.Length; i++) {
                char c = commandLine[i];
                
                if (escaped) {
                    currentArg.Append(c);
                    escaped = false;
                    continue;
                }
                
                if (c == '^' && i + 1 < commandLine.Length) {
                    escaped = true;
                    continue;
                }
                
                if (c == '"') {
                    inQuotes = !inQuotes;
                    continue;
                }
                
                if ((c == ' ' || c == '\t') && !inQuotes) {
                    if (currentArg.Length > 0) {
                        arguments.Add(currentArg.ToString());
                        currentArg.Clear();
                    }
                    continue;
                }
                
                currentArg.Append(c);
            }
            
            if (currentArg.Length > 0) {
                arguments.Add(currentArg.ToString());
            }
            
            return arguments;
        }
    }
}

