using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using KG_Zapret.Config;

namespace KG_Zapret.Services {
    public class StrategyService : IStrategyService {
        private readonly IRegistryService _registryService;
        private readonly IFileService _fileService;
        private readonly ILoggerService _loggerService;
        private const string RegistryPath = @"Software\KG-Zapret";
        private const string LastStrategyKey = "LastStrategy";
        private Dictionary<string, StrategyInfo>? _strategiesCache;

        public StrategyService(IRegistryService registryService, IFileService fileService, ILoggerService loggerService) {
            _registryService = registryService;
            _fileService = fileService;
            _loggerService = loggerService;
        }

        public Task<Dictionary<string, StrategyInfo>> GetStrategiesAsync() {
            if (_strategiesCache != null) {
                return Task.FromResult(_strategiesCache);
            }

            try {
                var indexPath = Path.Combine(AppConfig.IndexJsonFolder, "index.json");
                
                if (!_fileService.FileExists(indexPath)) {
                    _loggerService.LogWarning("index.json not found, returning empty strategies");
                    _strategiesCache = new Dictionary<string, StrategyInfo>();
                    return Task.FromResult(_strategiesCache);
                }

                var jsonContent = _fileService.ReadAllText(indexPath);
                var jsonObject = JObject.Parse(jsonContent);
                
                _strategiesCache = new Dictionary<string, StrategyInfo>();
                
                foreach (var property in jsonObject.Properties()) {
                    var strategyId = property.Name;
                    var strategyData = property.Value as JObject;
                    
                    if (strategyData == null) continue;
                    
                    var strategyInfo = new StrategyInfo {
                        Id = strategyId,
                        Name = strategyData["name"]?.ToString() ?? strategyId,
                        FilePath = strategyData["file"]?.ToString() ?? "",
                        Version = strategyData["version"]?.ToString()
                    };
                    
                    _strategiesCache[strategyId] = strategyInfo;
                }
                
                _loggerService.LogInfo($"Loaded {_strategiesCache.Count} strategies from index.json");
                return Task.FromResult(_strategiesCache);
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error loading strategies: {ex.Message}");
                _strategiesCache = new Dictionary<string, StrategyInfo>();
                return Task.FromResult(_strategiesCache);
            }
        }

        public async Task<string?> DownloadStrategyAsync(string strategyId) {
            try {
                _loggerService.LogInfo($"Downloading strategy: {strategyId}");
                
                var strategies = await GetStrategiesAsync();
                if (!strategies.TryGetValue(strategyId, out var strategyInfo)) {
                    _loggerService.LogError($"Strategy {strategyId} not found");
                    return null;
                }
                
                var batPath = Path.Combine(AppConfig.BatFolder, strategyInfo.FilePath);
                
                if (_fileService.FileExists(batPath)) {
                    _loggerService.LogDebug($"Strategy file already exists: {batPath}");
                    return batPath;
                }
                
                var downloadUrl = strategyInfo.DownloadUrl;
                if (string.IsNullOrEmpty(downloadUrl)) {
                    downloadUrl = $"https://raw.githubusercontent.com/Bot-Yan/zapret-gui-strategies/main/bat/{strategyInfo.FilePath}";
                }
                
                _loggerService.LogInfo($"Downloading from: {downloadUrl}");
                
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "KG-Zapret/1.0");
                
                var response = await httpClient.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                
                _fileService.CreateDirectory(Path.GetDirectoryName(batPath));
                
                _fileService.WriteAllText(batPath, content);
                
                _loggerService.LogInfo($"Strategy downloaded successfully: {batPath}");
                return batPath;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error downloading strategy: {ex.Message}");
                return null;
            }
        }
        
        public async Task<bool> RefreshStrategiesFromInternetAsync() {
            try {
                _loggerService.LogInfo("Refreshing strategies from internet");
                
                var indexUrl = "https://raw.githubusercontent.com/Bot-Yan/zapret-gui-strategies/main/index.json";
                var backupUrls = new[] {
                    "https://gitflic.ru/project/bot-yan/zapret-gui-strategies/blob/raw?file=index.json",
                    "https://api.github.com/repos/Bot-Yan/zapret-gui-strategies/contents/index.json"
                };
                
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "KG-Zapret/1.0");
                
                string? jsonContent = null;
                Exception? lastException = null;
                
                try {
                    _loggerService.LogInfo($"Trying main URL: {indexUrl}");
                    var response = await httpClient.GetAsync(indexUrl);
                    response.EnsureSuccessStatusCode();
                    jsonContent = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex) {
                    lastException = ex;
                    _loggerService.LogWarning($"Main URL failed: {ex.Message}");
                    
                    foreach (var backupUrl in backupUrls) {
                        try {
                            _loggerService.LogInfo($"Trying backup URL: {backupUrl}");
                            var response = await httpClient.GetAsync(backupUrl);
                            response.EnsureSuccessStatusCode();
                            jsonContent = await response.Content.ReadAsStringAsync();
                            
                            if (backupUrl.Contains("api.github.com")) {
                                var apiResponse = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                                var base64Content = apiResponse?.content?.ToString();
                                if (!string.IsNullOrEmpty(base64Content)) {
                                    var bytes = Convert.FromBase64String(base64Content.Replace("\n", ""));
                                    jsonContent = System.Text.Encoding.UTF8.GetString(bytes);
                                }
                            }
                            
                            lastException = null;
                            break;
                        }
                        catch (Exception backupEx) {
                            lastException = backupEx;
                            _loggerService.LogWarning($"Backup URL failed: {backupEx.Message}");
                        }
                    }
                }
                
                if (jsonContent == null) {
                    _loggerService.LogError($"All download attempts failed. Last error: {lastException?.Message}");
                    return false;
                }
                
                var indexPath = Path.Combine(AppConfig.IndexJsonFolder, "index.json");
                _fileService.CreateDirectory(AppConfig.IndexJsonFolder);
                _fileService.WriteAllText(indexPath, jsonContent);
                
                _strategiesCache = null;
                
                var strategies = await GetStrategiesAsync();
                _loggerService.LogInfo($"Strategies refreshed successfully: {strategies.Count} strategies");
                
                return true;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error refreshing strategies: {ex.Message}");
                return false;
            }
        }

        public string? GetLastStrategy() {
            return _registryService.GetValue(RegistryPath, LastStrategyKey);
        }

        public void SetLastStrategy(string strategyName) {
            _registryService.SetValue(RegistryPath, LastStrategyKey, strategyName);
        }
    }
}

