using System;
using System.IO;
using UnityEngine;

namespace Utsuru.Capture
{
    /// <summary>Metadata foto, mengikuti tbl_photo di DPPL. Aman untuk JsonUtility.</summary>
    [Serializable]
    public class PhotoRecord
    {
        public string photoId;
        public int albumId;
        public int vocabularyId;
        public string objectId;
        public string objectName;
        public string imagePath;   // relatif terhadap persistentDataPath
        public string dateTaken;   // ISO 8601
    }

    /// <summary>
    /// Menyimpan snapshot sebagai berkas PNG di persistentDataPath (offline-first, DPPL Local Save).
    /// Database/IDataStorage hanya menyimpan PhotoRecord (path), bukan gambarnya.
    /// </summary>
    public static class PhotoFileStore
    {
        private const string Folder = "Photos";

        public static PhotoRecord Save(PhotoCaptureResult result, int albumId)
        {
            string directory = Path.Combine(Application.persistentDataPath, Folder);
            Directory.CreateDirectory(directory);

            string photoId = Guid.NewGuid().ToString("N");
            string fileName = $"{result.TakenAt:yyyyMMdd_HHmmss}_{Sanitize(result.Info.objectId)}_{photoId.Substring(0, 6)}.png";

            File.WriteAllBytes(Path.Combine(directory, fileName), result.Snapshot.EncodeToPNG());
            Debug.Log($"[PhotoFileStore] Foto disimpan di: {Path.Combine(directory, fileName)}");

            return new PhotoRecord
            {
                photoId = photoId,
                albumId = albumId,
                vocabularyId = result.Info.vocabulary.vocabularyId,
                objectId = result.Info.objectId,
                objectName = result.Info.objectName,
                imagePath = Folder + "/" + fileName,
                dateTaken = result.TakenAt.ToString("o")
            };
        }

        /// <summary>Muat kembali foto untuk galeri Dictionary. Mengembalikan null bila berkas tidak ada.</summary>
        public static Texture2D Load(string relativePath)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, relativePath);
            if (!File.Exists(fullPath)) return null;

            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(fullPath));
            return texture;
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }
    }
}
