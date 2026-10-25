namespace Utsuru.Capture
{
    /// <summary>
    /// Abstraksi lapisan persistensi data (DPPL: IDataStorage).
    /// Implementasi sekarang: LocalStorage (offline-first). CloudDatabase bisa menyusul tanpa mengubah pemanggil.
    /// </summary>
    public interface IDataStorage
    {
        bool Save<T>(string accountId, string key, T data);
        bool TryLoad<T>(string accountId, string key, out T data);
    }
}
