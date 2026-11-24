using System;
using System.Collections.Generic;

namespace Utsuru.FlashcardSystem
{
    /// <summary>
    /// Spesialisasi Interface Repository untuk Flashcard berbasis foto (Pilar OOP: Abstraction & Interface Inheritance).
    /// Menambahkan kontrak query spesifik seperti filter status hafalan dan cascade delete.
    /// </summary>
    public interface IFlashcardRepository : ICRUDRepository<PhotoFlashcard, string>
    {
        /// <summary>Read: Mengambil kartu berdasarkan tingkat penguasaan hafalan.</summary>
        IReadOnlyList<PhotoFlashcard> GetByMastery(FlashcardMasteryStatus status);

        /// <summary>Read: Mencari kartu yang terikat pada foto tertentu.</summary>
        PhotoFlashcard GetByPhotoId(string photoId);

        /// <summary>Delete (Cascade): Menghapus seluruh flashcard yang terikat pada photoId yang dihapus.</summary>
        int DeleteByPhotoId(string photoId);

        /// <summary>Event notifikasi ketika data dalam repositori bertambah/berubah/berkurang.</summary>
        event Action OnDataChanged;

        /// <summary>Menyimpan perubahan memori ke disk penyimpanan.</summary>
        void Save();

        /// <summary>Memuat ulang data dari disk penyimpanan.</summary>
        void Load();
    }
}
