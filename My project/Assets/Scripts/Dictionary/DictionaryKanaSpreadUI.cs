using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Mengelola tampilan interaktif Tabel Huruf Hiragana & Katakana:
    /// - Lembar 1 (Hal 1-2): Gojūon (46 Karakter Dasar) - Hiragana Kiri, Katakana Kanan
    /// - Lembar 2 (Hal 3-4): Dakuon & Handakuon (25 Karakter Bunyi Sengau)
    /// - Lembar 3 (Hal 5-6): Yōon (Kombinasi Karakter Kecil Kya, Shu, Cho, dll)
    /// </summary>
    public class DictionaryKanaSpreadUI : MonoBehaviour
    {
        [Header("Referensi Root Per Lembar (Opsional)")]
        [SerializeField] private GameObject gojuonSpreadRoot;
        [SerializeField] private GameObject dakuonSpreadRoot;
        [SerializeField] private GameObject yoonSpreadRoot;

        [Header("Konten Halaman Kiri & Kanan Per Lembar")]
        [SerializeField] private GameObject gojuonLeft;
        [SerializeField] private GameObject gojuonRight;
        [SerializeField] private GameObject dakuonLeft;
        [SerializeField] private GameObject dakuonRight;
        [SerializeField] private GameObject yoonLeft;
        [SerializeField] private GameObject yoonRight;

        [Header("Tombol Sub-Kategori")]
        [SerializeField] private Button[] tabGojuonButtons;
        [SerializeField] private Button[] tabDakuonButtons;
        [SerializeField] private Button[] tabYoonButtons;
        [SerializeField] private Image[] tabGojuonBackgrounds;
        [SerializeField] private Image[] tabDakuonBackgrounds;
        [SerializeField] private Image[] tabYoonBackgrounds;

        [Header("Warna Highlight Tab")]
        [SerializeField] private Color tabActiveColor = new Color(0.38f, 0.32f, 0.65f, 1f); // Deep Royal Purple
        [SerializeField] private Color tabInactiveColor = new Color(0.92f, 0.90f, 0.96f, 0.85f);
        [SerializeField] private Color textActiveColor = Color.white;
        [SerializeField] private Color textInactiveColor = new Color(0.35f, 0.30f, 0.48f, 1f);

        [Header("Referensi Parent Controller")]
        [SerializeField] private DictionaryBookSpreadUI bookSpread;

        private int currentSpread = 0;

        private void Awake()
        {
            if (bookSpread == null)
                bookSpread = GetComponentInParent<DictionaryBookSpreadUI>();

            RegisterTabClicks(tabGojuonButtons, 0);
            RegisterTabClicks(tabDakuonButtons, 1);
            RegisterTabClicks(tabYoonButtons, 2);
        }

        private void RegisterTabClicks(Button[] buttons, int targetSpread)
        {
            if (buttons == null) return;
            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        if (bookSpread != null) bookSpread.SetKanaSpread(targetSpread);
                        else SetSpread(targetSpread);
                    });
                }
            }
        }

        public void SetSpread(int spreadIndex)
        {
            currentSpread = Mathf.Clamp(spreadIndex, 0, 2);

            bool isGojuon = (currentSpread == 0);
            bool isDakuon = (currentSpread == 1);
            bool isYoon = (currentSpread == 2);

            if (gojuonSpreadRoot != null) gojuonSpreadRoot.SetActive(isGojuon);
            if (dakuonSpreadRoot != null) dakuonSpreadRoot.SetActive(isDakuon);
            if (yoonSpreadRoot != null) yoonSpreadRoot.SetActive(isYoon);

            if (gojuonLeft != null) gojuonLeft.SetActive(isGojuon);
            if (gojuonRight != null) gojuonRight.SetActive(isGojuon);
            if (dakuonLeft != null) dakuonLeft.SetActive(isDakuon);
            if (dakuonRight != null) dakuonRight.SetActive(isDakuon);
            if (yoonLeft != null) yoonLeft.SetActive(isYoon);
            if (yoonRight != null) yoonRight.SetActive(isYoon);

            UpdateTabHighlights();
        }

        private void UpdateTabHighlights()
        {
            SetGroupVisual(tabGojuonBackgrounds, tabGojuonButtons, currentSpread == 0);
            SetGroupVisual(tabDakuonBackgrounds, tabDakuonButtons, currentSpread == 1);
            SetGroupVisual(tabYoonBackgrounds, tabYoonButtons, currentSpread == 2);
        }

        private void SetGroupVisual(Image[] imgs, Button[] btns, bool isActive)
        {
            if (imgs != null)
            {
                foreach (var img in imgs)
                {
                    if (img != null) img.color = isActive ? tabActiveColor : tabInactiveColor;
                }
            }
            if (btns != null)
            {
                foreach (var btn in btns)
                {
                    if (btn != null)
                    {
                        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) tmp.color = isActive ? textActiveColor : textInactiveColor;
                    }
                }
            }
        }
    }
}
