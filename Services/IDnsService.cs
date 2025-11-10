using System.Collections.Generic;
using System.Threading.Tasks;

namespace KG_Zapret.Services {
    public interface IDnsService {
        Task<List<(string name, string description)>> GetNetworkAdaptersAsync();
        Task<List<string>> GetDnsServersAsync(string adapterName, string addressFamily = "IPv4");
        Task<bool> SetDnsServersAsync(string adapterName, string primaryDns, string? secondaryDns = null, string addressFamily = "IPv4");
        Task<bool> SetAutoDnsAsync(string adapterName, string? addressFamily = null);
        Task<bool> CheckIpv6ConnectivityAsync();
        Task<bool> FlushDnsCacheAsync();
    }
}

