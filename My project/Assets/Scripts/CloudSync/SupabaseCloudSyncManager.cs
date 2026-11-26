using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Utsuru.Authentication;
using Utsuru.Authentication.Configuration;
using Utsuru.Authentication.Models;
using Utsuru.Authentication.Security;
using Utsuru.Capture;

namespace Utsuru.CloudSync
{
    /// <summary>
    /// Pengelola sinkronisasi data progres game ke Supabase secara opsional / on-demand (Manual Save).
    /// Dipicu secara eksplisit oleh tombol "Save to Cloud" di HomeScreen atau menu permainan.
    /// Menyimpan:
    /// 1. Poin akumulasi ke tabel player_progress
    /// 2. Kosakata & objek yang ditemukan ke player_discovered_vocabularies / objects
    /// 3. File foto Polaroid lokal ke Supabase Storage (Bucket 'photos') & metadata ke player_photos
    /// </summary>
    public class SupabaseCloudSyncManager : MonoBehaviour
    {
        public static SupabaseCloudSyncManager Instance { get; private set; }

        public static SupabaseCloudSyncManager EnsureInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<SupabaseCloudSyncManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("SupabaseCloudSyncManager");
                    Instance = go.AddComponent<SupabaseCloudSyncManager>();
                }
            }
            return Instance;
        }

        [Header("Konfigurasi")]
        [SerializeField] private SupabaseProjectConfig config;

        public bool IsSaving { get; private set; } = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (config == null)
            {
                config = Resources.Load<SupabaseProjectConfig>("SupabaseProjectConfig");
            }
        }

        public bool IsUserLoggedIn(out string userId, out string accessToken)
        {
            userId = null;
            accessToken = null;

            // 1. Cek sesi aktif di memori SessionManager
            if (SessionManager.Instance != null && SessionManager.Instance.IsRegistered)
            {
                var session = SessionManager.Instance.ActiveAuthSession;
                if (session != null && !string.IsNullOrEmpty(session.AccessToken) && !string.IsNullOrEmpty(session.UserId))
                {
                    userId = session.UserId;
                    accessToken = session.AccessToken;
                    return true;
                }
            }

            // 2. Fallback: pulihkan dari brankas kredensial terenkripsi di disk jika Remember Me aktif
            try
            {
                var vault = SecureCredentialStorage.LoadCredentials();
                if (vault != null && !string.IsNullOrEmpty(vault.access_token) && !string.IsNullOrEmpty(vault.auth_user_id))
                {
                    userId = vault.auth_user_id;
                    accessToken = vault.access_token;

                    var sessionMgr = SessionManager.EnsureInstance();
                    if (sessionMgr != null && sessionMgr.ActiveAuthSession == null)
                    {
                        var restoredSession = new AuthSession(
                            vault.auth_user_id,
                            sessionMgr.Email ?? "",
                            sessionMgr.Username ?? "",
                            vault.access_token,
                            vault.refresh_token,
                            (int)vault.expires_in,
                            vault.token_type,
                            DateTime.UtcNow
                        );
                        sessionMgr.SaveRegisteredSession(restoredSession, true);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseCloudSync] Exception saat memeriksa vault: {ex.Message}");
            }

            return false;
        }

        private string ResolveAccountUsername()
        {
            if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.Username))
                return SessionManager.Instance.Username;
            return "player1";
        }

        /// <summary>
        /// Memicu penyimpanan seluruh progres saat ini ke Supabase Cloud (Manual / On-Demand).
        /// </summary>
        public void SaveAllProgressToCloud(Action<bool, string> onComplete)
        {
            if (IsSaving)
            {
                onComplete?.Invoke(false, "Proses penyimpanan cloud sedang berjalan.");
                return;
            }

            StartCoroutine(SaveAllProgressRoutine(onComplete));
        }

        private IEnumerator SaveAllProgressRoutine(Action<bool, string> onComplete)
        {
            if (!IsUserLoggedIn(out string userId, out string token))
            {
                string msg = "Harap login akun terlebih dahulu untuk menyimpan ke Cloud.";
                Debug.LogWarning($"[SupabaseCloudSync] {msg}");
                onComplete?.Invoke(false, msg);
                yield break;
            }

            IsSaving = true;
            Debug.Log("[SupabaseCloudSync] Memulai penyimpanan progres ke Cloud Supabase...");

            IDataStorage storage = new LocalStorage();
            string accountName = ResolveAccountUsername();

            // 1. Ambil Poin (dari memori PointRewardSystem jika aktif, atau dari disk Save/points.json)
            int pointsToSync = 0;
            if (PointRewardSystem.Instance != null)
            {
                pointsToSync = PointRewardSystem.Instance.Points;
            }
            else if (storage.TryLoad<PointsData>(accountName, "points", out PointsData pData) && pData != null)
            {
                pointsToSync = pData.points;
            }

            // Kirim poin ke Supabase player_progress
            yield return UpsertPointsToCloud(userId, token, pointsToSync);

            // 2. Ambil Data Kamus & Penemuan Objek
            List<int> vocabIds = null;
            List<string> objIds = null;
            List<PhotoRecord> photos = null;

            if (DictionaryManager.Instance != null)
            {
                if (DictionaryManager.Instance.DiscoveredVocabularyIds != null)
                    vocabIds = new List<int>(DictionaryManager.Instance.DiscoveredVocabularyIds);
                if (DictionaryManager.Instance.DiscoveredObjectIds != null)
                    objIds = new List<string>(DictionaryManager.Instance.DiscoveredObjectIds);
                if (DictionaryManager.Instance.Photos != null)
                    photos = new List<PhotoRecord>(DictionaryManager.Instance.Photos);
            }
            else if (storage.TryLoad<DictionaryData>(accountName, "dictionary", out DictionaryData dData) && dData != null)
            {
                vocabIds = dData.discoveredVocabularyIds;
                objIds = dData.discoveredObjectIds;
                photos = dData.photos;
            }

            // Kirim kata yang ditemukan
            if (vocabIds != null && vocabIds.Count > 0)
            {
                foreach (int vid in vocabIds)
                {
                    yield return PostDiscovery(userId, token, null, vid);
                }
            }

            // Kirim objek yang ditemukan
            if (objIds != null && objIds.Count > 0)
            {
                foreach (string oid in objIds)
                {
                    yield return PostDiscovery(userId, token, oid, 0);
                }
            }

            // Kirim foto polaroid (upload PNG ke Storage + catat metadata ke database)
            int uploadedPhotos = 0;
            if (photos != null && photos.Count > 0)
            {
                foreach (var photo in photos)
                {
                    yield return UploadPhotoToStorageAndDatabase(userId, token, photo);
                    uploadedPhotos++;
                }
            }

            IsSaving = false;
            Debug.Log($"[SupabaseCloudSync] Save to Cloud berhasil diselesaikan! (Poin: {pointsToSync}, Foto: {uploadedPhotos})");
            onComplete?.Invoke(true, "Progres berhasil disimpan ke Cloud!");
        }

        private IEnumerator UpsertPointsToCloud(string userId, string token, int points)
        {
            string url = $"{config.ProjectUrl}/rest/v1/player_progress";
            string payload = $"{{\"user_id\":\"{userId}\",\"points\":{points},\"updated_at\":\"{DateTime.UtcNow:o}\"}}";

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                ApplyHeaders(req, token);
                req.SetRequestHeader("Prefer", "resolution=merge-duplicates");

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[SupabaseCloudSync] Gagal update poin ke cloud: {req.error}");
                }
            }
        }

        private IEnumerator PostDiscovery(string userId, string token, string objectId, int vocabularyId)
        {
            if (vocabularyId > 0)
            {
                string vocabUrl = $"{config.ProjectUrl}/rest/v1/player_discovered_vocabularies";
                string payload = $"{{\"user_id\":\"{userId}\",\"vocabulary_id\":{vocabularyId}}}";

                using (UnityWebRequest req = new UnityWebRequest(vocabUrl, "POST"))
                {
                    byte[] body = Encoding.UTF8.GetBytes(payload);
                    req.uploadHandler = new UploadHandlerRaw(body);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    ApplyHeaders(req, token);
                    req.SetRequestHeader("Prefer", "resolution=ignore-duplicates");
                    yield return req.SendWebRequest();
                }
            }

            if (!string.IsNullOrEmpty(objectId))
            {
                string objUrl = $"{config.ProjectUrl}/rest/v1/player_discovered_objects";
                string payload = $"{{\"user_id\":\"{userId}\",\"object_id\":\"{objectId}\"}}";

                using (UnityWebRequest req = new UnityWebRequest(objUrl, "POST"))
                {
                    byte[] body = Encoding.UTF8.GetBytes(payload);
                    req.uploadHandler = new UploadHandlerRaw(body);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    ApplyHeaders(req, token);
                    req.SetRequestHeader("Prefer", "resolution=ignore-duplicates");
                    yield return req.SendWebRequest();
                }
            }
        }

        private IEnumerator UploadPhotoToStorageAndDatabase(string userId, string token, PhotoRecord record)
        {
            if (record == null) yield break;

            string localFilePath = Path.Combine(Application.persistentDataPath, record.imagePath);
            if (!File.Exists(localFilePath))
            {
                yield break;
            }

            byte[] imageBytes = File.ReadAllBytes(localFilePath);
            string storageFileName = $"{record.photoId}.png";
            string storagePath = $"{userId}/{storageFileName}";
            string storageUrl = $"{config.ProjectUrl}/storage/v1/object/photos/{storagePath}";

            // 1. Upload file fisik ke Supabase Storage (Bucket 'photos')
            using (UnityWebRequest uploadReq = new UnityWebRequest(storageUrl, "POST"))
            {
                uploadReq.uploadHandler = new UploadHandlerRaw(imageBytes);
                uploadReq.downloadHandler = new DownloadHandlerBuffer();
                ApplyHeaders(uploadReq, token);
                uploadReq.SetRequestHeader("Content-Type", "image/png");

                yield return uploadReq.SendWebRequest();
            }

            // 2. Catat metadata foto ke tabel player_photos di PostgreSQL
            string dbUrl = $"{config.ProjectUrl}/rest/v1/player_photos";
            string safeObjName = EscapeJson(record.objectName);
            string safeDate = !string.IsNullOrEmpty(record.dateTaken) ? record.dateTaken : DateTime.UtcNow.ToString("o");

            string metadataPayload = "{"
                + $"\"photo_id\":\"{record.photoId}\","
                + $"\"user_id\":\"{userId}\","
                + $"\"vocabulary_id\":{record.vocabularyId},"
                + $"\"object_id\":\"{record.objectId}\","
                + $"\"object_name\":\"{safeObjName}\","
                + $"\"storage_path\":\"photos/{storagePath}\","
                + $"\"date_taken\":\"{safeDate}\","
                + $"\"is_favorite\":{(record.isFavorite ? "true" : "false")},"
                + $"\"is_liked\":{(record.isLiked ? "true" : "false")}"
                + "}";

            using (UnityWebRequest dbReq = new UnityWebRequest(dbUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(metadataPayload);
                dbReq.uploadHandler = new UploadHandlerRaw(bodyRaw);
                dbReq.downloadHandler = new DownloadHandlerBuffer();
                ApplyHeaders(dbReq, token);
                dbReq.SetRequestHeader("Prefer", "resolution=merge-duplicates");

                yield return dbReq.SendWebRequest();
            }
        }

        private void ApplyHeaders(UnityWebRequest req, string bearerToken)
        {
            req.SetRequestHeader("apikey", config.PublishableKey);
            if (!string.IsNullOrEmpty(bearerToken))
            {
                req.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
            }
            req.SetRequestHeader("Content-Type", "application/json");
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
