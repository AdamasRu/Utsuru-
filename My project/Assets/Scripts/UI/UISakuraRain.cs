using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.UI
{
    /// <summary>
    /// Renders a lightweight, non-intrusive sakura petal rain inside UI canvases.
    /// Uses Assets/Resources/UI/SakuraPetal.png (with a curved fallback) and supports
    /// smooth visibility fades so Options/Settings stays clean without jarring pops.
    /// </summary>
    public class UISakuraRain : MonoBehaviour
    {
        private const string PetalResourcePath = "UI/SakuraPetal";

        [Header("Petal Settings")]
        [SerializeField] private Sprite customPetalSprite;
        [SerializeField] private int petalCount = 24;
        [SerializeField] private Vector2 petalSizeRange = new Vector2(20f, 36f);
        [SerializeField] private Vector2 fallSpeedRange = new Vector2(42f, 88f);
        [SerializeField] private Vector2 driftSpeedRange = new Vector2(-18f, 34f);
        [SerializeField] private float swayAmplitude = 26f;

        private struct PetalState
        {
            public RectTransform rect;
            public Image image;
            public Vector2 position;
            public float size;
            public float fallSpeed;
            public float driftSpeed;
            public float swayFreq;
            public float swayPhase;
            public float rotation;
            public float rotSpeed;
            public float flutterFreq;
            public float flutterPhase;
            public float baseAlpha;
        }

        private RectTransform containerRect;
        private CanvasGroup canvasGroup;
        private readonly List<PetalState> petals = new List<PetalState>();
        private static Sprite cachedPetalSprite;
        private Coroutine visibilityCoroutine;
        private bool initialized;
        private float lastBoundsHeight;

        private static readonly Color[] PetalPalette = new Color[]
        {
            new Color(1.00f, 0.98f, 0.99f, 1f), // Pure luminous sakura highlight
            new Color(1.00f, 0.92f, 0.95f, 1f), // Soft blush tint
            new Color(1.00f, 0.87f, 0.92f, 1f), // Pastel cherry blossom
            new Color(0.99f, 0.95f, 0.97f, 1f), // Pale cream petal
        };

        /// <summary>
        /// Ensures a UISakuraRain instance exists in the active scene under its Canvas.
        /// </summary>
        public static UISakuraRain EnsureForCanvas(Canvas canvas)
        {
            if (canvas == null) return null;

            UISakuraRain existing = FindFirstObjectByType<UISakuraRain>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.InitializeIfNeeded();
                return existing;
            }

            GameObject go = new GameObject("SakuraRainLayer", typeof(RectTransform), typeof(CanvasGroup), typeof(UISakuraRain));
            go.transform.SetParent(canvas.transform, false);

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = false;

            UISakuraRain rain = go.GetComponent<UISakuraRain>();
            rain.InitializeIfNeeded();
            return rain;
        }

        /// <summary>
        /// Places the sakura overlay directly above SharedBackground on the Canvas (siblingIndex = 1)
        /// so petals stay continuous across panel switches behind foreground UI.
        /// </summary>
        public void AttachToCanvasBackgroundLayer(Canvas canvas, bool visible = true)
        {
            InitializeIfNeeded();
            if (canvas == null) return;

            if (transform.parent != canvas.transform)
            {
                transform.SetParent(canvas.transform, false);
                StretchToFill(containerRect);
            }

            int desiredIndex = canvas.transform.Find("SharedBackground") != null ? 1 : 0;
            if (transform.GetSiblingIndex() != desiredIndex)
            {
                transform.SetSiblingIndex(desiredIndex);
            }

            SetVisible(visible, immediate: true);
        }

        /// <summary>
        /// Smoothly shows or hides the sakura rain without popping.
        /// </summary>
        public void SetVisible(bool visible, bool immediate = false)
        {
            InitializeIfNeeded();
            if (canvasGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            if (visibilityCoroutine != null)
            {
                StopCoroutine(visibilityCoroutine);
                visibilityCoroutine = null;
            }

            if (immediate)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                gameObject.SetActive(visible);
                return;
            }

            if (visible && !gameObject.activeSelf)
            {
                canvasGroup.alpha = 0f;
                gameObject.SetActive(true);
            }

            if (!gameObject.activeSelf && !visible) return;

            visibilityCoroutine = StartCoroutine(FadeVisibilityRoutine(visible ? 1f : 0f, visible));
        }

        /// <summary>
        /// Kept for backwards compatibility if a caller attaches directly to a specific panel.
        /// </summary>
        public void AttachToPanel(GameObject targetPanel)
        {
            InitializeIfNeeded();

            if (targetPanel == null)
            {
                SetVisible(false);
                return;
            }

            Transform targetTransform = targetPanel.transform;
            if (transform.parent != targetTransform)
            {
                transform.SetParent(targetTransform, false);
                StretchToFill(containerRect);
            }

            int desiredIndex = targetTransform.childCount > 1 ? 1 : 0;
            if (transform.GetSiblingIndex() != desiredIndex)
            {
                transform.SetSiblingIndex(desiredIndex);
            }

            SetVisible(true, immediate: true);
        }

        private IEnumerator FadeVisibilityRoutine(float targetAlpha, bool stayActive)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            const float duration = 0.18f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            if (!stayActive)
            {
                gameObject.SetActive(false);
            }
            visibilityCoroutine = null;
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void InitializeIfNeeded()
        {
            if (initialized) return;
            initialized = true;

            containerRect = GetComponent<RectTransform>();
            if (containerRect == null)
            {
                containerRect = gameObject.AddComponent<RectTransform>();
            }
            StretchToFill(containerRect);

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false;

            Sprite sprite = GetOrCreatePetalSprite();
            Vector2 bounds = GetReferenceBounds();
            lastBoundsHeight = bounds.y;

            for (int i = 0; i < petalCount; i++)
            {
                GameObject pGo = new GameObject($"SakuraPetal_{i:00}", typeof(RectTransform), typeof(Image));
                pGo.transform.SetParent(containerRect, false);

                Image img = pGo.GetComponent<Image>();
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.preserveAspect = true;

                RectTransform rt = pGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                PetalState state = CreatePetalState(rt, img, bounds, prewarmY: true);
                ApplyPetalTransform(state, 0f);
                petals.Add(state);
            }
        }

        private void Update()
        {
            if (petals.Count == 0) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (dt <= 0f) return;

            Vector2 bounds = GetReferenceBounds();
            if (Mathf.Abs(bounds.y - lastBoundsHeight) > 10f)
            {
                float ratio = bounds.y / Mathf.Max(1f, lastBoundsHeight);
                lastBoundsHeight = bounds.y;
                for (int i = 0; i < petals.Count; i++)
                {
                    PetalState resized = petals[i];
                    resized.position *= ratio;
                    resized.size *= ratio;
                    resized.rect.sizeDelta = new Vector2(resized.size, resized.size * 1.18f);
                    petals[i] = resized;
                }
            }

            float scaleFactor = Mathf.Clamp(bounds.y / 1080f, 0.35f, 2f);
            float halfW = bounds.x * 0.5f + 60f * scaleFactor;
            float topY = bounds.y * 0.5f + 50f * scaleFactor;
            float bottomY = -bounds.y * 0.5f - 50f * scaleFactor;
            float t = Time.unscaledTime;

            for (int i = 0; i < petals.Count; i++)
            {
                PetalState p = petals[i];
                float sway = Mathf.Sin(t * p.swayFreq + p.swayPhase) * swayAmplitude * scaleFactor;
                p.position.y -= p.fallSpeed * scaleFactor * dt;
                p.position.x += (p.driftSpeed * scaleFactor + sway * 0.45f) * dt;
                p.rotation += p.rotSpeed * dt;

                if (p.position.y < bottomY || p.position.x > halfW + 40f * scaleFactor || p.position.x < -halfW - 40f * scaleFactor)
                {
                    p = CreatePetalState(p.rect, p.image, bounds, prewarmY: false);
                    p.position.y = topY + Random.Range(0f, 45f * scaleFactor);
                }

                ApplyPetalTransform(p, t);
                petals[i] = p;
            }
        }

        private PetalState CreatePetalState(RectTransform rt, Image img, Vector2 bounds, bool prewarmY)
        {
            float scaleFactor = Mathf.Clamp(bounds.y / 1080f, 0.35f, 2f);
            float halfW = bounds.x * 0.5f;
            float halfH = bounds.y * 0.5f;
            float size = Random.Range(petalSizeRange.x, petalSizeRange.y) * scaleFactor;
            float baseAlpha = Random.Range(0.36f, 0.68f);

            Color col = PetalPalette[Random.Range(0, PetalPalette.Length)];
            col.a = baseAlpha;
            img.color = col;
            rt.sizeDelta = new Vector2(size, size * 1.18f);

            return new PetalState
            {
                rect = rt,
                image = img,
                size = size,
                position = new Vector2(
                    Random.Range(-halfW - 20f * scaleFactor, halfW + 20f * scaleFactor),
                    prewarmY ? Random.Range(-halfH, halfH) : halfH + Random.Range(20f, 80f) * scaleFactor),
                fallSpeed = Random.Range(fallSpeedRange.x, fallSpeedRange.y),
                driftSpeed = Random.Range(driftSpeedRange.x, driftSpeedRange.y),
                swayFreq = Random.Range(0.8f, 1.8f),
                swayPhase = Random.Range(0f, Mathf.PI * 2f),
                rotation = Random.Range(0f, 360f),
                rotSpeed = Random.Range(-75f, 75f),
                flutterFreq = Random.Range(1.4f, 3.0f),
                flutterPhase = Random.Range(0f, Mathf.PI * 2f),
                baseAlpha = baseAlpha,
            };
        }

        private static void ApplyPetalTransform(in PetalState p, float time)
        {
            if (p.rect == null) return;
            p.rect.anchoredPosition = p.position;
            p.rect.localRotation = Quaternion.Euler(0f, 0f, p.rotation);
            float flutter = 0.42f + 0.58f * Mathf.Abs(Mathf.Cos(time * p.flutterFreq + p.flutterPhase));
            p.rect.localScale = new Vector3(1f, flutter, 1f);
        }

        private Vector2 GetReferenceBounds()
        {
            if (containerRect != null)
            {
                Vector2 rectSize = containerRect.rect.size;
                if (rectSize.x > 100f && rectSize.y > 100f)
                {
                    return rectSize;
                }
            }

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                RectTransform canvasRt = rootCanvas.GetComponent<RectTransform>();
                if (canvasRt != null && canvasRt.rect.width > 100f && canvasRt.rect.height > 100f)
                {
                    return canvasRt.rect.size;
                }
            }

            return new Vector2(1920f, 1080f);
        }

        private static void StretchToFill(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private Sprite GetOrCreatePetalSprite()
        {
            if (customPetalSprite != null) return customPetalSprite;

            if (cachedPetalSprite != null) return cachedPetalSprite;

            Sprite loaded = Resources.Load<Sprite>(PetalResourcePath);
            if (loaded != null)
            {
                cachedPetalSprite = loaded;
                return cachedPetalSprite;
            }

            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ProceduralSakuraPetal",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float ny = (y / (float)(size - 1)) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x / (float)(size - 1)) * 2f - 1f;
                    float ax = nx - 0.05f * (1f - ny * ny);
                    float absX = Mathf.Abs(ax);

                    float t = Mathf.Clamp01((ny + 0.88f) / 1.74f);
                    float halfWidth = 0.62f * Mathf.Pow(Mathf.Sin(t * Mathf.PI * 0.84f), 0.78f);
                    float notch = 0.30f * Mathf.Exp(-(absX * absX) / (0.145f * 0.145f));
                    float lobeDrop = 0.42f * Mathf.Pow(Mathf.Max(0f, absX - 0.34f), 2f) + 0.18f * absX * absX;
                    float topEdge = 0.86f - notch - lobeDrop;
                    float bottomEdge = -0.88f + 0.32f * absX * absX;

                    float dx = absX - halfWidth;
                    float dyTop = ny - topEdge;
                    float dyBot = bottomEdge - ny;

                    float outX = Mathf.Max(0f, dx);
                    float outTop = Mathf.Max(0f, dyTop);
                    float outBot = Mathf.Max(0f, dyBot);
                    float outside = Mathf.Sqrt(outX * outX + outTop * outTop + outBot * outBot);
                    float inside = Mathf.Min(0f, Mathf.Max(dx, Mathf.Max(dyTop, dyBot)));
                    float sdf = outside + inside;

                    float alpha = 1f - Mathf.Clamp01((sdf + 0.025f) / 0.05f);
                    pixels[y * size + x] = new Color(1f, 0.94f, 0.96f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            cachedPetalSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            cachedPetalSprite.name = "ProceduralSakuraPetalSprite";
            return cachedPetalSprite;
        }
    }
}
