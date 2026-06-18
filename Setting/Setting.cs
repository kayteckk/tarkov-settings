using System;
using System.IO;
using Newtonsoft.Json;

namespace tarkov_settings.Setting
{
    internal class Settings<T> where T : new()
    {
        private const string DEFAULT_FILENAME = "settings.json";
        private const string SETTINGS_DIRECTORY = "Tarkov Settings";

        public void Save(string fileName = DEFAULT_FILENAME)
        {
            string path = ResolveFilePath(fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static T Load(string fileName = DEFAULT_FILENAME)
        {
            string path = ResolveFilePath(fileName);
            T t = new T();

            if (!File.Exists(path) && fileName == DEFAULT_FILENAME)
                path = ResolveLegacyFilePath(fileName);

            if (File.Exists(path))
                t = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            return t;
        }

        private static string ResolveFilePath(string fileName)
        {
            if (Path.IsPathRooted(fileName))
                return fileName;

            if (fileName == DEFAULT_FILENAME)
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(appDataPath, SETTINGS_DIRECTORY, DEFAULT_FILENAME);
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }

        private static string ResolveLegacyFilePath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }
    }
}
