using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Menangani satu slot thumbnail foto polaroid pada halaman buku album.
    /// Satu buku memuat 8 slot (4 di halaman kiri, 4 di halaman kanan).
    /// </summary>
    public class DictionaryPhotoSlotUI : MonoBehaviour
    {
        [Header("UI Elemen")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private RawImage photoImage;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button clickButton;
        [SerializeField] private Button favoriteButton;
        [SerializeField] private TMP_Text favoriteIconText; // Mis. "★" atau "☆"

        private PhotoRecord currentRecord;
        private Action<PhotoRecord> onClickCallback;
        private Action<PhotoRecord> onToggleFavoriteCallback;

        private void Awake()
        {
            if (rootObject == null) rootObject = gameObject;
            if (clickButton != null) clickButton.onClick.AddListener(HandleClick);
            if (favoriteButton != null) favoriteButton.onClick.AddListener(HandleFavorite);
        }

        public void Bind(PhotoRecord record, Action<PhotoRecord> onClick, Action<PhotoRecord> onToggleFav)
        {
            currentRecord = record;
            onClickCallback = onClick;
            onToggleFavoriteCallback = onToggleFav;

            if (record == null)
            {
                if (rootObject != null) rootObject.SetActive(false);
                return;
            }

            if (rootObject != null) rootObject.SetActive(true);
            if (titleLabel != null) titleLabel.text = string.IsNullOrEmpty(record.objectName) ? "Foto" : record.objectName;

            UpdateFavoriteVisual(record.isFavorite);

            if (photoImage != null)
            {
                Texture2D tex = PhotoFileStore.Load(record.imagePath);
                photoImage.texture = tex;
                photoImage.color = tex != null ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        public void UpdateFavoriteVisual(bool isFav)
        {
            if (favoriteIconText != null)
            {
                favoriteIconText.text = isFav ? "★" : "☆";
                favoriteIconText.color = isFav ? new Color(1f, 0.85f, 0.1f) : new Color(0.6f, 0.6f, 0.6f);
            }
        }

        private void HandleClick()
        {
            if (currentRecord != null) onClickCallback?.Invoke(currentRecord);
        }

        private void HandleFavorite()
        {
            if (currentRecord != null) onToggleFavoriteCallback?.Invoke(currentRecord);
        }
    }
}

