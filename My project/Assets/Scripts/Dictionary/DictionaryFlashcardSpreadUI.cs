using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;
using Utsuru.FlashcardSystem;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Mengelola tampilan 2 halaman (Spread) untuk fitur Flashcard di dalam buku Dictionary.
    /// Halaman Kiri: Manajemen Deck, Filter, Daftar Kartu Mini, dan tombol Buat Kartu (CREATE).
    /// Halaman Kanan: Kartu Belajar Interaktif (Flip 2 Sisi), Penilaian Diri (UPDATE), Edit & Hapus (UPDATE/DELETE).
    /// </summary>
    public class DictionaryFlashcardSpreadUI : MonoBehaviour
    {
        [Header("Referensi Sistem")]
        [SerializeField] private DictionaryManager dictionaryManager;
        [SerializeField] private DictionarySimpleDialog confirmDialog;
        [SerializeField] private FlashcardFormModalUI formModal;
        [SerializeField] private AudioSource audioSource;

        [Header("Halaman Kiri - Deck & Filter")]
        [SerializeField] private TextMeshProUGUI deckSummaryText;
        [SerializeField] private Button filterAllButton;
        [SerializeField] private Button filterLearningButton;
        [SerializeField] private Button filterMasteredButton;
        [SerializeField] private TextMeshProUGUI filterAllText;
        [SerializeField] private TextMeshProUGUI filterLearningText;
        [SerializeField] private TextMeshProUGUI filterMasteredText;

        [Header("Halaman Kiri - Aksi Deck")]
        [SerializeField] private Button createNewCardButton;
        [SerializeField] private Button autoGenerateButton;
        [SerializeField] private Transform cardListContent;
        [SerializeField] private GameObject cardItemPrefab;
        [SerializeField] private GameObject emptyDeckNotice;
        [SerializeField] private TextMeshProUGUI emptyDeckNoticeText;

        [Header("Halaman Kanan - Wadah Utama")]
        [SerializeField] private GameObject studyCardContainer;
        [SerializeField] private GameObject emptySelectionContainer;

        [Header("Halaman Kanan - Kartu Flip Interaktif")]
        [SerializeField] private RectTransform flipCardTransform;
        [SerializeField] private GameObject frontSideRoot;
        [SerializeField] private GameObject backSideRoot;

        [Header("Kartu Sisi Depan (Front)")]
        [SerializeField] private RawImage frontPhotoImage;
        [SerializeField] private TextMeshProUGUI frontPromptLabel;
        [SerializeField] private TextMeshProUGUI frontObjectNameText;
        [SerializeField] private TextMeshProUGUI categoryBadgeText;
        [SerializeField] private Button frontAudioButton;
        [SerializeField] private Button flipToBackButton;

        [Header("Kartu Sisi Belakang (Back)")]
        [SerializeField] private TextMeshProUGUI backJapaneseText;
        [SerializeField] private TextMeshProUGUI backRomajiText;
        [SerializeField] private TextMeshProUGUI backTranslationText;
        [SerializeField] private TextMeshProUGUI backNoteText;
        [SerializeField] private Button backAudioButton;
        [SerializeField] private Button flipToFrontButton;

        [Header("Halaman Kanan - Aksi Belajar (Review)")]
        [SerializeField] private Button markLearningButton;  // Belum Hafal
        [SerializeField] private Button markMasteredButton;  // Sudah Hafal
        [SerializeField] private Button editCardButton;        // Edit Catatan
        [SerializeField] private Button deleteCardButton;      // Hapus Kartu

        [Header("Pengaturan Animasi Flip")]
        [SerializeField] private float flipDuration = 0.2f;

        private enum FilterMode { All, Learning, Mastered }
        private FilterMode currentFilter = FilterMode.All;

        private PhotoFlashcard selectedCard = null;
        private bool isFlippedToBack = false;
        private bool isFlipping = false;
        private Coroutine flipCoroutine;

        private readonly List<DictionaryFlashcardItemUI> spawnedItems = new List<DictionaryFlashcardItemUI>();
        private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        private void Awake()
        {
            if (dictionaryManager == null)
                dictionaryManager = FindFirstObjectByType<DictionaryManager>();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Daftarkan listener tombol filter
            if (filterAllButton != null) filterAllButton.onClick.AddListener(() => SetFilter(FilterMode.All));
            if (filterLearningButton != null) filterLearningButton.onClick.AddListener(() => SetFilter(FilterMode.Learning));
            if (filterMasteredButton != null) filterMasteredButton.onClick.AddListener(() => SetFilter(FilterMode.Mastered));

            // Daftarkan tombol aksi CRUD
            if (createNewCardButton != null) createNewCardButton.onClick.AddListener(OpenCreateModal);
            if (autoGenerateButton != null) autoGenerateButton.onClick.AddListener(AutoGenerateFromAlbum);

            // Flip Card Listeners
            if (flipToBackButton != null) flipToBackButton.onClick.AddListener(() => FlipCard(true));
            if (flipToFrontButton != null) flipToFrontButton.onClick.AddListener(() => FlipCard(false));

            // Review Evaluasi Diri
            if (markLearningButton != null) markLearningButton.onClick.AddListener(() => HandleReview(false));
            if (markMasteredButton != null) markMasteredButton.onClick.AddListener(() => HandleReview(true));

            // Update & Delete
            if (editCardButton != null) editCardButton.onClick.AddListener(OpenEditModal);
            if (deleteCardButton != null) deleteCardButton.onClick.AddListener(ConfirmDeleteCurrentCard);

            // Audio Pelafalan
            if (frontAudioButton != null) frontAudioButton.onClick.AddListener(PlayCurrentCardAudio);
            if (backAudioButton != null) backAudioButton.onClick.AddListener(PlayCurrentCardAudio);
        }

        private void OnEnable()
        {
            if (FlashcardManager.Instance != null)
            {
                FlashcardManager.Instance.OnFlashcardCollectionChanged += RefreshUI;
            }
            RefreshUI();
        }

        private void OnDisable()
        {
            if (FlashcardManager.Instance != null)
            {
                FlashcardManager.Instance.OnFlashcardCollectionChanged -= RefreshUI;
            }
        }

        private void OnDestroy()
        {
            ClearTextureCache();
        }

        public void StartOrRefresh()
        {
            RefreshUI();
        }

        private void SetFilter(FilterMode filter)
        {
            currentFilter = filter;
            RefreshUI();
        }

        /// <summary>
        /// Refresh seluruh antarmuka 2 halaman secara reaktif.
        /// </summary>
        public void RefreshUI()
        {
            if (FlashcardManager.Instance == null || FlashcardManager.Instance.Repository == null)
            {
                return;
            }

            var allCards = FlashcardManager.Instance.GetAllFlashcards();
            int totalCount = allCards.Count;
            int masteredCount = 0;
            int learningCount = 0;

            foreach (var card in allCards)
            {
                if (card.MasteryStatus == FlashcardMasteryStatus.Mastered) masteredCount++;
                else if (card.MasteryStatus == FlashcardMasteryStatus.Learning) learningCount++;
            }

            // Update Header Statistik di Halaman Kiri
            if (deckSummaryText != null)
            {
                deckSummaryText.text = $"Total: {totalCount} Kartu  |  Hafal: {masteredCount}  |  Belajar: {learningCount}";
            }

            UpdateFilterTabsVisual(totalCount, learningCount, masteredCount);

            // Saring kartu sesuai filter aktif
            var filteredList = new List<PhotoFlashcard>();
            foreach (var card in allCards)
            {
                if (currentFilter == FilterMode.Learning && card.MasteryStatus != FlashcardMasteryStatus.Learning) continue;
                if (currentFilter == FilterMode.Mastered && card.MasteryStatus != FlashcardMasteryStatus.Mastered) continue;
                filteredList.Add(card);
            }

            PopulateCardList(filteredList);

            // Sinkronisasi kartu terpilih
            if (selectedCard != null)
            {
                // Cek apakah kartu terpilih masih ada di database
                selectedCard = FlashcardManager.Instance.Repository.GetById(selectedCard.Id);
            }

            if (selectedCard == null && filteredList.Count > 0)
            {
                selectedCard = filteredList[0];
            }

            UpdateStudyCardVisual();
        }

        private void UpdateFilterTabsVisual(int total, int learning, int mastered)
        {
            if (filterAllText != null) filterAllText.text = $"Semua ({total})";
            if (filterLearningText != null) filterLearningText.text = $"Belajar ({learning})";
            if (filterMasteredText != null) filterMasteredText.text = $"Hafal ({mastered})";
        }

        private void PopulateCardList(List<PhotoFlashcard> cards)
        {
            bool isEmpty = cards.Count == 0;
            if (emptyDeckNotice != null)
            {
                emptyDeckNotice.SetActive(isEmpty);
                if (isEmpty && emptyDeckNoticeText != null)
                {
                    if (currentFilter == FilterMode.Learning)
                        emptyDeckNoticeText.text = "Tidak ada kartu dalam status Belajar.\nSemua kartu sudah Anda kuasai!";
                    else if (currentFilter == FilterMode.Mastered)
                        emptyDeckNoticeText.text = "Belum ada kartu yang dihafal.\nBuka kartu di sebelah kanan dan tandai 'Sudah Hafal'!";
                    else
                        emptyDeckNoticeText.text = "Deck Anda masih kosong.\nTekan [+ Buat Flashcard] untuk membuat kartu dari foto!";
                }
            }

            if (cardListContent == null || cardItemPrefab == null) return;

            // Pastikan jumlah instance mencukupi
            while (spawnedItems.Count < cards.Count)
            {
                var go = Instantiate(cardItemPrefab, cardListContent);
                var itemUi = go.GetComponent<DictionaryFlashcardItemUI>();
                spawnedItems.Add(itemUi);
            }

            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (i < cards.Count)
                {
                    var card = cards[i];
                    Texture2D tex = GetOrLoadTexture(card.PhotoRelativePath);
                    bool isSelected = selectedCard != null && selectedCard.Id == card.Id;

                    spawnedItems[i].Bind(card, tex, isSelected, OnCardSelectedFromList);
                }
                else
                {
                    spawnedItems[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnCardSelectedFromList(PhotoFlashcard card)
        {
            selectedCard = card;
            isFlippedToBack = false; // Reset posisi tampak kartu ke depan

            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null && spawnedItems[i].gameObject.activeSelf)
                {
                    // Update highlight
                    spawnedItems[i].SetSelected(false);
                }
            }

            UpdateStudyCardVisual();
        }

        /// <summary>
        /// Update visual kartu belajar di Halaman Kanan.
        /// </summary>
        private void UpdateStudyCardVisual()
        {
            if (selectedCard == null)
            {
                if (studyCardContainer != null) studyCardContainer.SetActive(false);
                if (emptySelectionContainer != null) emptySelectionContainer.SetActive(true);
                return;
            }

            if (studyCardContainer != null) studyCardContainer.SetActive(true);
            if (emptySelectionContainer != null) emptySelectionContainer.SetActive(false);

            // Sisi Depan
            if (frontPhotoImage != null)
            {
                frontPhotoImage.texture = GetOrLoadTexture(selectedCard.PhotoRelativePath);
            }
            if (frontObjectNameText != null)
            {
                frontObjectNameText.text = selectedCard.ObjectName;
            }
            if (categoryBadgeText != null)
            {
                string category = "Vocabulary";
                if (dictionaryManager != null && dictionaryManager.Database != null &&
                    dictionaryManager.Database.TryGetObject(selectedCard.ObjectId, out ObjectInfo info) &&
                    info.vocabulary != null && !string.IsNullOrEmpty(info.vocabulary.category))
                {
                    category = info.vocabulary.category;
                }

                categoryBadgeText.text = $"Category: {category}";
            }

            // Sisi Belakang
            if (backJapaneseText != null)
            {
                backJapaneseText.text = selectedCard.DisplayJapanese;
            }
            if (backRomajiText != null)
            {
                backRomajiText.text = $"[{selectedCard.Romaji}]";
            }
            if (backTranslationText != null)
            {
                backTranslationText.text = selectedCard.Translation;
            }
            if (backNoteText != null)
            {
                if (string.IsNullOrEmpty(selectedCard.CustomNote))
                {
                    backNoteText.text = "<i>Belum ada catatan pribadi. Tekan [Edit Catatan] untuk menambahkan petunjuk mnemonik.</i>";
                }
                else
                {
                    backNoteText.text = $"Catatan: {selectedCard.CustomNote}";
                }
            }

            // Terapkan sisi depan / belakang
            ApplySideVisual(isFlippedToBack);
        }

        private void ApplySideVisual(bool toBack)
        {
            if (frontSideRoot != null) frontSideRoot.SetActive(!toBack);
            if (backSideRoot != null) backSideRoot.SetActive(toBack);
            if (flipCardTransform != null) flipCardTransform.localScale = Vector3.one;
        }

        public void FlipCard(bool toBack)
        {
            if (isFlipping) return;
            if (flipCoroutine != null) StopCoroutine(flipCoroutine);
            flipCoroutine = StartCoroutine(FlipCardRoutine(toBack));
        }

        private IEnumerator FlipCardRoutine(bool toBack)
        {
            isFlipping = true;

            float halfDuration = flipDuration * 0.5f;
            float elapsed = 0f;

            // 1. Scale X dari 1 ke 0 (menutup)
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float scaleX = Mathf.Lerp(1f, 0f, t);

                if (flipCardTransform != null)
                    flipCardTransform.localScale = new Vector3(scaleX, 1f, 1f);

                yield return null;
            }

            // 2. Ganti sisi tampilan kartu
            isFlippedToBack = toBack;
            if (frontSideRoot != null) frontSideRoot.SetActive(!toBack);
            if (backSideRoot != null) backSideRoot.SetActive(toBack);

            // 3. Scale X dari 0 ke 1 (membuka kembali dengan bounce halus)
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float scaleX = Mathf.Lerp(0f, 1f, t);

                if (flipCardTransform != null)
                    flipCardTransform.localScale = new Vector3(scaleX, 1f, 1f);

                yield return null;
            }

            if (flipCardTransform != null)
                flipCardTransform.localScale = Vector3.one;

            isFlipping = false;
            flipCoroutine = null;
        }

        private void HandleReview(bool isMastered)
        {
            if (selectedCard == null || FlashcardManager.Instance == null) return;

            FlashcardManager.Instance.ReviewFlashcard(selectedCard.Id, isMastered);
            RefreshUI();
        }

        private void OpenCreateModal()
        {
            if (formModal != null)
            {
                formModal.OpenCreate(dictionaryManager, () =>
                {
                    RefreshUI();
                });
            }
        }

        private void OpenEditModal()
        {
            if (selectedCard == null || formModal == null) return;

            formModal.OpenEdit(selectedCard, dictionaryManager, () =>
            {
                RefreshUI();
            });
        }

        private void ConfirmDeleteCurrentCard()
        {
            if (selectedCard == null) return;

            string cardTitle = selectedCard.DisplayJapanese;
            string cardIdToDelete = selectedCard.Id;

            if (confirmDialog != null)
            {
                confirmDialog.ShowConfirm(
                    "Hapus Flashcard",
                    $"Apakah Anda yakin ingin menghapus kartu '{cardTitle}' dari deck?\n(Foto fisik di Album tidak akan terhapus)",
                    "Hapus",
                    () =>
                    {
                        if (FlashcardManager.Instance != null)
                        {
                            FlashcardManager.Instance.DeleteFlashcard(cardIdToDelete);
                            selectedCard = null;
                            RefreshUI();
                        }
                    }
                );
            }
            else
            {
                // Fallback jika dialog tidak dihubungkan
                if (FlashcardManager.Instance != null)
                {
                    FlashcardManager.Instance.DeleteFlashcard(cardIdToDelete);
                    selectedCard = null;
                    RefreshUI();
                }
            }
        }

        /// <summary>
        /// Fitur pelengkap: Auto-generate flashcards dari semua foto di Album yang belum memiliki kartu.
        /// </summary>
        private void AutoGenerateFromAlbum()
        {
            if (dictionaryManager == null || dictionaryManager.Photos == null || FlashcardManager.Instance == null) return;

            int addedCount = 0;
            foreach (var photo in dictionaryManager.Photos)
            {
                if (!FlashcardManager.Instance.HasFlashcardForPhoto(photo.photoId))
                {
                    ObjectInfo info = null;
                    if (dictionaryManager.Database != null)
                    {
                        dictionaryManager.Database.TryGetObject(photo.objectId, out info);
                    }

                    FlashcardManager.Instance.CreateFlashcard(photo, info, "Auto-generated dari album");
                    addedCount++;
                }
            }

            Debug.Log($"[DictionaryFlashcardSpreadUI] Auto-generate selesai: {addedCount} kartu baru ditambahkan.");
            RefreshUI();
        }

        private void PlayCurrentCardAudio()
        {
            if (selectedCard == null || dictionaryManager == null || dictionaryManager.Database == null) return;

            if (dictionaryManager.Database.TryGetObject(selectedCard.ObjectId, out ObjectInfo info) && info.vocabulary != null)
            {
                AudioClip clip = info.vocabulary.audioClip;
                if (clip == null && !string.IsNullOrEmpty(info.vocabulary.audioPath))
                {
                    clip = Resources.Load<AudioClip>(info.vocabulary.audioPath);
                }

                if (clip != null && audioSource != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
        }

        private Texture2D GetOrLoadTexture(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

            if (textureCache.TryGetValue(relativePath, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D loaded = PhotoFileStore.Load(relativePath);
            if (loaded != null)
            {
                textureCache[relativePath] = loaded;
            }
            return loaded;
        }

        private void ClearTextureCache()
        {
            foreach (var kvp in textureCache)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            textureCache.Clear();
        }
    }
}
