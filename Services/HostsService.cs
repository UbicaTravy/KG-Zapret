using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KG_Zapret.Services {
    public class HostsService : IHostsService {
        private readonly IFileService _fileService;
        private readonly ILoggerService _loggerService;
        private const string HostsPath = @"C:\Windows\System32\drivers\etc\hosts";

        private readonly Dictionary<string, string> _proxyDomains = new() {
            { "chat.openai.com", "127.0.0.1" },
            { "api.openai.com", "127.0.0.1" },
            { "openai.com", "127.0.0.1" },
            { "spotify.com", "127.0.0.1" },
            { "www.spotify.com", "127.0.0.1" },
            { "twitch.tv", "127.0.0.1" },
            { "www.twitch.tv", "127.0.0.1" }
        };

        public HostsService(IFileService fileService, ILoggerService loggerService) {
            _fileService = fileService;
            _loggerService = loggerService;
        }

        public async Task<bool> AddProxyDomainsAsync() {
            try {
                if (!_fileService.FileExists(HostsPath)) {
                    _loggerService.LogError("Hosts file not found");
                    return false;
                }

                var content = _fileService.ReadAllText(HostsPath);
                var newContent = content.TrimEnd();
                
                if (!string.IsNullOrEmpty(newContent)) {
                    newContent += "\n\n";
                }

                foreach (var domain in _proxyDomains) {
                    newContent += $"{domain.Value} {domain.Key}\n";
                }

                _fileService.WriteAllText(HostsPath, newContent);
                _loggerService.LogInfo("Proxy domains added to hosts file");
                return await Task.FromResult(true);
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error adding proxy domains: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveProxyDomainsAsync() {
            try {
                if (!_fileService.FileExists(HostsPath)) {
                    return false;
                }

                var content = _fileService.ReadAllText(HostsPath);
                var lines = content.Split('\n');
                var newLines = new List<string>();

                foreach (var line in lines) {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#")) {
                        newLines.Add(line);
                        continue;
                    }

                    var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) {
                        var domain = parts[1];
                        if (!_proxyDomains.ContainsKey(domain)) {
                            newLines.Add(line);
                        }
                    }
                    else {
                        newLines.Add(line);
                    }
                }

                var newContent = string.Join("\n", newLines);
                _fileService.WriteAllText(HostsPath, newContent);
                _loggerService.LogInfo("Proxy domains removed from hosts file");
                return await Task.FromResult(true);
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error removing proxy domains: {ex.Message}");
                return false;
            }
        }

        public bool IsProxyDomainsActive() {
            try {
                if (!_fileService.FileExists(HostsPath)) {
                    return false;
                }

                var content = _fileService.ReadAllText(HostsPath);
                foreach (var domain in _proxyDomains.Keys) {
                    if (content.Contains(domain)) {
                        // Check if it's not commented out
                        var lines = content.Split('\n');
                        foreach (var line in lines) {
                            var trimmedLine = line.Trim();
                            if (!trimmedLine.StartsWith("#") && trimmedLine.Contains(domain)) {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch {
                return false;
            }
        }
    }
}

