using System.IO;

namespace KG_Zapret.Services {
    public class FileService : IFileService {
        public bool FileExists(string path) {
            return File.Exists(path);
        }

        public string ReadAllText(string path) {
            return File.ReadAllText(path);
        }

        public void WriteAllText(string path, string content) {
            File.WriteAllText(path, content);
        }

        public void CreateDirectory(string path) {
            Directory.CreateDirectory(path);
        }
    }
}

