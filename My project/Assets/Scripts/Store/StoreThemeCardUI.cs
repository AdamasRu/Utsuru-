using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.Store
{
    /// <summary>
    /// Komponen UI untuk satu kartu tema di modal Store (preview warna, nama tema, dan tombol harga/Owned).
    /// </summary>
    public class StoreThemeCardUI : MonoBehaviour
    {
        [SerializeField] private Image cardBackground;
        [SerializeField] private RawImage swatchPreview;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Image actionButtonBg;
        [SerializeField] private GameObject starIconObject;
        [SerializeField] private TMP_Text actionLabelText;

        private string currentThemeId;
        private Action<string> onBuyClicked;
        private Texture2D runtimeGradientTex;

        private static readonly Color OwnedCardTint = new Color(0.92f, 0.90f, 0.84f, 1f);
        private static readonly Color AvailableCardTint = Color.white;
        private static readonly Color OwnedPillColor = new Color(0.76f, 0.84f, 0.72f, 1f);
        private static readonly Color AvailablePillColor = new Color(0.86f, 0.80f, 0.72f, 1f);
        private static readonly Color UnaffordableTextColor = new Color(0.65f, 0.22f, 0.18f, 1f);
        private static readonly Color NormalTextColor = new Color(0.30f, 0.19f, 0.12f, 1f);

        private void Awake()
        {
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(HandleActionClicked);
            }
        }

        private void OnDestroy()
        {
            if (runtimeGradientTex != null)
            {
                Destroy(runtimeGradientTex);
                runtimeGradientTex = null;
            }
        }

        public void ConfigureReferences(
            Image cardBg,
            RawImage preview,
            TMP_Text title,
            Button button,
            Image buttonBg,
            GameObject starIcon,
            TMP_Text buttonLabel)
        {
            cardBackground = cardBg;
            swatchPreview = preview;
            titleText = title;
            actionButton = button;
            actionButtonBg = buttonBg;
            starIconObject = starIcon;
            actionLabelText = buttonLabel;

            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(HandleActionClicked);
            }
        }

        public void Setup(StoreThemeDefinition def, bool isOwned, bool canAfford, Action<string> buyCallback)
        {
            if (def == null) return;

            currentThemeId = def.id;
            onBuyClicked = buyCallback;

            if (titleText != null)
                titleText.text = def.displayName;

            ApplySwatchGradient(def.swatchTopColor, def.swatchBottomColor);

            if (cardBackground != null)
                cardBackground.color = isOwned ? OwnedCardTint : AvailableCardTint;

            if (actionButton != null)
            {
                // Tetap bisa diklik saat belum dimiliki agar saat poin kurang bisa memunculkan pesan peringatan DPPL.
                actionButton.interactable = !isOwned;
            }

            if (starIconObject != null)
                starIconObject.SetActive(!isOwned);

            if (actionButtonBg != null)
                actionButtonBg.color = isOwned ? OwnedPillColor : AvailablePillColor;

            if (actionLabelText != null)
            {
                if (isOwned)
                {
                    actionLabelText.text = "Owned";
                    actionLabelText.color = new Color(0.18f, 0.36f, 0.19f, 1f);
                }
                else
                {
                    actionLabelText.text = def.pricePoints.ToString();
                    actionLabelText.color = canAfford ? NormalTextColor : UnaffordableTextColor;
                }
            }
        }

        private void ApplySwatchGradient(Color topColor, Color bottomColor)
        {
            if (swatchPreview == null) return;

            if (runtimeGradientTex == null)
            {
                runtimeGradientTex = new Texture2D(2, 32, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            for (int y = 0; y < 32; y++)
            {
                float t = y / 31f;
                Color c = Color.Lerp(bottomColor, topColor, t);
                runtimeGradientTex.SetPixel(0, y, c);
                runtimeGradientTex.SetPixel(1, y, c);
            }
            runtimeGradientTex.Apply();

            swatchPreview.texture = runtimeGradientTex;
            swatchPreview.color = Color.white;
        }

        private void HandleActionClicked()
        {
            if (!string.IsNullOrEmpty(currentThemeId))
            {
                onBuyClicked?.Invoke(currentThemeId);
            }
        }
    }
}
