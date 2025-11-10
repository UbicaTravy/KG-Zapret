using System.IO;

namespace KG_Zapret.Config {
    public static class AppConfig {
        public const string AppName = "KG-Zapret";
        public const string AppVersion = "1.0.0";
        
        // диряктории
        public static string MainDirectory => GetMainDirectory();
        public static string BinFolder => Path.Combine(MainDirectory, "bin");
        public static string BatFolder => Path.Combine(MainDirectory, "bat");
        public static string ExeFolder => Path.Combine(MainDirectory, "exe");
        public static string ListsFolder => Path.Combine(MainDirectory, "lists");
        public static string ThemesFolder => Path.Combine(MainDirectory, "themes");
        public static string LogsFolder => Path.Combine(MainDirectory, "logs");
        public static string IndexJsonFolder => Path.Combine(MainDirectory, "json");
        
        // фяйлы
        public static string WinwsExe => Path.Combine(ExeFolder, "winws.exe");
        public static string IconPath => Path.Combine(MainDirectory, "ico", "Zapret1.ico");
        public static string WindivertFilter => Path.Combine(MainDirectory, "windivert.filter");
        
        // юайка
        public const int WindowWidth = 450;
        public const int WindowHeight = 730;
        public const int MaxLogFiles = 50;
        
        // регистри
        public const string RegistryPath = @"Software\KG-Zapret";
        public const string LastStrategyKey = "LastStrategy";
        public const string SelectedThemeKey = "SelectedTheme";
        
        private static string GetMainDirectory() {
            try {
                var mainModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
                if (mainModule?.FileName != null) {
                    var directory = Path.GetDirectoryName(mainModule.FileName);
                    if (!string.IsNullOrEmpty(directory)) {
                        return directory;
                    }
                }
            }
            catch {
                // П-О-Е-Б-А-Т-Ь!
            }
            
            return Directory.GetCurrentDirectory();
        }
    }
}

