using System;
using UnityEngine;

namespace Utsuru.Capture
{
    /// <summary>Data kosakata, mengikuti tbl_vocabulary di DPPL.</summary>
    [Serializable]
    public class VocabularyEntry
    {
        public int vocabularyId;

        [Tooltip("Penulisan Kanji, mis. 猫")]
        public string japaneseWord;

        [Tooltip("Penulisan Kana, mis. ねこ (opsional)")]
        public string kana;

        public string romaji;

        [Tooltip("Arti dalam bahasa Indonesia")]
        public string translation;

        [TextArea] public string exampleSentence;

        [Tooltip("Path audio pelafalan kata di folder Resources tanpa ekstensi, mis. Audio/Bicycle - ICK-01/vocab")]
        public string audioPath;

        [Tooltip("Opsional: AudioClip langsung. Jika diisi, dipakai menggantikan audioPath.")]
        public AudioClip audioClip;

        [Tooltip("Path audio contoh kalimat di folder Resources tanpa ekstensi, mis. Audio/Bicycle - ICK-01/sentence")]
        public string sentenceAudioPath;

        [Tooltip("Opsional: AudioClip kalimat langsung. Jika diisi, dipakai menggantikan sentenceAudioPath.")]
        public AudioClip sentenceAudioClip;

        public string category;

        /// <summary>Teks untuk Label Kanji/Kana di Polaroid, mis. "猫 / ねこ".</summary>
        public string DisplayJapanese =>
            string.IsNullOrEmpty(kana) ? japaneseWord : $"{japaneseWord} / {kana}";
    }

    /// <summary>Hasil pencocokan ID objek ke database (tbl_object + tbl_vocabulary).</summary>
    [Serializable]
    public class ObjectInfo
    {
        [Tooltip("ID unik objek, sama dengan Object Id pada InteractableObject di scene.")]
        public string objectId;

        [Tooltip("Nama objek yang tampil dekat crosshair.")]
        public string objectName;

        public VocabularyEntry vocabulary = new VocabularyEntry();
    }

    /// <summary>Wrapper untuk serialisasi JSON daftar ObjectInfo (kompatibel JsonUtility).</summary>
    [Serializable]
    public class VocabularyCatalogWrapper
    {
        public System.Collections.Generic.List<ObjectInfo> items = new System.Collections.Generic.List<ObjectInfo>();
    }
}
