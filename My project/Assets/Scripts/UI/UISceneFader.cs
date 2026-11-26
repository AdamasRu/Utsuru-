using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Utsuru.UI
{
    /// <summary>
    /// Persistent full-screen cross-scene fader so transitions between LoginPage,
    /// HomeScreen, and Day_Showcase feel smooth instead of snapping abruptly.
    /// </summary>
    public class UISceneFader : MonoBehaviour
    {
        private static UISceneFader instance;

        private CanvasGroup overlayGroup;
        private GraphicRaycaster raycaster;
        private bool isTransitioning;

        public static bool IsTransitioning => instance != null && instance.isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetRuntimeStateBeforeSceneLoad()
        {
            // Safeguard against scenes/ProjectSettings saved with timeScale == 0.
            Time.timeScale = 1f;
        }

        public static UISceneFader EnsureInstance()
        {
            if (instance != null) return instance;

            GameObject go = new GameObject("UISceneFader", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(UISceneFader));
            DontDestroyOnLoad(go);

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject fill = new GameObject("FadeOverlay", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(go.transform, false);
            RectTransform rt = fill.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            Image img = fill.GetComponent<Image>();
            img.color = new Color(0.11f, 0.08f, 0.07f, 1f); // Soft warm espresso dark tint
            img.raycastTarget = true;

            instance = go.GetComponent<UISceneFader>();
            instance.overlayGroup = go.GetComponent<CanvasGroup>();
            instance.raycaster = go.GetComponent<GraphicRaycaster>();
            instance.overlayGroup.alpha = 0f;
            instance.overlayGroup.blocksRaycasts = false;
            instance.overlayGroup.interactable = false;
            if (instance.raycaster != null)
            {
                instance.raycaster.enabled = false;
            }

            return instance;
        }

        public static void LoadSceneWithFade(string sceneName, float fadeOutDuration = 0.25f, float fadeInDuration = 0.30f)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            UISceneFader fader = EnsureInstance();
            if (fader.isTransitioning) return;
            fader.StartCoroutine(fader.LoadSceneRoutine(sceneName, fadeOutDuration, fadeInDuration));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, float fadeOutDuration, float fadeInDuration)
        {
            isTransitioning = true;
            overlayGroup.blocksRaycasts = true;
            if (raycaster != null) raycaster.enabled = true;

            yield return FadeRoutine(0f, 1f, fadeOutDuration);

            Time.timeScale = 1f;
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
            if (loadOp != null)
            {
                while (!loadOp.isDone)
                {
                    yield return null;
                }
            }

            Time.timeScale = 1f;

            // Allow one frame for the new scene's Start() / layout to settle before fading in.
            yield return null;

            yield return FadeRoutine(1f, 0f, fadeInDuration);

            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            if (raycaster != null) raycaster.enabled = false;
            isTransitioning = false;
        }

        private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration)
        {
            if (duration <= 0.001f)
            {
                overlayGroup.alpha = toAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t); // SmoothStep
                overlayGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
                yield return null;
            }

            overlayGroup.alpha = toAlpha;
        }
    }
}

