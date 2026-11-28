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
                if (!File.Exists(path))
                {
                    TryMigrateLegacySave(accountId, key, path);
                }
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

        public static bool HasLocalFile(string accountId, string key)
        {
            return File.Exists(PathFor(accountId, key));
        }

        private static void TryMigrateLegacySave(string accountId, string key, string targetPath)
        {
            try
            {
                DirectoryInfo persistentDir = new DirectoryInfo(Application.persistentDataPath);
                DirectoryInfo companyDir = persistentDir.Parent;
                DirectoryInfo localLowDir = companyDir != null ? companyDir.Parent : null;
                if (localLowDir == null) return;

                string legacyRoot = Path.Combine(localLowDir.FullName, "DefaultCompany", "My project");
                string legacySave = Path.Combine(legacyRoot, Folder, $"{Sanitize(accountId)}_{Sanitize(key)}.json");
                if (!File.Exists(legacySave)) return;

                string targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir)) Directory.CreateDirectory(targetDir);
                File.Copy(legacySave, targetPath, overwrite: false);

                if (string.Equals(key, "dictionary", StringComparison.OrdinalIgnoreCase))
                {
                    string legacyPhotosDir = Path.Combine(legacyRoot, "Photos");
                    string targetPhotosDir = Path.Combine(Application.persistentDataPath, "Photos");
                    if (Directory.Exists(legacyPhotosDir))
                    {
                        Directory.CreateDirectory(targetPhotosDir);
                        foreach (string srcPng in Directory.GetFiles(legacyPhotosDir, "*.png"))
                        {
                            string dstPng = Path.Combine(targetPhotosDir, Path.GetFileName(srcPng));
                            if (!File.Exists(dstPng))
                            {
                                File.Copy(srcPng, dstPng, overwrite: false);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocalStorage] Migrasi save lama dilewati: {e.Message}");
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
