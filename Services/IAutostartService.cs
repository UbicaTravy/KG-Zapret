using System.Threading.Tasks;

namespace KG_Zapret.Services {
    public interface IAutostartService {
        /// <summary>
        /// а шо если автостарт включон?
        /// </summary>
        bool IsAutostartEnabled();
        
        /// <summary>
        /// включует автозапуск через планировщик заданий Windows
        /// </summary>
        Task<bool> EnableAutostartAsync(string strategyId);
        
        /// <summary>
        /// отклучает автозапуск
        /// </summary>
        Task<bool> DisableAutostartAsync();
    }
}
