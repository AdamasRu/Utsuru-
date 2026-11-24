using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Modal detail kosakata saat kartu kosakata yang Discovered diklik di Tab Kosakata.
    /// Memuat pelafalan audio native dan contoh kalimat secara terpisah.
    /// </summary>
    public class DictionaryVocabDetailModal : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject modalRoot;

        [Header("UI Elemen")]
        [SerializeField] private TMP_Text kanjiLabel;
        [SerializeField] private TMP_Text romajiLabel;
        [SerializeField] private TMP_Text meaningLabel;
        [SerializeField] private TMP_Text categoryLabel;
        [SerializeField] private TMP_Text exampleLabel;
        [SerializeField] private Toggle exampleToggle;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropCloseButton;
        [SerializeField] private AudioSource audioSource;

        [Header("Audio UI (Vocabulary & Example Sentence)")]
        [Tooltip("Tombol audio pelafalan kosakata bahasa Jepang (dari audioPath/audioClip).")]
        [SerializeField] private Button vocabAudioButton;
        [Tooltip("Tombol audio contoh kalimat bahasa Jepang (dari sentenceAudioPath/sentenceAudioClip).")]
        [SerializeField] private Button sentenceAudioButton;
        [Tooltip("Tombol audio utama yang sudah ada di Inspector (dipertahankan untuk kompatibilitas).")]
        [SerializeField] private Button audioButton;

        [Header("Pesan Audio")]
        [SerializeField] private string vocabAudioErrorMessage = "File audio kosakata tidak tersedia untuk kata ini.";
        [SerializeField] private string sentenceAudioErrorMessage = "File audio contoh kalimat tidak tersedia.";

        private ObjectInfo currentInfo;

        private void Awake()
        {
            if (modalRoot == null) modalRoot = gameObject;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(Close);

            EnsureAudioSource();

            // Hubungkan tombol audio kosakata (prioritas vocabAudioButton, fallback ke audioButton)
            var vBtn = vocabAudioButton != null ? vocabAudioButton : audioButton;
            if (vBtn != null)
            {
                vBtn.onClick.RemoveListener(PlayVocabAudio);
                vBtn.onClick.AddListener(PlayVocabAudio);
            }

            // Hubungkan tombol audio kalimat contoh jika ada
            if (sentenceAudioButton != null)
            {
                sentenceAudioButton.onClick.RemoveListener(PlaySentenceAudio);
                sentenceAudioButton.onClick.AddListener(PlaySentenceAudio);
            }

            if (exampleToggle != null)
                exampleToggle.onValueChanged.AddListener(on =>
                {
                    if (exampleLabel != null) exampleLabel.gameObject.SetActive(on);
                });

            Hide();
        }

        public void Open(ObjectInfo info)
        {
            currentInfo = info;
            if (info == null || info.vocabulary == null)
            {
                Close();
                return;
            }

            var vocab = info.vocabulary;
            if (kanjiLabel != null) kanjiLabel.text = vocab.DisplayJapanese;
            if (romajiLabel != null) romajiLabel.text = vocab.romaji;
            if (meaningLabel != null) meaningLabel.text = vocab.translation;
            if (categoryLabel != null) categoryLabel.text = !string.IsNullOrEmpty(vocab.category) ? $"Kategori: {vocab.category}" : "";

            if (exampleLabel != null)
            {
                exampleLabel.text = !string.IsNullOrEmpty(vocab.exampleSentence) ? vocab.exampleSentence : "Belum ada contoh kalimat.";
                exampleLabel.gameObject.SetActive(false);
            }
            if (exampleToggle != null) exampleToggle.SetIsOnWithoutNotify(false);

            // Periksa ketersediaan audio dan atur status interactable tombol secara terpisah
            UpdateAudioButtonsState(vocab);

            if (modalRoot != null) modalRoot.SetActive(true);
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D UI Sound
        }

        /// <summary>
        /// Memperbarui status interactable tombol audio kosakata dan contoh kalimat secara terpisah.
        /// Tombol dinonaktifkan jika file audio terkait tidak tersedia.
        /// </summary>
        public void UpdateAudioButtonsState(VocabularyEntry vocab)
        {
            AudioClip vocabClip = ResolveVocabAudio(vocab);
            bool hasVocab = vocabClip != null;

            var vBtn = vocabAudioButton != null ? vocabAudioButton : audioButton;
            if (vBtn != null)
            {
                vBtn.interactable = hasVocab;
            }

            AudioClip sentenceClip = ResolveSentenceAudio(vocab);
            bool hasSentence = sentenceClip != null;

            if (sentenceAudioButton != null)
            {
                sentenceAudioButton.interactable = hasSentence;
            }
        }

        /// <summary>
        /// Memutar audio pelafalan kosakata bahasa Jepang secara terpisah menggunakan audioPath / audioClip.
        /// Jika audioPath kosong, null, atau tidak ditemukan, tombol dinonaktifkan dan pesan feedback ditampilkan tanpa error.
        /// </summary>
        public void PlayVocabAudio()
        {
            if (currentInfo == null || currentInfo.vocabulary == null)
            {
                Debug.LogWarning($"[DictionaryVocabDetailModal] {vocabAudioErrorMessage}");
                return;
            }

            var vocab = currentInfo.vocabulary;
            AudioClip clip = ResolveVocabAudio(vocab);

            if (clip == null)
            {
                var vBtn = vocabAudioButton != null ? vocabAudioButton : audioButton;
                if (vBtn != null) vBtn.interactable = false;

                Debug.LogWarning($"[DictionaryVocabDetailModal] {vocabAudioErrorMessage} ({vocab.DisplayJapanese})");
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(clip);
            Debug.Log($"[DictionaryVocabDetailModal] Memutar audio kosakata: '{vocab.DisplayJapanese}' ({clip.name})");
        }

        /// <summary>
        /// Memutar audio contoh kalimat secara terpisah menggunakan sentenceAudioPath / sentenceAudioClip.
        /// Jika audio kalimat tidak tersedia, tombol dinonaktifkan dan pesan feedback ditampilkan tanpa error.
        /// </summary>
        public void PlaySentenceAudio()
        {
            if (currentInfo == null || currentInfo.vocabulary == null)
            {
                Debug.LogWarning($"[DictionaryVocabDetailModal] {sentenceAudioErrorMessage}");
                return;
            }

            var vocab = currentInfo.vocabulary;
            AudioClip clip = ResolveSentenceAudio(vocab);

            if (clip == null)
            {
                if (sentenceAudioButton != null) sentenceAudioButton.interactable = false;

                Debug.LogWarning($"[DictionaryVocabDetailModal] {sentenceAudioErrorMessage} ({vocab.DisplayJapanese})");
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(clip);
            Debug.Log($"[DictionaryVocabDetailModal] Memutar audio contoh kalimat: '{clip.name}'");
        }

        /// <summary>
        /// Mempertahankan fungsi audio yang sudah ada untuk tombol bawaan/event listener lama.
        /// </summary>
        public void PlayAudio()
        {
            PlayVocabAudio();
        }

        /// <summary>
        /// Memuat audio pelafalan kosakata HANYA dari audioClip atau audioPath data kosakata.
        /// Tidak pernah menggunakan audio contoh kalimat sebagai pengganti.
        /// </summary>
        public static AudioClip ResolveVocabAudio(VocabularyEntry vocab)
        {
            if (vocab == null) return null;
            if (vocab.audioClip != null) return vocab.audioClip;
            if (string.IsNullOrWhiteSpace(vocab.audioPath)) return null;

            return LoadAudioFromResources(vocab.audioPath);
        }

        /// <summary>
        /// Memuat audio contoh kalimat HANYA dari sentenceAudioClip atau sentenceAudioPath.
        /// Tidak pernah menggunakan audio kosakata sebagai pengganti.
        /// </summary>
        public static AudioClip ResolveSentenceAudio(VocabularyEntry vocab)
        {
            if (vocab == null) return null;
            if (vocab.sentenceAudioClip != null) return vocab.sentenceAudioClip;
            if (string.IsNullOrWhiteSpace(vocab.sentenceAudioPath)) return null;

            return LoadAudioFromResources(vocab.sentenceAudioPath);
        }

        private static AudioClip LoadAudioFromResources(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            string cleanPath = path.Trim();
            if (cleanPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                cleanPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                cleanPath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(0, cleanPath.LastIndexOf('.'));
            }

            try
            {
                return Resources.Load<AudioClip>(cleanPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DictionaryVocabDetailModal] Gagal memuat audio '{cleanPath}': {ex.Message}");
                return null;
            }
        }

        public void Close()
        {
            Hide();
        }

        public void Hide()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            currentInfo = null;
        }
    }
}
