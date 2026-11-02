using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Menangani antarmuka Tab Kosakata di dalam buku Dictionary.
    /// Dilengkapi Search Bar, indikator progres belajar, dan paginasi buku (4 kiri + 4 kanan = 8 kartu).
    /// </summary>
    public class DictionaryVocabularyTabUI : MonoBehaviour
    {
        [Header("Referensi Sistem")]
        [SerializeField] private DictionaryManager dictionary;

        [Header("Kartu Kosakata Buku (8 Kartu: 0-3 Halaman Kiri, 4-7 Halaman Kanan)")]
        [SerializeField] private DictionaryVocabCardUI[] cards = new DictionaryVocabCardUI[8];

        [Header("Search Bar & Progress")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Text progressLabel;

        [Header("Paginasi Buku")]
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private TMP_Text pageIndicatorLabel;

        [Header("Modal Detail")]
        [SerializeField] private DictionaryVocabDetailModal detailModal;

        [Header("Empty State")]
        [SerializeField] private GameObject emptyStateObject;
        [SerializeField] private TMP_Text emptyStateLabel;

        private const int CardsPerPage = 8;
        private int currentPage = 0;
        private string searchKeyword = "";

        private readonly List<ObjectInfo> allObjects = new List<ObjectInfo>();
        private readonly List<ObjectInfo> filteredObjects = new List<ObjectInfo>();

        private void Awake()
        {
            if (dictionary == null) dictionary = FindFirstObjectByType<DictionaryManager>();

            if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchChanged);
            if (prevPageButton != null) prevPageButton.onClick.AddListener(PrevPage);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
        }

        private void OnEnable()
        {
            if (dictionary != null) dictionary.OnDictionaryChanged += RefreshUI;
            RefreshUI();
        }

        private void OnDisable()
        {
            if (dictionary != null) dictionary.OnDictionaryChanged -= RefreshUI;
        }

        public void RefreshUI()
        {
            if (dictionary == null) dictionary = FindFirstObjectByType<DictionaryManager>();
            if (dictionary == null) return;

            LoadCatalog();
            FilterList();
            UpdateProgress();
            RenderCurrentPage();
        }

        private void LoadCatalog()
        {
            allObjects.Clear();
            var catalog = dictionary.GetAllCatalogObjects();
            if (catalog != null)
            {
                allObjects.AddRange(catalog);
            }
        }

        private void OnSearchChanged(string query)
        {
            searchKeyword = query != null ? query.Trim().ToLowerInvariant() : "";
            currentPage = 0;
            FilterList();
            RenderCurrentPage();
        }

        private void FilterList()
        {
            filteredObjects.Clear();

            if (string.IsNullOrEmpty(searchKeyword))
            {
                filteredObjects.AddRange(allObjects);
            }
            else
            {
                foreach (var info in allObjects)
                {
                    if (info == null) continue;
                    var v = info.vocabulary;
                    bool match = false;

                    if (!string.IsNullOrEmpty(info.objectName) && info.objectName.ToLowerInvariant().Contains(searchKeyword)) match = true;
                    else if (v != null)
                    {
                        if (!string.IsNullOrEmpty(v.japaneseWord) && v.japaneseWord.ToLowerInvariant().Contains(searchKeyword)) match = true;
                        else if (!string.IsNullOrEmpty(v.kana) && v.kana.ToLowerInvariant().Contains(searchKeyword)) match = true;
                        else if (!string.IsNullOrEmpty(v.romaji) && v.romaji.ToLowerInvariant().Contains(searchKeyword)) match = true;
                        else if (!string.IsNullOrEmpty(v.translation) && v.translation.ToLowerInvariant().Contains(searchKeyword)) match = true;
                        else if (!string.IsNullOrEmpty(v.category) && v.category.ToLowerInvariant().Contains(searchKeyword)) match = true;
                    }

                    if (match) filteredObjects.Add(info);
                }
            }
        }

        private void UpdateProgress()
        {
            if (progressLabel == null || dictionary == null) return;

            int total = allObjects.Count;
            int discovered = 0;
            foreach (var info in allObjects)
            {
                if (info == null) continue;
                bool isDiscovered = dictionary.IsObjectDiscovered(info.objectId) ||
                                   (info.vocabulary != null && dictionary.IsVocabularyDiscovered(info.vocabulary.vocabularyId));
                if (isDiscovered) discovered++;
            }

            int percent = total > 0 ? Mathf.RoundToInt((float)discovered / total * 100f) : 0;
            progressLabel.text = $"Kosakata Ditemukan: {discovered} / {total} ({percent}%)";
        }

        private void RenderCurrentPage()
        {
            int total = filteredObjects.Count;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)total / CardsPerPage));

            if (currentPage >= totalPages) currentPage = totalPages - 1;
            if (currentPage < 0) currentPage = 0;

            if (pageIndicatorLabel != null)
            {
                pageIndicatorLabel.text = $"Halaman {currentPage + 1} / {totalPages}";
            }

            if (prevPageButton != null) prevPageButton.interactable = currentPage > 0;
            if (nextPageButton != null) nextPageButton.interactable = currentPage < totalPages - 1;

            int startIndex = currentPage * CardsPerPage;

            // Render ke 8 kartu (4 kiri, 4 kanan)
            for (int i = 0; i < CardsPerPage; i++)
            {
                if (i < cards.Length && cards[i] != null)
                {
                    int itemIndex = startIndex + i;
                    if (itemIndex < total)
                    {
                        var info = filteredObjects[itemIndex];
                        bool isDiscovered = dictionary.IsObjectDiscovered(info.objectId) ||
                                           (info.vocabulary != null && dictionary.IsVocabularyDiscovered(info.vocabulary.vocabularyId));
                        cards[i].Bind(info, isDiscovered, OnCardClicked);
                    }
                    else
                    {
                        cards[i].Bind(null, false, null);
                    }
                }
            }

            if (emptyStateObject != null)
            {
                emptyStateObject.SetActive(total == 0);
                if (emptyStateLabel != null)
                {
                    emptyStateLabel.text = string.IsNullOrEmpty(searchKeyword)
                        ? "Belum ada katalog kosakata yang terdaftar."
                        : $"Tidak ditemukan kosakata dengan kata kunci \"{searchKeyword}\".";
                }
            }
        }

        private void OnCardClicked(ObjectInfo info)
        {
            if (detailModal != null)
            {
                detailModal.Open(info);
            }
        }

        private void PrevPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                RenderCurrentPage();
            }
        }

        private void NextPage()
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredObjects.Count / CardsPerPage));
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                RenderCurrentPage();
            }
        }
    }
}

