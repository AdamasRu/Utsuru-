using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Store;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Tab khusus di dalam buku Dictionary untuk memilih dan menerapkan (Apply) tema buku yang dimiliki.
    /// Tema default Plain selalu dimiliki; 5 tema lainnya dibeli terlebih dahulu melalui Store (tekan F).
    /// </summary>
    public class DictionaryThemeSpreadUI : MonoBehaviour
    {
        [Serializable]
        public class ThemeOptionSlot
        {
            public Image cardBackground;
            public Image bookPreviewImage;
            public Image swatchAccent;
            public TMP_Text titleText;
            public TMP_Text statusBadgeText;
            public Button actionButton;
            public Image actionButtonBg;
            public TMP_Text actionButtonText;
        }

        [Header("Referensi")]
        [SerializeField] private DictionaryThemeApplier themeApplier;
        [SerializeField] private TMP_Text statusNoticeText;
        [SerializeField] private Button openStoreShortcutButton;
        [SerializeField] private List<ThemeOptionSlot> optionSlots = new List<ThemeOptionSlot>();

        private StoreThemeManager themeManager;

        private static readonly Color ActiveCardBg = new Color(0.92f, 0.97f, 0.91f, 1f);
        private static readonly Color OwnedCardBg = new Color(0.99f, 0.97f, 0.93f, 1f);
        private static readonly Color LockedCardBg = new Color(0.91f, 0.89f, 0.86f, 0.9f);

        private static readonly Color ActiveBtnColor = new Color(0.24f, 0.58f, 0.34f, 1f);
        private static readonly Color ApplyBtnColor = new Color(0.36f, 0.25f, 0.58f, 1f);
        private static readonly Color LockedBtnColor = new Color(0.55f, 0.44f, 0.35f, 1f);

        private void Awake()
        {
            if (openStoreShortcutButton != null)
            {
                openStoreShortcutButton.onClick.RemoveAllListeners();
                openStoreShortcutButton.onClick.AddListener(OpenStoreFromDictionary);
            }
        }

        private void OnEnable()
        {
            EnsureBound();
            Refresh();
        }

        private void OnDestroy()
        {
            if (themeManager != null)
            {
                themeManager.OnThemesUpdated -= Refresh;
                themeManager.OnActiveThemeChanged -= HandleThemeChanged;
            }
        }

        public void ConfigureReferences(
            DictionaryThemeApplier applier,
            TMP_Text noticeText,
            Button storeShortcutBtn,
            List<ThemeOptionSlot> slots)
        {
            themeApplier = applier;
            statusNoticeText = noticeText;
            openStoreShortcutButton = storeShortcutBtn;
            if (slots != null) optionSlots = slots;

            if (openStoreShortcutButton != null)
            {
                openStoreShortcutButton.onClick.RemoveAllListeners();
                openStoreShortcutButton.onClick.AddListener(OpenStoreFromDictionary);
            }
        }

        private void EnsureBound()
        {
            if (themeManager == null)
            {
                themeManager = StoreThemeManager.EnsureInstance();
                if (themeManager != null)
                {
                    themeManager.OnThemesUpdated -= Refresh;
                    themeManager.OnThemesUpdated += Refresh;
                    themeManager.OnActiveThemeChanged -= HandleThemeChanged;
                    themeManager.OnActiveThemeChanged += HandleThemeChanged;
                }
            }

            if (themeApplier == null)
            {
                themeApplier = FindFirstObjectByType<DictionaryThemeApplier>(FindObjectsInactive.Include);
            }
        }

        private void HandleThemeChanged(string _)
        {
            Refresh();
        }

        public void Refresh()
        {
            EnsureBound();
            if (themeManager == null) return;

            IReadOnlyList<StoreThemeDefinition> all = themeManager.AllThemes;

            for (int i = 0; i < optionSlots.Count; i++)
            {
                ThemeOptionSlot slot = optionSlots[i];
                if (slot == null) continue;

                if (i >= all.Count)
                {
                    if (slot.cardBackground != null)
                        slot.cardBackground.gameObject.SetActive(false);
                    continue;
                }

                StoreThemeDefinition def = all[i];
                if (slot.cardBackground != null)
                    slot.cardBackground.gameObject.SetActive(true);

                bool isOwned = themeManager.IsOwned(def.id);
                bool isApplied = themeManager.IsApplied(def.id);

                if (slot.titleText != null)
                    slot.titleText.text = def.displayName;

                if (slot.swatchAccent != null)
                    slot.swatchAccent.color = def.swatchBottomColor;

                if (slot.bookPreviewImage != null)
                {
                    Sprite bookSprite = themeApplier != null ? themeApplier.GetBookSprite(def.id) : null;
                    if (bookSprite != null)
                    {
                        slot.bookPreviewImage.sprite = bookSprite;
                        slot.bookPreviewImage.color = isOwned ? Color.white : new Color(0.65f, 0.65f, 0.65f, 0.85f);
                    }
                    else
                    {
                        slot.bookPreviewImage.color = def.swatchBottomColor;
                    }
                }

                if (slot.cardBackground != null)
                {
                    slot.cardBackground.color = isApplied ? ActiveCardBg : (isOwned ? OwnedCardBg : LockedCardBg);
                }

                if (slot.statusBadgeText != null)
                {
                    if (isApplied)
                    {
                        slot.statusBadgeText.text = "Sedang Dipakai";
                        slot.statusBadgeText.color = new Color(0.14f, 0.45f, 0.22f, 1f);
                    }
                    else if (isOwned)
                    {
                        slot.statusBadgeText.text = def.isDefaultFree ? "Default Gratis" : "Dimiliki";
                        slot.statusBadgeText.color = new Color(0.28f, 0.24f, 0.45f, 1f);
                    }
                    else
                    {
                        slot.statusBadgeText.text = $"Terkunci • {def.pricePoints} Poin";
                        slot.statusBadgeText.color = new Color(0.55f, 0.35f, 0.25f, 1f);
                    }
                }

                if (slot.actionButton != null)
                {
                    slot.actionButton.onClick.RemoveAllListeners();
                    string targetId = def.id;

                    if (isApplied)
                    {
                        slot.actionButton.interactable = false;
                        if (slot.actionButtonBg != null) slot.actionButtonBg.color = ActiveBtnColor;
                        if (slot.actionButtonText != null) slot.actionButtonText.text = "Active";
                    }
                    else if (isOwned)
                    {
                        slot.actionButton.interactable = true;
                        if (slot.actionButtonBg != null) slot.actionButtonBg.color = ApplyBtnColor;
                        if (slot.actionButtonText != null) slot.actionButtonText.text = "Apply";
                        slot.actionButton.onClick.AddListener(() => OnApplyClicked(targetId));
                    }
                    else
                    {
                        slot.actionButton.interactable = true;
                        if (slot.actionButtonBg != null) slot.actionButtonBg.color = LockedBtnColor;
                        if (slot.actionButtonText != null) slot.actionButtonText.text = "Buy (Store)";
                        slot.actionButton.onClick.AddListener(OpenStoreFromDictionary);
                    }
                }
            }
        }

        private void OnApplyClicked(string themeId)
        {
            EnsureBound();
            if (themeManager == null) return;

            bool ok = themeManager.ApplyTheme(themeId, out string message);
            if (statusNoticeText != null)
            {
                statusNoticeText.text = message;
                statusNoticeText.color = ok
                    ? new Color(0.16f, 0.46f, 0.24f, 1f)
                    : new Color(0.65f, 0.20f, 0.18f, 1f);
            }
            Refresh();
        }

        private void OpenStoreFromDictionary()
        {
            var dict = FindFirstObjectByType<DictionaryUIController>();
            var store = StoreUIController.Instance ?? FindFirstObjectByType<StoreUIController>(FindObjectsInactive.Include);

            if (store != null)
            {
                if (dict != null && dict.IsOpen)
                {
                    dict.Close();
                }
                store.Open();
            }
            else if (statusNoticeText != null)
            {
                statusNoticeText.text = "Tekan F di eksplorasi untuk membuka Store!";
            }
        }
    }
}
