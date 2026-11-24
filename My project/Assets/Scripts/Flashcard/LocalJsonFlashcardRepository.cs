using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Utsuru.FlashcardSystem
{
    /// <summary>
    /// Implementasi konkret IFlashcardRepository dengan penyimpanan berkas JSON lokal (PBO Repository Pattern).
    /// Menyimpan berkas di Application.persistentDataPath/Save/{accountId}_flashcards.json.
    /// </summary>
    public class LocalJsonFlashcardRepository : IFlashcardRepository
    {
        private const string FolderName = "Save";
        private const string FileSuffix = "_flashcards.json";

        private readonly string _accountId;
        private readonly List<PhotoFlashcard> _cards = new List<PhotoFlashcard>();
        private readonly Dictionary<string, PhotoFlashcard> _cardMap = new Dictionary<string, PhotoFlashcard>();

        public event Action OnDataChanged;

        public LocalJsonFlashcardRepository(string accountId = "player1")
        {
            _accountId = string.IsNullOrEmpty(accountId) ? "player1" : accountId;
            Load();
        }

        private string GetFilePath()
        {
            string sanitizedAccount = SanitizeFileName(_accountId);
            return Path.Combine(Application.persistentDataPath, FolderName, $"{sanitizedAccount}{FileSuffix}");
        }

        #region ICRUDRepository Implementation

        public void Create(PhotoFlashcard entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // Jika kartu dengan ID yang sama sudah ada, lewati atau perbarui
            if (_cardMap.ContainsKey(entity.Id))
            {
                Update(entity);
                return;
            }

            _cards.Insert(0, entity); // Stack order: kartu terbaru di atas
            _cardMap[entity.Id] = entity;

            Save();
            OnDataChanged?.Invoke();
            Debug.Log($"[LocalJsonFlashcardRepository] Kartu baru dibuat: '{entity.DisplayJapanese}' (ID: {entity.Id})");
        }

        public PhotoFlashcard GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _cardMap.TryGetValue(id, out PhotoFlashcard card);
            return card;
        }

        public IReadOnlyList<PhotoFlashcard> GetAll()
        {
            return _cards.AsReadOnly();
        }

        public bool Update(PhotoFlashcard entity)
        {
            if (entity == null || string.IsNullOrEmpty(entity.Id)) return false;

            if (_cardMap.TryGetValue(entity.Id, out PhotoFlashcard existing))
            {
                int index = _cards.IndexOf(existing);
                if (index >= 0)
                {
                    _cards[index] = entity;
                }
                _cardMap[entity.Id] = entity;

                Save();
                OnDataChanged?.Invoke();
                Debug.Log($"[LocalJsonFlashcardRepository] Kartu berhasil diupdate: '{entity.DisplayJapanese}' (ID: {entity.Id})");
                return true;
            }

            return false;
        }

        public bool Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            if (_cardMap.TryGetValue(id, out PhotoFlashcard card))
            {
                _cards.Remove(card);
                _cardMap.Remove(id);

                Save();
                OnDataChanged?.Invoke();
                Debug.Log($"[LocalJsonFlashcardRepository] Kartu dihapus: ID {id}");
                return true;
            }

            return false;
        }

        #endregion

        #region IFlashcardRepository Extended Implementation

        public IReadOnlyList<PhotoFlashcard> GetByMastery(FlashcardMasteryStatus status)
        {
            var filtered = new List<PhotoFlashcard>();
            foreach (var card in _cards)
            {
                if (card.MasteryStatus == status)
                    filtered.Add(card);
            }
            return filtered;
        }

        public PhotoFlashcard GetByPhotoId(string photoId)
        {
            if (string.IsNullOrEmpty(photoId)) return null;
            return _cards.Find(c => c.PhotoId == photoId);
        }

        /// <summary>
        /// Cascade Delete: Menghapus kartu yang foto aslinya dihapus dari galeri album.
        /// </summary>
        public int DeleteByPhotoId(string photoId)
        {
            if (string.IsNullOrEmpty(photoId)) return 0;

            int removedCount = _cards.RemoveAll(c =>
            {
                if (c.PhotoId == photoId)
                {
                    _cardMap.Remove(c.Id);
                    return true;
                }
                return false;
            });

            if (removedCount > 0)
            {
                Save();
                OnDataChanged?.Invoke();
                Debug.Log($"[LocalJsonFlashcardRepository] Cascade Delete: {removedCount} kartu dihapus karena foto '{photoId}' telah dihapus.");
            }

            return removedCount;
        }

        public void Save()
        {
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, FolderName);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var wrapper = new FlashcardCollectionWrapper();
                foreach (var card in _cards)
                {
                    wrapper.cards.Add(FlashcardDataRecord.FromEntity(card));
                }

                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(GetFilePath(), json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalJsonFlashcardRepository] Gagal menyimpan flashcards: {ex.Message}");
            }
        }

        public void Load()
        {
            _cards.Clear();
            _cardMap.Clear();

            try
            {
                string path = GetFilePath();
                if (!File.Exists(path)) return;

                string json = File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<FlashcardCollectionWrapper>(json);

                if (wrapper != null && wrapper.cards != null)
                {
                    foreach (var record in wrapper.cards)
                    {
                        var entity = FlashcardDataRecord.ToEntity(record);
                        if (entity != null)
                        {
                            _cards.Add(entity);
                            _cardMap[entity.Id] = entity;
                        }
                    }
                }

                Debug.Log($"[LocalJsonFlashcardRepository] Berhasil memuat {_cards.Count} flashcards dari disk.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalJsonFlashcardRepository] Gagal memuat flashcards: {ex.Message}");
            }
        }

        #endregion

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "player1";
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }
            return value;
        }
    }
}
