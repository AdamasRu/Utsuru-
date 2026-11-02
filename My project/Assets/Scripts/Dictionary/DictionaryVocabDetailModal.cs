using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Modal detail kosakata saat kartu kosakata yang Discovered diklik di Tab Kosakata.
    /// Memuat pelafalan audio native dan contoh kalimat.
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
        [SerializeField] private Button audioButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropCloseButton;
        [SerializeField] private AudioSource audioSource;

        private ObjectInfo currentInfo;

        private void Awake()
        {
            if (modalRoot == null) modalRoot = gameObject;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(Close);
            if (audioButton != null) audioButton.onClick.AddListener(PlayAudio);
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

            if (modalRoot != null) modalRoot.SetActive(true);
        }

        private void PlayAudio()
        {
            if (currentInfo == null || currentInfo.vocabulary == null || audioSource == null) return;
            var vocab = currentInfo.vocabulary;

            AudioClip clip = vocab.audioClip;
            if (clip == null && !string.IsNullOrEmpty(vocab.audioPath))
            {
                clip = Resources.Load<AudioClip>(vocab.audioPath);
            }

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning($"[DictionaryVocabDetailModal] Audio tidak tersedia untuk kata '{vocab.DisplayJapanese}'.");
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

