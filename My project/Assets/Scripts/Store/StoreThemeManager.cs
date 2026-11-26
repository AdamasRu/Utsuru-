using System;
using System.Collections.Generic;
using UnityEngine;
using Utsuru.Capture;

namespace Utsuru.Store
{
    /// <summary>
    /// Mengelola pembelian tema Kamus di Store (DPPL.UTSURU.012) dan penerapan tema aktif (DPPL.UTSURU.013).
    /// Data disimpan offline-first per akun melalui IDataStorage (LocalStorage).
    /// </summary>
    public class StoreThemeManager : MonoBehaviour
    {
        public static StoreThemeManager Instance { get; private set; }

        public const string SaveKey = "themes";

        [Tooltip("ID akun cadangan jika belum login via SessionManager.")]
        [SerializeField] private string fallbackAccountId = "player1";

        private IDataStorage storage;
        private StoreThemeSaveData data;
        private readonly HashSet<string> ownedThemeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Dipicu setiap kali daftar kepemilikan atau tema aktif berubah.</summary>
        public event Action OnThemesUpdated;

        /// <summary>Dipicu ketika tema aktif berubah, membawa themeId terbaru.</summary>
        public event Action<string> OnActiveThemeChanged;

        public string ActiveThemeId => data != null && !string.IsNullOrEmpty(data.activeThemeId)
            ? data.activeThemeId
            : StoreThemeCatalog.DefaultThemeId;

        public IReadOnlyList<StoreThemeDefinition> AllThemes => StoreThemeCatalog.All;
        public IReadOnlyList<StoreThemeDefinition> StoreItems => StoreThemeCatalog.StoreItems;

        public static StoreThemeManager EnsureInstance()
        {
            if (Instance != null) return Instance;

            var existing = FindFirstObjectByType<StoreThemeManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var go = new GameObject("StoreThemeManager");
            Instance = go.AddComponent<StoreThemeManager>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[StoreThemeManager] Duplicate instance detected; destroying duplicate.", this);
                Destroy(this);
                return;
            }
            Instance = this;

            storage = new LocalStorage();
            LoadThemes();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public string ResolveAccountId()
        {
            if (Utsuru.Authentication.SessionManager.Instance != null && Utsuru.Authentication.SessionManager.Instance.IsLoggedIn)
            {
                if (!string.IsNullOrEmpty(Utsuru.Authentication.SessionManager.Instance.Username))
                    return Utsuru.Authentication.SessionManager.Instance.Username;
                if (!string.IsNullOrEmpty(Utsuru.Authentication.SessionManager.Instance.LocalProfileId))
                    return Utsuru.Authentication.SessionManager.Instance.LocalProfileId;
            }
            return string.IsNullOrEmpty(fallbackAccountId) ? "player1" : fallbackAccountId;
        }

        public void LoadThemes()
        {
            if (storage == null) storage = new LocalStorage();
            string account = ResolveAccountId();

            if (!storage.TryLoad<StoreThemeSaveData>(account, SaveKey, out data) || data == null)
            {
                data = new StoreThemeSaveData();
            }

            if (data.ownedThemeIds == null)
                data.ownedThemeIds = new List<string>();

            if (data.purchaseRecords == null)
                data.purchaseRecords = new List<ThemePurchaseRecord>();

            ownedThemeIds.Clear();
            ownedThemeIds.Add(StoreThemeCatalog.DefaultThemeId);
            if (!data.ownedThemeIds.Contains(StoreThemeCatalog.DefaultThemeId))
                data.ownedThemeIds.Insert(0, StoreThemeCatalog.DefaultThemeId);

            foreach (string id in data.ownedThemeIds)
            {
                if (!string.IsNullOrEmpty(id) && StoreThemeCatalog.GetById(id) != null)
                {
                    ownedThemeIds.Add(id);
                }
            }

            if (string.IsNullOrEmpty(data.activeThemeId) || !ownedThemeIds.Contains(data.activeThemeId))
            {
                data.activeThemeId = StoreThemeCatalog.DefaultThemeId;
            }
        }

        public bool IsOwned(string themeId)
        {
            if (string.IsNullOrEmpty(themeId)) return false;
            if (string.Equals(themeId, StoreThemeCatalog.DefaultThemeId, StringComparison.OrdinalIgnoreCase))
                return true;
            return ownedThemeIds.Contains(themeId);
        }

        public bool IsApplied(string themeId)
        {
            if (string.IsNullOrEmpty(themeId)) return false;
            return string.Equals(ActiveThemeId, themeId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetCurrentPoints()
        {
            var pts = PointRewardSystem.Instance ?? FindFirstObjectByType<PointRewardSystem>();
            return pts != null ? pts.Points : 0;
        }

        /// <summary>
        /// Membeli tema di Store (DPPL.UTSURU.012-1). Memotong poin via PointRewardSystem.
        /// </summary>
        public bool BuyTheme(string themeId, out string message)
        {
            StoreThemeDefinition def = StoreThemeCatalog.GetById(themeId);
            if (def == null)
            {
                message = "Tema tidak ditemukan.";
                return false;
            }

            if (def.isDefaultFree || IsOwned(def.id))
            {
                message = $"{def.displayName} sudah dimiliki.";
                return false;
            }

            var pointsSystem = PointRewardSystem.Instance ?? FindFirstObjectByType<PointRewardSystem>();
            if (pointsSystem == null)
            {
                message = "Sistem poin belum siap.";
                return false;
            }

            if (pointsSystem.Points < def.pricePoints)
            {
                message = "Poin tidak mencukupi untuk membeli tema ini!";
                return false;
            }

            if (!pointsSystem.SpendPoints(def.pricePoints))
            {
                message = "Poin tidak mencukupi untuk membeli tema ini!";
                return false;
            }

            ownedThemeIds.Add(def.id);
            if (!data.ownedThemeIds.Contains(def.id))
                data.ownedThemeIds.Add(def.id);

            data.purchaseRecords.Add(new ThemePurchaseRecord
            {
                themeId = def.id,
                purchasedAt = DateTime.UtcNow.ToString("o")
            });

            Persist();
            Debug.Log($"[StoreThemeManager] Tema '{def.displayName}' berhasil dibeli (-{def.pricePoints} poin).");

            OnThemesUpdated?.Invoke();
            message = $"{def.displayName} berhasil dibeli! Terapkan melalui tab Theme di Dictionary.";
            return true;
        }

        /// <summary>
        /// Menerapkan tema yang sudah dimiliki ke tampilan Kamus (DPPL.UTSURU.013-1).
        /// </summary>
        public bool ApplyTheme(string themeId, out string message)
        {
            StoreThemeDefinition def = StoreThemeCatalog.GetById(themeId);
            if (def == null)
            {
                message = "Tema tidak ditemukan.";
                return false;
            }

            if (!IsOwned(def.id))
            {
                message = "Tema belum dimiliki. Beli terlebih dahulu di Store (tekan F).";
                return false;
            }

            data.activeThemeId = def.id;
            Persist();

            Debug.Log($"[StoreThemeManager] Tema aktif diubah ke '{def.displayName}'.");
            OnActiveThemeChanged?.Invoke(def.id);
            OnThemesUpdated?.Invoke();

            message = $"{def.displayName} berhasil diterapkan!";
            return true;
        }

        public List<string> GetOwnedThemeIdsSnapshot()
        {
            var result = new List<string>(ownedThemeIds);
            return result;
        }

        public List<ThemePurchaseRecord> GetPurchaseRecordsSnapshot()
        {
            return data != null && data.purchaseRecords != null
                ? new List<ThemePurchaseRecord>(data.purchaseRecords)
                : new List<ThemePurchaseRecord>();
        }

        private void Persist()
        {
            if (storage == null) storage = new LocalStorage();
            if (data == null) data = new StoreThemeSaveData();

            string account = ResolveAccountId();
            storage.Save(account, SaveKey, data);
        }

        [ContextMenu("Reset Tema ke Plain Default")]
        public void ResetThemesForTesting()
        {
            data = new StoreThemeSaveData();
            data.ownedThemeIds.Add(StoreThemeCatalog.DefaultThemeId);
            data.activeThemeId = StoreThemeCatalog.DefaultThemeId;

            ownedThemeIds.Clear();
            ownedThemeIds.Add(StoreThemeCatalog.DefaultThemeId);

            Persist();
            OnActiveThemeChanged?.Invoke(data.activeThemeId);
            OnThemesUpdated?.Invoke();
            Debug.Log($"[StoreThemeManager] Tema direset ke default untuk akun '{ResolveAccountId()}'.");
        }
    }
}
