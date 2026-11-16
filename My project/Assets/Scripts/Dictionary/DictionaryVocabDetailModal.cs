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
            string translation = vocab.translation;
            string example = vocab.exampleSentence;
            if (Utsuru.Localization.LocalizationManager.Instance != null)
            {
                translation = Utsuru.Localization.LocalizationManager.Instance.GetVocabTranslation(info.objectId, translation);
                example = Utsuru.Localization.LocalizationManager.Instance.GetExampleSentence(info.objectId, example);
            }

            if (kanjiLabel != null) kanjiLabel.text = vocab.DisplayJapanese;
            if (romajiLabel != null) romajiLabel.text = vocab.romaji;
            if (meaningLabel != null) meaningLabel.text = translation;
            if (categoryLabel != null)
            {
                string catFmt = Utsuru.Localization.LocalizationManager.Instance != null 
                    ? Utsuru.Localization.LocalizationManager.Instance.GetText("MODAL_CATEGORY_LABEL", "Kategori: {0}") 
                    : "Kategori: {0}";
                string catVal = Utsuru.Localization.LocalizationManager.Instance != null 
                    ? Utsuru.Localization.LocalizationManager.Instance.GetCategoryText(vocab.category) 
                    : vocab.category;
                categoryLabel.text = !string.IsNullOrEmpty(vocab.category) ? string.Format(catFmt, catVal) : "";
            }

            if (exampleLabel != null)
            {
                string noEx = Utsuru.Localization.LocalizationManager.Instance != null 
                    ? Utsuru.Localization.LocalizationManager.Instance.GetText("NO_EXAMPLE_SENTENCE", "Belum ada contoh kalimat.") 
                    : "Belum ada contoh kalimat.";
                exampleLabel.text = !string.IsNullOrEmpty(example) ? example : noEx;
                exampleLabel.gameObject.SetActive(false);
            }
            if (exampleToggle != null) exampleToggle.SetIsOnWithoutNotify(false);

            if (modalRoot != null) modalRoot.SetActive(true);
        }

        private void PlayAudio()
        {
            if (currentInfo == null || currentInfo.vocabulary == null || audioSource == null) return;
            var vocab = currentInfo.vocabulary;

            AudioClip clip = null;
            // Jika contoh kalimat sedang dibuka, dahulukan audio kalimat jika tersedia
            if (exampleToggle != null && exampleToggle.isOn && (!string.IsNullOrEmpty(vocab.sentenceAudioPath) || vocab.sentenceAudioClip != null))
            {
                clip = vocab.sentenceAudioClip != null ? vocab.sentenceAudioClip : Resources.Load<AudioClip>(vocab.sentenceAudioPath);
            }

            if (clip == null)
            {
                clip = vocab.audioClip != null ? vocab.audioClip : (!string.IsNullOrEmpty(vocab.audioPath) ? Resources.Load<AudioClip>(vocab.audioPath) : null);
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

