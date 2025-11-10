using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using KG_Zapret.Config;

namespace KG_Zapret.Services {
    public class LoggerService : ILoggerService {
        private readonly ILogger<LoggerService> _logger;
        private readonly string _sessionLogFile;
        private readonly object _fileLock = new object();
        private const int MaxLogFiles = 50;

        public LoggerService(ILogger<LoggerService> logger) {
            _logger = logger;
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            _sessionLogFile = Path.Combine(AppConfig.LogsFolder, $"session_{timestamp}_{sessionId}.log");
            
            EnsureLogsDirectory();
            RotateOldLogs();
            
            WriteToFile($"=== Session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        }
        
        private void EnsureLogsDirectory() {
            try {
                if (!Directory.Exists(AppConfig.LogsFolder)) {
                    Directory.CreateDirectory(AppConfig.LogsFolder);
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to create logs directory");
            }
        }
        
        private void RotateOldLogs() {
            try {
                var logFiles = Directory.GetFiles(AppConfig.LogsFolder, "session_*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();
                
                if (logFiles.Count > MaxLogFiles) {
                    var filesToDelete = logFiles.Skip(MaxLogFiles);
                    foreach (var file in filesToDelete) {
                        try {
                            file.Delete();
                            _logger.LogDebug($"Deleted old log file: {file.Name}");
                        }
                        catch (Exception ex) {
                            _logger.LogWarning($"Failed to delete old log file {file.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to rotate old logs");
            }
        }
        
        private void WriteToFile(string message) {
            try {
                lock (_fileLock) {
                    File.AppendAllText(_sessionLogFile, message + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to write to log file");
            }
        }

        public void LogInfo(string message) {
            _logger.LogInformation(message);
            WriteToFile($"[{DateTime.Now:HH:mm:ss}] [INFO] {message}");
        }

        public void LogWarning(string message) {
            _logger.LogWarning(message);
            WriteToFile($"[{DateTime.Now:HH:mm:ss}] [WARN] {message}");
        }

        public void LogError(string message) {
            _logger.LogError(message);
            WriteToFile($"[{DateTime.Now:HH:mm:ss}] [ERROR] {message}");
        }

        public void LogDebug(string message) {
            _logger.LogDebug(message);
            WriteToFile($"[{DateTime.Now:HH:mm:ss}] [DEBUG] {message}");
        }
        
        public string GetCurrentLogFilePath() {
            return _sessionLogFile;
        }
    }
}

