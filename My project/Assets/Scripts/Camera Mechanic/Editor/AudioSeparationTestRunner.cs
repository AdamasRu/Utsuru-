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

            // 5. Test Button interactable state & isolasi tombol
            GameObject tempGo = new GameObject("TestAudioButtonsUI");
            var cardUI = tempGo.AddComponent<PolaroidCardUI>();

            var vBtnGo = new GameObject("BtnVocabAudio", typeof(RectTransform), typeof(Button));
            vBtnGo.transform.SetParent(tempGo.transform);
            var vBtn = vBtnGo.GetComponent<Button>();

            var sBtnGo = new GameObject("BtnAudio", typeof(RectTransform), typeof(Button));
            sBtnGo.transform.SetParent(tempGo.transform);
            var sBtn = sBtnGo.GetComponent<Button>();

            var so = new SerializedObject(cardUI);
            so.FindProperty("vocabAudioButton").objectReferenceValue = vBtn;
            so.FindProperty("sentenceAudioButton").objectReferenceValue = sBtn;
            so.FindProperty("audioButton").objectReferenceValue = sBtn;
            so.ApplyModifiedProperties();

            // 5a. Uji saat audio kosakata kosong (audioPath = "")
            cardUI.UpdateAudioButtonsState(vocabNoVocab);
            Debug.Log($"[AudioTest] 5a. Vocab Kosong: VocabBtn.interactable={vBtn.interactable}, SentenceBtn.interactable={sBtn.interactable}");
            if (vBtn.interactable)
            {
                Debug.LogError("[AudioTest] GAGAL: Tombol kosakata harus dinonaktifkan saat audioPath kosong!");
                UnityEngine.Object.DestroyImmediate(tempGo);
                return;
            }
            if (!sBtn.interactable)
            {
                Debug.LogError("[AudioTest] GAGAL: Tombol Example Sentence TIDAK BOLEH terganggu saat audioPath kosong!");
                UnityEngine.Object.DestroyImmediate(tempGo);
                return;
            }
            Debug.Log("[AudioTest] PASSED 5a: Tombol kosakata nonaktif tanpa mengganggu tombol Example Sentence.");

            // 5b. Uji saat audio contoh kalimat kosong (sentenceAudioPath = "")
            cardUI.UpdateAudioButtonsState(vocabNoSentence);
            Debug.Log($"[AudioTest] 5b. Sentence Kosong: VocabBtn.interactable={vBtn.interactable}, SentenceBtn.interactable={sBtn.interactable}");
            if (!vBtn.interactable)
            {
                Debug.LogError("[AudioTest] GAGAL: Tombol kosakata TIDAK BOLEH terganggu saat sentenceAudioPath kosong!");
                UnityEngine.Object.DestroyImmediate(tempGo);
                return;
            }
            if (sBtn.interactable)
            {
                Debug.LogError("[AudioTest] GAGAL: Tombol kalimat harus dinonaktifkan saat sentenceAudioPath kosong!");
                UnityEngine.Object.DestroyImmediate(tempGo);
                return;
            }
            Debug.Log("[AudioTest] PASSED 5b: Tombol kalimat nonaktif tanpa mengganggu tombol kosakata.");

            // 5c. Uji saat kedua audio tersedia
            cardUI.UpdateAudioButtonsState(vocabBicycle);
            Debug.Log($"[AudioTest] 5c. Kedua Audio Ada: VocabBtn.interactable={vBtn.interactable}, SentenceBtn.interactable={sBtn.interactable}");
            if (!vBtn.interactable || !sBtn.interactable)
            {
                Debug.LogError("[AudioTest] GAGAL: Kedua tombol harus aktif saat kedua audio tersedia!");
                UnityEngine.Object.DestroyImmediate(tempGo);
                return;
            }
            Debug.Log("[AudioTest] PASSED 5c: Kedua tombol aktif secara independen.");

            UnityEngine.Object.DestroyImmediate(tempGo);

            // 6. Uji UI Tombol Kosakata Bulat Biru (tanpa label/teks anak)
            GameObject testUiRoot = new GameObject("TestVocabCircleUI");
            var testCardUI = testUiRoot.AddComponent<PolaroidCardUI>();
            var kanjiGo = new GameObject("KanjiLabel", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            kanjiGo.transform.SetParent(testUiRoot.transform);
            var kTmp = kanjiGo.GetComponent<TMPro.TextMeshProUGUI>();
            kTmp.text = "道";
            var kRt = kanjiGo.GetComponent<RectTransform>();
            kRt.sizeDelta = new Vector2(265f, 32f);
            kRt.anchoredPosition = new Vector2(-215f, -320f);

            var soTest = new SerializedObject(testCardUI);
            soTest.FindProperty("kanjiLabel").objectReferenceValue = kTmp;
            soTest.ApplyModifiedProperties();

            testCardUI.EnsureVocabAudioButton();
            Transform vocabBtnTr = testUiRoot.transform.Find("BtnVocabAudio");
            if (vocabBtnTr == null)
            {
                Debug.LogError("[AudioTest] GAGAL: BtnVocabAudio tidak terbentuk!");
                UnityEngine.Object.DestroyImmediate(testUiRoot);
                return;
            }

            if (vocabBtnTr.childCount > 0)
            {
                Debug.LogError("[AudioTest] GAGAL: Tombol kosakata tidak boleh memiliki teks atau label anak!");
                UnityEngine.Object.DestroyImmediate(testUiRoot);
                return;
            }

            var btnRt = vocabBtnTr.GetComponent<RectTransform>();
            if (btnRt.sizeDelta.x > 50f || btnRt.sizeDelta.y > 50f)
            {
                Debug.LogError($"[AudioTest] GAGAL: Ukuran tombol kosakata ({btnRt.sizeDelta}) bukan berupa ikon tombol bulat kecil!");
                UnityEngine.Object.DestroyImmediate(testUiRoot);
                return;
            }

            Debug.Log($"[AudioTest] PASSED 6: Tombol kosakata bulat biru terverifikasi tanpa label anak, ukuran: {btnRt.sizeDelta}");
            UnityEngine.Object.DestroyImmediate(testUiRoot);

            Debug.Log("==========================================================");
            Debug.Log("[AudioTest] SEMUA PENGUJIAN SELESAI DENGAN STATUS: BERHASIL!");
            Debug.Log("==========================================================");
        }
    }
}
#endif
