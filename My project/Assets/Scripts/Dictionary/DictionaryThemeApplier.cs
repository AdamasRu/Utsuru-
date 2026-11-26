using System;
using System.Collections;
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
        private Image crossfadeOverlay;
        private Coroutine crossfadeCoroutine;

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
            StopCrossfade();
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

        /// <summary>
        /// Menampilkan preview tema secara sementara pada sampul buku tanpa mengubah tema aktif yang tersimpan.
        /// Panggil ApplyCurrentTheme() untuk mengembalikan ke tema aktif sebenarnya.
        /// </summary>
        public void PreviewTheme(string themeId)
        {
            ApplyThemeVisual(themeId);
        }

        /// <summary>
        /// Melakukan transisi crossfade halus pada sampul buku menuju tema target.
        /// </summary>
        public void CrossfadeToTheme(string themeId, float duration = 0.35f)
        {
            if (bookCoverImage == null) return;

            Sprite targetSprite = GetBookSprite(themeId);
            if (targetSprite == null) return;

            if (duration <= 0f || !gameObject.activeInHierarchy || bookCoverImage.sprite == null || bookCoverImage.sprite == targetSprite)
            {
                ApplyThemeVisual(themeId);
                return;
            }

            EnsureCrossfadeOverlay();
            if (crossfadeOverlay == null)
            {
                ApplyThemeVisual(themeId);
                return;
            }

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
                crossfadeCoroutine = null;
            }

            crossfadeOverlay.sprite = bookCoverImage.sprite;
            crossfadeOverlay.type = bookCoverImage.type;
            crossfadeOverlay.preserveAspect = bookCoverImage.preserveAspect;
            crossfadeOverlay.color = Color.white;
            crossfadeOverlay.gameObject.SetActive(true);

            bookCoverImage.sprite = targetSprite;
            bookCoverImage.color = Color.white;

            crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(duration));
        }

        private IEnumerator CrossfadeRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.SmoothStep(1f, 0f, t);
                if (crossfadeOverlay != null)
                {
                    crossfadeOverlay.color = new Color(1f, 1f, 1f, alpha);
                }
                yield return null;
            }

            if (crossfadeOverlay != null)
            {
                crossfadeOverlay.color = new Color(1f, 1f, 1f, 0f);
                crossfadeOverlay.gameObject.SetActive(false);
            }
            crossfadeCoroutine = null;
        }

        private void EnsureCrossfadeOverlay()
        {
            if (crossfadeOverlay != null || bookCoverImage == null) return;

            Transform existing = bookCoverImage.transform.Find("_ThemeCrossfadeOverlay");
            if (existing != null)
            {
                crossfadeOverlay = existing.GetComponent<Image>();
            }

            if (crossfadeOverlay == null)
            {
                GameObject overlayGo = new GameObject("_ThemeCrossfadeOverlay", typeof(RectTransform), typeof(Image));
                overlayGo.transform.SetParent(bookCoverImage.transform, false);
                overlayGo.transform.SetAsFirstSibling();

                RectTransform rt = overlayGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;

                crossfadeOverlay = overlayGo.GetComponent<Image>();
                crossfadeOverlay.raycastTarget = false;
            }

            crossfadeOverlay.gameObject.SetActive(false);
        }

        private void StopCrossfade()
        {
            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
                crossfadeCoroutine = null;
            }
            if (crossfadeOverlay != null)
            {
                crossfadeOverlay.gameObject.SetActive(false);
            }
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
            StopCrossfade();
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
