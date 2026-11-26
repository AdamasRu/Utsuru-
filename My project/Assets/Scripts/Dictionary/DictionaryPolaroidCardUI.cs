using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Mengelola satu kartu Polaroid di halaman buku Dictionary.
    /// Memiliki sisi depan (foto, kata, like, favorite) dan sisi belakang (contoh kalimat),
    /// serta animasi flip 3D (rotasi sumbu Y) saat kartu diklik.
    /// </summary>
    public class DictionaryPolaroidCardUI : MonoBehaviour
    {
        [Header("Container & Flip")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private CanvasGroup cardCanvasGroup;
        [SerializeField] private GameObject frontFace;
        [SerializeField] private GameObject backFace;
        [SerializeField] private Button frontClickButton;
        [SerializeField] private Button backClickButton;
        [SerializeField] private Button cardClickButton; // Fallback jika frontClickButton belum di-assign
        [SerializeField] private float flipDuration = 0.22f;

        [Header("Elemen Depan (Front)")]
        [SerializeField] private Image tapeImage;
        [SerializeField] private RawImage photoImage;
        [SerializeField] private TextMeshProUGUI kanjiLabel;
        [SerializeField] private TextMeshProUGUI meaningLabel;
        [SerializeField] private Button likeButton;
        [SerializeField] private Image likeIcon;
        [SerializeField] private Button favoriteButton;
        [SerializeField] private Image favoriteIcon;

        [Header("Sprite State Like & Favorite")]
        [SerializeField] private Sprite likeOffSprite;
        [SerializeField] private Sprite likeOnSprite;
        [SerializeField] private Sprite favoriteOffSprite;
        [SerializeField] private Sprite favoriteOnSprite;

        [Header("Elemen Belakang (Back - Placeholder Kalimat)")]
        [SerializeField] private TextMeshProUGUI backKanjiLabel;
        [SerializeField] private TextMeshProUGUI backSentenceJapanese;
        [SerializeField] private TextMeshProUGUI backSentenceEnglish;

        private PhotoRecord currentRecord;
        private ObjectInfo currentInfo;
        private Action<PhotoRecord, bool> onLikeChanged;
        private Action<PhotoRecord, bool> onFavoriteChanged;

        private bool isFlippedBack = false;
        private bool isFlipping = false;
        private Coroutine flipCoroutine;

        private static readonly Color[] TapeColors = new Color[]
        {
            new Color(1f, 0.72f, 0.78f, 0.9f), // Pink
            new Color(0.7f, 0.9f, 0.95f, 0.9f), // Cyan
            new Color(1f, 0.92f, 0.65f, 0.9f), // Yellow
            new Color(0.85f, 0.75f, 0.95f, 0.9f), // Lavender
            new Color(0.72f, 0.94f, 0.8f, 0.9f)  // Mint
        };

        private void Awake()
        {
            var frontBtn = frontClickButton != null ? frontClickButton : cardClickButton;
            if (frontBtn != null)
                frontBtn.onClick.AddListener(OnCardClicked);

            if (backClickButton != null)
                backClickButton.onClick.AddListener(OnCardClicked);

            if (likeButton != null)
                likeButton.onClick.AddListener(OnLikeClicked);

            if (favoriteButton != null)
                favoriteButton.onClick.AddListener(OnFavoriteClicked);

            // Pastikan sisi belakang menghadap ke belakang (180 derajat)
            // sehingga saat container berputar 180 derajat, tulisan belakang terbaca normal (tidak mirror)
            if (backFace != null)
            {
                backFace.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            if (cardCanvasGroup == null)
            {
                if (cardContainer != null)
                    cardCanvasGroup = cardContainer.GetComponent<CanvasGroup>();
                if (cardCanvasGroup == null)
                    cardCanvasGroup = GetComponent<CanvasGroup>();
                if (cardCanvasGroup == null)
                {
                    if (cardContainer != null)
                        cardCanvasGroup = cardContainer.gameObject.AddComponent<CanvasGroup>();
                    else
                        cardCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            ResetFlip(instant: true);
            ResetVisualState();
        }

        /// <summary>
        /// Mengatur alpha dan scale kartu untuk animasi domino transisi halaman.
        /// </summary>
        public void SetVisualState(float alpha, float scale)
        {
            if (cardCanvasGroup != null)
                cardCanvasGroup.alpha = alpha;
            if (cardContainer != null)
                cardContainer.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// Mengembalikan visual kartu ke kondisi default (alpha 1.0, scale 1.0).
        /// </summary>
        public void ResetVisualState()
        {
            if (cardCanvasGroup != null)
                cardCanvasGroup.alpha = 1f;
            if (cardContainer != null)
                cardContainer.localScale = Vector3.one;
        }

        /// <summary>
        /// Mengisi slot dengan data foto, thumbnail, dan info kosakata.
        /// </summary>
        public void Bind(
            PhotoRecord record, 
            Texture2D texture, 
            ObjectInfo info, 
            int slotIndex,
            Action<PhotoRecord, bool> onLikeCallback, 
            Action<PhotoRecord, bool> onFavoriteCallback)
        {
            currentRecord = record;
            currentInfo = info;
            onLikeChanged = onLikeCallback;
            onFavoriteChanged = onFavoriteCallback;

            gameObject.SetActive(true);
            ResetFlip(instant: true);

            // 1. Variasi warna washi tape berdasarkan indeks slot
            if (tapeImage != null)
            {
                tapeImage.color = TapeColors[slotIndex % TapeColors.Length];
            }

            // 2. Foto Snapshot (Center-crop UV agar rasio foto tidak pernah gepeng)
            if (photoImage != null)
            {
                photoImage.texture = texture;
                photoImage.gameObject.SetActive(texture != null);
                if (texture != null)
                {
                    ApplyAspectCropUV(photoImage, texture);
                }
            }

            // 3. Teks Kosakata Depan (Max 2 baris rapi: Kanji di atas, Furigana di bawah bila panjang)
            if (info != null && info.vocabulary != null)
            {
                var v = info.vocabulary;
                if (kanjiLabel != null)
                {
                    kanjiLabel.fontSize = 19f;
                    kanjiLabel.enableAutoSizing = true;
                    kanjiLabel.fontSizeMin = 12f;
                    kanjiLabel.fontSizeMax = 19f;
                    kanjiLabel.lineSpacing = -6f;
                    kanjiLabel.textWrappingMode = TextWrappingModes.NoWrap;
                    kanjiLabel.overflowMode = TextOverflowModes.Ellipsis;
                    kanjiLabel.alignment = TextAlignmentOptions.TopLeft;
                    if (string.IsNullOrEmpty(v.kana))
                    {
                        kanjiLabel.text = $"<b>{v.japaneseWord}</b>";
                    }
                    else
                    {
                        int totalLen = (v.japaneseWord?.Length ?? 0) + v.kana.Length;
                        kanjiLabel.text = totalLen > 6
                            ? $"<b>{v.japaneseWord}</b>\n<size=72%><color=#5B5056>({v.kana})</color></size>"
                            : $"<b>{v.japaneseWord}</b> <size=74%><color=#5B5056>({v.kana})</color></size>";
                    }
                }

                if (meaningLabel != null)
                {
                    meaningLabel.fontSize = 14f;
                    meaningLabel.enableAutoSizing = true;
                    meaningLabel.fontSizeMin = 10.5f;
                    meaningLabel.fontSizeMax = 14f;
                    meaningLabel.lineSpacing = -4f;
                    meaningLabel.textWrappingMode = TextWrappingModes.Normal;
                    meaningLabel.overflowMode = TextOverflowModes.Ellipsis;
                    meaningLabel.alignment = TextAlignmentOptions.TopLeft;
                    string romajiText = string.IsNullOrEmpty(v.romaji) ? "" : v.romaji;
                    string englishText = string.IsNullOrEmpty(v.translation) ? (info.objectName ?? "") : v.translation;
                    meaningLabel.text = $"{romajiText} <color=#94A3B8>•</color> {englishText}";
                }

                // Sisi Belakang (Contoh Kalimat)
                if (backKanjiLabel != null)
                {
                    backKanjiLabel.fontSize = 23f;
                    backKanjiLabel.enableAutoSizing = true;
                    backKanjiLabel.fontSizeMin = 15f;
                    backKanjiLabel.fontSizeMax = 23f;
                    backKanjiLabel.textWrappingMode = TextWrappingModes.NoWrap;
                    backKanjiLabel.overflowMode = TextOverflowModes.Ellipsis;
                    backKanjiLabel.alignment = TextAlignmentOptions.Center;
                    backKanjiLabel.text = $"<b>{v.japaneseWord}</b>";
                }

                if (backSentenceJapanese != null)
                {
                    backSentenceJapanese.fontSize = 15.5f;
                    backSentenceJapanese.enableAutoSizing = true;
                    backSentenceJapanese.fontSizeMin = 10.5f;
                    backSentenceJapanese.fontSizeMax = 15.5f;
                    backSentenceJapanese.lineSpacing = -2f;
                    backSentenceJapanese.textWrappingMode = TextWrappingModes.Normal;
                    backSentenceJapanese.overflowMode = TextOverflowModes.Ellipsis;
                    backSentenceJapanese.alignment = TextAlignmentOptions.Center;
                    backSentenceJapanese.text = string.IsNullOrEmpty(v.exampleSentence) ? "「例文準備中」" : v.exampleSentence;
                }

                if (backSentenceEnglish != null)
                {
                    backSentenceEnglish.fontSize = 13.5f;
                    backSentenceEnglish.enableAutoSizing = true;
                    backSentenceEnglish.fontSizeMin = 9.5f;
                    backSentenceEnglish.fontSizeMax = 13.5f;
                    backSentenceEnglish.lineSpacing = -2f;
                    backSentenceEnglish.textWrappingMode = TextWrappingModes.Normal;
                    backSentenceEnglish.overflowMode = TextOverflowModes.Ellipsis;
                    backSentenceEnglish.alignment = TextAlignmentOptions.Center;
                    string english = string.IsNullOrEmpty(v.translation) ? "" : $"Meaning: {v.translation}";
                    backSentenceEnglish.text = english;
                }

                if (backSentenceJapanese != null && backSentenceJapanese.transform.parent is RectTransform sentRt)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(sentRt);
                }
            }
            else
            {
                if (kanjiLabel != null) kanjiLabel.text = record != null ? record.objectName : "—";
                if (meaningLabel != null) meaningLabel.text = "Photo";
            }

            // 4. Update status tombol Like & Favorite
            UpdateLikeVisual(record != null && record.isLiked);
            UpdateFavoriteVisual(record != null && record.isFavorite);
        }

        /// <summary>
        /// Mengosongkan slot jika halaman tidak memiliki cukup foto.
        /// </summary>
        public void SetEmpty()
        {
            currentRecord = null;
            currentInfo = null;
            gameObject.SetActive(false);
        }

        private void OnLikeClicked()
        {
            if (currentRecord == null) return;
            bool newLiked = !currentRecord.isLiked;
            currentRecord.isLiked = newLiked;
            UpdateLikeVisual(newLiked);
            onLikeChanged?.Invoke(currentRecord, newLiked);
        }

        private void OnFavoriteClicked()
        {
            if (currentRecord == null) return;
            bool newFav = !currentRecord.isFavorite;
            currentRecord.isFavorite = newFav;
            UpdateFavoriteVisual(newFav);
            onFavoriteChanged?.Invoke(currentRecord, newFav);
        }

        private void UpdateLikeVisual(bool isLiked)
        {
            if (likeIcon != null)
            {
                likeIcon.sprite = isLiked ? likeOnSprite : likeOffSprite;
            }
        }

        private void UpdateFavoriteVisual(bool isFav)
        {
            if (favoriteIcon != null)
            {
                favoriteIcon.sprite = isFav ? favoriteOnSprite : favoriteOffSprite;
            }
        }

        public static void ApplyAspectCropUV(RawImage rawImg, Texture tex)
        {
            if (rawImg == null || tex == null || tex.width <= 0 || tex.height <= 0)
            {
                if (rawImg != null) rawImg.uvRect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            Rect r = rawImg.rectTransform.rect;
            float targetAspect = (r.width > 1f && r.height > 1f) ? (r.width / r.height) : (244f / 174f);
            float texAspect = (float)tex.width / tex.height;

            if (Mathf.Approximately(texAspect, targetAspect) || Mathf.Abs(texAspect - targetAspect) < 0.01f)
            {
                rawImg.uvRect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            if (texAspect > targetAspect)
            {
                float w = targetAspect / texAspect;
                rawImg.uvRect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            else
            {
                float h = texAspect / targetAspect;
                rawImg.uvRect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
            }
        }

        private void OnCardClicked()
        {
            // Abaikan klik HANYA selama animasi berputar sedang berlangsung
            if (isFlipping) return;

            if (flipCoroutine != null) StopCoroutine(flipCoroutine);
            flipCoroutine = StartCoroutine(FlipRoutine(!isFlippedBack));
        }

        public void ResetFlip(bool instant = false)
        {
            if (flipCoroutine != null)
            {
                StopCoroutine(flipCoroutine);
                flipCoroutine = null;
            }

            isFlipping = false;
            isFlippedBack = false;
            if (cardContainer != null)
            {
                cardContainer.localRotation = Quaternion.identity;
                cardContainer.localScale = Vector3.one;
            }

            if (frontFace != null) frontFace.SetActive(true);
            if (backFace != null) backFace.SetActive(false);
        }

        private IEnumerator FlipRoutine(bool targetFlipped)
        {
            if (cardContainer == null) yield break;

            isFlipping = true; // Kunci klik HANYA untuk jeda durasi animasi

            float halfDur = flipDuration * 0.5f;
            float elapsed = 0f;

            float startY = isFlippedBack ? 180f : 0f;
            float midY = 90f;
            float endY = targetFlipped ? 180f : 0f;

            // Tahap 1: Putar dari rotasi saat ini ke 90 derajat (kartu sedikit terangkat & mengecil ke 0.92x)
            while (elapsed < halfDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDur);
                float ease = t * t;
                float y = Mathf.Lerp(startY, midY, ease);
                cardContainer.localRotation = Quaternion.Euler(0f, y, 0f);

                float scale = Mathf.Lerp(1f, 0.92f, ease);
                cardContainer.localScale = new Vector3(scale, scale, 1f);

                yield return null;
            }

            // Ganti wajah kartu tepat di sudut 90 derajat
            isFlippedBack = targetFlipped;
            if (frontFace != null) frontFace.SetActive(!isFlippedBack);
            if (backFace != null) backFace.SetActive(isFlippedBack);
            if (isFlippedBack && backSentenceJapanese != null && backSentenceJapanese.transform.parent is RectTransform sentRt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(sentRt);
            }

            // Tahap 2: Lanjutkan dari 90 derajat ke endY dengan overshoot spring bounce saat mendarat
            elapsed = 0f;
            while (elapsed < halfDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDur);
                float ease = 1f - (1f - t) * (1f - t);
                float y = Mathf.Lerp(midY, endY, ease);
                cardContainer.localRotation = Quaternion.Euler(0f, y, 0f);

                // Spring bounce: mengembang dari 0.92x -> membal empuk 1.05x -> mendarat di 1.0x
                float bounce = Mathf.Sin(t * Mathf.PI) * 0.08f;
                float baseScale = Mathf.Lerp(0.92f, 1f, ease);
                float currentScale = baseScale + bounce;
                cardContainer.localScale = new Vector3(currentScale, currentScale, 1f);

                yield return null;
            }

            cardContainer.localRotation = Quaternion.Euler(0f, endY, 0f);
            cardContainer.localScale = Vector3.one;
            flipCoroutine = null;
            isFlipping = false; // Langsung bisa diklik lagi tanpa jeda!
        }
    }
}
