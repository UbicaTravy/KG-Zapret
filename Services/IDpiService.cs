using System.Threading.Tasks;

namespace KG_Zapret.Services {
    public interface IDpiService {
        Task<bool> StartDpiAsync(string strategyId);
        Task<bool> StopDpiAsync();
        bool IsDpiRunning();
    }
}

