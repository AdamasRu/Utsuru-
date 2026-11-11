using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    public enum PhotoSortMode
    {
        NewestStack = 0,    // Stack LIFO (Default)
        Oldest = 1,         // FIFO
        FavoriteFirst = 2,  // Favorit di atas
        JapaneseAlphabet = 3 // Urutan abjad (A-I-U-E-O berdasarkan Romaji/Kana)
    }

    /// <summary>
    /// Menangani antarmuka Tab Album di dalam buku Dictionary.
    /// Format buku terbuka: 4 foto di halaman kiri + 4 foto di halaman kanan (total 8 slot per halaman buku).
    /// </summary>
    public class DictionaryAlbumTabUI : MonoBehaviour
    {
        [Header("Referensi Sistem")]
        [SerializeField] private DictionaryManager dictionary;

        [Header("Slot Foto Buku (8 Slot: 0-3 Halaman Kiri, 4-7 Halaman Kanan)")]
        [SerializeField] private DictionaryPhotoSlotUI[] slots = new DictionaryPhotoSlotUI[8];

        [Header("Paginasi Buku")]
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private TMP_Text pageIndicatorLabel;

        [Header("Kontrol Album")]
        [SerializeField] private TMP_Dropdown albumDropdown;
        [SerializeField] private Button createAlbumButton;
        [SerializeField] private Button renameAlbumButton;
        [SerializeField] private Button deleteAlbumButton;

        [Header("Filter & Sorting")]
        [SerializeField] private Button favoriteFilterButton;
        [SerializeField] private TMP_Text favoriteFilterText;
        [SerializeField] private TMP_Dropdown sortDropdown;

        [Header("Modal & Dialog")]
        [SerializeField] private DictionaryPhotoDetailModal detailModal;
        [SerializeField] private DictionarySimpleDialog dialog;

        [Header("Empty State")]
        [SerializeField] private GameObject emptyStateObject;
        [SerializeField] private TMP_Text emptyStateLabel;

        private const int PhotosPerPage = 8;
        private int currentPage = 0;
        private bool filterFavoriteOnly = false;
        private PhotoSortMode currentSortMode = PhotoSortMode.NewestStack;

        private readonly List<PhotoRecord> currentFilteredPhotos = new List<PhotoRecord>();
        private readonly List<AlbumRecord> cachedAlbums = new List<AlbumRecord>();

        private void Awake()
        {
            if (dictionary == null) dictionary = FindFirstObjectByType<DictionaryManager>();

            if (prevPageButton != null) prevPageButton.onClick.AddListener(PrevPage);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);

            if (albumDropdown != null) albumDropdown.onValueChanged.AddListener(OnAlbumSelected);
            if (createAlbumButton != null) createAlbumButton.onClick.AddListener(PromptCreateAlbum);
            if (renameAlbumButton != null) renameAlbumButton.onClick.AddListener(PromptRenameAlbum);
            if (deleteAlbumButton != null) deleteAlbumButton.onClick.AddListener(PromptDeleteAlbum);

            if (favoriteFilterButton != null) favoriteFilterButton.onClick.AddListener(ToggleFavoriteFilter);
            if (sortDropdown != null)
            {
                sortDropdown.ClearOptions();
                sortDropdown.AddOptions(new List<string> { "Terbaru (Stack)", "Terlama", "Favorit Teratas", "Abjad Jepang (A-I-U)" });
                sortDropdown.onValueChanged.AddListener(OnSortChanged);
            }
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

            RefreshAlbumDropdown();
            RefreshPhotoList();
            RenderCurrentPage();
        }

        private void RefreshAlbumDropdown()
        {
            if (albumDropdown == null) return;

            cachedAlbums.Clear();
            albumDropdown.ClearOptions();

            var options = new List<string>();
            int selectedIndex = 0;
            int i = 0;
            foreach (var a in dictionary.Albums)
            {
                cachedAlbums.Add(a);
                options.Add(a.albumName);
                if (a.albumId == dictionary.ActiveAlbumId) selectedIndex = i;
                i++;
            }

            albumDropdown.AddOptions(options);
            albumDropdown.SetValueWithoutNotify(selectedIndex);
        }

        private void OnAlbumSelected(int index)
        {
            if (index >= 0 && index < cachedAlbums.Count)
            {
                dictionary.SetActiveAlbum(cachedAlbums[index].albumId);
                currentPage = 0;
                RefreshPhotoList();
                RenderCurrentPage();
            }
        }

        private void ToggleFavoriteFilter()
        {
            filterFavoriteOnly = !filterFavoriteOnly;
            if (favoriteFilterText != null)
            {
                favoriteFilterText.text = filterFavoriteOnly ? "★ Favorit Saja" : "☆ Semua Foto";
                favoriteFilterText.color = filterFavoriteOnly ? new Color(1f, 0.85f, 0.1f) : Color.white;
            }
            currentPage = 0;
            RefreshPhotoList();
            RenderCurrentPage();
        }

        private void OnSortChanged(int index)
        {
            currentSortMode = (PhotoSortMode)index;
            currentPage = 0;
            RefreshPhotoList();
            RenderCurrentPage();
        }

        private void RefreshPhotoList()
        {
            currentFilteredPhotos.Clear();
            int activeAlbumId = dictionary.ActiveAlbumId;

            // Ambil foto di album aktif
            IEnumerable<PhotoRecord> query = dictionary.Photos.Where(p => p.albumId == activeAlbumId);

            // Filter favorit jika aktif
            if (filterFavoriteOnly)
            {
                query = query.Where(p => p.isFavorite);
            }

            // Sorting
            switch (currentSortMode)
            {
                case PhotoSortMode.NewestStack:
                    // Urutan simpan Stack (LIFO): dictionary.Photos sudah disimpan LIFO di index 0
                    currentFilteredPhotos.AddRange(query);
                    break;

                case PhotoSortMode.Oldest:
                    currentFilteredPhotos.AddRange(query.Reverse());
                    break;

                case PhotoSortMode.FavoriteFirst:
                    currentFilteredPhotos.AddRange(query.OrderByDescending(p => p.isFavorite));
                    break;

                case PhotoSortMode.JapaneseAlphabet:
                    currentFilteredPhotos.AddRange(query.OrderBy(p => GetSortKey(p)));
                    break;
            }
        }

        private string GetSortKey(PhotoRecord photo)
        {
            if (dictionary.Database != null && dictionary.Database.TryGetObject(photo.objectId, out ObjectInfo info) && info.vocabulary != null)
            {
                if (!string.IsNullOrEmpty(info.vocabulary.kana)) return info.vocabulary.kana;
                if (!string.IsNullOrEmpty(info.vocabulary.romaji)) return info.vocabulary.romaji;
            }
            return photo.objectName ?? "";
        }

        private void RenderCurrentPage()
        {
            int totalPhotos = currentFilteredPhotos.Count;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalPhotos / PhotosPerPage));

            if (currentPage >= totalPages) currentPage = totalPages - 1;
            if (currentPage < 0) currentPage = 0;

            if (pageIndicatorLabel != null)
            {
                pageIndicatorLabel.text = $"Halaman {currentPage + 1} / {totalPages}";
            }

            if (prevPageButton != null) prevPageButton.interactable = currentPage > 0;
            if (nextPageButton != null) nextPageButton.interactable = currentPage < totalPages - 1;

            int startIndex = currentPage * PhotosPerPage;

            // Render ke 8 slot (4 kiri: index 0-3, 4 kanan: index 4-7)
            for (int i = 0; i < PhotosPerPage; i++)
            {
                if (i < slots.Length && slots[i] != null)
                {
                    int photoIndex = startIndex + i;
                    if (photoIndex < totalPhotos)
                    {
                        slots[i].Bind(currentFilteredPhotos[photoIndex], OnPhotoClicked, OnSlotToggleFavorite);
                    }
                    else
                    {
                        slots[i].Bind(null, null, null);
                    }
                }
            }

            if (emptyStateObject != null)
            {
                emptyStateObject.SetActive(totalPhotos == 0);
                if (emptyStateLabel != null)
                {
                    emptyStateLabel.text = filterFavoriteOnly
                        ? "Belum ada foto favorit di album ini."
                        : "Album ini masih kosong.\nBuka Mode Kamera (C) untuk memotret objek jalanan!";
                }
            }
        }

        private void OnPhotoClicked(PhotoRecord record)
        {
            if (detailModal != null)
            {
                detailModal.Open(record, dictionary, RefreshUI);
            }
        }

        private void OnSlotToggleFavorite(PhotoRecord record)
        {
            if (record != null && dictionary != null)
            {
                dictionary.ToggleFavorite(record.photoId);
                RefreshUI();
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
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)currentFilteredPhotos.Count / PhotosPerPage));
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                RenderCurrentPage();
            }
        }

        private void PromptCreateAlbum()
        {
            if (dialog == null) return;
            dialog.ShowInput("Buat Album Baru", "Album Baru", "Buat", (name) =>
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var newAlbum = dictionary.CreateAlbum(name);
                    dictionary.SetActiveAlbum(newAlbum.albumId);
                    RefreshUI();
                }
            });
        }

        private void PromptRenameAlbum()
        {
            if (dialog == null) return;
            var currentAlbum = dictionary.Albums.FirstOrDefault(a => a.albumId == dictionary.ActiveAlbumId);
            if (currentAlbum == null) return;

            dialog.ShowInput("Ganti Nama Album", currentAlbum.albumName, "Simpan", (newName) =>
            {
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    dictionary.RenameAlbum(currentAlbum.albumId, newName);
                    RefreshUI();
                }
            });
        }

        private void PromptDeleteAlbum()
        {
            if (dialog == null) return;
            if (dictionary.Albums.Count <= 1)
            {
                dialog.ShowConfirm("Perhatian", "Tidak bisa menghapus album terakhir. Minimal harus ada 1 album di Dictionary.", "Tutup", null);
                return;
            }

            var currentAlbum = dictionary.Albums.FirstOrDefault(a => a.albumId == dictionary.ActiveAlbumId);
            if (currentAlbum == null) return;

            dialog.ShowConfirm("Hapus Album", $"Apakah kamu yakin ingin menghapus album '{currentAlbum.albumName}'?\nSemua foto di dalamnya juga akan terhapus.", "Hapus", () =>
            {
                dictionary.DeleteAlbum(currentAlbum.albumId);
                RefreshUI();
            });
        }
    }
}

