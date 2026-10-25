using UnityEngine;

namespace Utsuru.Capture
{
    /// <summary>
    /// Titik sambung ke database objek dan kosakata.
    /// Skrip database yang sebenarnya cukup mewarisi kelas ini dan mengisi TryGetObject.
    /// Sistem kamera tidak peduli database-nya apa (JSON, SQLite, ScriptableObject, dll).
    /// </summary>
    public abstract class ObjectDatabaseProvider : MonoBehaviour
    {
        /// <summary>
        /// Cocokkan ID objek ke data objek beserta kosakatanya.
        /// Dipanggil saat target bidikan berubah dan saat memotret (bukan tiap frame),
        /// tapi tetap sebaiknya cepat, mis. memakai Dictionary yang dimuat saat Awake.
        /// </summary>
        public abstract bool TryGetObject(string objectId, out ObjectInfo info);
    }
}
