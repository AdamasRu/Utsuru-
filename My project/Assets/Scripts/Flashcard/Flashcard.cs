using System;

namespace Utsuru.FlashcardSystem
{
    /// <summary>
    /// Base Class abstrak untuk entitas Flashcard (Pilar OOP: Abstraction & Encapsulation).
    /// Menyimpan state umum kartu belajar serta aturan bisnis review kartu.
    /// </summary>
    public abstract class Flashcard
    {
        // Enkapsulasi: field private / protected dengan getter publik
        protected string _id;
        protected string _createdAt;
        protected string _lastReviewedAt;
        protected FlashcardMasteryStatus _masteryStatus;
        protected int _reviewCount;
        protected int _correctCount;

        public string Id => _id;
        public string CreatedAt => _createdAt;
        public string LastReviewedAt => _lastReviewedAt;
        public FlashcardMasteryStatus MasteryStatus => _masteryStatus;
        public int ReviewCount => _reviewCount;
        public int CorrectCount => _correctCount;

        /// <summary>
        /// Konstruktor inisialisasi kartu baru.
        /// </summary>
        protected Flashcard(string id, string createdAt = null)
        {
            _id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            _createdAt = string.IsNullOrEmpty(createdAt) ? DateTime.UtcNow.ToString("o") : createdAt;
            _lastReviewedAt = string.Empty;
            _masteryStatus = FlashcardMasteryStatus.New;
            _reviewCount = 0;
            _correctCount = 0;
        }

        /// <summary>
        /// Konstruktor hidrasi dari penyimpanan (Local Storage).
        /// </summary>
        protected Flashcard(string id, string createdAt, string lastReviewedAt, FlashcardMasteryStatus status, int reviewCount, int correctCount)
        {
            _id = id;
            _createdAt = createdAt;
            _lastReviewedAt = lastReviewedAt;
            _masteryStatus = status;
            _reviewCount = reviewCount;
            _correctCount = correctCount;
        }

        /// <summary>
        /// Logika bisnis saat kartu di-review oleh pemain (Pilar OOP: Encapsulation).
        /// </summary>
        /// <param name="isMastered">True jika pemain menekan 'Sudah Hafal', False jika 'Belum Hafal'</param>
        public virtual void RecordReview(bool isMastered)
        {
            _reviewCount++;
            _lastReviewedAt = DateTime.UtcNow.ToString("o");

            if (isMastered)
            {
                _correctCount++;
                _masteryStatus = FlashcardMasteryStatus.Mastered;
            }
            else
            {
                _masteryStatus = FlashcardMasteryStatus.Learning;
            }
        }

        /// <summary>
        /// Reset progres hafalan kartu kembali ke status awal (New).
        /// </summary>
        public virtual void ResetProgress()
        {
            _masteryStatus = FlashcardMasteryStatus.New;
            _reviewCount = 0;
            _correctCount = 0;
            _lastReviewedAt = string.Empty;
        }

        // Method abstrak yang wajib diimplementasikan oleh subclass (Pilar OOP: Polymorphism)
        public abstract string GetFrontDisplay();
        public abstract string GetBackDisplay();
    }
}
