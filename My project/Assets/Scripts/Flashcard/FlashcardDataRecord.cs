using System;
using System.Collections.Generic;

namespace Utsuru.FlashcardSystem
{
    /// <summary>
    /// DTO (Data Transfer Object) serializable untuk penyimpanan format JSON melalui JsonUtility.
    /// Memisahkan representasi serialisasi disk dari objek domain bisnis (PBO Data Mapper pattern).
    /// </summary>
    [Serializable]
    public class FlashcardDataRecord
    {
        public string id;
        public string createdAt;
        public string lastReviewedAt;
        public int masteryStatus; // cast ke FlashcardMasteryStatus
        public int reviewCount;
        public int correctCount;

        public string photoId;
        public string photoRelativePath;
        public string objectId;
        public string objectName;
        public int vocabularyId;
        public string japaneseWord;
        public string kana;
        public string romaji;
        public string translation;
        public string customNote;

        /// <summary>
        /// Mengubah domain entity PhotoFlashcard menjadi DTO serializable.
        /// </summary>
        public static FlashcardDataRecord FromEntity(PhotoFlashcard card)
        {
            if (card == null) return null;
            return new FlashcardDataRecord
            {
                id = card.Id,
                createdAt = card.CreatedAt,
                lastReviewedAt = card.LastReviewedAt,
                masteryStatus = (int)card.MasteryStatus,
                reviewCount = card.ReviewCount,
                correctCount = card.CorrectCount,
                photoId = card.PhotoId,
                photoRelativePath = card.PhotoRelativePath,
                objectId = card.ObjectId,
                objectName = card.ObjectName,
                vocabularyId = card.VocabularyId,
                japaneseWord = card.JapaneseWord,
                kana = card.Kana,
                romaji = card.Romaji,
                translation = card.Translation,
                customNote = card.CustomNote
            };
        }

        /// <summary>
        /// Mengubah DTO serializable kembali menjadi domain entity PhotoFlashcard.
        /// </summary>
        public static PhotoFlashcard ToEntity(FlashcardDataRecord record)
        {
            if (record == null) return null;
            return new PhotoFlashcard(
                record.id,
                record.createdAt,
                record.lastReviewedAt,
                (FlashcardMasteryStatus)record.masteryStatus,
                record.reviewCount,
                record.correctCount,
                record.photoId,
                record.photoRelativePath,
                record.objectId,
                record.objectName,
                record.vocabularyId,
                record.japaneseWord,
                record.kana,
                record.romaji,
                record.translation,
                record.customNote
            );
        }
    }

    /// <summary>
    /// Wrapper list untuk JsonUtility Unity.
    /// </summary>
    [Serializable]
    public class FlashcardCollectionWrapper
    {
        public List<FlashcardDataRecord> cards = new List<FlashcardDataRecord>();
    }
}
