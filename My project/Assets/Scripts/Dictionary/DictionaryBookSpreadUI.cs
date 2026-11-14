using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    public enum DictionaryBookMode
    {
        Album,
        Like,
        Favorite,
        Quiz,
        Hiragana,
        Module
    }

    /// <summary>
    /// Mengelola tampilan spread 2 halaman buku Dictionary (kiri & kanan).
    /// Berisi 8 slot Polaroid (4 di kiri, 4 di kanan dengan urutan Z-pattern).
    /// Mendukung klik halaman kiri (mundur), klik halaman kanan (maju), dan filtering.
    /// </summary>
    public class DictionaryBookSpreadUI : MonoBehaviour
    {
        [Header("Referensi Sistem")]
        [SerializeField] private DictionaryManager dictionaryManager;

        [Header("Grid Slot Polaroid (Total 8 Slot)")]
        [Tooltip("Urutan: [0-3] Halaman Kiri (TL, TR, BL, BR), [4-7] Halaman Kanan (TL, TR, BL, BR)")]
        [SerializeField] private DictionaryPolaroidCardUI[] polaroidSlots = new DictionaryPolaroidCardUI[8];

        [Header("Area Klik Halaman (Pagination)")]
        [SerializeField] private Button leftPageClickArea;
        [SerializeField] private Button rightPageClickArea;

        [Header("Indikator Halaman (Nomor Halaman)")]
        [SerializeField] private TextMeshProUGUI leftPageNumberText;
        [SerializeField] private TextMeshProUGUI rightPageNumberText;

        [Header("Panels & Placeholder")]
        [SerializeField] private GameObject photoSpreadRoot;
        [SerializeField] private GameObject placeholderRoot;
        [SerializeField] private TextMeshProUGUI placeholderTitleText;
        [SerializeField] private TextMeshProUGUI placeholderDescText;
        [SerializeField] private GameObject emptyStateNotice;
        [SerializeField] private TextMeshProUGUI emptyStateText;

        [Header("Animasi Domino Pergantian Halaman")]
        [Tooltip("Jeda waktu antar pasangan slot domino (detik)")]
        [SerializeField] private float dominoStaggerDelay = 0.045f;
        [Tooltip("Durasi fade out per kartu (detik)")]
        [SerializeField] private float dominoFadeOutDuration = 0.10f;
        [Tooltip("Durasi bouncy pop in per kartu (detik)")]
        [SerializeField] private float dominoPopInDuration = 0.22f;

        // Pasangan slot domino berurutan:
        // Langkah 0: Slot 1 (kiri TL) & Slot 5 (kanan TL)
        // Langkah 1: Slot 2 (kiri TR) & Slot 6 (kanan TR)
        // Langkah 2: Slot 3 (kiri BL) & Slot 7 (kanan BL)
        // Langkah 3: Slot 4 (kiri BR) & Slot 8 (kanan BR)
        private static readonly int[][] DominoPairs = new int[][]
        {
            new int[] { 0, 4 }, // Slot 1 & 5
            new int[] { 1, 5 }, // Slot 2 & 6
            new int[] { 2, 6 }, // Slot 3 & 7
            new int[] { 3, 7 }  // Slot 4 & 8
        };

        private bool isPageTurning = false;
        private Coroutine pageTurnCoroutine;

        private DictionaryBookMode currentMode = DictionaryBookMode.Album;
        private int currentSpreadIndex = 0; // 0 = halaman 1-2, 1 = halaman 3-4, dst.
        private readonly List<PhotoRecord> currentFilteredPhotos = new List<PhotoRecord>();
        private readonly Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();

        public DictionaryBookMode CurrentMode => currentMode;
        public int CurrentSpreadIndex => currentSpreadIndex;

        private void Awake()
        {
            if (dictionaryManager == null)
                dictionaryManager = FindFirstObjectByType<DictionaryManager>();

            if (leftPageClickArea != null)
                leftPageClickArea.onClick.AddListener(PreviousSpread);

            if (rightPageClickArea != null)
                rightPageClickArea.onClick.AddListener(NextSpread);
        }

        private void OnDestroy()
        {
            ClearTextureCache();
        }

        public void SetMode(DictionaryBookMode mode)
        {
            if (pageTurnCoroutine != null)
            {
                StopCoroutine(pageTurnCoroutine);
                pageTurnCoroutine = null;
            }
            isPageTurning = false;

            ResetAllSlotsVisual();

            currentMode = mode;
            currentSpreadIndex = 0;
            Refresh();
        }

        public void PreviousSpread()
        {
            if (isPageTurning) return; // Jeda HANYA berlaku selama animasi transisi
            if (currentSpreadIndex > 0)
            {
                if (photoSpreadRoot != null && gameObject.activeInHierarchy)
                {
                    if (pageTurnCoroutine != null) StopCoroutine(pageTurnCoroutine);
                    pageTurnCoroutine = StartCoroutine(DominoPageTurnRoutine(forward: false));
                }
                else
                {
                    currentSpreadIndex--;
                    Refresh();
                }
            }
        }

        public void NextSpread()
        {
            if (isPageTurning) return; // Jeda HANYA berlaku selama animasi transisi
            int totalSpreads = GetTotalSpreads();
            if (currentSpreadIndex < totalSpreads - 1)
            {
                if (photoSpreadRoot != null && gameObject.activeInHierarchy)
                {
                    if (pageTurnCoroutine != null) StopCoroutine(pageTurnCoroutine);
                    pageTurnCoroutine = StartCoroutine(DominoPageTurnRoutine(forward: true));
                }
                else
                {
                    currentSpreadIndex++;
                    Refresh();
                }
            }
        }

        private System.Collections.IEnumerator DominoPageTurnRoutine(bool forward)
        {
            isPageTurning = true;

            float totalFadeTime = (DominoPairs.Length - 1) * dominoStaggerDelay + dominoFadeOutDuration;
            float elapsed = 0f;

            // 1. Domino Fade-Out: Slot 1&5 -> Slot 2&6 -> Slot 3&7 -> Slot 4&8
            while (elapsed < totalFadeTime)
            {
                elapsed += Time.unscaledDeltaTime;

                for (int step = 0; step < DominoPairs.Length; step++)
                {
                    float pairStartTime = step * dominoStaggerDelay;
                    float pairElapsed = elapsed - pairStartTime;
                    if (pairElapsed < 0f) continue; // Belum giliran pasangan ini

                    float t = Mathf.Clamp01(pairElapsed / dominoFadeOutDuration);
                    float easeIn = t * t;

                    float alpha = Mathf.Lerp(1f, 0f, easeIn);
                    float scale = Mathf.Lerp(1f, 0.90f, easeIn);

                    foreach (int slotIdx in DominoPairs[step])
                    {
                        if (slotIdx < polaroidSlots.Length && polaroidSlots[slotIdx] != null && polaroidSlots[slotIdx].gameObject.activeSelf)
                        {
                            polaroidSlots[slotIdx].SetVisualState(alpha, scale);
                        }
                    }
                }

                yield return null;
            }

            // Pastikan semua slot 0 alpha sebelum data bertukar
            for (int i = 0; i < polaroidSlots.Length; i++)
            {
                if (polaroidSlots[i] != null)
                {
                    polaroidSlots[i].SetVisualState(0f, 0.85f);
                }
            }

            // 2. Titik Pergantian Data Halaman (saat kartu tersembunyi)
            if (forward) currentSpreadIndex++;
            else currentSpreadIndex--;
            Refresh();

            // Set slot yang baru diisi ke state tersembunyi agar siap menerima domino pop-in
            for (int i = 0; i < polaroidSlots.Length; i++)
            {
                if (polaroidSlots[i] != null && polaroidSlots[i].gameObject.activeSelf)
                {
                    polaroidSlots[i].SetVisualState(0f, 0.85f);
                }
            }

            // 3. Domino Fade-In & Bouncy Spring Pop: Slot 1&5 -> Slot 2&6 -> Slot 3&7 -> Slot 4&8
            float totalPopTime = (DominoPairs.Length - 1) * dominoStaggerDelay + dominoPopInDuration;
            elapsed = 0f;

            while (elapsed < totalPopTime)
            {
                elapsed += Time.unscaledDeltaTime;

                for (int step = 0; step < DominoPairs.Length; step++)
                {
                    float pairStartTime = step * dominoStaggerDelay;
                    float pairElapsed = elapsed - pairStartTime;
                    if (pairElapsed < 0f) continue; // Belum giliran pasangan ini

                    float t = Mathf.Clamp01(pairElapsed / dominoPopInDuration);
                    float easeOut = 1f - (1f - t) * (1f - t);

                    float alpha = Mathf.Clamp01(t * 1.8f);
                    // Efek bouncy spring: membal melebihi ukuran normal (1.06x) lalu settle di 1.0x
                    float bounce = Mathf.Sin(t * Mathf.PI) * 0.08f;
                    float baseScale = Mathf.Lerp(0.85f, 1f, easeOut);
                    float currentScale = baseScale + bounce;

                    foreach (int slotIdx in DominoPairs[step])
                    {
                        if (slotIdx < polaroidSlots.Length && polaroidSlots[slotIdx] != null && polaroidSlots[slotIdx].gameObject.activeSelf)
                        {
                            polaroidSlots[slotIdx].SetVisualState(alpha, currentScale);
                        }
                    }
                }

                yield return null;
            }

            // 4. Finalisasi: Pastikan semua slot kembali 100% normal (alpha 1, scale 1)
            ResetAllSlotsVisual();

            isPageTurning = false;
            pageTurnCoroutine = null;
        }

        private void ResetAllSlotsVisual()
        {
            if (polaroidSlots == null) return;
            for (int i = 0; i < polaroidSlots.Length; i++)
            {
                if (polaroidSlots[i] != null)
                {
                    polaroidSlots[i].ResetVisualState();
                }
            }
        }

        public void Refresh()
        {
            if (dictionaryManager == null)
                dictionaryManager = FindFirstObjectByType<DictionaryManager>();

            // 1. Tangani mode Placeholder (Quiz, Hiragana, Module)
            if (currentMode == DictionaryBookMode.Quiz || 
                currentMode == DictionaryBookMode.Hiragana || 
                currentMode == DictionaryBookMode.Module)
            {
                ShowPlaceholderMode(currentMode);
                return;
            }

            // 2. Mode Foto (Album, Like, Favorite)
            if (photoSpreadRoot != null) photoSpreadRoot.SetActive(true);
            if (placeholderRoot != null) placeholderRoot.SetActive(false);

            FilterPhotos();

            int totalPhotos = currentFilteredPhotos.Count;
            int totalSpreads = Mathf.Max(1, Mathf.CeilToInt(totalPhotos / 8f));

            if (currentSpreadIndex >= totalSpreads)
                currentSpreadIndex = Mathf.Max(0, totalSpreads - 1);

            int startIndex = currentSpreadIndex * 8;

            // Periksa jika tidak ada foto sama sekali pada filter ini
            bool isEmpty = totalPhotos == 0;
            if (emptyStateNotice != null)
            {
                emptyStateNotice.SetActive(isEmpty);
                if (isEmpty && emptyStateText != null)
                {
                    if (currentMode == DictionaryBookMode.Like)
                        emptyStateText.text = "No liked photos yet.\nTap the heart icon on any photo in your Album to add it here!";
                    else if (currentMode == DictionaryBookMode.Favorite)
                        emptyStateText.text = "No favorite photos yet.\nTap the star icon on any photo in your Album to add it here!";
                    else
                        emptyStateText.text = "No photos captured yet.\nExplore the town and snap photos of objects to fill your dictionary!";
                }
            }

            // Isi 8 slot polaroid
            for (int i = 0; i < 8; i++)
            {
                int photoIndex = startIndex + i;
                if (i < polaroidSlots.Length && polaroidSlots[i] != null)
                {
                    if (photoIndex < totalPhotos)
                    {
                        var record = currentFilteredPhotos[photoIndex];
                        Texture2D tex = GetOrLoadTexture(record.imagePath);

                        ObjectInfo info = null;
                        if (dictionaryManager != null && dictionaryManager.Database != null)
                        {
                            dictionaryManager.Database.TryGetObject(record.objectId, out info);
                        }

                        polaroidSlots[i].Bind(
                            record, 
                            tex, 
                            info, 
                            i,
                            OnPhotoLikeToggled, 
                            OnPhotoFavoriteToggled
                        );
                    }
                    else
                    {
                        polaroidSlots[i].SetEmpty();
                    }
                }
            }

            // Update nomor halaman (Left: 2 * n + 1, Right: 2 * n + 2)
            int leftPageNum = (currentSpreadIndex * 2) + 1;
            int rightPageNum = (currentSpreadIndex * 2) + 2;

            if (leftPageNumberText != null)
                leftPageNumberText.text = leftPageNum.ToString();

            if (rightPageNumberText != null)
                rightPageNumberText.text = rightPageNum.ToString();

            // Status tombol panah/klik halaman (interactable jika masih ada halaman sebelumnya/selanjutnya)
            if (leftPageClickArea != null)
                leftPageClickArea.interactable = currentSpreadIndex > 0;

            if (rightPageClickArea != null)
                rightPageClickArea.interactable = currentSpreadIndex < (totalSpreads - 1);
        }

        private void FilterPhotos()
        {
            currentFilteredPhotos.Clear();
            if (dictionaryManager == null || dictionaryManager.Photos == null) return;

            foreach (var photo in dictionaryManager.Photos)
            {
                if (currentMode == DictionaryBookMode.Like && !photo.isLiked) continue;
                if (currentMode == DictionaryBookMode.Favorite && !photo.isFavorite) continue;
                currentFilteredPhotos.Add(photo);
            }
        }

        private int GetTotalSpreads()
        {
            int total = currentFilteredPhotos.Count;
            return Mathf.Max(1, Mathf.CeilToInt(total / 8f));
        }

        private void OnPhotoLikeToggled(PhotoRecord record, bool isLiked)
        {
            if (dictionaryManager != null)
            {
                dictionaryManager.SetLike(record.photoId, isLiked);
            }

            // Jika sedang di tab Like dan foto di-unlike, refresh spread agar foto langsung hilang
            if (currentMode == DictionaryBookMode.Like)
            {
                Refresh();
            }
        }

        private void OnPhotoFavoriteToggled(PhotoRecord record, bool isFavorite)
        {
            if (dictionaryManager != null)
            {
                dictionaryManager.SetFavorite(record.photoId, isFavorite);
            }

            // Jika sedang di tab Favorite dan foto di-unfavorite, refresh spread agar foto langsung hilang
            if (currentMode == DictionaryBookMode.Favorite)
            {
                Refresh();
            }
        }

        private void ShowPlaceholderMode(DictionaryBookMode mode)
        {
            if (photoSpreadRoot != null) photoSpreadRoot.SetActive(false);
            if (placeholderRoot != null) placeholderRoot.SetActive(true);
            if (emptyStateNotice != null) emptyStateNotice.SetActive(false);

            if (placeholderTitleText != null)
            {
                if (mode == DictionaryBookMode.Quiz) placeholderTitleText.text = "QUIZ CHALLENGE";
                else if (mode == DictionaryBookMode.Hiragana) placeholderTitleText.text = "HIRAGANA & KATAKANA";
                else if (mode == DictionaryBookMode.Module) placeholderTitleText.text = "LEARNING MODULES";
            }

            if (placeholderDescText != null)
            {
                if (mode == DictionaryBookMode.Quiz) 
                    placeholderDescText.text = "Test your vocabulary retention with quick quizzes!\n(Feature coming soon in future update)";
                else if (mode == DictionaryBookMode.Hiragana) 
                    placeholderDescText.text = "A complete interactive Japanese character chart.\n(Feature coming soon in future update)";
                else if (mode == DictionaryBookMode.Module) 
                    placeholderDescText.text = "Guided learning chapters and grammar points.\n(Feature coming soon in future update)";
            }

            if (leftPageNumberText != null) leftPageNumberText.text = "—";
            if (rightPageNumberText != null) rightPageNumberText.text = "—";
            if (leftPageClickArea != null) leftPageClickArea.interactable = false;
            if (rightPageClickArea != null) rightPageClickArea.interactable = false;
        }

        private Texture2D GetOrLoadTexture(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            if (loadedTextures.TryGetValue(relativePath, out var tex) && tex != null)
            {
                return tex;
            }

            tex = PhotoFileStore.Load(relativePath);
            if (tex != null)
            {
                loadedTextures[relativePath] = tex;
            }
            return tex;
        }

        private void ClearTextureCache()
        {
            foreach (var kvp in loadedTextures)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            loadedTextures.Clear();
        }
    }
}

