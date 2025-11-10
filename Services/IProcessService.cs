using System;

namespace KG_Zapret.Services {
    public interface IProcessService {
        bool IsProcessRunning(string processName);
        bool IsProcessRunningWmi(string processName, bool silent = false);
        void KillProcess(string processName);
        void StartMonitoring(string processName, int intervalMs = 2000);
        void StopMonitoring();
        int GetProcessId(string processName);
        event EventHandler<ProcessStatusChangedEventArgs>? ProcessStatusChanged;
    }
}

