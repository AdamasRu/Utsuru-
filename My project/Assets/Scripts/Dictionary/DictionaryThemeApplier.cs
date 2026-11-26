using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Store;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Menghubungkan themeId milik StoreThemeManager dengan Sprite sampul buku Kamus (Book_*.png)
    /// dan menerapkannya ke BookContainer secara instan (DPPL.UTSURU.013).
    /// </summary>
    public class DictionaryThemeApplier : MonoBehaviour
    {
        [Serializable]
        public class ThemeSpriteBinding
        {
            public string themeId;
            public Sprite bookSprite;
        }

        [SerializeField] private Image bookCoverImage;
        [SerializeField] private List<ThemeSpriteBinding> themeSprites = new List<ThemeSpriteBinding>();

        private StoreThemeManager boundManager;

        private void OnEnable()
        {
            BindManager();
            ApplyCurrentTheme();
        }

        private void Start()
        {
            BindManager();
            ApplyCurrentTheme();
        }

        private void OnDisable()
        {
            if (boundManager != null)
            {
                boundManager.OnActiveThemeChanged -= HandleActiveThemeChanged;
                boundManager = null;
            }
        }

        public void ConfigureReferences(Image coverImage, List<ThemeSpriteBinding> bindings)
        {
            bookCoverImage = coverImage;
            if (bindings != null)
            {
                themeSprites = bindings;
            }
        }

        public Sprite GetBookSprite(string themeId)
        {
            if (themeSprites == null) return null;

            for (int i = 0; i < themeSprites.Count; i++)
            {
                if (themeSprites[i] != null &&
                    string.Equals(themeSprites[i].themeId, themeId, StringComparison.OrdinalIgnoreCase) &&
                    themeSprites[i].bookSprite != null)
                {
                    return themeSprites[i].bookSprite;
                }
            }

            // Fallback ke Plain jika ada
            for (int i = 0; i < themeSprites.Count; i++)
            {
                if (themeSprites[i] != null &&
                    string.Equals(themeSprites[i].themeId, StoreThemeCatalog.DefaultThemeId, StringComparison.OrdinalIgnoreCase) &&
                    themeSprites[i].bookSprite != null)
                {
                    return themeSprites[i].bookSprite;
                }
            }

            return null;
        }

        public void ApplyCurrentTheme()
        {
            BindManager();
            string activeId = boundManager != null ? boundManager.ActiveThemeId : StoreThemeCatalog.DefaultThemeId;
            ApplyThemeVisual(activeId);
        }

        private void BindManager()
        {
            if (boundManager != null) return;
            boundManager = StoreThemeManager.Instance ?? FindFirstObjectByType<StoreThemeManager>(FindObjectsInactive.Include);
            if (boundManager == null)
            {
                boundManager = StoreThemeManager.EnsureInstance();
            }
            if (boundManager != null)
            {
                boundManager.OnActiveThemeChanged -= HandleActiveThemeChanged;
                boundManager.OnActiveThemeChanged += HandleActiveThemeChanged;
            }
        }

        private void HandleActiveThemeChanged(string themeId)
        {
            ApplyThemeVisual(themeId);
        }

        private void ApplyThemeVisual(string themeId)
        {
            if (bookCoverImage == null) return;

            Sprite targetSprite = GetBookSprite(themeId);
            if (targetSprite != null)
            {
                bookCoverImage.sprite = targetSprite;
                bookCoverImage.color = Color.white;
            }
        }
    }
}
