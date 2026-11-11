using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Controller utama buku Dictionary Rework (DPPL UI-05).
    /// Mengelola fading background (BG_dictionary.png), buka/tutup container buku (Book),
    /// serta 7 tab bookmark di atas buku (Album, Like, Favorite, Quiz, Hiragana, Module, Exit).
    /// Berada di GameObject Canvas/Dictionary yang selalu aktif mendengarkan input 'B' / Escape.
    /// </summary>
    public class DictionaryUIController : MonoBehaviour
    {
        [Header("Tampilan Utama (Yang Di-toggle)")]
        [Tooltip("Kontainer DictionaryView yang memuat Dimmer + BookContainer.")]
        [SerializeField] private GameObject viewRoot;
        [SerializeField] private CanvasGroup viewCanvasGroup;
        [SerializeField] private RectTransform bookContainerRt;

        [Header("Pengaturan Transisi")]
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private KeyCode toggleKey = KeyCode.B;

        [Header("Tab Bookmark Atas")]
        [SerializeField] private Button tabAlbumButton;
        [SerializeField] private Button tabLikeButton;
        [SerializeField] private Button tabFavoriteButton;
        [SerializeField] private Button tabQuizButton;
        [SerializeField] private Button tabHiraganaButton;
        [SerializeField] private Button tabModuleButton;
        [SerializeField] private Button tabExitButton;

        [Header("Spread Buku (Isi 2 Halaman)")]
        [SerializeField] private DictionaryBookSpreadUI bookSpread;

        public bool IsOpen => viewRoot != null && viewRoot.activeSelf;

        private float prevTimeScale = 1f;
        private Coroutine transitionCoroutine;
        private DictionaryBookMode currentTab = DictionaryBookMode.Album;

        private void Awake()
        {
            // Daftarkan listener tombol tab
            if (tabAlbumButton != null) tabAlbumButton.onClick.AddListener(() => SelectTab(DictionaryBookMode.Album));
            if (tabLikeButton != null) tabLikeButton.onClick.AddListener(() => SelectTab(DictionaryBookMode.Like));
            if (tabFavoriteButton != null) tabFavoriteButton.onClick.AddListener(() => SelectTab(DictionaryBookMode.Favorite));
            if (tabQuizButton != null) tabQuizButton.onClick.AddListener(() => SelectTab(DictionaryBookMode.Quiz));
            if (tabHiraganaButton != null) tabHiraganaButton.onClick.AddListener(() => SelectTab(DictionaryBookMode.Hiragana));
            if (tabModuleButton != null) tabModuleButton.onClick.AddListener(() => SelectTab(DictionaryBookMode.Module));
            if (tabExitButton != null) tabExitButton.onClick.AddListener(Close);

            // Default: tertutup saat awal mulai
            if (viewRoot != null) viewRoot.SetActive(false);
            if (viewCanvasGroup != null) viewCanvasGroup.alpha = 0f;
        }

        private void Update()
        {
            if (IsTogglePressed())
            {
                Toggle();
            }
            else if (IsOpen && IsEscapePressed())
            {
                Close();
            }
        }

        private bool IsTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.bKey.wasPressedThisFrame) return true;
#endif
            try
            {
                if (Input.GetKeyDown(toggleKey)) return true;
            }
            catch {}
            return false;
        }

        private bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) return true;
            }
            catch {}
            return false;
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open(DictionaryBookMode startingTab = DictionaryBookMode.Album)
        {
            if (IsOpen) return;

            // Blok shutter kamera agar tidak memotret saat buka buku
            if (CameraCaptureSystem.Instance != null)
            {
                CameraCaptureSystem.Instance.InputBlocked = true;
            }

            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            var cameraMode = FindFirstObjectByType<CameraModeController>();
            if (cameraMode != null)
            {
                cameraMode.SetControlsLocked(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (viewRoot != null) viewRoot.SetActive(true);

            SelectTab(startingTab);

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(FadeRoutine(true));

            Debug.Log("[DictionaryUIController] Dictionary DIBUKA.");
        }

        public void Close()
        {
            if (!IsOpen) return;

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(FadeRoutine(false, () =>
            {
                if (viewRoot != null) viewRoot.SetActive(false);

                Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;

                if (CameraCaptureSystem.Instance != null)
                {
                    CameraCaptureSystem.Instance.InputBlocked = false;
                }

                var cameraMode = FindFirstObjectByType<CameraModeController>();
                if (cameraMode != null)
                {
                    cameraMode.SetControlsLocked(false);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                Debug.Log("[DictionaryUIController] Dictionary DITUTUP.");
            }));
        }

        public void SelectTab(DictionaryBookMode tab)
        {
            currentTab = tab;
            if (bookSpread != null)
            {
                bookSpread.SetMode(tab);
            }
            UpdateTabVisuals();
        }

        private void UpdateTabVisuals()
        {
            // Highlight visual tab aktif: tab yang aktif sedikit menjorok ke bawah / full opacity
            SetTabHighlight(tabAlbumButton, currentTab == DictionaryBookMode.Album);
            SetTabHighlight(tabLikeButton, currentTab == DictionaryBookMode.Like);
            SetTabHighlight(tabFavoriteButton, currentTab == DictionaryBookMode.Favorite);
            SetTabHighlight(tabQuizButton, currentTab == DictionaryBookMode.Quiz);
            SetTabHighlight(tabHiraganaButton, currentTab == DictionaryBookMode.Hiragana);
            SetTabHighlight(tabModuleButton, currentTab == DictionaryBookMode.Module);
        }

        private void SetTabHighlight(Button btn, bool isActive)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? Color.white : new Color(0.85f, 0.85f, 0.85f, 0.88f);
            }
        }

        private IEnumerator FadeRoutine(bool fadeIn, System.Action onComplete = null)
        {
            float elapsed = 0f;
            float startAlpha = viewCanvasGroup != null ? viewCanvasGroup.alpha : (fadeIn ? 0f : 1f);
            float targetAlpha = fadeIn ? 1f : 0f;

            Vector3 startScale = fadeIn ? Vector3.one * 0.94f : Vector3.one;
            Vector3 targetScale = fadeIn ? Vector3.one : Vector3.one * 0.94f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                // Ease out cubic
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (viewCanvasGroup != null)
                    viewCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, ease);

                if (bookContainerRt != null)
                    bookContainerRt.localScale = Vector3.Lerp(startScale, targetScale, ease);

                yield return null;
            }

            if (viewCanvasGroup != null) viewCanvasGroup.alpha = targetAlpha;
            if (bookContainerRt != null) bookContainerRt.localScale = targetScale;

            onComplete?.Invoke();
            transitionCoroutine = null;
        }
    }
}
