using System.Collections.Generic;
using System.Threading.Tasks;

namespace KG_Zapret.Services {
    public interface IStrategyService {
        Task<Dictionary<string, StrategyInfo>> GetStrategiesAsync();
        Task<string?> DownloadStrategyAsync(string strategyId);
        Task<bool> RefreshStrategiesFromInternetAsync();
        string? GetLastStrategy();
        void SetLastStrategy(string strategyName);
    }

    public class StrategyInfo {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string? DownloadUrl { get; set; }
        public string? Description { get; set; }
        public bool IsCombined { get; set; }
        public List<string>? CombinedWith { get; set; }
    }
}

