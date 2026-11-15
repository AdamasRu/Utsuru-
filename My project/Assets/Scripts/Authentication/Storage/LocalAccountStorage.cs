using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Utsuru.Authentication.Storage
{
    /// <summary>
    /// Data structure representing local account metadata stored on disk.
    /// Does NOT store passwords or sensitive tokens.
    /// </summary>
    [Serializable]
    public class LocalProfileData
    {
        [JsonProperty("login_mode")]
        public string login_mode = "Registered";

        [JsonProperty("is_logged_in")]
        public bool is_logged_in;

        [JsonProperty("auth_user_id")]
        public string auth_user_id;

        [JsonProperty("local_profile_id")]
        public string local_profile_id;

        [JsonProperty("email")]
        public string email;

        [JsonProperty("username")]
        public string username;

        [JsonProperty("last_login_timestamp")]
        public string last_login_timestamp;
    }

    /// <summary>
    /// Handles atomic storage, retrieval, and validation of local account metadata.
    /// Location: Application.persistentDataPath/Save/local_profile.json
    /// </summary>
    public static class LocalAccountStorage
    {
        private const string SaveFolderName = "Save";
        private const string ProfileFileName = "local_profile.json";
        private const string TempFileName = "local_profile.json.tmp";
        private const string BackupFileName = "local_profile.json.bak";

        private static string customSaveDirectory = null;

        /// <summary>
        /// Allows test runners or unit tests to specify an explicit save directory.
        /// </summary>
        public static void SetCustomSaveDirectory(string path)
        {
            customSaveDirectory = path;
        }

        /// <summary>
        /// Gets the absolute directory path where account metadata is stored.
        /// </summary>
        public static string GetSaveDirectory()
        {
            if (!string.IsNullOrEmpty(customSaveDirectory))
            {
                return customSaveDirectory;
            }

            try
            {
                return Path.Combine(Application.persistentDataPath, SaveFolderName);
            }
            catch
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppData + "Low", "DefaultCompany", "My project", SaveFolderName);
            }
        }

        /// <summary>
        /// Gets the absolute file path to local_profile.json.
        /// </summary>
        public static string GetProfileFilePath()
        {
            return Path.Combine(GetSaveDirectory(), ProfileFileName);
        }

        private static void LogInfo(string msg) { try { Debug.Log(msg); } catch { } }
        private static void LogWarn(string msg) { try { Debug.LogWarning(msg); } catch { } }
        private static void LogErr(string msg) { try { Debug.LogError(msg); } catch { } }

        /// <summary>
        /// Checks whether a valid remembered account marker exists on disk.
        /// Returns true only if file exists, JSON is valid, is_logged_in is true, and auth_user_id is valid.
        /// </summary>
        public static bool HasRememberedAccount()
        {
            try
            {
                string path = GetProfileFilePath();
                if (!File.Exists(path)) return false;

                LocalProfileData profile = LoadProfile();
                if (profile == null) return false;

                return profile.is_logged_in && !string.IsNullOrWhiteSpace(profile.auth_user_id);
            }
            catch (Exception ex)
            {
                LogWarn($"[LocalAccountStorage] Error checking remembered account: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely loads and validates local account metadata from disk.
        /// Returns null if file is missing, empty, or corrupted.
        /// </summary>
        public static LocalProfileData LoadProfile()
        {
            string path = GetProfileFilePath();
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    LogWarn("[LocalAccountStorage] local_profile.json is empty.");
                    return TryLoadBackup();
                }

                LocalProfileData profile = JsonConvert.DeserializeObject<LocalProfileData>(json);
                if (profile == null)
                {
                    LogWarn("[LocalAccountStorage] Failed to deserialize local_profile.json.");
                    return TryLoadBackup();
                }

                return profile;
            }
            catch (JsonException jsonEx)
            {
                LogWarn($"[LocalAccountStorage] Corrupted JSON in local_profile.json: {jsonEx.Message}");
                return TryLoadBackup();
            }
            catch (Exception ex)
            {
                LogErr($"[LocalAccountStorage] Unexpected error reading local_profile.json: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Atomically saves account metadata to local_profile.json.
        /// </summary>
        public static bool SaveProfile(LocalProfileData profile)
        {
            if (profile == null)
            {
                LogErr("[LocalAccountStorage] Cannot save null profile.");
                return false;
            }

            // Ensure stable local_profile_id exists
            if (string.IsNullOrWhiteSpace(profile.local_profile_id))
            {
                profile.local_profile_id = Guid.NewGuid().ToString();
            }

            // Always update timestamp in ISO-8601 UTC
            profile.last_login_timestamp = DateTime.UtcNow.ToString("o");

            string saveDir = GetSaveDirectory();
            string targetPath = GetProfileFilePath();
            string tempPath = Path.Combine(saveDir, TempFileName);
            string backupPath = Path.Combine(saveDir, BackupFileName);

            try
            {
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                string json = JsonConvert.SerializeObject(profile, Formatting.Indented);

                // 1. Write to temporary file first (atomic write preparation)
                File.WriteAllText(tempPath, json);

                // 2. Create backup of existing file if it exists and is valid
                if (File.Exists(targetPath))
                {
                    try
                    {
                        File.Copy(targetPath, backupPath, true);
                    }
                    catch (Exception bakEx)
                    {
                        LogWarn($"[LocalAccountStorage] Failed to create profile backup: {bakEx.Message}");
                    }
                }

                // 3. Atomically replace target with temp file
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                File.Move(tempPath, targetPath);

                LogInfo("[LocalAccountStorage] local_profile.json saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                LogErr($"[LocalAccountStorage] Failed to atomically save local_profile.json: {ex.Message}");
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
                return false;
            }
        }

        /// <summary>
        /// Updates the profile to is_logged_in = false without deleting local game progress or profile history.
        /// </summary>
        public static bool SetLoggedInState(bool isLoggedIn)
        {
            LocalProfileData profile = LoadProfile();
            if (profile == null) return false;

            profile.is_logged_in = isLoggedIn;
            return SaveProfile(profile);
        }

        /// <summary>
        /// Explicit logout helper: sets is_logged_in to false on the stored profile.
        /// </summary>
        public static bool ClearRememberedAccount()
        {
            return SetLoggedInState(false);
        }

        private static LocalProfileData TryLoadBackup()
        {
            string backupPath = Path.Combine(GetSaveDirectory(), BackupFileName);
            if (!File.Exists(backupPath)) return null;

            try
            {
                string json = File.ReadAllText(backupPath);
                if (string.IsNullOrWhiteSpace(json)) return null;

                LocalProfileData backup = JsonConvert.DeserializeObject<LocalProfileData>(json);
                if (backup != null)
                {
                    Debug.LogWarning("[LocalAccountStorage] Successfully recovered profile from backup file.");
                    return backup;
                }
            }
            catch { }

            return null;
        }
    }
}
