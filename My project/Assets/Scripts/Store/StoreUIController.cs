using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;
using Utsuru.DictionaryUI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Utsuru.Store
{
    /// <summary>
    /// Controller modal Store Tema Kamus (DPPL.UTSURU.012 / Layout007).
    /// Dibuka/tutup dengan tombol 'T' (dan ditutup via tombol 'Close' atau 'Esc').
    /// Menangkap dan mem-blur latar gameplay 3D saat aktif.
    /// </summary>
    public class StoreUIController : MonoBehaviour
    {
        public static StoreUIController Instance { get; private set; }

        [Header("UI Root & Animation")]
        [SerializeField] private GameObject viewRoot;
        [SerializeField] private CanvasGroup viewCanvasGroup;
        [SerializeField] private RectTransform mainPanelRt;
        [SerializeField] private float fadeDuration = 0.2f;

        [Header("Background Blur")]
        [SerializeField] private RawImage blurBackdrop;
        [SerializeField] private Image tintOverlay;
        [SerializeField] private Material blurMaterial;
        [SerializeField, Range(1, 6)] private int blurIterations = 4;
        [SerializeField, Range(0.5f, 5f)] private float blurSpread = 2.0f;
        [SerializeField, Range(1, 4)] private int downsampleFactor = 2;

        [System.Serializable]
        public class ThemePreviewBinding
        {
            public string themeId;
            public Sprite previewSprite;
        }

        [Header("Header & Cards")]
        [SerializeField] private TMP_Text pointsBalanceText;
        [SerializeField] private List<StoreThemeCardUI> themeCards = new List<StoreThemeCardUI>();
        [SerializeField] private List<ThemePreviewBinding> themePreviews = new List<ThemePreviewBinding>();
        [SerializeField] private Button closeButton;
        [SerializeField] private KeyCode toggleKey = KeyCode.T;

        [Header("Feedback Toast")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private Image feedbackBackground;
        [SerializeField] private TMP_Text feedbackText;

        public bool IsOpen => viewRoot != null && viewRoot.activeSelf;

        private static readonly int BlurSizeId = Shader.PropertyToID("_BlurSize");

        private RenderTexture activeBlurRt;
        private Material runtimeBlurMaterial;
        private float prevTimeScale = 1f;
        private Coroutine transitionCoroutine;
        private Coroutine feedbackCoroutine;

        private StoreThemeManager themeManager;
        private PointRewardSystem pointsSystem;
        private DictionaryUIController cachedDictionaryUI;
        private PolaroidCardUI cachedPolaroidUI;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[StoreUIController] Duplicate instance detected; destroying duplicate.", this);
                Destroy(this);
                return;
            }
            Instance = this;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (viewRoot != null) viewRoot.SetActive(false);
            if (viewCanvasGroup != null) viewCanvasGroup.alpha = 0f;
            if (blurBackdrop != null) blurBackdrop.enabled = false;
        }

        private void Start()
        {
            themeManager = StoreThemeManager.EnsureInstance();
            if (themeManager != null)
            {
                themeManager.OnThemesUpdated += HandleStateUpdated;
            }

            pointsSystem = PointRewardSystem.Instance ?? FindFirstObjectByType<PointRewardSystem>();
            if (pointsSystem != null)
            {
                pointsSystem.OnPointsChanged += HandlePointsChanged;
            }
        }

        private void OnDestroy()
        {
            if (themeManager != null)
            {
                themeManager.OnThemesUpdated -= HandleStateUpdated;
            }
            if (pointsSystem != null)
            {
                pointsSystem.OnPointsChanged -= HandlePointsChanged;
            }

            ReleaseBlurTexture();

            if (runtimeBlurMaterial != null)
            {
                Destroy(runtimeBlurMaterial);
                runtimeBlurMaterial = null;
            }

            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (IsOpen)
            {
                if (IsTogglePressed() || IsEscapePressed())
                {
                    Close();
                }
            }
            else if (IsTogglePressed() && CanOpenStore())
            {
                Open();
            }
        }

        private bool IsTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (toggleKey == KeyCode.T && kb.tKey.wasPressedThisFrame) return true;
                if (toggleKey == KeyCode.F && kb.fKey.wasPressedThisFrame) return true;
            }
#endif
            try
            {
                if (Input.GetKeyDown(toggleKey)) return true;
            }
            catch { }
            return false;
        }

        private bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) return true;
            }
            catch { }
            return false;
        }

        private bool CanOpenStore()
        {
            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
                return false;

            if (DialogueManager.Instance != null &&
                (DialogueManager.Instance.IsDialogueActive || DialogueManager.Instance.WasClosedThisFrame))
                return false;

            if (Utsuru.Achievements.AchievementUIController.Instance != null &&
                Utsuru.Achievements.AchievementUIController.Instance.IsOpen)
                return false;

            if (cachedDictionaryUI == null)
                cachedDictionaryUI = FindFirstObjectByType<DictionaryUIController>();
            if (cachedDictionaryUI != null && cachedDictionaryUI.IsOpen)
                return false;

            if (cachedPolaroidUI == null)
                cachedPolaroidUI = FindFirstObjectByType<PolaroidCardUI>();
            if (cachedPolaroidUI != null && cachedPolaroidUI.IsOpen)
                return false;

            return true;
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else if (CanOpenStore()) Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            if (!CanOpenStore()) return;

            if (themeManager == null)
                themeManager = StoreThemeManager.EnsureInstance();

            if (pointsSystem == null)
            {
                pointsSystem = PointRewardSystem.Instance ?? FindFirstObjectByType<PointRewardSystem>();
                if (pointsSystem != null)
                {
                    pointsSystem.OnPointsChanged -= HandlePointsChanged;
                    pointsSystem.OnPointsChanged += HandlePointsChanged;
                }
            }

            CaptureAndBlurBackground();

            if (CameraCaptureSystem.Instance != null)
                CameraCaptureSystem.Instance.InputBlocked = true;

            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            var cameraMode = CameraModeController.Instance ?? FindFirstObjectByType<CameraModeController>();
            if (cameraMode != null)
            {
                cameraMode.SetControlsLocked(true);
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (viewRoot != null) viewRoot.SetActive(true);

            RefreshUI();

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(FadeRoutine(true));
        }

        public void Close()
        {
            if (!IsOpen) return;

            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
                feedbackCoroutine = null;
            }

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(FadeRoutine(false, () =>
            {
                if (viewRoot != null) viewRoot.SetActive(false);
                if (feedbackBanner != null) feedbackBanner.SetActive(false);

                ReleaseBlurTexture();

                Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;

                bool dialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
                if (CameraCaptureSystem.Instance != null)
                    CameraCaptureSystem.Instance.InputBlocked = dialogueActive;

                var cameraMode = CameraModeController.Instance ?? FindFirstObjectByType<CameraModeController>();
                if (cameraMode != null)
                {
                    cameraMode.SetControlsLocked(dialogueActive);
                }
                else if (!dialogueActive)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }));
        }

        public void RefreshUI()
        {
            if (themeManager == null) themeManager = StoreThemeManager.EnsureInstance();

            int currentPoints = themeManager.GetCurrentPoints();
            if (pointsBalanceText != null)
            {
                pointsBalanceText.text = currentPoints.ToString();
            }

            IReadOnlyList<StoreThemeDefinition> items = themeManager.StoreItems;
            for (int i = 0; i < themeCards.Count; i++)
            {
                if (themeCards[i] == null) continue;

                if (i < items.Count)
                {
                    themeCards[i].gameObject.SetActive(true);
                    StoreThemeDefinition def = items[i];
                    bool isOwned = themeManager.IsOwned(def.id);
                    bool canAfford = currentPoints >= def.pricePoints;
                    Sprite previewSprite = GetPreviewSprite(def.id);
                    themeCards[i].Setup(def, previewSprite, isOwned, canAfford, HandleBuyThemeClicked);
                }
                else
                {
                    themeCards[i].gameObject.SetActive(false);
                }
            }
        }

        private Sprite GetPreviewSprite(string themeId)
        {
            if (themePreviews == null) return null;
            for (int i = 0; i < themePreviews.Count; i++)
            {
                if (themePreviews[i] != null &&
                    string.Equals(themePreviews[i].themeId, themeId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return themePreviews[i].previewSprite;
                }
            }
            return null;
        }

        private void HandleBuyThemeClicked(string themeId)
        {
            if (themeManager == null) themeManager = StoreThemeManager.EnsureInstance();

            bool success = themeManager.BuyTheme(themeId, out string message);
            ShowFeedback(message, success);
            RefreshUI();
        }

        private void HandlePointsChanged(int _)
        {
            if (IsOpen) RefreshUI();
        }

        private void HandleStateUpdated()
        {
            if (IsOpen) RefreshUI();
        }

        private void ShowFeedback(string message, bool isSuccess)
        {
            if (feedbackBanner == null || feedbackText == null) return;

            if (feedbackCoroutine != null)
                StopCoroutine(feedbackCoroutine);

            feedbackText.text = message;
            if (feedbackBackground != null)
            {
                feedbackBackground.color = isSuccess
                    ? new Color(0.20f, 0.56f, 0.28f, 0.96f)
                    : new Color(0.72f, 0.22f, 0.18f, 0.96f);
            }

            feedbackBanner.SetActive(true);
            feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
        }

        private IEnumerator HideFeedbackAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            feedbackCoroutine = null;
        }

        private Material ResolveBlurMaterial()
        {
            if (blurMaterial != null) return blurMaterial;
            if (runtimeBlurMaterial != null) return runtimeBlurMaterial;

            Material resMat = Resources.Load<Material>("AchievementsBlur");
            if (resMat != null)
            {
                runtimeBlurMaterial = new Material(resMat);
                return runtimeBlurMaterial;
            }

            Shader shader = Shader.Find("Hidden/Utsuru/UIBackgroundBlur");
            if (shader != null)
            {
                runtimeBlurMaterial = new Material(shader);
                return runtimeBlurMaterial;
            }

            return null;
        }

        private void CaptureAndBlurBackground()
        {
            if (blurBackdrop == null) return;

            ReleaseBlurTexture();

            Camera cam = null;
            var cameraMode = CameraModeController.Instance ?? FindFirstObjectByType<CameraModeController>();
            if (cameraMode != null && cameraMode.PlayerCamera != null)
                cam = cameraMode.PlayerCamera;
            if (cam == null) cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();

            Material mat = ResolveBlurMaterial();

            if (cam == null || mat == null)
            {
                blurBackdrop.enabled = false;
                if (tintOverlay != null) tintOverlay.color = new Color(0.08f, 0.05f, 0.03f, 0.72f);
                return;
            }

            int fullW = Mathf.Max(640, Screen.width);
            int fullH = Mathf.Max(360, Screen.height);
            int blurW = Mathf.Max(320, fullW / downsampleFactor);
            int blurH = Mathf.Max(180, fullH / downsampleFactor);

            RenderTexture captureRt = RenderTexture.GetTemporary(fullW, fullH, 24, RenderTextureFormat.Default);
            RenderTexture rtB = RenderTexture.GetTemporary(blurW, blurH, 0, RenderTextureFormat.Default);
            activeBlurRt = new RenderTexture(blurW, blurH, 0, RenderTextureFormat.Default)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            activeBlurRt.Create();

            RenderTexture prevTarget = cam.targetTexture;
            try
            {
                cam.targetTexture = captureRt;
                cam.Render();
            }
            finally
            {
                cam.targetTexture = prevTarget;
            }

            Graphics.Blit(captureRt, activeBlurRt);
            RenderTexture.ReleaseTemporary(captureRt);

            for (int i = 0; i < blurIterations; i++)
            {
                mat.SetFloat(BlurSizeId, blurSpread * (1f + i * 0.45f));
                Graphics.Blit(activeBlurRt, rtB, mat, 0);
                Graphics.Blit(rtB, activeBlurRt, mat, 1);
            }

            RenderTexture.ReleaseTemporary(rtB);

            blurBackdrop.texture = activeBlurRt;
            blurBackdrop.color = Color.white;
            blurBackdrop.enabled = true;

            if (tintOverlay != null)
            {
                tintOverlay.color = new Color(0.08f, 0.05f, 0.03f, 0.22f);
            }
        }

        private void ReleaseBlurTexture()
        {
            if (blurBackdrop != null)
            {
                blurBackdrop.texture = null;
                blurBackdrop.enabled = false;
            }

            if (activeBlurRt != null)
            {
                activeBlurRt.Release();
                Destroy(activeBlurRt);
                activeBlurRt = null;
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
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (viewCanvasGroup != null)
                    viewCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, ease);

                if (mainPanelRt != null)
                    mainPanelRt.localScale = Vector3.Lerp(startScale, targetScale, ease);

                yield return null;
            }

            if (viewCanvasGroup != null) viewCanvasGroup.alpha = targetAlpha;
            if (mainPanelRt != null) mainPanelRt.localScale = targetScale;

            onComplete?.Invoke();
            transitionCoroutine = null;
        }
    }
}
