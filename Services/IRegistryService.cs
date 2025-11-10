namespace KG_Zapret.Services {
    public interface IRegistryService {
        string? GetValue(string key, string valueName);
        string? GetValue(string valueName);
        void SetValue(string key, string valueName, object value);
        void SetValue(string valueName, object value);
        void DeleteValue(string valueName);
        bool KeyExists(string key);
    }
}

