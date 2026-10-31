using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utsuru.Capture
{
    /// <summary>Album foto (tbl_album di DPPL).</summary>
    [Serializable]
    public class AlbumRecord
    {
        public int albumId;
        public string albumName;
        public string createdAt; // ISO 8601
    }

    /// <summary>Seluruh data Dictionary milik pemain yang disimpan lewat IDataStorage.</summary>
    [Serializable]
    public class DictionaryData
    {
        public List<string> discoveredObjectIds = new List<string>();
        public List<int> discoveredVocabularyIds = new List<int>();
        public List<AlbumRecord> albums = new List<AlbumRecord>();
        public List<PhotoRecord> photos = new List<PhotoRecord>();
        public int activeAlbumId;
        public int nextAlbumId = 1;
    }

    /// <summary>
    /// DictionaryManager (DPPL): mencatat objek/kosakata yang sudah ditemukan,
    /// album, dan foto. Data tersimpan otomatis setiap ada perubahan.
    /// </summary>
    public class DictionaryManager : MonoBehaviour
    {
        [Tooltip("Sementara tetap. Nanti diganti ID akun dari sistem login.")]
        [SerializeField] private string accountId = "player1";

        [SerializeField] private string defaultAlbumName = "Album Utama";

        private const string SaveKey = "dictionary";

        private IDataStorage storage;
        private DictionaryData data;
        private readonly HashSet<string> discoveredObjects = new HashSet<string>();

        /// <summary>Dipicu setiap data Dictionary berubah (untuk refresh UI).</summary>
        public event Action OnDictionaryChanged;

        public int ActiveAlbumId => data.activeAlbumId;
        public IReadOnlyList<AlbumRecord> Albums => data.albums;
        public IReadOnlyList<PhotoRecord> Photos => data.photos;
        public int DiscoveredVocabularyCount => data.discoveredVocabularyIds.Count;

        private void Awake()
        {
            storage = new LocalStorage();

            if (!storage.TryLoad<DictionaryData>(accountId, SaveKey, out data) || data == null)
                data = new DictionaryData();

            foreach (string id in data.discoveredObjectIds) discoveredObjects.Add(id);

            EnsureDefaultAlbum();
        }

        public bool IsObjectDiscovered(string objectId)
        {
            return discoveredObjects.Contains(objectId);
        }

        public bool IsVocabularyDiscovered(int vocabularyId)
        {
            return data.discoveredVocabularyIds.Contains(vocabularyId);
        }

        /// <summary>Catat objek/kosakata sebagai Discovered (terjadi saat shutter, walau foto tidak disimpan).</summary>
        public void MarkDiscovered(string objectId, int vocabularyId)
        {
            if (discoveredObjects.Add(objectId)) data.discoveredObjectIds.Add(objectId);
            if (!data.discoveredVocabularyIds.Contains(vocabularyId)) data.discoveredVocabularyIds.Add(vocabularyId);
            Persist();
        }

        /// <summary>Simpan foto ke album (insert tbl_photo).</summary>
        public void AddPhoto(PhotoRecord record)
        {
            data.photos.Add(record);
            Persist();
        }

        public AlbumRecord CreateAlbum(string albumName)
        {
            var album = new AlbumRecord
            {
                albumId = data.nextAlbumId++,
                albumName = albumName,
                createdAt = DateTime.Now.ToString("o")
            };
            data.albums.Add(album);
            if (data.activeAlbumId == 0) data.activeAlbumId = album.albumId;

            Persist();
            return album;
        }

        public void SetActiveAlbum(int albumId)
        {
            data.activeAlbumId = albumId;
            Persist();
        }

        // Foto selalu butuh album aktif, jadi pastikan minimal ada satu album.
        private void EnsureDefaultAlbum()
        {
            if (data.albums.Count == 0)
            {
                CreateAlbum(defaultAlbumName);
                return;
            }

            bool activeExists = data.albums.Exists(a => a.albumId == data.activeAlbumId);
            if (!activeExists)
            {
                data.activeAlbumId = data.albums[0].albumId;
                Persist();
            }
        }

        private void Persist()
        {
            storage.Save(accountId, SaveKey, data);
            OnDictionaryChanged?.Invoke();
        }

        /// <summary>Menu klik kanan di Inspector untuk mereset seluruh progres kata dan album saat testing.</summary>
        [ContextMenu("Reset Save Data (Hapus Progres)")]
        public void ResetSaveData()
        {
            discoveredObjects.Clear();
            data = new DictionaryData();
            EnsureDefaultAlbum();
            if (storage == null) storage = new LocalStorage();
            Persist();
            Debug.Log("[DictionaryManager] Data progres berhasil direset!");
        }
    }
}
