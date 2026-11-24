using System;

namespace Utsuru.FlashcardSystem
{
    /// <summary>
    /// Entitas Flashcard berbasis foto hasil jepretan kamera dalam game (Pilar OOP: Inheritance).
    /// Menghubungkan metadata foto pemain dengan entitas kosakata bahasa Jepang.
    /// </summary>
    public class PhotoFlashcard : Flashcard
    {
        private string _photoId;
        private string _photoRelativePath;
        private string _objectId;
        private string _objectName;
        private int _vocabularyId;
        private string _japaneseWord;
        private string _kana;
        private string _romaji;
        private string _translation;
        private string _customNote;

        public string PhotoId => _photoId;
        public string PhotoRelativePath => _photoRelativePath;
        public string ObjectId => _objectId;
        public string ObjectName => _objectName;
        public int VocabularyId => _vocabularyId;
        public string JapaneseWord => _japaneseWord;
        public string Kana => _kana;
        public string Romaji => _romaji;
        public string Translation => _translation;
        public string CustomNote => _customNote;

        /// <summary>
        /// Format tampilan Jepang: Kanji / Kana jika ada kana, atau Kanji saja.
        /// </summary>
        public string DisplayJapanese => string.IsNullOrEmpty(_kana) ? _japaneseWord : $"{_japaneseWord} / {_kana}";

        /// <summary>
        /// Konstruktor untuk pembuatan kartu baru dari jepretan foto kamera.
        /// </summary>
        public PhotoFlashcard(
            string photoId,
            string photoRelativePath,
            string objectId,
            string objectName,
            int vocabularyId,
            string japaneseWord,
            string kana,
            string romaji,
            string translation,
            string customNote = "",
            string cardId = null) : base(cardId)
        {
            _photoId = photoId;
            _photoRelativePath = photoRelativePath;
            _objectId = objectId;
            _objectName = objectName;
            _vocabularyId = vocabularyId;
            _japaneseWord = japaneseWord;
            _kana = kana;
            _romaji = romaji;
            _translation = translation;
            _customNote = customNote ?? string.Empty;
        }

        /// <summary>
        /// Konstruktor hidrasi dari penyimpanan disk (JSON DTO).
        /// </summary>
        public PhotoFlashcard(
            string id,
            string createdAt,
            string lastReviewedAt,
            FlashcardMasteryStatus status,
            int reviewCount,
            int correctCount,
            string photoId,
            string photoRelativePath,
            string objectId,
            string objectName,
            int vocabularyId,
            string japaneseWord,
            string kana,
            string romaji,
            string translation,
            string customNote) 
            : base(id, createdAt, lastReviewedAt, status, reviewCount, correctCount)
        {
            _photoId = photoId;
            _photoRelativePath = photoRelativePath;
            _objectId = objectId;
            _objectName = objectName;
            _vocabularyId = vocabularyId;
            _japaneseWord = japaneseWord;
            _kana = kana;
            _romaji = romaji;
            _translation = translation;
            _customNote = customNote ?? string.Empty;
        }

        /// <summary>
        /// Mengubah catatan kustom/petunjuk hafalan pemain (Operasi UPDATE CRUD).
        /// </summary>
        public void UpdateCustomNote(string newNote)
        {
            _customNote = newNote ?? string.Empty;
        }

        /// <summary>
        /// Mengganti foto referensi kartu jika pemain memilih foto alternatif (Operasi UPDATE CRUD).
        /// </summary>
        public void UpdatePhoto(string newPhotoId, string newPhotoPath)
        {
            _photoId = newPhotoId;
            _photoRelativePath = newPhotoPath;
        }

        /// <summary>
        /// Implementasi polimorfik tampilan bagian depan kartu (Pilar OOP: Polymorphism).
        /// </summary>
        public override string GetFrontDisplay()
        {
            return $"Apa bahasa Jepang dari benda ini? ({_objectName})";
        }

        /// <summary>
        /// Implementasi polimorfik tampilan bagian belakang kartu (Pilar OOP: Polymorphism).
        /// </summary>
        public override string GetBackDisplay()
        {
            return $"{DisplayJapanese}\n[{_romaji}]\nArtinya: {_translation}";
        }
    }
}
