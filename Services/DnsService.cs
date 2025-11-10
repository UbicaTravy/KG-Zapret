using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace KG_Zapret.Services {
    public class DnsService : IDnsService {
        private readonly ILoggerService _loggerService;

        public DnsService(ILoggerService loggerService) {
            _loggerService = loggerService;
        }

        public async Task<List<(string name, string description)>> GetNetworkAdaptersAsync() {
            var adapters = new List<(string name, string description)>();
            
            try {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True");
                foreach (ManagementObject adapter in searcher.Get()) {
                    var name = adapter["NetConnectionID"]?.ToString();
                    var description = adapter["Description"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(description)) {
                        adapters.Add((name, description));
                    }
                }
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error getting network adapters: {ex.Message}");
            }

            return await Task.FromResult(adapters);
        }

        public async Task<List<string>> GetDnsServersAsync(string adapterName, string addressFamily = "IPv4") {
            try {
                var escapedAdapter = adapterName.Replace("'", "''");
                var script = $@"
                    try {{
                        Get-DnsClientServerAddress -InterfaceAlias '{escapedAdapter}' -AddressFamily {addressFamily} |
                            Select -Expand ServerAddresses
                    }} catch {{}}
                ";
                
                var startInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null) return new List<string>();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0) return new List<string>();
                
                var servers = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                
                return servers;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error getting DNS servers: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<bool> SetDnsServersAsync(string adapterName, string primaryDns, string? secondaryDns = null, string addressFamily = "IPv4") {
            try {
                _loggerService.LogInfo($"Setting {addressFamily} DNS servers for adapter: {adapterName}");
                
                var servers = string.IsNullOrEmpty(secondaryDns) 
                    ? $"@('{primaryDns}')"
                    : $"@('{primaryDns}','{secondaryDns}')";
                
                var script = $"Set-DnsClientServerAddress -InterfaceAlias '{adapterName}' -AddressFamily {addressFamily} -ServerAddresses {servers}";
                
                var startInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null) {
                    if (addressFamily == "IPv4") {
                        return await SetDnsViaNetshAsync(adapterName, primaryDns, secondaryDns);
                    }
                    return false;
                }
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0) {
                    var error = await process.StandardError.ReadToEndAsync();
                    _loggerService.LogError($"PowerShell DNS set error: {error}");
                    
                    if (addressFamily == "IPv4") {
                        return await SetDnsViaNetshAsync(adapterName, primaryDns, secondaryDns);
                    }
                    return false;
                }
                
                _loggerService.LogInfo($"{addressFamily} DNS servers set successfully");
                return true;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error setting DNS servers: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> SetDnsViaNetshAsync(string adapterName, string primaryDns, string? secondaryDns) {
            try {
                var cmd1 = $"netsh interface ipv4 set dnsservers \"{adapterName}\" static {primaryDns} primary";
                var startInfo1 = new System.Diagnostics.ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = $"/c {cmd1}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };
                
                using var process1 = System.Diagnostics.Process.Start(startInfo1);
                if (process1 == null) return false;
                
                await process1.WaitForExitAsync();
                if (process1.ExitCode != 0) return false;
                
                if (!string.IsNullOrEmpty(secondaryDns)) {
                    var cmd2 = $"netsh interface ipv4 add dnsservers \"{adapterName}\" {secondaryDns} index=2";
                    var startInfo2 = new System.Diagnostics.ProcessStartInfo {
                        FileName = "cmd.exe",
                        Arguments = $"/c {cmd2}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var process2 = System.Diagnostics.Process.Start(startInfo2);
                    await process2?.WaitForExitAsync()!;
                }
                
                return true;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Netsh fallback error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SetAutoDnsAsync(string adapterName, string? addressFamily = null) {
            try {
                _loggerService.LogInfo($"Setting auto DNS for adapter: {adapterName}");
                
                var famFlag = string.IsNullOrEmpty(addressFamily) ? "" : $"-AddressFamily {addressFamily}";
                var script = $"Set-DnsClientServerAddress -InterfaceAlias '{adapterName}' {famFlag} -ResetServerAddresses";
                
                var startInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };
                
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null) return false;
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0) {
                    var error = await process.StandardError.ReadToEndAsync();
                    _loggerService.LogError($"Error setting auto DNS: {error}");
                    return false;
                }
                
                _loggerService.LogInfo("Auto DNS set successfully");
                return true;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error setting auto DNS: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> CheckIpv6ConnectivityAsync() {
            try {
                var startInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "ping",
                    Arguments = "-6 -n 1 -w 1500 2001:4860:4860::8888",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null) return false;
                
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch {
                return false;
            }
        }
        
        public async Task<bool> FlushDnsCacheAsync() {
            try {
                var startInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null) return false;
                
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch (Exception ex) {
                _loggerService.LogError($"Error flushing DNS cache: {ex.Message}");
                return false;
            }
        }
    }
}

