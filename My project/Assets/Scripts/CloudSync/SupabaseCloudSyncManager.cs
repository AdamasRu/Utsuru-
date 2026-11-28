using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Utsuru.Achievements;
using Utsuru.Authentication;
using Utsuru.Authentication.Configuration;
using Utsuru.Authentication.Models;
using Utsuru.Authentication.Security;
using Utsuru.Capture;
using Utsuru.Store;

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
        public bool IsLoadingFromCloud { get; private set; } = false;

        private readonly HashSet<string> syncedUserIds = new HashSet<string>(StringComparer.Ordinal);

        [Serializable]
        private class JsonArrayWrapper<T>
        {
            public T[] items;
        }

        [Serializable]
        private class CloudProgressRow
        {
            public int points;
        }

        [Serializable]
        private class CloudVocabRow
        {
            public int vocabulary_id;
        }

        [Serializable]
        private class CloudObjectRow
        {
            public string object_id;
        }

        [Serializable]
        private class CloudPhotoRow
        {
            public string photo_id;
            public int vocabulary_id;
            public string object_id;
            public string object_name;
            public string storage_path;
            public string date_taken;
            public bool is_favorite;
            public bool is_liked;
        }

        [Serializable]
        private class CloudThemeRow
        {
            public string theme_id;
            public bool is_applied;
            public string purchased_at;
        }

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
        /// Mereset penanda sesi sinkronisasi agar login akun baru otomatis menarik progres cloud-nya.
        /// </summary>
        public void ResetSessionSyncFlag()
        {
            syncedUserIds.Clear();
        }

        /// <summary>
        /// Menarik progres terbaru dari Cloud secara otomatis jika user sudah login dan belum disinkronkan pada sesi ini.
        /// </summary>
        public void SyncFromCloudIfLoggedIn(Action<bool, string> onComplete = null, bool force = false)
        {
            if (!IsUserLoggedIn(out string userId, out _))
            {
                onComplete?.Invoke(false, "User belum login.");
                return;
            }

            if (!force && (syncedUserIds.Contains(userId) || IsLoadingFromCloud || IsSaving))
            {
                return;
            }

            LoadAllProgressFromCloud(onComplete);
        }

        /// <summary>
        /// Mengunduh dan menggabungkan progres akun login dari Supabase Cloud ke LocalStorage aktif.
        /// </summary>
        public void LoadAllProgressFromCloud(Action<bool, string> onComplete = null)
        {
            if (IsLoadingFromCloud)
            {
                onComplete?.Invoke(false, "Cloud sync is already in progress.");
                return;
            }

            StartCoroutine(LoadAllProgressRoutine(onComplete));
        }

        private IEnumerator LoadAllProgressRoutine(Action<bool, string> onComplete)
        {
            if (!IsUserLoggedIn(out string userId, out string token))
            {
                onComplete?.Invoke(false, "Not logged in.");
                yield break;
            }

            if (config == null)
            {
                config = Resources.Load<SupabaseProjectConfig>("SupabaseProjectConfig");
            }

            if (config == null || string.IsNullOrEmpty(config.ProjectUrl))
            {
                onComplete?.Invoke(false, "Supabase configuration is missing.");
                yield break;
            }

            IsLoadingFromCloud = true;
            string accountName = ResolveAccountUsername();
            IDataStorage storage = new LocalStorage();
            string escapedUserId = UnityWebRequest.EscapeURL(userId);

            // 1. Pull player_progress (points)
            string progressJson = null;
            yield return SendCloudGet(
                $"{config.ProjectUrl}/rest/v1/player_progress?user_id=eq.{escapedUserId}&select=points&limit=1",
                token,
                json => progressJson = json);

            CloudProgressRow[] progressRows = ParseJsonArray<CloudProgressRow>(progressJson);
            if (progressRows != null && progressRows.Length > 0)
            {
                int cloudPoints = Mathf.Max(0, progressRows[0].points);
                bool hasLocalPoints = LocalStorage.HasLocalFile(accountName, "points");
                storage.TryLoad<PointsData>(accountName, "points", out PointsData localPoints);

                if (!hasLocalPoints || localPoints == null || (localPoints.points == 0 && cloudPoints > 0))
                {
                    storage.Save(accountName, "points", new PointsData { points = cloudPoints });
                }
            }

            // 2. Pull discovered vocabularies, objects, and polaroid photos
            string vocabJson = null;
            yield return SendCloudGet(
                $"{config.ProjectUrl}/rest/v1/player_discovered_vocabularies?user_id=eq.{escapedUserId}&select=vocabulary_id",
                token,
                json => vocabJson = json);

            string objectJson = null;
            yield return SendCloudGet(
                $"{config.ProjectUrl}/rest/v1/player_discovered_objects?user_id=eq.{escapedUserId}&select=object_id",
                token,
                json => objectJson = json);

            string photoJson = null;
            yield return SendCloudGet(
                $"{config.ProjectUrl}/rest/v1/player_photos?user_id=eq.{escapedUserId}&select=photo_id,vocabulary_id,object_id,object_name,storage_path,date_taken,is_favorite,is_liked",
                token,
                json => photoJson = json);

            CloudVocabRow[] vocabRows = ParseJsonArray<CloudVocabRow>(vocabJson);
            CloudObjectRow[] objectRows = ParseJsonArray<CloudObjectRow>(objectJson);
            CloudPhotoRow[] photoRows = ParseJsonArray<CloudPhotoRow>(photoJson);

            bool hasDictRows = (vocabRows != null && vocabRows.Length > 0)
                || (objectRows != null && objectRows.Length > 0)
                || (photoRows != null && photoRows.Length > 0);

            if (hasDictRows)
            {
                if (!storage.TryLoad<DictionaryData>(accountName, "dictionary", out DictionaryData dictData) || dictData == null)
                {
                    dictData = new DictionaryData();
                }

                if (dictData.discoveredVocabularyIds == null) dictData.discoveredVocabularyIds = new List<int>();
                if (dictData.discoveredObjectIds == null) dictData.discoveredObjectIds = new List<string>();
                if (dictData.albums == null) dictData.albums = new List<AlbumRecord>();
                if (dictData.photos == null) dictData.photos = new List<PhotoRecord>();

                bool dictChanged = false;
                if (dictData.albums.Count == 0)
                {
                    int defaultAlbumId = dictData.nextAlbumId > 0 ? dictData.nextAlbumId : 1;
                    dictData.albums.Add(new AlbumRecord
                    {
                        albumId = defaultAlbumId,
                        albumName = "Album Utama",
                        createdAt = DateTime.UtcNow.ToString("o")
                    });
                    dictData.activeAlbumId = defaultAlbumId;
                    dictData.nextAlbumId = defaultAlbumId + 1;
                    dictChanged = true;
                }
                else if (dictData.activeAlbumId <= 0)
                {
                    dictData.activeAlbumId = dictData.albums[0].albumId;
                    dictChanged = true;
                }

                if (vocabRows != null)
                {
                    var vocabSet = new HashSet<int>(dictData.discoveredVocabularyIds);
                    for (int i = 0; i < vocabRows.Length; i++)
                    {
                        int vid = vocabRows[i].vocabulary_id;
                        if (vid > 0 && vocabSet.Add(vid))
                        {
                            dictData.discoveredVocabularyIds.Add(vid);
                            dictChanged = true;
                        }
                    }
                }

                if (objectRows != null)
                {
                    var objectSet = new HashSet<string>(dictData.discoveredObjectIds, StringComparer.Ordinal);
                    for (int i = 0; i < objectRows.Length; i++)
                    {
                        string oid = objectRows[i].object_id;
                        if (!string.IsNullOrEmpty(oid) && objectSet.Add(oid))
                        {
                            dictData.discoveredObjectIds.Add(oid);
                            dictChanged = true;
                        }
                    }
                }

                if (photoRows != null)
                {
                    var photoIds = new HashSet<string>(StringComparer.Ordinal);
                    for (int i = 0; i < dictData.photos.Count; i++)
                    {
                        if (dictData.photos[i] != null && !string.IsNullOrEmpty(dictData.photos[i].photoId))
                            photoIds.Add(dictData.photos[i].photoId);
                    }

                    string photosFolder = Path.Combine(Application.persistentDataPath, "Photos");
                    for (int i = 0; i < photoRows.Length; i++)
                    {
                        CloudPhotoRow row = photoRows[i];
                        if (row == null || string.IsNullOrEmpty(row.photo_id)) continue;

                        string relPath = $"Photos/{row.photo_id}.png";
                        string localPhotoPath = Path.Combine(Application.persistentDataPath, relPath);

                        if (!File.Exists(localPhotoPath) && !string.IsNullOrEmpty(row.storage_path))
                        {
                            string downloadUrl = $"{config.ProjectUrl}/storage/v1/object/{row.storage_path.TrimStart('/')}";
                            using (UnityWebRequest dlReq = UnityWebRequest.Get(downloadUrl))
                            {
                                dlReq.SetRequestHeader("apikey", config.PublishableKey);
                                if (!string.IsNullOrEmpty(token))
                                    dlReq.SetRequestHeader("Authorization", $"Bearer {token}");
                                yield return dlReq.SendWebRequest();

                                if (dlReq.result == UnityWebRequest.Result.Success &&
                                    dlReq.downloadHandler != null &&
                                    dlReq.downloadHandler.data != null &&
                                    dlReq.downloadHandler.data.Length > 0)
                                {
                                    Directory.CreateDirectory(photosFolder);
                                    File.WriteAllBytes(localPhotoPath, dlReq.downloadHandler.data);
                                }
                            }
                        }

                        if (photoIds.Add(row.photo_id))
                        {
                            dictData.photos.Add(new PhotoRecord
                            {
                                photoId = row.photo_id,
                                albumId = dictData.activeAlbumId,
                                vocabularyId = row.vocabulary_id,
                                objectId = row.object_id,
                                objectName = row.object_name,
                                imagePath = relPath,
                                dateTaken = !string.IsNullOrEmpty(row.date_taken) ? row.date_taken : DateTime.UtcNow.ToString("o"),
                                isFavorite = row.is_favorite,
                                isLiked = row.is_liked
                            });
                            dictChanged = true;
                        }
                    }
                }

                if (dictChanged || !LocalStorage.HasLocalFile(accountName, "dictionary"))
                {
                    storage.Save(accountName, "dictionary", dictData);
                }
            }

            // 3. Pull player_themes
            string themesJson = null;
            yield return SendCloudGet(
                $"{config.ProjectUrl}/rest/v1/player_themes?user_id=eq.{escapedUserId}&select=theme_id,is_applied,purchased_at",
                token,
                json => themesJson = json);

            CloudThemeRow[] themeRows = ParseJsonArray<CloudThemeRow>(themesJson);
            if (themeRows != null && themeRows.Length > 0)
            {
                bool hasLocalThemes = LocalStorage.HasLocalFile(accountName, StoreThemeManager.SaveKey);
                if (!storage.TryLoad<StoreThemeSaveData>(accountName, StoreThemeManager.SaveKey, out StoreThemeSaveData themeData) || themeData == null)
                {
                    themeData = new StoreThemeSaveData();
                }

                if (themeData.ownedThemeIds == null) themeData.ownedThemeIds = new List<string>();
                if (themeData.purchaseRecords == null) themeData.purchaseRecords = new List<ThemePurchaseRecord>();

                bool themesChanged = false;
                var ownedSet = new HashSet<string>(themeData.ownedThemeIds, StringComparer.OrdinalIgnoreCase);
                if (ownedSet.Add(StoreThemeCatalog.DefaultThemeId))
                {
                    themeData.ownedThemeIds.Insert(0, StoreThemeCatalog.DefaultThemeId);
                    themesChanged = true;
                }

                var recordedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < themeData.purchaseRecords.Count; i++)
                {
                    if (themeData.purchaseRecords[i] != null && !string.IsNullOrEmpty(themeData.purchaseRecords[i].themeId))
                        recordedSet.Add(themeData.purchaseRecords[i].themeId);
                }

                string cloudActiveThemeId = null;
                for (int i = 0; i < themeRows.Length; i++)
                {
                    CloudThemeRow row = themeRows[i];
                    if (row == null || string.IsNullOrEmpty(row.theme_id) || StoreThemeCatalog.GetById(row.theme_id) == null)
                        continue;

                    if (ownedSet.Add(row.theme_id))
                    {
                        themeData.ownedThemeIds.Add(row.theme_id);
                        themesChanged = true;
                    }

                    if (recordedSet.Add(row.theme_id))
                    {
                        themeData.purchaseRecords.Add(new ThemePurchaseRecord
                        {
                            themeId = row.theme_id,
                            purchasedAt = !string.IsNullOrEmpty(row.purchased_at) ? row.purchased_at : DateTime.UtcNow.ToString("o")
                        });
                        themesChanged = true;
                    }

                    if (row.is_applied)
                    {
                        cloudActiveThemeId = row.theme_id;
                    }
                }

                bool localUsingDefault = string.IsNullOrEmpty(themeData.activeThemeId)
                    || string.Equals(themeData.activeThemeId, StoreThemeCatalog.DefaultThemeId, StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(cloudActiveThemeId) && (!hasLocalThemes || localUsingDefault))
                {
                    if (!string.Equals(themeData.activeThemeId, cloudActiveThemeId, StringComparison.OrdinalIgnoreCase))
                    {
                        themeData.activeThemeId = cloudActiveThemeId;
                        themesChanged = true;
                    }
                }

                if (themesChanged || !hasLocalThemes)
                {
                    storage.Save(accountName, StoreThemeManager.SaveKey, themeData);
                }
            }

            // 4. Reload live runtime managers so HomeScreen & gameplay scenes reflect merged data immediately
            if (PointRewardSystem.Instance != null)
                PointRewardSystem.Instance.LoadPoints();

            if (DictionaryManager.Instance != null)
                DictionaryManager.Instance.ReloadFromStorage();

            if (StoreThemeManager.Instance != null)
                StoreThemeManager.Instance.ReloadFromStorage();

            if (AchievementManager.Instance != null)
                AchievementManager.Instance.ReloadAndEvaluate();

            syncedUserIds.Add(userId);
            IsLoadingFromCloud = false;
            Debug.Log($"[SupabaseCloudSync] Cloud progress synced for account '{accountName}'.");
            onComplete?.Invoke(true, "Cloud progress restored.");
        }

        private IEnumerator SendCloudGet(string url, string token, Action<string> onSuccess)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                ApplyHeaders(req, token);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success && req.downloadHandler != null)
                {
                    onSuccess?.Invoke(req.downloadHandler.text);
                }
                else
                {
                    Debug.LogWarning($"[SupabaseCloudSync] Cloud GET failed ({url}): {req.error}");
                }
            }
        }

        private static T[] ParseJsonArray<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<T>();

            string trimmed = json.Trim();
            if (!trimmed.StartsWith("["))
                return Array.Empty<T>();

            try
            {
                JsonArrayWrapper<T> wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>($"{{\"items\":{trimmed}}}");
                return wrapper != null && wrapper.items != null ? wrapper.items : Array.Empty<T>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseCloudSync] Failed to parse cloud JSON array: {ex.Message}");
                return Array.Empty<T>();
            }
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

            // 3. Ambil & Sinkronkan Kepemilikan Tema Kamus + Tema Aktif ke player_themes
            List<string> ownedThemes = null;
            string activeThemeId = StoreThemeCatalog.DefaultThemeId;
            List<ThemePurchaseRecord> themePurchaseRecords = null;

            if (StoreThemeManager.Instance != null)
            {
                ownedThemes = StoreThemeManager.Instance.GetOwnedThemeIdsSnapshot();
                activeThemeId = StoreThemeManager.Instance.ActiveThemeId;
                themePurchaseRecords = StoreThemeManager.Instance.GetPurchaseRecordsSnapshot();
            }
            else if (storage.TryLoad<StoreThemeSaveData>(accountName, StoreThemeManager.SaveKey, out StoreThemeSaveData tData) && tData != null)
            {
                ownedThemes = tData.ownedThemeIds != null ? new List<string>(tData.ownedThemeIds) : new List<string>();
                if (!string.IsNullOrEmpty(tData.activeThemeId))
                    activeThemeId = tData.activeThemeId;
                themePurchaseRecords = tData.purchaseRecords;
            }
            else
            {
                ownedThemes = new List<string>();
            }

            if (!ownedThemes.Contains(StoreThemeCatalog.DefaultThemeId))
            {
                ownedThemes.Add(StoreThemeCatalog.DefaultThemeId);
            }

            foreach (string themeId in ownedThemes)
            {
                if (string.IsNullOrEmpty(themeId)) continue;
                bool isApplied = string.Equals(themeId, activeThemeId, StringComparison.OrdinalIgnoreCase);
                string purchasedAt = FindThemePurchaseTimestamp(themePurchaseRecords, themeId);
                yield return UpsertThemeToCloud(userId, token, themeId, isApplied, purchasedAt);
            }

            IsSaving = false;
            Debug.Log($"[SupabaseCloudSync] Save to Cloud berhasil diselesaikan! (Poin: {pointsToSync}, Foto: {uploadedPhotos}, Tema: {ownedThemes.Count})");
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

        private static string FindThemePurchaseTimestamp(List<ThemePurchaseRecord> records, string themeId)
        {
            if (records != null)
            {
                for (int i = 0; i < records.Count; i++)
                {
                    if (records[i] != null &&
                        string.Equals(records[i].themeId, themeId, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrEmpty(records[i].purchasedAt))
                    {
                        return records[i].purchasedAt;
                    }
                }
            }
            return DateTime.UtcNow.ToString("o");
        }

        private IEnumerator UpsertThemeToCloud(string userId, string token, string themeId, bool isApplied, string purchasedAt)
        {
            string url = $"{config.ProjectUrl}/rest/v1/player_themes?on_conflict=user_id,theme_id";
            string safeThemeId = EscapeJson(themeId);
            string safeDate = EscapeJson(!string.IsNullOrEmpty(purchasedAt) ? purchasedAt : DateTime.UtcNow.ToString("o"));
            string payload = "{"
                + $"\"user_id\":\"{userId}\","
                + $"\"theme_id\":\"{safeThemeId}\","
                + $"\"is_applied\":{(isApplied ? "true" : "false")},"
                + $"\"purchased_at\":\"{safeDate}\""
                + "}";

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
                    Debug.LogWarning($"[SupabaseCloudSync] Gagal sync tema '{themeId}' ke cloud: {req.error}");
                }
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
