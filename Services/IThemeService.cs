using System.Collections.Generic;

namespace KG_Zapret.Services {
    public interface IThemeService {
        List<string> GetAvailableThemes();
        string? GetSelectedTheme();
        void ApplyTheme(string themeName);
        void SetSelectedTheme(string themeName);
    }
}

