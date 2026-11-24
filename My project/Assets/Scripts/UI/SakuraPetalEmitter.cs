using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.UI
{
    /// <summary>
    /// Sakura Falling Petals Particle Controller for UI Canvas.
    /// Spawns delicate individual sakura petals matching the custom SakuraPetal sprites.
    /// Rendered size is kept realistically small (approximately 6-12px length, 2-5px width)
    /// with gentle falling from the TOP edge, horizontal breeze drifting, swaying,
    /// subtle rotation, and smooth recycling at the bottom edge.
    /// </summary>
    public class SakuraPetalEmitter : MonoBehaviour
    {
        [Header("Petal Appearance & Sprites")]
        [Tooltip("The custom individual sakura petal sprites (variations 01, 02, 03).")]
        [SerializeField] private Sprite[] petalSprites;

        [Tooltip("Number of active sakura petals falling simultaneously across screen (target: 20-35).")]
        [SerializeField] private int petalCount = 30;

        [Header("Size Settings (Scaled 2x from Latest Implementation / 200% Scale)")]
        [Tooltip("Target base pixel width of petals (scaled by 2x from 9-16.5px to 18-33px).")]
        [SerializeField] private float minPixelWidth = 18f;
        [SerializeField] private float maxPixelWidth = 33f;

        [Tooltip("Target base pixel height of petals (scaled by 2x from 21-36px to 42-72px).")]
        [SerializeField] private float minPixelHeight = 42f;
        [SerializeField] private float maxPixelHeight = 72f;

        [Header("Falling & Drifting Movement")]
        [SerializeField] private float minFallSpeed = 40f;
        [SerializeField] private float maxFallSpeed = 85f;
        [SerializeField] private float minDriftSpeed = -20f;
        [SerializeField] private float maxDriftSpeed = 30f;

        [Header("Sway & Fluttering")]
        [SerializeField] private float minSwayAmplitude = 20f;
        [SerializeField] private float maxSwayAmplitude = 50f;
        [SerializeField] private float minSwayFrequency = 0.8f;
        [SerializeField] private float maxSwayFrequency = 1.6f;

        [Header("Rotation")]
        [SerializeField] private float minRotationSpeed = 15f;
        [SerializeField] private float maxRotationSpeed = 50f;

        [Header("Opacity")]
        [SerializeField] private float minAlpha = 0.55f;
        [SerializeField] private float maxAlpha = 0.90f;

        private class PetalData
        {
            public RectTransform rect;
            public Image image;
            public CanvasGroup canvasGroup;
            public Vector2 baseSize;
            public float fallSpeed;
            public float driftSpeed;
            public float swayAmplitude;
            public float swayFrequency;
            public float swayPhase;
            public float rotationSpeed;
            public float rotationDirection;
            public float targetAlpha;
            public float xBase;
            public float yPos;
            public float currentRotZ;
            public float flutterTimer;
            public float flutterSpeed;
        }

        private RectTransform canvasRect;
        private List<PetalData> petals = new List<PetalData>();
        private bool isInitialized = false;

        private void Awake()
        {
            InitializePetals();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                InitializePetals();
            }
        }

        public void InitializePetals()
        {
            if (isInitialized) return;

            // Find parent Canvas RectTransform
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasRect = parentCanvas.GetComponent<RectTransform>();
            }
            if (canvasRect == null)
            {
                canvasRect = GetComponent<RectTransform>();
            }

            // Load sprite variations if not assigned
            if (petalSprites == null || petalSprites.Length == 0)
            {
                LoadPetalSprites();
            }

            // Create pooled petals
            for (int i = 0; i < petalCount; i++)
            {
                PetalData petal = CreatePetal();
                // Randomize initial Y across the screen for immediate continuous effect on start
                ResetPetal(petal, randomizeInitialY: true);
                petals.Add(petal);
            }

            isInitialized = true;
        }

        private void LoadPetalSprites()
        {
            List<Sprite> loaded = new List<Sprite>();
            for (int i = 1; i <= 3; i++)
            {
                var s = Resources.Load<Sprite>($"UI/Sakura/SakuraPetal_0{i}");
#if UNITY_EDITOR
                if (s == null)
                {
                    s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/UI/Sakura/SakuraPetal_0{i}.png");
                }
#endif
                if (s != null)
                {
                    loaded.Add(s);
                }
            }

            if (loaded.Count > 0)
            {
                petalSprites = loaded.ToArray();
            }
        }

        private PetalData CreatePetal()
        {
            var go = new GameObject("SakuraPetal", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;

            // Pick a sprite variation
            Sprite selectedSprite = null;
            if (petalSprites != null && petalSprites.Length > 0)
            {
                selectedSprite = petalSprites[Random.Range(0, petalSprites.Length)];
            }

            if (selectedSprite != null)
            {
                img.sprite = selectedSprite;
                img.preserveAspect = true;
            }

            // Size: small & delicate (target 2-5px width, 6-12px height)
            float targetW = Random.Range(minPixelWidth, maxPixelWidth);
            float targetH = Random.Range(minPixelHeight, maxPixelHeight);
            rt.sizeDelta = new Vector2(targetW, targetH);

            return new PetalData
            {
                rect = rt,
                image = img,
                canvasGroup = cg,
                baseSize = new Vector2(targetW, targetH)
            };
        }

        private void ResetPetal(PetalData petal, bool randomizeInitialY = false)
        {
            float halfWidth = canvasRect != null ? canvasRect.rect.width * 0.5f : 960f;
            float halfHeight = canvasRect != null ? canvasRect.rect.height * 0.5f : 540f;

            // Spawn across screen width plus slight margin for drift
            petal.xBase = Random.Range(-halfWidth - 80f, halfWidth + 80f);

            if (randomizeInitialY)
            {
                // Initial load: spread vertically across the screen
                petal.yPos = Random.Range(-halfHeight - 20f, halfHeight + 60f);
            }
            else
            {
                // Respawn behavior: strictly from the TOP edge
                petal.yPos = halfHeight + Random.Range(10f, 80f);
            }

            // Dynamic size variation per respawn
            float targetW = Random.Range(minPixelWidth, maxPixelWidth);
            float targetH = Random.Range(minPixelHeight, maxPixelHeight);
            petal.baseSize = new Vector2(targetW, targetH);
            petal.rect.sizeDelta = petal.baseSize;

            // Randomize sprite variation upon respawn
            if (petalSprites != null && petalSprites.Length > 0)
            {
                petal.image.sprite = petalSprites[Random.Range(0, petalSprites.Length)];
            }

            petal.fallSpeed = Random.Range(minFallSpeed, maxFallSpeed);
            petal.driftSpeed = Random.Range(minDriftSpeed, maxDriftSpeed);
            petal.swayAmplitude = Random.Range(minSwayAmplitude, maxSwayAmplitude);
            petal.swayFrequency = Random.Range(minSwayFrequency, maxSwayFrequency);
            petal.swayPhase = Random.Range(0f, Mathf.PI * 2f);
            petal.rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
            petal.rotationDirection = Random.value > 0.5f ? 1f : -1f;
            petal.currentRotZ = Random.Range(0f, 360f);
            petal.targetAlpha = Random.Range(minAlpha, maxAlpha);
            petal.flutterTimer = Random.Range(0f, 10f);
            petal.flutterSpeed = Random.Range(1.2f, 2.8f);

            petal.canvasGroup.alpha = petal.targetAlpha;
            petal.rect.anchoredPosition = new Vector2(petal.xBase, petal.yPos);
            petal.rect.localRotation = Quaternion.Euler(0f, 0f, petal.currentRotZ);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float halfHeight = canvasRect != null ? canvasRect.rect.height * 0.5f : 540f;
            float bottomLimit = -halfHeight - 40f;

            for (int i = 0; i < petals.Count; i++)
            {
                PetalData p = petals[i];

                // 1. Vertical falling
                p.yPos -= p.fallSpeed * dt;

                // 2. Horizontal drifting & gentle sine swaying
                p.xBase += p.driftSpeed * dt;
                float swayOffset = Mathf.Sin(Time.time * p.swayFrequency + p.swayPhase) * p.swayAmplitude;
                float currentX = p.xBase + swayOffset;

                // 3. Subtle rotation while falling
                p.currentRotZ += p.rotationDirection * p.rotationSpeed * dt;

                // 4. Subtle 3D fluttering / leaf tilt simulation (scale X oscillates like a tumbling petal)
                p.flutterTimer += dt * p.flutterSpeed;
                float flutterScaleX = 0.4f + 0.6f * Mathf.Abs(Mathf.Cos(p.flutterTimer));

                p.rect.anchoredPosition = new Vector2(currentX, p.yPos);
                p.rect.localRotation = Quaternion.Euler(0f, 0f, p.currentRotZ);
                p.rect.localScale = new Vector3(flutterScaleX, 1f, 1f);

                // 5. Respawn at top when reached bottom edge
                if (p.yPos < bottomLimit)
                {
                    ResetPetal(p, randomizeInitialY: false);
                }
            }
        }
    }
}
