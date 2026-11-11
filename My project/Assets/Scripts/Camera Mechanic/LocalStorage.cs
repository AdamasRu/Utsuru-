using System;
using System.IO;
using UnityEngine;

namespace Utsuru.Capture
{
    /// <summary>Implementasi IDataStorage: simpan sebagai berkas JSON di persistentDataPath/Save.</summary>
    public class LocalStorage : IDataStorage
    {
        private const string Folder = "Save";

        public bool Save<T>(string accountId, string key, T data)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, Folder));
                File.WriteAllText(PathFor(accountId, key), JsonUtility.ToJson(data, true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalStorage] Gagal menyimpan '{key}': {e.Message}");
                return false;
            }
        }

        public bool TryLoad<T>(string accountId, string key, out T data)
        {
            data = default;
            try
            {
                string path = PathFor(accountId, key);
                if (!File.Exists(path)) return false;

                data = JsonUtility.FromJson<T>(File.ReadAllText(path));
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalStorage] Gagal memuat '{key}': {e.Message}");
                return false;
            }
        }

        private static string PathFor(string accountId, string key)
        {
            return Path.Combine(Application.persistentDataPath, Folder, $"{Sanitize(accountId)}_{Sanitize(key)}.json");
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "default";
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }
    }
}
