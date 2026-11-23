using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Mengelola tampilan Tab Module (Course):
    /// - Halaman Kiri: Daftar Isi (Table of Contents / TOC) 8 Bab
    /// - Halaman Kanan: Scrollable Reader (ScrollRect) yang memuat modul per bab
    /// - Fitur Jump-to-Section (mirip tag <a> di HTML)
    /// </summary>
    public class DictionaryModuleSpreadUI : MonoBehaviour
    {
        [Header("Komponen Navigasi Kanan (Scroll View)")]
        [SerializeField] private ScrollRect rightScrollRect;
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private RectTransform[] chapterSections;

        [Header("Tombol Daftar Isi Kiri (TOC Buttons)")]
        [SerializeField] private Button[] tocButtons;
        [SerializeField] private Image[] tocButtonBackgrounds;

        [Header("Pengaturan Warna Sorotan TOC")]
        [SerializeField] private Color tocSelectedColor = new Color(0.92f, 0.88f, 1f, 1f); // Soft purple highlight
        [SerializeField] private Color tocNormalColor = new Color(1f, 1f, 1f, 0.82f);

        [Header("Pengaturan Animasi Scroll")]
        [SerializeField] private float smoothScrollDuration = 0.25f;

        private Coroutine scrollCoroutine;
        private int currentChapterIndex = 0;

        private void Awake()
        {
            if (tocButtons != null)
            {
                for (int i = 0; i < tocButtons.Length; i++)
                {
                    int idx = i;
                    if (tocButtons[i] != null)
                    {
                        tocButtons[i].onClick.AddListener(() => JumpToChapter(idx));
                    }
                }
            }
        }

        private void OnEnable()
        {
            HighlightTOC(currentChapterIndex);
        }

        public void JumpToChapter(int chapterIndex)
        {
            if (chapterSections == null || chapterIndex < 0 || chapterIndex >= chapterSections.Length) return;
            if (rightScrollRect == null || scrollContent == null) return;

            currentChapterIndex = chapterIndex;
            HighlightTOC(chapterIndex);

            RectTransform target = chapterSections[chapterIndex];
            if (target == null) return;

            // Target position Y relatif terhadap scrollContent
            float targetY = -target.localPosition.y;

            float viewHeight = rightScrollRect.viewport != null ? rightScrollRect.viewport.rect.height : 780f;
            float maxY = Mathf.Max(0f, scrollContent.rect.height - viewHeight);
            targetY = Mathf.Clamp(targetY, 0f, maxY);

            if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
            if (gameObject.activeInHierarchy)
            {
                scrollCoroutine = StartCoroutine(SmoothScrollRoutine(targetY));
            }
            else
            {
                scrollContent.anchoredPosition = new Vector2(scrollContent.anchoredPosition.x, targetY);
            }
        }

        private IEnumerator SmoothScrollRoutine(float targetY)
        {
            float startY = scrollContent.anchoredPosition.y;
            float elapsed = 0f;

            while (elapsed < smoothScrollDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / smoothScrollDuration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                scrollContent.anchoredPosition = new Vector2(scrollContent.anchoredPosition.x, Mathf.Lerp(startY, targetY, ease));
                yield return null;
            }

            scrollContent.anchoredPosition = new Vector2(scrollContent.anchoredPosition.x, targetY);
            scrollCoroutine = null;
        }

        private void HighlightTOC(int selectedIndex)
        {
            if (tocButtonBackgrounds == null) return;
            for (int i = 0; i < tocButtonBackgrounds.Length; i++)
            {
                if (tocButtonBackgrounds[i] != null)
                {
                    tocButtonBackgrounds[i].color = (i == selectedIndex) ? tocSelectedColor : tocNormalColor;
                }
            }
        }
    }
}
