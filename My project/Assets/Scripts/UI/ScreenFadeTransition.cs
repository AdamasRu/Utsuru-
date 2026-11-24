using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.UI
{
    /// <summary>
    /// Global Screen Fade Transition Manager for Utsuru.
    /// Provides smooth pure white (#FFFFFF) fade transitions between screens,
    /// panels, and tabs without interfering with UI layout or permanently blocking input.
    /// </summary>
    public class ScreenFadeTransition : MonoBehaviour
    {
        public static ScreenFadeTransition Instance { get; private set; }

        [Header("Transition Settings")]
        [Tooltip("Fade out duration in seconds (screen goes to white)")]
        [SerializeField] private float fadeOutDuration = 0.35f;

        [Tooltip("Fade in duration in seconds (screen returns from white)")]
        [SerializeField] private float fadeInDuration = 0.45f;

        [Tooltip("Pure white overlay color")]
        [SerializeField] private Color transitionColor = Color.white;

        private Canvas transitionCanvas;
        private Image fadeOverlayImage;
        private CanvasGroup canvasGroup;
        private bool isTransitioning = false;

        public bool IsTransitioning => isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var go = new GameObject("[ScreenFadeTransition]");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<ScreenFadeTransition>();
                Instance.SetupOverlayCanvas();
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupOverlayCanvas();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void SetupOverlayCanvas()
        {
            if (transitionCanvas != null) return;

            // Create Canvas as a child
            var canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform, false);

            transitionCanvas = canvasObj.AddComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 9999; // Topmost, above everything

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Overlay Image
            var imageObj = new GameObject("WhiteOverlay");
            imageObj.transform.SetParent(canvasObj.transform, false);

            var rt = imageObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            fadeOverlayImage = imageObj.AddComponent<Image>();
            fadeOverlayImage.color = transitionColor;
            fadeOverlayImage.raycastTarget = true; // Blocks input during transition

            canvasGroup = imageObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            imageObj.SetActive(false);
        }

        /// <summary>
        /// Performs a smooth white fade transition.
        /// 1. Fades out to pure white.
        /// 2. Executes onCovered action while screen is fully white.
        /// 3. Fades in back to transparent.
        /// 4. Calls onComplete action (e.g. to start staggered button entrance).
        /// </summary>
        public void DoTransition(Action onCovered, Action onComplete = null)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[ScreenFadeTransition] A transition is already running. Ignoring duplicate request.");
                return;
            }

            StartCoroutine(TransitionRoutine(onCovered, onComplete));
        }

        /// <summary>
        /// Performs a white fade out before executing an exit action (e.g. Application.Quit).
        /// </summary>
        public void DoExitTransition(Action onExit)
        {
            if (isTransitioning) return;
            StartCoroutine(ExitRoutine(onExit));
        }

        private IEnumerator TransitionRoutine(Action onCovered, Action onComplete)
        {
            isTransitioning = true;
            EnsureComponents();

            fadeOverlayImage.gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;

            // 1. Fade Out to Pure White
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                // Ease In-Out
                canvasGroup.alpha = SmoothStep(t);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // Wait 1 frame to ensure fully covered rendering
            yield return null;

            // 2. Perform Navigation / Screen switch
            try
            {
                onCovered?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenFadeTransition] Exception during onCovered callback: {ex}");
            }

            // Brief hold while covered to hide popping
            yield return new WaitForSecondsRealtime(0.05f);

            // 3. Fade In from Pure White
            elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                canvasGroup.alpha = 1f - SmoothStep(t);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            fadeOverlayImage.gameObject.SetActive(false);

            isTransitioning = false;

            // 4. Trigger completion callback (e.g. staggered button entrance)
            try
            {
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenFadeTransition] Exception during onComplete callback: {ex}");
            }
        }

        private IEnumerator ExitRoutine(Action onExit)
        {
            isTransitioning = true;
            EnsureComponents();

            fadeOverlayImage.gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                canvasGroup.alpha = SmoothStep(t);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(0.1f);
            onExit?.Invoke();
        }

        private void EnsureComponents()
        {
            if (fadeOverlayImage == null || canvasGroup == null)
            {
                SetupOverlayCanvas();
            }
        }

        private float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
