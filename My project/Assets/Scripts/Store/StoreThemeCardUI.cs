using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.Store
{
    /// <summary>
    /// Komponen UI untuk satu kartu tema di modal Store (preview item_*.png, nama tema, dan tombol harga/Owned).
    /// </summary>
    public class StoreThemeCardUI : MonoBehaviour
    {
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image swatchPreview;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Image actionButtonBg;
        [SerializeField] private GameObject starIconObject;
        [SerializeField] private TMP_Text actionLabelText;

        private string currentThemeId;
        private Action<string> onBuyClicked;

        private static readonly Color OwnedCardTint = new Color(0.92f, 0.90f, 0.84f, 1f);
        private static readonly Color AvailableCardTint = Color.white;
        private static readonly Color OwnedButtonTint = new Color(0.86f, 0.93f, 0.84f, 1f);
        private static readonly Color AvailableButtonTint = Color.white;
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

        public void ConfigureReferences(
            Image cardBg,
            Image preview,
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

        public void Setup(
            StoreThemeDefinition def,
            Sprite previewSprite,
            bool isOwned,
            bool canAfford,
            Action<string> buyCallback)
        {
            if (def == null) return;

            currentThemeId = def.id;
            onBuyClicked = buyCallback;

            if (titleText != null)
                titleText.text = def.displayName;

            if (swatchPreview != null)
            {
                if (previewSprite != null)
                {
                    swatchPreview.sprite = previewSprite;
                    swatchPreview.color = Color.white;
                }
                else
                {
                    swatchPreview.sprite = null;
                    swatchPreview.color = def.swatchBottomColor;
                }
            }

            if (cardBackground != null)
                cardBackground.color = isOwned ? OwnedCardTint : AvailableCardTint;

            if (actionButton != null)
            {
                actionButton.interactable = !isOwned;
            }

            if (starIconObject != null)
                starIconObject.SetActive(!isOwned);

            if (actionButtonBg != null)
                actionButtonBg.color = isOwned ? OwnedButtonTint : AvailableButtonTint;

            if (actionLabelText != null)
            {
                RectTransform labelRt = actionLabelText.rectTransform;
                if (isOwned)
                {
                    labelRt.offsetMin = new Vector2(8f, 0f);
                    labelRt.offsetMax = new Vector2(-8f, 0f);
                    actionLabelText.text = "Owned";
                    actionLabelText.color = new Color(0.18f, 0.36f, 0.19f, 1f);
                }
                else
                {
                    labelRt.offsetMin = new Vector2(44f, 0f);
                    labelRt.offsetMax = new Vector2(-10f, 0f);
                    actionLabelText.text = def.pricePoints.ToString();
                    actionLabelText.color = canAfford ? NormalTextColor : UnaffordableTextColor;
                }
            }
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
