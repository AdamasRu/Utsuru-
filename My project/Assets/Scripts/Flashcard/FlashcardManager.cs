using System;
using System.Collections.Generic;
using UnityEngine;
using Utsuru.Capture;

namespace Utsuru.FlashcardSystem
{
    /// <summary>
    /// Manager singleton/komponen penghubung untuk layanan Flashcard di scene (Pilar OOP: Service Provider).
    /// Mengelola instance IFlashcardRepository dan menghubungkannya dengan DictionaryManager (termasuk Cascade Delete).
    /// </summary>
    public class FlashcardManager : MonoBehaviour
    {
        public static FlashcardManager Instance { get; private set; }

        [Header("Konfigurasi Akun")]
        [SerializeField] private string defaultAccountId = "player1";

        private IFlashcardRepository _repository;
        private DictionaryManager _dictionaryManager;

        public IFlashcardRepository Repository => _repository;

        /// <summary>Event yang diteruskan dari repositori agar UI dapat me-refresh tampilan secara reaktif.</summary>
        public event Action OnFlashcardCollectionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeRepository();
        }

        private void Start()
        {
            HookDictionaryManager();
        }

        private void OnDestroy()
        {
            UnhookDictionaryManager();
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Inisialisasi repository dengan pola Dependency Inversion (mengacu ke interface IFlashcardRepository).
        /// </summary>
        private void InitializeRepository()
        {
            string accountId = ResolveActiveAccountId();
            _repository = new LocalJsonFlashcardRepository(accountId);
            _repository.OnDataChanged += HandleRepositoryDataChanged;
        }

        private void HandleRepositoryDataChanged()
        {
            OnFlashcardCollectionChanged?.Invoke();
        }

        /// <summary>
        /// Menemukan ID akun yang aktif (baik dari SessionManager login atau default player1).
        /// </summary>
        private string ResolveActiveAccountId()
        {
#if UNITY_EDITOR || !UNITY_EDITOR
            try
            {
                if (Utsuru.Authentication.SessionManager.Instance != null && 
                    !string.IsNullOrEmpty(Utsuru.Authentication.SessionManager.Instance.LocalProfileId))
                {
                    return Utsuru.Authentication.SessionManager.Instance.LocalProfileId;
                }
            }
            catch {}
#endif
            return defaultAccountId;
        }

        /// <summary>
        /// Menghubungkan ke DictionaryManager untuk memonitor aksi penghapusan foto (Cascade Delete).
        /// </summary>
        private void HookDictionaryManager()
        {
            if (_dictionaryManager == null)
            {
                _dictionaryManager = FindFirstObjectByType<DictionaryManager>();
            }

            if (_dictionaryManager != null)
            {
                // Hook saat dictionary berubah untuk mengecek konsistensi foto
                _dictionaryManager.OnDictionaryChanged += ValidateCascadeDelete;
            }
        }

        private void UnhookDictionaryManager()
        {
            if (_dictionaryManager != null)
            {
                _dictionaryManager.OnDictionaryChanged -= ValidateCascadeDelete;
            }

            if (_repository != null)
            {
                _repository.OnDataChanged -= HandleRepositoryDataChanged;
            }
        }

        /// <summary>
        /// Memeriksa apakah ada foto yang sudah dihapus dari album DictionaryManager,
        /// lalu menghapus flashcard terkait (Cascade Delete).
        /// </summary>
        public void ValidateCascadeDelete()
        {
            if (_dictionaryManager == null || _dictionaryManager.Photos == null || _repository == null) return;

            var existingPhotoIds = new HashSet<string>();
            foreach (var photo in _dictionaryManager.Photos)
            {
                if (!string.IsNullOrEmpty(photo.photoId))
                {
                    existingPhotoIds.Add(photo.photoId);
                }
            }

            var allCards = _repository.GetAll();
            var cardsToDelete = new List<string>();

            foreach (var card in allCards)
            {
                if (!existingPhotoIds.Contains(card.PhotoId))
                {
                    cardsToDelete.Add(card.PhotoId);
                }
            }

            foreach (var photoId in cardsToDelete)
            {
                _repository.DeleteByPhotoId(photoId);
            }
        }

        #region Helper Facade Methods (Mempermudah UI)

        /// <summary>
        /// CRUD: CREATE - Membuat kartu baru dari PhotoRecord dan ObjectInfo.
        /// </summary>
        public PhotoFlashcard CreateFlashcard(PhotoRecord photo, ObjectInfo info, string customNote = "")
        {
            if (photo == null || _repository == null) return null;

            string jpn = info?.vocabulary?.japaneseWord ?? photo.objectName;
            string kana = info?.vocabulary?.kana ?? "";
            string romaji = info?.vocabulary?.romaji ?? "";
            string trans = info?.vocabulary?.translation ?? photo.objectName;
            int vocabId = info?.vocabulary?.vocabularyId ?? photo.vocabularyId;

            var newCard = new PhotoFlashcard(
                photo.photoId,
                photo.imagePath,
                photo.objectId,
                photo.objectName,
                vocabId,
                jpn,
                kana,
                romaji,
                trans,
                customNote
            );

            _repository.Create(newCard);
            return newCard;
        }

        /// <summary>
        /// CRUD: READ - Mengambil seluruh kartu yang tersimpan.
        /// </summary>
        public IReadOnlyList<PhotoFlashcard> GetAllFlashcards()
        {
            return _repository != null ? _repository.GetAll() : Array.Empty<PhotoFlashcard>();
        }

        /// <summary>
        /// CRUD: UPDATE - Mengubah catatan personal dari kartu.
        /// </summary>
        public bool UpdateFlashcardNote(string cardId, string newNote)
        {
            if (_repository == null) return false;
            var card = _repository.GetById(cardId);
            if (card != null)
            {
                card.UpdateCustomNote(newNote);
                return _repository.Update(card);
            }
            return false;
        }

        /// <summary>
        /// CRUD: UPDATE - Mencatat hasil review belajar kartu (Belum Hafal / Sudah Hafal).
        /// </summary>
        public bool ReviewFlashcard(string cardId, bool isMastered)
        {
            if (_repository == null) return false;
            var card = _repository.GetById(cardId);
            if (card != null)
            {
                card.RecordReview(isMastered);
                return _repository.Update(card);
            }
            return false;
        }

        /// <summary>
        /// CRUD: DELETE - Menghapus kartu berdasarkan ID.
        /// </summary>
        public bool DeleteFlashcard(string cardId)
        {
            return _repository != null && _repository.Delete(cardId);
        }

        /// <summary>
        /// Memeriksa apakah foto tertentu sudah pernah dijadikan flashcard.
        /// </summary>
        public bool HasFlashcardForPhoto(string photoId)
        {
            return _repository != null && _repository.GetByPhotoId(photoId) != null;
        }

        #endregion
    }
}
