namespace KG_Zapret.Services {
    public interface IFileService {
        bool FileExists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string content);
        void CreateDirectory(string path);
    }
}

