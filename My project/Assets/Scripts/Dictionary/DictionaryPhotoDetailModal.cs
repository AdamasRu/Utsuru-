using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Modal preview foto resolusi penuh saat foto polaroid di Tab Album diklik.
    /// Dilengkapi tombol Favorit, Pindah Album, dan Hapus Foto.
    /// </summary>
    public class DictionaryPhotoDetailModal : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject modalRoot;

        [Header("Komponen Preview")]
        [SerializeField] private RawImage previewImage;
        [SerializeField] private TMP_Text objectNameLabel;
        [SerializeField] private TMP_Text dateTakenLabel;
        [SerializeField] private TMP_Text vocabLabel; // Kanji / Kana / Romaji / Arti

        [Header("Aksi")]
        [SerializeField] private Button favoriteButton;
        [SerializeField] private TMP_Text favoriteButtonText;
        [SerializeField] private TMP_Dropdown moveAlbumDropdown;
        [SerializeField] private Button moveAlbumButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropCloseButton;

        [Header("Dialog Konfirmasi")]
        [SerializeField] private DictionarySimpleDialog confirmDialog;

        private DictionaryManager dictionary;
        private PhotoRecord currentRecord;
        private Action onDataChanged;
        private readonly List<AlbumRecord> availableAlbums = new List<AlbumRecord>();

        private void Awake()
        {
            if (modalRoot == null) modalRoot = gameObject;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(Close);
            if (favoriteButton != null) favoriteButton.onClick.AddListener(ToggleFavorite);
            if (moveAlbumButton != null) moveAlbumButton.onClick.AddListener(MoveToSelectedAlbum);
            if (deleteButton != null) deleteButton.onClick.AddListener(ConfirmDelete);

            Hide();
        }

        public void Open(PhotoRecord record, DictionaryManager dict, Action onModifiedCallback)
        {
            currentRecord = record;
            dictionary = dict;
            onDataChanged = onModifiedCallback;

            if (currentRecord == null || dictionary == null)
            {
                Close();
                return;
            }

            if (objectNameLabel != null) objectNameLabel.text = currentRecord.objectName;
            if (dateTakenLabel != null)
            {
                if (DateTime.TryParse(currentRecord.dateTaken, out DateTime dt))
                    dateTakenLabel.text = $"Diambil: {dt:dd/MM/yyyy HH:mm}";
                else
                    dateTakenLabel.text = $"Diambil: {currentRecord.dateTaken}";
            }

            // Tampilkan foto
            if (previewImage != null)
            {
                Texture2D tex = PhotoFileStore.Load(currentRecord.imagePath);
                previewImage.texture = tex;
            }

            // Tampilkan info kosakata jika ada di database
            if (vocabLabel != null)
            {
                if (dictionary.Database != null && dictionary.Database.TryGetObject(currentRecord.objectId, out ObjectInfo info) && info.vocabulary != null)
                {
                    vocabLabel.text = $"{info.vocabulary.DisplayJapanese} ({info.vocabulary.romaji})\nArti: {info.vocabulary.translation}";
                }
                else
                {
                    vocabLabel.text = currentRecord.objectName;
                }
            }

            UpdateFavoriteVisual();
            PopulateMoveDropdown();

            if (modalRoot != null) modalRoot.SetActive(true);
        }

        private void UpdateFavoriteVisual()
        {
            if (favoriteButtonText != null && currentRecord != null)
            {
                favoriteButtonText.text = currentRecord.isFavorite ? "★ Favorit" : "☆ Tambah Favorit";
                favoriteButtonText.color = currentRecord.isFavorite ? new Color(1f, 0.85f, 0.1f) : Color.white;
            }
        }

        private void ToggleFavorite()
        {
            if (currentRecord == null || dictionary == null) return;
            dictionary.ToggleFavorite(currentRecord.photoId);
            UpdateFavoriteVisual();
            onDataChanged?.Invoke();
        }

        private void PopulateMoveDropdown()
        {
            if (moveAlbumDropdown == null || dictionary == null) return;

            availableAlbums.Clear();
            moveAlbumDropdown.ClearOptions();

            var options = new List<string>();
            int selectedIndex = 0;
            int i = 0;
            foreach (var a in dictionary.Albums)
            {
                availableAlbums.Add(a);
                options.Add(a.albumName);
                if (a.albumId == currentRecord.albumId) selectedIndex = i;
                i++;
            }

            moveAlbumDropdown.AddOptions(options);
            moveAlbumDropdown.value = selectedIndex;
        }

        private void MoveToSelectedAlbum()
        {
            if (currentRecord == null || dictionary == null || moveAlbumDropdown == null) return;
            if (moveAlbumDropdown.value >= 0 && moveAlbumDropdown.value < availableAlbums.Count)
            {
                int targetAlbumId = availableAlbums[moveAlbumDropdown.value].albumId;
                if (targetAlbumId != currentRecord.albumId)
                {
                    dictionary.MovePhoto(currentRecord.photoId, targetAlbumId);
                    onDataChanged?.Invoke();
                    Close();
                }
            }
        }

        private void ConfirmDelete()
        {
            if (confirmDialog != null)
            {
                confirmDialog.ShowConfirm(
                    "Hapus Foto",
                    $"Apakah kamu yakin ingin menghapus foto '{currentRecord.objectName}'?",
                    "Hapus",
                    () =>
                    {
                        if (dictionary != null && currentRecord != null)
                        {
                            dictionary.DeletePhoto(currentRecord.photoId);
                            onDataChanged?.Invoke();
                            Close();
                        }
                    }
                );
            }
            else
            {
                // Fallback jika tidak ada dialog
                dictionary.DeletePhoto(currentRecord.photoId);
                onDataChanged?.Invoke();
                Close();
            }
        }

        public void Close()
        {
            Hide();
        }

        public void Hide()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            if (previewImage != null && previewImage.texture != null)
            {
                // Texture yang di-load dari disk
                Destroy(previewImage.texture);
                previewImage.texture = null;
            }
            currentRecord = null;
        }
    }
}

