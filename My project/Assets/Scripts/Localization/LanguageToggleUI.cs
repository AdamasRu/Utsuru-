using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.Localization
{
    /// <summary>
    /// Kontrol sakelar bahasa (Switch Language) pada menu Settings
    /// sesuai desain Figma:
    /// - Palette:
    ///   * Dark Brown: #4A2E1B (State OFF)
    ///   * Medium Brown: #6B4A33 (State ON)
    ///   * Cream Knob/Text: #F8EEE0
    /// - IND (OFF: knob kiri, ON: knob kanan)
    /// - ENG (OFF: knob kiri, ON: knob kanan)
    /// </summary>
    public class LanguageToggleUI : MonoBehaviour
    {
        [Header("Tombol IND")]
        [SerializeField] private Button indButton;
        [SerializeField] private Image indBackground;
        [SerializeField] private RectTransform indKnob;
        [SerializeField] private TMP_Text indText;

        [Header("Tombol ENG")]
        [SerializeField] private Button engButton;
        [SerializeField] private Image engBackground;
        [SerializeField] private RectTransform engKnob;
        [SerializeField] private TMP_Text engText;

        [Header("Label Judul Baris")]
        [SerializeField] private TMP_Text rowLabel;

        [Header("Animasi / Posisi Knob")]
        [SerializeField] private float knobOffsetLeft = 14f;
        [SerializeField] private float knobOffsetRight = 64f;
        [SerializeField] private float transitionDuration = 0.15f;

        // Warna palet Figma
        private static readonly Color ColorDarkBrown = new Color32(0x4A, 0x2E, 0x1B, 0xFF);   // #4A2E1B (OFF)
        private static readonly Color ColorMediumBrown = new Color32(0x6B, 0x4A, 0x33, 0xFF); // #6B4A33 (ON)
        private static readonly Color ColorCream = new Color32(0xF8, 0xEE, 0xE0, 0xFF);       // #F8EEE0

        private Coroutine indAnimRoutine;
        private Coroutine engAnimRoutine;

        private void Awake()
        {
            if (indButton != null) indButton.onClick.AddListener(SelectIndonesian);
            if (engButton != null) engButton.onClick.AddListener(SelectEnglish);
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            UpdateVisualsInstant();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        public void SelectIndonesian()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(GameLanguage.Indonesian);
            }
        }

        public void SelectEnglish()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(GameLanguage.English);
            }
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateVisualsAnimated();
            UpdateRowLabel();
        }

        public void UpdateVisualsInstant()
        {
            bool isInd = (LocalizationManager.Instance != null && LocalizationManager.Instance.IsIndonesian);

            // IND
            SetButtonVisual(indBackground, indKnob, indText, isInd, instant: true);

            // ENG
            SetButtonVisual(engBackground, engKnob, engText, !isInd, instant: true);

            UpdateRowLabel();
        }

        private void UpdateVisualsAnimated()
        {
            bool isInd = (LocalizationManager.Instance != null && LocalizationManager.Instance.IsIndonesian);

            if (indAnimRoutine != null) StopCoroutine(indAnimRoutine);
            indAnimRoutine = StartCoroutine(AnimateButtonVisual(indBackground, indKnob, indText, isInd));

            if (engAnimRoutine != null) StopCoroutine(engAnimRoutine);
            engAnimRoutine = StartCoroutine(AnimateButtonVisual(engBackground, engKnob, engText, !isInd));
        }

        private void SetButtonVisual(Image bg, RectTransform knob, TMP_Text text, bool isOn, bool instant)
        {
            if (bg != null) bg.color = isOn ? ColorMediumBrown : ColorDarkBrown;
            if (text != null) text.color = ColorCream;

            if (knob != null)
            {
                float targetX = isOn ? knobOffsetRight : knobOffsetLeft;
                knob.anchoredPosition = new Vector2(targetX, knob.anchoredPosition.y);
            }
        }

        private IEnumerator AnimateButtonVisual(Image bg, RectTransform knob, TMP_Text text, bool isOn)
        {
            if (text != null) text.color = ColorCream;

            Color startBg = bg != null ? bg.color : (isOn ? ColorDarkBrown : ColorMediumBrown);
            Color targetBg = isOn ? ColorMediumBrown : ColorDarkBrown;

            float startX = knob != null ? knob.anchoredPosition.x : 0f;
            float targetX = isOn ? knobOffsetRight : knobOffsetLeft;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                t = Mathf.SmoothStep(0f, 1f, t);

                if (bg != null) bg.color = Color.Lerp(startBg, targetBg, t);
                if (knob != null) knob.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t), knob.anchoredPosition.y);

                yield return null;
            }

            if (bg != null) bg.color = targetBg;
            if (knob != null) knob.anchoredPosition = new Vector2(targetX, knob.anchoredPosition.y);
        }

        private void UpdateRowLabel()
        {
            if (rowLabel != null && LocalizationManager.Instance != null)
            {
                rowLabel.text = LocalizationManager.Instance.GetText("LABEL_LANGUAGE", "Language");
            }
        }
    }
}

