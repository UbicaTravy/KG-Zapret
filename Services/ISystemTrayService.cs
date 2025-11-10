namespace KG_Zapret.Services {
    public interface ISystemTrayService {
        void Initialize(System.Windows.Window mainWindow);
        void Show();
        void Hide();
        void ShowNotification(string title, string message);
        void ShowWindow();
        bool IsInitialized { get; }
    }
}

