using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;
using Utsuru.FlashcardSystem;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Komponen slot/item kartu flashcard mini untuk daftar di Halaman Kiri (PBO View Item).
    /// </summary>
    public class DictionaryFlashcardItemUI : MonoBehaviour
    {
        [Header("Elemen UI")]
        [SerializeField] private Button selectButton;
        [SerializeField] private RawImage thumbnailImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI translationText;
        [SerializeField] private Image statusBadgeImage;
        [SerializeField] private TextMeshProUGUI statusBadgeText;
        [SerializeField] private GameObject selectedHighlightBorder;

        [Header("Warna Status")]
        [SerializeField] private Color newStatusColor = new Color(0.45f, 0.52f, 0.65f, 1f);     // Abu kebiruan
        [SerializeField] private Color learningStatusColor = new Color(0.92f, 0.55f, 0.20f, 1f); // Oranye hangat
        [SerializeField] private Color masteredStatusColor = new Color(0.18f, 0.72f, 0.42f, 1f); // Hijau emerald

        private PhotoFlashcard _card;
        private Action<PhotoFlashcard> _onSelectedCallback;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(HandleClick);
            }
        }

        public void Bind(PhotoFlashcard card, Texture2D thumbnail, bool isSelected, Action<PhotoFlashcard> onSelected)
        {
            _card = card;
            _onSelectedCallback = onSelected;

            if (card == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // Teks judul Jepang
            if (titleText != null)
            {
                titleText.text = card.DisplayJapanese;
            }

            // Teks arti
            if (translationText != null)
            {
                translationText.text = card.Translation;
            }

            // Thumbnail foto jepretan kamera
            if (thumbnailImage != null)
            {
                thumbnailImage.texture = thumbnail;
                thumbnailImage.gameObject.SetActive(thumbnail != null);
            }

            // Status Badge
            UpdateStatusBadge(card.MasteryStatus);

            // Highlight border jika item ini sedang aktif dipilih
            if (selectedHighlightBorder != null)
            {
                selectedHighlightBorder.SetActive(isSelected);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedHighlightBorder != null)
            {
                selectedHighlightBorder.SetActive(isSelected);
            }
        }

        private void UpdateStatusBadge(FlashcardMasteryStatus status)
        {
            if (statusBadgeText == null) return;

            switch (status)
            {
                case FlashcardMasteryStatus.Mastered:
                    statusBadgeText.text = "HAFAL";
                    if (statusBadgeImage != null) statusBadgeImage.color = masteredStatusColor;
                    break;
                case FlashcardMasteryStatus.Learning:
                    statusBadgeText.text = "BELAJAR";
                    if (statusBadgeImage != null) statusBadgeImage.color = learningStatusColor;
                    break;
                default:
                    statusBadgeText.text = "BARU";
                    if (statusBadgeImage != null) statusBadgeImage.color = newStatusColor;
                    break;
            }
        }

        private void HandleClick()
        {
            if (_card != null)
            {
                _onSelectedCallback?.Invoke(_card);
            }
        }
    }
}
