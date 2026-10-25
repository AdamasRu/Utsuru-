using System.Collections.Generic;
using UnityEngine;

namespace Utsuru.Capture
{
    /// <summary>
    /// SEMENTARA: database yang diisi lewat Inspector, untuk menguji kamera.
    /// Saat database yang sebenarnya siap, ganti komponen ini dengan skrip turunan ObjectDatabaseProvider.
    /// </summary>
    public class InspectorObjectDatabase : ObjectDatabaseProvider
    {
        [SerializeField] private List<ObjectInfo> entries = new List<ObjectInfo>();

        private readonly Dictionary<string, ObjectInfo> lookup = new Dictionary<string, ObjectInfo>();

        private void Awake()
        {
            // Seed default entries agar pengujian objek jalanan langsung memiliki kosakata lengkap
            EnsureDefault("ICK-01", "Sepeda", 1, "自転車", "じてんしゃ", "Jitensha", "Sepeda", "駅前に自転車がたくさん止まっています。", "Transport");
            EnsureDefault("ICK-02", "Mesin Penjual Otomatis", 2, "自動販売機", "じどうはんばいき", "Jidouhanbaiki", "Mesin Penjual Otomatis", "自動販売機で冷たいお茶を買いました。", "Sign");

            foreach (ObjectInfo entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.objectId)) continue;
                lookup[entry.objectId] = entry; // Timpa dengan entri dari Inspector jika ada
            }
        }

        private void EnsureDefault(string id, string name, int vocabId, string jp, string kana, string romaji, string arti, string example, string cat)
        {
            if (!lookup.ContainsKey(id))
            {
                lookup[id] = new ObjectInfo
                {
                    objectId = id,
                    objectName = name,
                    vocabulary = new VocabularyEntry
                    {
                        vocabularyId = vocabId,
                        japaneseWord = jp,
                        kana = kana,
                        romaji = romaji,
                        translation = arti,
                        exampleSentence = example,
                        category = cat
                    }
                };
            }
        }

        public override bool TryGetObject(string objectId, out ObjectInfo info)
        {
            if (string.IsNullOrEmpty(objectId))
            {
                info = null;
                return false;
            }
            return lookup.TryGetValue(objectId, out info);
        }
    }
}
