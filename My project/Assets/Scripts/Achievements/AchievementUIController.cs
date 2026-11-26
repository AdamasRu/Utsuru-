using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;
using Utsuru.DictionaryUI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Utsuru.Achievements
{
    /// <summary>
    /// Controls the Achievements modal UI opened/closed with keybind 'Q' (and closed via 'Close' or 'Esc').
    /// Captures and Gaussian-blurs the 3D gameplay background while open.
    /// </summary>
    public class AchievementUIController : MonoBehaviour
    {
        public static AchievementUIController Instance { get; private set; }

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

        [Header("List & Controls")]
        [SerializeField] private Transform contentContainer;
        [SerializeField] private AchievementItemUI itemTemplate;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button closeButton;
        [SerializeField] private KeyCode toggleKey = KeyCode.Q;

        public bool IsOpen => viewRoot != null && viewRoot.activeSelf;

        private static readonly int BlurSizeId = Shader.PropertyToID("_BlurSize");

        private readonly List<AchievementItemUI> spawnedItems = new List<AchievementItemUI>();
        private RenderTexture activeBlurRt;
        private Material runtimeBlurMaterial;
        private float prevTimeScale = 1f;
        private Coroutine transitionCoroutine;

        private DictionaryUIController cachedDictionaryUI;
        private PolaroidCardUI cachedPolaroidUI;
        private AchievementManager achievementManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AchievementUIController] Duplicate instance detected; destroying duplicate.", this);
                Destroy(this);
                return;
            }
            Instance = this;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (itemTemplate != null && itemTemplate.gameObject.activeSelf)
            {
                itemTemplate.gameObject.SetActive(false);
            }

            if (viewRoot != null) viewRoot.SetActive(false);
            if (viewCanvasGroup != null) viewCanvasGroup.alpha = 0f;
            if (blurBackdrop != null) blurBackdrop.enabled = false;
        }

        private void Start()
        {
            achievementManager = AchievementManager.EnsureInstance();
            if (achievementManager != null)
            {
                achievementManager.OnAchievementsUpdated += HandleAchievementsUpdated;
            }
        }

        private void OnDestroy()
        {
            if (achievementManager != null)
            {
                achievementManager.OnAchievementsUpdated -= HandleAchievementsUpdated;
            }

            ReleaseBlurResources();

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
            else if (IsTogglePressed() && CanOpenAchievements())
            {
                Open();
            }
        }

        private bool IsTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.qKey.wasPressedThisFrame) return true;
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

        private bool CanOpenAchievements()
        {
            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
                return false;

            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                return false;

            if (Utsuru.Store.StoreUIController.Instance != null && Utsuru.Store.StoreUIController.Instance.IsOpen)
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
            else if (CanOpenAchievements()) Open();
        }

        public void Open()
        {
            if (IsOpen) return;

            if (achievementManager == null)
                achievementManager = AchievementManager.EnsureInstance();

            achievementManager.EvaluateAll();

            // Capture and blur the 3D scene BEFORE showing the modal UI
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
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (viewRoot != null) viewRoot.SetActive(true);

            RefreshList();

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(FadeRoutine(true));
        }

        public void Close()
        {
            if (!IsOpen) return;

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(FadeRoutine(false, () =>
            {
                if (viewRoot != null) viewRoot.SetActive(false);

                ReleaseBlurTexture();

                Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;

                if (CameraCaptureSystem.Instance != null)
                    CameraCaptureSystem.Instance.InputBlocked = false;

                var cameraMode = CameraModeController.Instance ?? FindFirstObjectByType<CameraModeController>();
                if (cameraMode != null)
                {
                    cameraMode.SetControlsLocked(false);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }));
        }

        public void RefreshList()
        {
            if (contentContainer == null || itemTemplate == null) return;
            if (achievementManager == null) achievementManager = AchievementManager.EnsureInstance();

            IReadOnlyList<AchievementDefinition> items = achievementManager.AllAchievements;

            while (spawnedItems.Count < items.Count)
            {
                AchievementItemUI clone = Instantiate(itemTemplate, contentContainer);
                clone.name = $"AchievementItem_{spawnedItems.Count + 1}";
                spawnedItems.Add(clone);
            }

            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (i < items.Count)
                {
                    spawnedItems[i].gameObject.SetActive(true);
                    bool unlocked = achievementManager.IsUnlocked(items[i].id);
                    bool claimed = achievementManager.IsRewardClaimed(items[i].id);
                    spawnedItems[i].Setup(items[i], unlocked, claimed, HandleCollectClicked);
                }
                else
                {
                    spawnedItems[i].gameObject.SetActive(false);
                }
            }

            if (contentContainer is RectTransform contentRt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            }
        }

        private void HandleCollectClicked(string achievementId)
        {
            if (achievementManager == null) achievementManager = AchievementManager.EnsureInstance();
            if (achievementManager != null && achievementManager.ClaimReward(achievementId))
            {
                RefreshList();
            }
        }

        private void HandleAchievementsUpdated()
        {
            if (IsOpen)
            {
                RefreshList();
            }
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

        private void ReleaseBlurResources()
        {
            ReleaseBlurTexture();
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
