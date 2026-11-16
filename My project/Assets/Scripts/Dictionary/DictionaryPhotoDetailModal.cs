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

        private void OnEnable()
        {
            Utsuru.Localization.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            Utsuru.Localization.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(Utsuru.Localization.GameLanguage lang)
        {
            if (modalRoot != null && modalRoot.activeSelf && currentRecord != null && dictionary != null)
                Open(currentRecord, dictionary, onDataChanged);
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

            string objName = currentRecord.objectName;
            if (Utsuru.Localization.LocalizationManager.Instance != null)
            {
                objName = Utsuru.Localization.LocalizationManager.Instance.GetObjectName(currentRecord.objectId, objName);
            }
            if (objectNameLabel != null) objectNameLabel.text = objName;

            string dateFmt = Utsuru.Localization.LocalizationManager.Instance != null 
                ? Utsuru.Localization.LocalizationManager.Instance.GetText("PHOTO_DATE_TAKEN", "Diambil: {0}") 
                : "Diambil: {0}";
            if (dateTakenLabel != null)
            {
                if (DateTime.TryParse(currentRecord.dateTaken, out DateTime dt))
                    dateTakenLabel.text = string.Format(dateFmt, dt.ToString("dd/MM/yyyy HH:mm"));
                else
                    dateTakenLabel.text = string.Format(dateFmt, currentRecord.dateTaken);
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
                    string translation = info.vocabulary.translation;
                    if (Utsuru.Localization.LocalizationManager.Instance != null)
                    {
                        translation = Utsuru.Localization.LocalizationManager.Instance.GetVocabTranslation(info.objectId, translation);
                    }
                    string meaningPrefix = Utsuru.Localization.LocalizationManager.Instance != null 
                        ? Utsuru.Localization.LocalizationManager.Instance.GetText("PHOTO_MEANING_LABEL", "Arti: {0}") 
                        : "Arti: {0}";
                    vocabLabel.text = $"{info.vocabulary.DisplayJapanese} ({info.vocabulary.romaji})\n{string.Format(meaningPrefix, translation)}";
                }
                else
                {
                    vocabLabel.text = objName;
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
                if (currentRecord.isFavorite)
                {
                    favoriteButtonText.text = Utsuru.Localization.LocalizationManager.Instance != null
                        ? Utsuru.Localization.LocalizationManager.Instance.GetText("FAVORITE_REMOVE", "★ Favorite")
                        : "★ Favorite";
                }
                else
                {
                    favoriteButtonText.text = Utsuru.Localization.LocalizationManager.Instance != null
                        ? Utsuru.Localization.LocalizationManager.Instance.GetText("FAVORITE_ADD", "☆ Add Favorite")
                        : "☆ Add Favorite";
                }
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
                string title = Utsuru.Localization.LocalizationManager.Instance != null
                    ? Utsuru.Localization.LocalizationManager.Instance.GetText("DIALOG_DELETE_TITLE", "Delete Photo")
                    : "Delete Photo";
                string msgFmt = Utsuru.Localization.LocalizationManager.Instance != null
                    ? Utsuru.Localization.LocalizationManager.Instance.GetText("DIALOG_DELETE_MSG", "Are you sure you want to delete '{0}'?")
                    : "Are you sure you want to delete '{0}'?";
                string confirmBtn = Utsuru.Localization.LocalizationManager.Instance != null
                    ? Utsuru.Localization.LocalizationManager.Instance.GetText("DIALOG_DELETE_CONFIRM", "Delete")
                    : "Delete";

                confirmDialog.ShowConfirm(
                    title,
                    string.Format(msgFmt, currentRecord.objectName),
                    confirmBtn,
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

