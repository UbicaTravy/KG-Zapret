using System.Threading.Tasks;

namespace KG_Zapret.Services {
    public interface IHostsService {
        Task<bool> AddProxyDomainsAsync();
        Task<bool> RemoveProxyDomainsAsync();
        bool IsProxyDomainsActive();
    }
}

