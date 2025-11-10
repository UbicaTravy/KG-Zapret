using System;
using Microsoft.Win32;
using KG_Zapret.Config;

namespace KG_Zapret.Services {
    public class RegistryService : IRegistryService {
        private const string DEFAULT_KEY = AppConfig.RegistryPath;

        public string? GetValue(string key, string valueName) {
            try {
                using var registryKey = Registry.CurrentUser.OpenSubKey(key);
                return registryKey?.GetValue(valueName)?.ToString();
            }
            catch {
                return null;
            }
        }

        public string? GetValue(string valueName) {
            return GetValue(DEFAULT_KEY, valueName);
        }

        public void SetValue(string key, string valueName, object value) {
            try {
                using var registryKey = Registry.CurrentUser.CreateSubKey(key, true);
                registryKey?.SetValue(valueName, value);
            }
            catch (Exception ex) {
                throw new Exception($"Ошибка записи в реестр: {ex.Message}", ex);
            }
        }

        public void SetValue(string valueName, object value) {
            SetValue(DEFAULT_KEY, valueName, value);
        }

        public void DeleteValue(string valueName) {
            try {
                using var registryKey = Registry.CurrentUser.OpenSubKey(DEFAULT_KEY, true);
                if (registryKey != null) {
                    registryKey.DeleteValue(valueName, false);
                }
            }
            catch {
                // бебебебебе
            }
        }

        public bool KeyExists(string key) {
            try {
                using var registryKey = Registry.CurrentUser.OpenSubKey(key);
                return registryKey != null;
            }
            catch {
                return false;
            }
        }
    }
}

