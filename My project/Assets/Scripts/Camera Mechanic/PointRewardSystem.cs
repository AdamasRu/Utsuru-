using System;
using UnityEngine;

namespace Utsuru.Capture
{
    [Serializable]
    public class PointsData
    {
        public int points;
    }

    /// <summary>
    /// PointRewardSystem (DPPL): menyimpan dan menghitung poin pemain.
    /// Poin dipakai untuk membeli hiasan (UI-06) dan ditampilkan di HUD.
    /// Berfungsi sebagai single source of truth untuk seluruh perolehan poin (Fotografi & Dialog NPC).
    /// </summary>
    public class PointRewardSystem : MonoBehaviour
    {
        public static PointRewardSystem Instance { get; private set; }

        [Tooltip("ID akun cadangan jika belum login. Jika ada sesi aktif, otomatis menggunakan username dari SessionManager.")]
        [SerializeField] private string accountId = "player1";

        [Tooltip("Poin saat menemukan objek baru. DPPL menulis '+[N] Poin' tanpa nilai pasti, jadi ini placeholder.")]
        [SerializeField] private int pointsPerNewWord = 10;

        private const string SaveKey = "points";

        private IDataStorage storage;

        public int Points { get; private set; }
        public int PointsPerNewWord => pointsPerNewWord;

        /// <summary>Dipicu tiap total poin berubah, membawa total terbaru.</summary>
        public event Action<int> OnPointsChanged;

        /// <summary>Mendapatkan ID akun aktif saat ini.</summary>
        public string CurrentAccountId => ResolveAccountId();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Debug.LogWarning("[PointRewardSystem] Ditemukan instance duplikat. Menggunakan instance yang sudah ada.");
            }

            storage = new LocalStorage();
            LoadPoints();

            // Hubungkan otomatis dengan ScoreManager jika sudah ada di scene
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.BindPointRewardSystem(this);
            }
        }

        private void Start()
        {
            // Pastikan sinkronisasi dengan ScoreManager di tahap Start jika ScoreManager baru inisialisasi
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.BindPointRewardSystem(this);
            }

            Utsuru.CloudSync.SupabaseCloudSyncManager.EnsureInstance().SyncFromCloudIfLoggedIn();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Menentukan akun aktif secara dinamis: jika pengguna login via SessionManager,
        /// gunakan Username/ProfileId-nya; jika tidak, fallback ke accountId ("player1").
        /// </summary>
        public string ResolveAccountId()
        {
            if (Utsuru.Authentication.SessionManager.Instance != null && Utsuru.Authentication.SessionManager.Instance.IsLoggedIn)
            {
                if (!string.IsNullOrEmpty(Utsuru.Authentication.SessionManager.Instance.Username))
                    return Utsuru.Authentication.SessionManager.Instance.Username;
                if (!string.IsNullOrEmpty(Utsuru.Authentication.SessionManager.Instance.LocalProfileId))
                    return Utsuru.Authentication.SessionManager.Instance.LocalProfileId;
            }
            return string.IsNullOrEmpty(accountId) ? "player1" : accountId;
        }

        /// <summary>
        /// Memuat poin dari penyimpanan lokal berdasarkan akun aktif.
        /// </summary>
        public void LoadPoints()
        {
            if (storage == null) storage = new LocalStorage();
            string activeId = ResolveAccountId();
            if (storage.TryLoad<PointsData>(activeId, SaveKey, out PointsData loaded) && loaded != null)
            {
                Points = loaded.points;
            }
            else
            {
                Points = 0;
            }

            OnPointsChanged?.Invoke(Points);
        }

        public void EarnPoints(int amount)
        {
            if (amount <= 0) return;

            Points += amount;
            Persist();
        }

        /// <summary>Kurangi poin (untuk pembelian). Mengembalikan false bila poin tidak cukup.</summary>
        public bool SpendPoints(int amount)
        {
            if (amount <= 0 || amount > Points) return false;

            Points -= amount;
            Persist();
            return true;
        }

        private void Persist()
        {
            if (storage == null) storage = new LocalStorage();
            string activeId = ResolveAccountId();
            storage.Save(activeId, SaveKey, new PointsData { points = Points });
            OnPointsChanged?.Invoke(Points);
        }

        [ContextMenu("Reset Poin ke 0")]
        public void ResetPoints()
        {
            Points = 0;
            if (storage == null) storage = new LocalStorage();
            Persist();
            Debug.Log($"[PointRewardSystem] Poin direset ke 0 untuk akun '{ResolveAccountId()}'!");
        }
    }
}
