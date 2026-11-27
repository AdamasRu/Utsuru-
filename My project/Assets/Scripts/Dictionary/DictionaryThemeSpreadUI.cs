using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Store;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Tab Theme model slide per tema (1 slide = 1 tema):
    /// - Page Kiri kosong agar fokus ke live preview sampul buku.
    /// - Page Kanan menampilkan Header (nama tema), deskripsi/indikator slide,
    ///   tombol status (Apply / Applied / Locked), dan tombol hint buka Store (jika belum dimiliki).
    /// - Perpindahan slide menggunakan Btn_PreviousPage & Btn_NextPage dengan animasi Fade.
    /// </summary>
    public class DictionaryThemeSpreadUI : MonoBehaviour
    {
        [Header("Referensi Sistem")]
        [SerializeField] private DictionaryThemeApplier themeApplier;
        [SerializeField] private DictionaryBookSpreadUI bookSpread;

        [Header("Halaman & Animasi Fade")]
        [SerializeField] private GameObject leftPageRoot;
        [SerializeField] private GameObject rightPageRoot;
        [SerializeField] private CanvasGroup rightPageCanvasGroup;
        [SerializeField] private float fadeOutDuration = 0.18f;
        [SerializeField] private float fadeInDuration = 0.22f;

        [Header("Konten Halaman Kanan")]
        [SerializeField] private TMP_Text themeNameHeaderText;
        [SerializeField] private TMP_Text slideSubtitleText;
        [SerializeField] private Button statusActionButton;
        [SerializeField] private Image statusActionButtonBg;
        [SerializeField] private TMP_Text statusActionButtonText;
        [SerializeField] private Button storeHintButton;
        [SerializeField] private TMP_Text storeHintButtonText;
        [SerializeField] private TMP_Text statusNoticeText;

        private StoreThemeManager themeManager;
        private int currentSlideIndex = 0;
        private bool isFading = false;
        private int lastEnterFrame = -1;
        private Coroutine fadeCoroutine;

        // Warna tombol status selaras dengan desain Store
        private static readonly Color AppliedButtonTint = new Color(0.84f, 0.94f, 0.83f, 1f);
        private static readonly Color ApplyButtonTint = Color.white;
        private static readonly Color LockedButtonTint = new Color(0.86f, 0.82f, 0.78f, 0.95f);

        private static readonly Color AppliedTextColor = new Color(0.16f, 0.42f, 0.20f, 1f);
        private static readonly Color DarkBrownTextColor = new Color(0.373f, 0.224f, 0.137f, 1f);
        private static readonly Color LockedTextColor = new Color(0.45f, 0.36f, 0.30f, 1f);

        public int CurrentSlideIndex => currentSlideIndex;
        public int TotalSlides => themeManager != null ? themeManager.AllThemes.Count : StoreThemeCatalog.All.Count;
        public bool CanGoPrevious => currentSlideIndex > 0 && !isFading;
        public bool CanGoNext => currentSlideIndex < TotalSlides - 1 && !isFading;

        private void Awake()
        {
            if (bookSpread == null)
                bookSpread = GetComponentInParent<DictionaryBookSpreadUI>();

            if (storeHintButton != null)
            {
                storeHintButton.onClick.RemoveAllListeners();
                storeHintButton.onClick.AddListener(OpenStoreFromDictionary);
            }
        }

        private void OnEnable()
        {
            EnsureBound();
            ResetAndEnterFirstSlide();
        }

        private void OnDisable()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            isFading = false;

            if (rightPageCanvasGroup != null)
                rightPageCanvasGroup.alpha = 1f;

            // Kembalikan sampul buku ke tema aktif sebenarnya jika pemain hanya melihat preview
            if (themeApplier != null)
            {
                themeApplier.ApplyCurrentTheme();
            }
        }

        private void OnDestroy()
        {
            if (themeManager != null)
            {
                themeManager.OnThemesUpdated -= HandleThemesUpdated;
                themeManager.OnActiveThemeChanged -= HandleThemeChanged;
            }
        }

        public void ConfigureReferences(
            DictionaryThemeApplier applier,
            DictionaryBookSpreadUI spread,
            GameObject leftRoot,
            GameObject rightRoot,
            CanvasGroup rightCg,
            TMP_Text headerText,
            TMP_Text subtitleText,
            Button statusBtn,
            Image statusBtnBg,
            TMP_Text statusBtnText,
            Button hintStoreBtn,
            TMP_Text hintStoreText,
            TMP_Text noticeText)
        {
            themeApplier = applier;
            bookSpread = spread;
            leftPageRoot = leftRoot;
            rightPageRoot = rightRoot;
            rightPageCanvasGroup = rightCg;
            themeNameHeaderText = headerText;
            slideSubtitleText = subtitleText;
            statusActionButton = statusBtn;
            statusActionButtonBg = statusBtnBg;
            statusActionButtonText = statusBtnText;
            storeHintButton = hintStoreBtn;
            storeHintButtonText = hintStoreText;
            statusNoticeText = noticeText;

            if (storeHintButton != null)
            {
                storeHintButton.onClick.RemoveAllListeners();
                storeHintButton.onClick.AddListener(OpenStoreFromDictionary);
            }
        }

        private void EnsureBound()
        {
            if (themeManager == null)
            {
                themeManager = StoreThemeManager.EnsureInstance();
                if (themeManager != null)
                {
                    themeManager.OnThemesUpdated -= HandleThemesUpdated;
                    themeManager.OnThemesUpdated += HandleThemesUpdated;
                    themeManager.OnActiveThemeChanged -= HandleThemeChanged;
                    themeManager.OnActiveThemeChanged += HandleThemeChanged;
                }
            }

            if (themeApplier == null)
            {
                themeApplier = FindFirstObjectByType<DictionaryThemeApplier>(FindObjectsInactive.Include);
            }

            if (bookSpread == null)
            {
                bookSpread = GetComponentInParent<DictionaryBookSpreadUI>();
            }

            if (rightPageCanvasGroup == null && rightPageRoot != null)
            {
                rightPageCanvasGroup = rightPageRoot.GetComponent<CanvasGroup>();
                if (rightPageCanvasGroup == null)
                    rightPageCanvasGroup = rightPageRoot.AddComponent<CanvasGroup>();
            }
        }

        private void HandleThemesUpdated()
        {
            RefreshCurrentSlideVisual(applyBookPreview: gameObject.activeInHierarchy);
        }

        private void HandleThemeChanged(string _)
        {
            RefreshCurrentSlideVisual(applyBookPreview: gameObject.activeInHierarchy);
        }

        public void Refresh()
        {
            EnsureBound();
            ResetAndEnterFirstSlide();
        }

        private void ResetAndEnterFirstSlide()
        {
            if (Application.isPlaying && lastEnterFrame == Time.frameCount)
                return;
            lastEnterFrame = Time.frameCount;

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            isFading = false;

            currentSlideIndex = 0; // Page 1 selalu tema Plain
            if (statusNoticeText != null) statusNoticeText.text = "";

            if (rightPageCanvasGroup != null) rightPageCanvasGroup.alpha = 1f;
            RefreshCurrentSlideVisual(applyBookPreview: true);
        }

        public void PreviousSpread()
        {
            if (!CanGoPrevious) return;
            SwitchSlide(currentSlideIndex - 1);
        }

        public void NextSpread()
        {
            if (!CanGoNext) return;
            SwitchSlide(currentSlideIndex + 1);
        }

        private void SwitchSlide(int targetIndex)
        {
            int total = TotalSlides;
            if (targetIndex < 0 || targetIndex >= total) return;

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            if (gameObject.activeInHierarchy && rightPageCanvasGroup != null)
            {
                fadeCoroutine = StartCoroutine(FadeSlideRoutine(targetIndex));
            }
            else
            {
                currentSlideIndex = targetIndex;
                if (statusNoticeText != null) statusNoticeText.text = "";
                RefreshCurrentSlideVisual(applyBookPreview: true);
            }
        }

        private IEnumerator FadeSlideRoutine(int targetIndex)
        {
            isFading = true;
            UpdateNavigationButtons();

            // Mulai crossfade sampul buku bersamaan dengan fade halaman kanan
            IReadOnlyList<StoreThemeDefinition> all = themeManager != null ? themeManager.AllThemes : null;
            if (all != null && targetIndex >= 0 && targetIndex < all.Count && themeApplier != null)
            {
                themeApplier.CrossfadeToTheme(all[targetIndex].id, fadeOutDuration + fadeInDuration);
            }

            // 1. Fade-Out Halaman Kanan
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                if (rightPageCanvasGroup != null)
                    rightPageCanvasGroup.alpha = Mathf.SmoothStep(1f, 0f, t);
                yield return null;
            }

            if (rightPageCanvasGroup != null) rightPageCanvasGroup.alpha = 0f;

            // 2. Ganti Slide (sampul buku sudah ditangani oleh CrossfadeToTheme)
            currentSlideIndex = targetIndex;
            if (statusNoticeText != null) statusNoticeText.text = "";
            RefreshCurrentSlideVisual(applyBookPreview: false);

            // 3. Fade-In Halaman Kanan
            elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                if (rightPageCanvasGroup != null)
                    rightPageCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }

            if (rightPageCanvasGroup != null) rightPageCanvasGroup.alpha = 1f;

            isFading = false;
            fadeCoroutine = null;
            UpdateNavigationButtons();
        }

        private void RefreshCurrentSlideVisual(bool applyBookPreview)
        {
            EnsureBound();
            if (themeManager == null) return;

            IReadOnlyList<StoreThemeDefinition> all = themeManager.AllThemes;
            if (all == null || all.Count == 0) return;

            currentSlideIndex = Mathf.Clamp(currentSlideIndex, 0, all.Count - 1);
            StoreThemeDefinition def = all[currentSlideIndex];

            bool isOwned = themeManager.IsOwned(def.id);
            bool isApplied = themeManager.IsApplied(def.id);

            // Live preview sampul buku sesuai slide yang dibuka
            if (applyBookPreview && themeApplier != null)
            {
                themeApplier.PreviewTheme(def.id);
            }

            // Pastikan page kiri tetap kosong & aktif (atau container kosong)
            if (leftPageRoot != null) leftPageRoot.SetActive(true);
            if (rightPageRoot != null) rightPageRoot.SetActive(true);

            // 1. Header (Nama Tema) & Subtitle Slide
            if (themeNameHeaderText != null)
                themeNameHeaderText.text = def.displayName;

            if (slideSubtitleText != null)
                slideSubtitleText.text = $"Theme {currentSlideIndex + 1} / {all.Count}";

            // 2. Tombol Status (Apply / Applied / Locked)
            if (statusActionButton != null)
            {
                statusActionButton.onClick.RemoveAllListeners();
                string targetId = def.id;

                if (isApplied)
                {
                    statusActionButton.interactable = false;
                    if (statusActionButtonBg != null) statusActionButtonBg.color = AppliedButtonTint;
                    if (statusActionButtonText != null)
                    {
                        statusActionButtonText.text = "Applied";
                        statusActionButtonText.color = AppliedTextColor;
                    }
                }
                else if (isOwned)
                {
                    statusActionButton.interactable = true;
                    if (statusActionButtonBg != null) statusActionButtonBg.color = ApplyButtonTint;
                    if (statusActionButtonText != null)
                    {
                        statusActionButtonText.text = "Apply";
                        statusActionButtonText.color = DarkBrownTextColor;
                    }
                    statusActionButton.onClick.AddListener(() => OnApplyClicked(targetId));
                }
                else
                {
                    statusActionButton.interactable = false;
                    if (statusActionButtonBg != null) statusActionButtonBg.color = LockedButtonTint;
                    if (statusActionButtonText != null)
                    {
                        statusActionButtonText.text = "Locked";
                        statusActionButtonText.color = LockedTextColor;
                    }
                }
            }

            // 3. Tombol Hint Buka Store (hanya muncul jika tema BELUM terbuka)
            if (storeHintButton != null)
            {
                storeHintButton.gameObject.SetActive(!isOwned);
                if (!isOwned && storeHintButtonText != null)
                {
                    storeHintButtonText.text = $"Open Store (T)  •  {def.pricePoints} Points";
                }
            }

            UpdateNavigationButtons();
        }

        public void UpdateNavigationButtons()
        {
            if (bookSpread != null)
            {
                bookSpread.UpdateThemeNavigationState(CanGoPrevious, CanGoNext);
            }
        }

        private void OnApplyClicked(string themeId)
        {
            EnsureBound();
            if (themeManager == null) return;

            bool ok = themeManager.ApplyTheme(themeId, out string message);
            if (statusNoticeText != null)
            {
                statusNoticeText.text = message;
                statusNoticeText.color = ok
                    ? AppliedTextColor
                    : new Color(0.65f, 0.20f, 0.18f, 1f);
            }
            RefreshCurrentSlideVisual(applyBookPreview: true);
        }

        private void OpenStoreFromDictionary()
        {
            var dict = FindFirstObjectByType<DictionaryUIController>();
            var store = StoreUIController.Instance ?? FindFirstObjectByType<StoreUIController>(FindObjectsInactive.Include);

            if (store != null)
            {
                if (dict != null && dict.IsOpen)
                {
                    dict.Close();
                }
                store.Open();
            }
            else if (statusNoticeText != null)
            {
                statusNoticeText.text = "Tekan F di eksplorasi untuk membuka Store!";
            }
        }
    }
}
