using System.Collections.Generic;

namespace Utsuru.FlashcardSystem
{
    /// <summary>
    /// Generic Interface untuk repositori data dengan prinsip CRUD (Pilar OOP: Abstraction).
    /// Memenuhi kaidah pola desain Repository Pattern.
    /// </summary>
    /// <typeparam name="TEntity">Tipe entitas objek</typeparam>
    /// <typeparam name="TKey">Tipe primary key</typeparam>
    public interface ICRUDRepository<TEntity, TKey>
    {
        /// <summary>Create: Menambahkan entitas baru ke repositori.</summary>
        void Create(TEntity entity);

        /// <summary>Read: Mengambil entitas berdasarkan ID unik.</summary>
        TEntity GetById(TKey id);

        /// <summary>Read: Mengambil seluruh entitas yang tersimpan.</summary>
        IReadOnlyList<TEntity> GetAll();

        /// <summary>Update: Memperbarui data entitas yang sudah ada.</summary>
        bool Update(TEntity entity);

        /// <summary>Delete: Menghapus entitas berdasarkan ID unik.</summary>
        bool Delete(TKey id);
    }
}
