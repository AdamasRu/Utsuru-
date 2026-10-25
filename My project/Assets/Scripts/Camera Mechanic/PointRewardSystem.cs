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
    /// </summary>
    public class PointRewardSystem : MonoBehaviour
    {
        [Tooltip("Sementara tetap. Nanti diganti ID akun dari sistem login.")]
        [SerializeField] private string accountId = "player1";

        [Tooltip("Poin saat menemukan objek baru. DPPL menulis '+[N] Poin' tanpa nilai pasti, jadi ini placeholder.")]
        [SerializeField] private int pointsPerNewWord = 10;

        private const string SaveKey = "points";

        private IDataStorage storage;

        public int Points { get; private set; }
        public int PointsPerNewWord => pointsPerNewWord;

        /// <summary>Dipicu tiap total poin berubah, membawa total terbaru.</summary>
        public event Action<int> OnPointsChanged;

        private void Awake()
        {
            storage = new LocalStorage();
            if (storage.TryLoad<PointsData>(accountId, SaveKey, out PointsData loaded) && loaded != null)
                Points = loaded.points;
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
            storage.Save(accountId, SaveKey, new PointsData { points = Points });
            OnPointsChanged?.Invoke(Points);
        }

        [ContextMenu("Reset Poin ke 0")]
        public void ResetPoints()
        {
            Points = 0;
            if (storage == null) storage = new LocalStorage();
            Persist();
            Debug.Log("[PointRewardSystem] Poin direset ke 0!");
        }
    }
}
