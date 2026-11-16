using TMPro;
using UnityEngine;

namespace Utsuru.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizeText : MonoBehaviour
    {
        [Tooltip("Key lokalisasi pada berkas ui_id / ui_en, mis. TAB_GAMEPLAY, BTN_APPLY")]
        [SerializeField] private string localizationKey;

        [Tooltip("Fallback teks bahasa Inggris jika key tidak ditemukan")]
        [SerializeField] private string fallbackText;

        private TMP_Text textComponent;

        public string LocalizationKey
        {
            get => localizationKey;
            set
            {
                localizationKey = value;
                Refresh();
            }
        }

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
            if (string.IsNullOrEmpty(fallbackText) && textComponent != null)
            {
                fallbackText = textComponent.text;
            }
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (textComponent == null) textComponent = GetComponent<TMP_Text>();
            if (textComponent == null) return;

            if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(localizationKey))
            {
                textComponent.text = LocalizationManager.Instance.GetText(localizationKey, fallbackText);
            }
            else if (!string.IsNullOrEmpty(fallbackText))
            {
                textComponent.text = fallbackText;
            }
        }
    }
}

