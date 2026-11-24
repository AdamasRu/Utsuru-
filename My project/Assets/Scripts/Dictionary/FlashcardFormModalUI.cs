using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;
using Utsuru.FlashcardSystem;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Modal form untuk CREATE dan UPDATE Flashcard (PBO View Controller).
    /// Memungkinkan pemain memilih foto jepretan kamera, memvalidasi kosakata,
    /// serta menginput catatan hafalan personal.
    /// </summary>
    public class FlashcardFormModalUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject modalRoot;

        [Header("Header & Info")]
        [SerializeField] private TextMeshProUGUI modalTitleText;
        [SerializeField] private TMP_Dropdown photoDropdown;
        [SerializeField] private RawImage photoPreviewImage;
        [SerializeField] private TextMeshProUGUI vocabInfoText;

        [Header("Input Catatan Personal (UPDATE)")]
        [SerializeField] private TMP_InputField noteInputField;

        [Header("Aksi")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button backdropCloseButton;
        [SerializeField] private TextMeshProUGUI saveButtonText;

        private DictionaryManager _dictionaryManager;
        private PhotoFlashcard _editingCard; // null jika mode CREATE, terisi jika mode UPDATE
        private Action _onSaveCompletedCallback;
        private readonly List<PhotoRecord> _availablePhotos = new List<PhotoRecord>();

        private void Awake()
        {
            if (modalRoot == null) modalRoot = gameObject;

            if (saveButton != null) saveButton.onClick.AddListener(HandleSave);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
            if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(Close);
            if (photoDropdown != null) photoDropdown.onValueChanged.AddListener(OnPhotoDropdownChanged);

            Hide();
        }

        /// <summary>
        /// Buka modal dalam mode CREATE (Buat Kartu Baru dari Foto).
        /// </summary>
        public void OpenCreate(DictionaryManager dictManager, Action onCompleted, PhotoRecord preselectedPhoto = null)
        {
            _editingCard = null;
            _dictionaryManager = dictManager;
            _onSaveCompletedCallback = onCompleted;

            if (modalTitleText != null) modalTitleText.text = "Buat Flashcard Baru";
            if (saveButtonText != null) saveButtonText.text = "Simpan Kartu";
            if (noteInputField != null) noteInputField.text = "";

            if (photoDropdown != null) photoDropdown.interactable = true;

            PopulatePhotoDropdown(preselectedPhoto);
            Show();
        }

        /// <summary>
        /// Buka modal dalam mode UPDATE (Edit Catatan Kartu yang sudah ada).
        /// </summary>
        public void OpenEdit(PhotoFlashcard cardToEdit, DictionaryManager dictManager, Action onCompleted)
        {
            _editingCard = cardToEdit;
            _dictionaryManager = dictManager;
            _onSaveCompletedCallback = onCompleted;

            if (cardToEdit == null)
            {
                Close();
                return;
            }

            if (modalTitleText != null) modalTitleText.text = "Edit Catatan Flashcard";
            if (saveButtonText != null) saveButtonText.text = "Perbarui";
            if (noteInputField != null) noteInputField.text = cardToEdit.CustomNote;

            // Kunci pilihan foto saat mode edit agar integritas kosakata tetap konsisten
            if (photoDropdown != null)
            {
                photoDropdown.ClearOptions();
                photoDropdown.options.Add(new TMP_Dropdown.OptionData(cardToEdit.ObjectName));
                photoDropdown.value = 0;
                photoDropdown.interactable = false;
            }

            // Tampilkan foto kartu saat ini
            if (photoPreviewImage != null)
            {
                Texture2D tex = PhotoFileStore.Load(cardToEdit.PhotoRelativePath);
                photoPreviewImage.texture = tex;
                photoPreviewImage.gameObject.SetActive(tex != null);
            }

            // Tampilkan info kosakata
            if (vocabInfoText != null)
            {
                vocabInfoText.text = $"{cardToEdit.DisplayJapanese} ({cardToEdit.Romaji})\nArtinya: {cardToEdit.Translation}";
            }

            Show();
        }

        private void PopulatePhotoDropdown(PhotoRecord preselectedPhoto)
        {
            _availablePhotos.Clear();
            if (photoDropdown == null) return;

            photoDropdown.ClearOptions();

            if (_dictionaryManager == null || _dictionaryManager.Photos == null || _dictionaryManager.Photos.Count == 0)
            {
                photoDropdown.options.Add(new TMP_Dropdown.OptionData("- Tidak ada foto di Album -"));
                photoDropdown.value = 0;
                photoDropdown.RefreshShownValue();
                UpdatePreviewWithPhoto(null);
                return;
            }

            int selectedIndex = 0;
            var options = new List<TMP_Dropdown.OptionData>();

            for (int i = 0; i < _dictionaryManager.Photos.Count; i++)
            {
                var photo = _dictionaryManager.Photos[i];
                _availablePhotos.Add(photo);

                string label = $"{i + 1}. {photo.objectName}";
                options.Add(new TMP_Dropdown.OptionData(label));

                if (preselectedPhoto != null && photo.photoId == preselectedPhoto.photoId)
                {
                    selectedIndex = i;
                }
            }

            photoDropdown.AddOptions(options);
            photoDropdown.value = selectedIndex;
            photoDropdown.RefreshShownValue();

            if (_availablePhotos.Count > 0)
            {
                UpdatePreviewWithPhoto(_availablePhotos[selectedIndex]);
            }
        }

        private void OnPhotoDropdownChanged(int index)
        {
            if (_editingCard != null) return; // Abaikan saat mode edit

            if (index >= 0 && index < _availablePhotos.Count)
            {
                UpdatePreviewWithPhoto(_availablePhotos[index]);
            }
            else
            {
                UpdatePreviewWithPhoto(null);
            }
        }

        private void UpdatePreviewWithPhoto(PhotoRecord record)
        {
            if (record == null)
            {
                if (photoPreviewImage != null) photoPreviewImage.gameObject.SetActive(false);
                if (vocabInfoText != null) vocabInfoText.text = "Pilih foto dari daftar di atas.";
                return;
            }

            if (photoPreviewImage != null)
            {
                Texture2D tex = PhotoFileStore.Load(record.imagePath);
                photoPreviewImage.texture = tex;
                photoPreviewImage.gameObject.SetActive(tex != null);
            }

            if (vocabInfoText != null)
            {
                ObjectInfo info = null;
                if (_dictionaryManager != null && _dictionaryManager.Database != null)
                {
                    _dictionaryManager.Database.TryGetObject(record.objectId, out info);
                }

                if (info != null && info.vocabulary != null)
                {
                    vocabInfoText.text = $"{info.vocabulary.DisplayJapanese} ({info.vocabulary.romaji})\nArtinya: {info.vocabulary.translation}";
                }
                else
                {
                    vocabInfoText.text = $"{record.objectName}";
                }
            }
        }

        private void HandleSave()
        {
            string customNote = noteInputField != null ? noteInputField.text.Trim() : "";

            if (_editingCard != null)
            {
                // CRUD: UPDATE catatan
                if (FlashcardManager.Instance != null)
                {
                    FlashcardManager.Instance.UpdateFlashcardNote(_editingCard.Id, customNote);
                }
            }
            else
            {
                // CRUD: CREATE kartu baru
                if (_availablePhotos.Count == 0 || photoDropdown.value >= _availablePhotos.Count)
                {
                    Debug.LogWarning("[FlashcardFormModalUI] Tidak ada foto yang dipilih untuk dibuatkan flashcard.");
                    return;
                }

                var selectedPhoto = _availablePhotos[photoDropdown.value];
                ObjectInfo info = null;
                if (_dictionaryManager != null && _dictionaryManager.Database != null)
                {
                    _dictionaryManager.Database.TryGetObject(selectedPhoto.objectId, out info);
                }

                if (FlashcardManager.Instance != null)
                {
                    FlashcardManager.Instance.CreateFlashcard(selectedPhoto, info, customNote);
                }
            }

            _onSaveCompletedCallback?.Invoke();
            Close();
        }

        public void Show()
        {
            if (modalRoot != null) modalRoot.SetActive(true);
        }

        public void Close()
        {
            Hide();
        }

        public void Hide()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
        }
    }
}
