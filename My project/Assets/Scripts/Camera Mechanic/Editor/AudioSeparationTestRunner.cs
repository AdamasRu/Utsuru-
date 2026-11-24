#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;
using Utsuru.DictionaryUI;

namespace Utsuru.Capture.Editor
{
    public static class AudioSeparationTestRunner
    {
        [MenuItem("Utsuru/Test Audio Separation (Vocab vs Sentence)")]
        public static void RunTests()
        {
            Debug.Log("==========================================================");
            Debug.Log("[AudioTest] MEMULAI PENGUJIAN AUDIO VOCABULARY & SENTENCE");
            Debug.Log("==========================================================");

            // 1. Data lengkap (Bicycle)
            var vocabBicycle = new VocabularyEntry
            {
                vocabularyId = 1,
                japaneseWord = "自転車",
                kana = "じてんしゃ",
                romaji = "Jitensha",
                translation = "Sepeda",
                audioPath = "Audio/Bicycle - ICK-01/vocab",
                sentenceAudioPath = "Audio/Bicycle - ICK-01/sentence"
            };

            AudioClip vClipBicycle = PolaroidCardUI.ResolveVocabAudio(vocabBicycle);
            AudioClip sClipBicycle = PolaroidCardUI.ResolveSentenceAudio(vocabBicycle);

            Debug.Log($"[AudioTest] 1. Data Lengkap: VocabClip={(vClipBicycle != null ? vClipBicycle.name : "NULL")}, SentenceClip={(sClipBicycle != null ? sClipBicycle.name : "NULL")}");

            if (vClipBicycle == null || sClipBicycle == null)
            {
                Debug.LogError("[AudioTest] GAGAL: Audio Bicycle gagal dimuat!");
                return;
            }

            if (vClipBicycle == sClipBicycle)
            {
                Debug.LogError("[AudioTest] GAGAL: Audio vocab dan sentence sama/tercampur!");
                return;
            }

            Debug.Log("[AudioTest] PASSED 1: Audio kosakata dan kalimat terpisah dan keduanya valid.");

            // 2. Data tanpa audio vocab (audioPath kosong)
            var vocabNoVocab = new VocabularyEntry
            {
                vocabularyId = 99,
                japaneseWord = "ポスター",
                audioPath = "",
                sentenceAudioPath = "Audio/Bicycle - ICK-01/sentence"
            };

            AudioClip vClipEmpty = PolaroidCardUI.ResolveVocabAudio(vocabNoVocab);
            AudioClip sClipValid = PolaroidCardUI.ResolveSentenceAudio(vocabNoVocab);

            Debug.Log($"[AudioTest] 2. Vocab Kosong: VocabClip={(vClipEmpty != null ? vClipEmpty.name : "NULL")}, SentenceClip={(sClipValid != null ? sClipValid.name : "NULL")}");

            if (vClipEmpty != null)
            {
                Debug.LogError("[AudioTest] GAGAL: Audio vocab tidak boleh fallback ke sentence audio!");
                return;
            }
            if (sClipValid == null)
            {
                Debug.LogError("[AudioTest] GAGAL: Audio sentence harus tetap valid!");
                return;
            }

            Debug.Log("[AudioTest] PASSED 2: Audio kosakata null saat audioPath kosong, tidak fallback ke kalimat.");

            // 3. Data tanpa audio sentence (sentenceAudioPath kosong)
            var vocabNoSentence = new VocabularyEntry
            {
                vocabularyId = 100,
                japaneseWord = "本",
                audioPath = "Audio/Bicycle - ICK-01/vocab",
                sentenceAudioPath = ""
            };

            AudioClip vClipValid = PolaroidCardUI.ResolveVocabAudio(vocabNoSentence);
            AudioClip sClipEmpty = PolaroidCardUI.ResolveSentenceAudio(vocabNoSentence);

            Debug.Log($"[AudioTest] 3. Sentence Kosong: VocabClip={(vClipValid != null ? vClipValid.name : "NULL")}, SentenceClip={(sClipEmpty != null ? sClipEmpty.name : "NULL")}");

            if (vClipValid == null)
            {
                Debug.LogError("[AudioTest] GAGAL: Audio vocab harus valid!");
                return;
            }
            if (sClipEmpty != null)
            {
                Debug.LogError("[AudioTest] GAGAL: Audio sentence tidak boleh fallback ke vocab audio!");
                return;
            }

            Debug.Log("[AudioTest] PASSED 3: Audio kalimat null saat sentenceAudioPath kosong, tidak fallback ke kosakata.");

            // 4. Test DictionaryVocabDetailModal resolusi
            AudioClip dVocab = DictionaryVocabDetailModal.ResolveVocabAudio(vocabBicycle);
            AudioClip dSentence = DictionaryVocabDetailModal.ResolveSentenceAudio(vocabBicycle);

            if (dVocab == null || dSentence == null || dVocab == dSentence)
            {
                Debug.LogError("[AudioTest] GAGAL: Resolusi audio di DictionaryVocabDetailModal bermasalah!");
                return;
            }

            Debug.Log("[AudioTest] PASSED 4: DictionaryVocabDetailModal berhasil memuat kedua audio secara terpisah.");

            // 5. Test Button interactable state
            GameObject tempGo = new GameObject("TestAudioButtonsUI");
            var cardUI = tempGo.AddComponent<PolaroidCardUI>();

            var vBtnGo = new GameObject("VBtn", typeof(RectTransform), typeof(Button));
            vBtnGo.transform.SetParent(tempGo.transform);
            var vBtn = vBtnGo.GetComponent<Button>();

            var sBtnGo = new GameObject("SBtn", typeof(RectTransform), typeof(Button));
            sBtnGo.transform.SetParent(tempGo.transform);
            var sBtn = sBtnGo.GetComponent<Button>();

            var so = new SerializedObject(cardUI);
            so.FindProperty("vocabAudioButton").objectReferenceValue = vBtn;
            so.FindProperty("sentenceAudioButton").objectReferenceValue = sBtn;
            so.ApplyModifiedProperties();

            // Uji saat audio tidak tersedia
            var noAudioVocab = new VocabularyEntry { audioPath = "", sentenceAudioPath = "" };
            cardUI.UpdateAudioButtonsState(noAudioVocab);

            Debug.Log($"[AudioTest] 5. State Tanpa Audio: VocabBtn.interactable={vBtn.interactable}, SentenceBtn.interactable={sBtn.interactable}");

            if (vBtn.interactable || sBtn.interactable)
            {
                Debug.LogError("[AudioTest] GAGAL: Tombol harus nonaktif (interactable = false) saat audio tidak tersedia!");
                UnityEngine.Object.DestroyImmediate(tempGo);
                return;
            }

            // Uji saat audio tersedia
            cardUI.UpdateAudioButtonsState(vocabBicycle);

            Debug.Log($"[AudioTest] 5. State Dengan Audio: VocabBtn.interactable={vBtn.interactable}, SentenceBtn.interactable={sBtn.interactable}");

            if (!vBtn.interactable || !sBtn.interactable)
            {
                Debug.LogError("[AudioTest] GAGAL: Tombol harus aktif (interactable = true) saat audio tersedia!");
                UnityEngine.Object.DestroyImmediate(tempGo);
                return;
            }

            UnityEngine.Object.DestroyImmediate(tempGo);

            Debug.Log("==========================================================");
            Debug.Log("[AudioTest] SEMUA PENGUJIAN SELESAI DENGAN STATUS: BERHASIL!");
            Debug.Log("==========================================================");
        }
    }
}
#endif
