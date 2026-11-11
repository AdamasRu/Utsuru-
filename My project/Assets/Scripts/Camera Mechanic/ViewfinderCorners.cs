using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.Capture
{
    /// <summary>
    /// Menampilkan overlay UI Viewfinder kamera yang modern dan estetik:
    /// - 4 Siku pembingkai layar (framing corners) bergaris bersih dan kontras.
    /// - 4 Siku reticle fokus tengah yang berubah warna (hijau) saat objek interaktif terdeteksi.
    /// - Garis pemandu bidikan (guide ticks) dan teks status kamera.
    /// </summary>
    public class ViewfinderCorners : MonoBehaviour
    {
        [Header("Garis Sudut Pembingkai (Outer Frame)")]
        [SerializeField] private Color frameColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
        [Tooltip("Jarak siku dari tepi layar.")]
        [SerializeField] private float margin = 45f;
        [Tooltip("Panjang lengan siku luar.")]
        [SerializeField] private float armLength = 75f;
        [SerializeField] private float thickness = 3.5f;

        [Header("Reticle Fokus Tengah (Center Focus Box)")]
        [SerializeField] private Color idleFocusColor = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color lockedFocusColor = new Color(0.2f, 1f, 0.5f, 0.95f);
        [SerializeField] private float focusBoxSize = 70f;
        [SerializeField] private float focusArmLength = 16f;
        [SerializeField] private float focusThickness = 2.5f;

        [Header("Teks Status OSD")]
        [SerializeField] private bool showCameraOSD = true;

        private readonly List<Image> focusBracketImages = new List<Image>();
        private InteractableDetector detector;
        private bool overlayBuilt;

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnEnable()
        {
            EnsureBuilt();

            if (detector == null) detector = FindFirstObjectByType<InteractableDetector>();
            if (detector != null)
            {
                detector.OnTargetChanged += HandleTargetChanged;
                HandleTargetChanged(detector.CurrentTarget);
            }
        }

        private void OnDisable()
        {
            if (detector != null) detector.OnTargetChanged -= HandleTargetChanged;
        }

        private void HandleTargetChanged(InteractableObject target)
        {
            Color targetColor = (target != null) ? lockedFocusColor : idleFocusColor;
            for (int i = 0; i < focusBracketImages.Count; i++)
            {
                if (focusBracketImages[i] != null) focusBracketImages[i].color = targetColor;
            }
        }

        private void EnsureBuilt()
        {
            if (overlayBuilt) return;
            overlayBuilt = true;

            var container = new GameObject("__CameraViewfinderOverlay", typeof(RectTransform));
            container.transform.SetParent(transform, false);

            var rootRect = container.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // 1. Empat sudut pembingkai luar (Outer Framing Corners)
            BuildCornerBracket(container.transform, "Corner_TopLeft", 0, 1, margin, armLength, thickness);
            BuildCornerBracket(container.transform, "Corner_TopRight", 1, 1, margin, armLength, thickness);
            BuildCornerBracket(container.transform, "Corner_BottomLeft", 0, 0, margin, armLength, thickness);
            BuildCornerBracket(container.transform, "Corner_BottomRight", 1, 0, margin, armLength, thickness);

            // 2. Empat siku reticle fokus tengah (Center Focus Reticle)
            BuildCenterFocusBox(container.transform);

            // 3. Garis pemandu (Crosshair Guide Ticks)
            BuildGuideTicks(container.transform);

            // 4. OSD Status Kamera
            if (showCameraOSD)
            {
                BuildCameraOSD(container.transform);
            }
        }

        private void BuildCornerBracket(Transform parent, string name, int ax, int ay, float inset, float len, float thk)
        {
            var bracket = new GameObject(name, typeof(RectTransform));
            bracket.transform.SetParent(parent, false);

            var rect = bracket.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(ax, ay);
            rect.anchorMax = new Vector2(ax, ay);
            rect.pivot = new Vector2(ax, ay);

            float posX = ax == 0 ? inset : -inset;
            float posY = ay == 0 ? inset : -inset;
            rect.anchoredPosition = new Vector2(posX, posY);
            rect.sizeDelta = new Vector2(len, len);

            // Batang Horizontal & Vertikal (dengan shadow ganda untuk kontras tinggi di semua latar)
            CreateBar(bracket.transform, "H_Shadow", ax, ay, new Vector2(len + 2, thk + 2), new Vector2(0, 0), shadowColor);
            CreateBar(bracket.transform, "V_Shadow", ax, ay, new Vector2(thk + 2, len + 2), new Vector2(0, 0), shadowColor);

            CreateBar(bracket.transform, "H", ax, ay, new Vector2(len, thk), new Vector2(0, 0), frameColor);
            CreateBar(bracket.transform, "V", ax, ay, new Vector2(thk, len), new Vector2(0, 0), frameColor);
        }

        private void BuildCenterFocusBox(Transform parent)
        {
            var centerBox = new GameObject("Center_FocusBox", typeof(RectTransform));
            centerBox.transform.SetParent(parent, false);

            var rect = centerBox.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(focusBoxSize, focusBoxSize);

            float half = focusBoxSize * 0.5f;

            // Buat 4 sudut fokus kecil
            BuildFocusCorner(centerBox.transform, "Focus_TL", -half, half, 0, 1);
            BuildFocusCorner(centerBox.transform, "Focus_TR", half, half, 1, 1);
            BuildFocusCorner(centerBox.transform, "Focus_BL", -half, -half, 0, 0);
            BuildFocusCorner(centerBox.transform, "Focus_BR", half, -half, 1, 0);
        }

        private void BuildFocusCorner(Transform parent, string name, float x, float y, int ax, int ay)
        {
            var corner = new GameObject(name, typeof(RectTransform));
            corner.transform.SetParent(parent, false);

            var rect = corner.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(ax, ay);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(focusArmLength, focusArmLength);

            var hImg = CreateBar(corner.transform, "H", ax, ay, new Vector2(focusArmLength, focusThickness), Vector2.zero, idleFocusColor);
            var vImg = CreateBar(corner.transform, "V", ax, ay, new Vector2(focusThickness, focusArmLength), Vector2.zero, idleFocusColor);

            focusBracketImages.Add(hImg);
            focusBracketImages.Add(vImg);
        }

        private void BuildGuideTicks(Transform parent)
        {
            float dist = focusBoxSize * 0.5f + 25f;
            float tickLen = 10f;
            float tickThk = 2f;
            Color tickColor = new Color(1f, 1f, 1f, 0.5f);

            CreateCenteredTick(parent, "Tick_Top", 0, dist, new Vector2(tickThk, tickLen), tickColor);
            CreateCenteredTick(parent, "Tick_Bottom", 0, -dist, new Vector2(tickThk, tickLen), tickColor);
            CreateCenteredTick(parent, "Tick_Left", -dist, 0, new Vector2(tickLen, tickThk), tickColor);
            CreateCenteredTick(parent, "Tick_Right", dist, 0, new Vector2(tickLen, tickThk), tickColor);
        }

        private void CreateCenteredTick(Transform parent, string name, float x, float y, Vector2 size, Color c)
        {
            var tick = new GameObject(name, typeof(RectTransform), typeof(Image));
            tick.transform.SetParent(parent, false);

            var rect = tick.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = size;

            var img = tick.GetComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
        }

        private void BuildCameraOSD(Transform parent)
        {
            // Teks Status Atas Kiri: ● [KAMERA AKTIF]
            var osdTop = new GameObject("OSD_TopInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
            osdTop.transform.SetParent(parent, false);

            var rectTop = osdTop.GetComponent<RectTransform>();
            rectTop.anchorMin = new Vector2(0f, 1f);
            rectTop.anchorMax = new Vector2(0f, 1f);
            rectTop.pivot = new Vector2(0f, 1f);
            rectTop.anchoredPosition = new Vector2(margin + 10f, -margin - 12f);
            rectTop.sizeDelta = new Vector2(300f, 35f);

            var tmpTop = osdTop.GetComponent<TextMeshProUGUI>();
            tmpTop.text = "● <b>KAMERA AKTIF</b> [F/2.8  1/250]";
            tmpTop.fontSize = 17;
            tmpTop.color = new Color(1f, 1f, 1f, 0.9f);
            tmpTop.raycastTarget = false;

            // Teks Petunjuk Bawah Tengah: [C] Tutup  |  [Klik Kiri] Jepret
            var osdBottom = new GameObject("OSD_ControlsHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            osdBottom.transform.SetParent(parent, false);

            var rectBottom = osdBottom.GetComponent<RectTransform>();
            rectBottom.anchorMin = new Vector2(0.5f, 0f);
            rectBottom.anchorMax = new Vector2(0.5f, 0f);
            rectBottom.pivot = new Vector2(0.5f, 0f);
            rectBottom.anchoredPosition = new Vector2(0f, margin + 12f);
            rectBottom.sizeDelta = new Vector2(600f, 35f);

            var tmpBottom = osdBottom.GetComponent<TextMeshProUGUI>();
            tmpBottom.text = "<color=#FFFF88>[C]</color> Keluar Kamera    <color=#FFFF88>[KLIK KIRI]</color> Ambil Foto";
            tmpBottom.fontSize = 16;
            tmpBottom.alignment = TextAlignmentOptions.Center;
            tmpBottom.color = new Color(1f, 1f, 1f, 0.85f);
            tmpBottom.raycastTarget = false;
        }

        private Image CreateBar(Transform parent, string barName, int ax, int ay, Vector2 size, Vector2 position, Color barColor)
        {
            var go = new GameObject(barName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(ax, ay);
            rect.anchorMax = new Vector2(ax, ay);
            rect.pivot = new Vector2(ax, ay);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = go.GetComponent<Image>();
            image.color = barColor;
            image.raycastTarget = false;
            return image;
        }
    }
}
