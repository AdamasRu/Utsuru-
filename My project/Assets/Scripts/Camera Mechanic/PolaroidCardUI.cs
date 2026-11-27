using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.Capture
{
    /// <summary>
    /// Pop-up kartu Polaroid (DPPL UI-04). Pasang skrip ini di objek yang SELALU aktif
    /// (mis. root Canvas), dan isi panelRoot dengan panel pop-up yang di-nonaktifkan.
    /// </summary>
    public class PolaroidCardUI : MonoBehaviour
    {
        [Header("Referensi sistem")]
        [SerializeField] private CameraCaptureSystem captureSystem;
        [SerializeField] private CameraModeController cameraMode;
        [SerializeField] private DictionaryManager dictionary;
        [SerializeField] private CameraHUD hud;

        [Header("UI-04")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RawImage photoImage;
        [SerializeField] private TMP_Text kanjiLabel;
        [SerializeField] private TMP_Text romajiLabel;
        [SerializeField] private TMP_Text meaningLabel;
        [SerializeField] private TMP_Text exampleLabel;
        [SerializeField] private Toggle exampleToggle;
        [SerializeField] private Button audioButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private AudioSource audioSource;

        [Header("Audio UI (Vocabulary & Example Sentence)")]
        [Tooltip("Tombol audio baru di samping Kanji (道) untuk pelafalan kosakata bahasa Jepang (dari audioPath/audioClip).")]
        [SerializeField] private Button vocabAudioButton;
        [Tooltip("Tombol audio yang sudah ada di bawah Example Sentence untuk contoh kalimat (dari sentenceAudioPath/sentenceAudioClip).")]
        [SerializeField] private Button sentenceAudioButton;
        [Tooltip("Sprite tombol play bulat biru untuk audio kosakata.")]
        [SerializeField] private Sprite vocabPlayCircleSprite;

        [Header("Banner & Badges")]
        [SerializeField] private Image bannerImage;
        [SerializeField] private Sprite bannerNewKotobaSprite;
        [SerializeField] private Sprite bannerAlreadyDiscoveredSprite;
        [SerializeField] private TMP_Text dateStampLabel;
        [SerializeField] private TMP_Text categoryLabel;
        [SerializeField] private TMP_Text pointBadgeLabel;
        [SerializeField] private GameObject pointBadgeObject;

        [Header("Save & Retake Buttons")]
        [SerializeField] private Image saveButtonImage;
        [SerializeField] private Sprite saveButtonWithPointsSprite;
        [SerializeField] private Sprite saveButtonWithoutPointsSprite;
        [SerializeField] private Button retakeButton;

        [Header("Example Sentence")]
        [SerializeField] private TMP_Text exampleJapaneseLabel;
        [SerializeField] private TMP_Text exampleTranslationLabel;

        [Header("Kemudahan Pengguna (UX)")]
        [Tooltip("Label status perolehan kata (BARU / KAJI ULANG). Opsional.")]
        [SerializeField] private TMP_Text statusBadgeLabel;
        [Tooltip("Tombol/area latar belakang gelap untuk menutup kartu saat diklik di luar. Opsional.")]
        [SerializeField] private Button backdropCloseButton;

        [Header("Pesan (DPPL UI-04)")]
        [SerializeField] private string audioErrorMessage = "Audio kosakata tidak tersedia";
        [SerializeField] private string sentenceAudioErrorMessage = "Audio contoh kalimat tidak tersedia";
        [SerializeField] private string savedMessage = "Foto berhasil disimpan ke album Dictionary!";

        /// <summary>Dipicu saat BtnSimpan ditekan, setelah foto masuk ke Dictionary.</summary>
        public event Action<PhotoRecord> OnPhotoSaved;

        private PhotoCaptureResult current;
        private bool testMode;

        private void Awake()
        {
            if (captureSystem == null) captureSystem = FindFirstObjectByType<CameraCaptureSystem>();
            if (cameraMode == null) cameraMode = FindFirstObjectByType<CameraModeController>();
            if (dictionary == null) dictionary = FindFirstObjectByType<DictionaryManager>();
            if (hud == null) hud = FindFirstObjectByType<CameraHUD>();

            if (panelRoot != null) panelRoot.SetActive(false);

            EnsureAudioSource();
            EnsureVocabAudioButton();

            // Hubungkan tombol audio kosakata baru di samping Kanji
            if (vocabAudioButton != null)
            {
                vocabAudioButton.onClick.RemoveListener(PlayVocabAudio);
                vocabAudioButton.onClick.AddListener(PlayVocabAudio);
            }

            // Balikin tombol audio yang sudah ada di bawah Example Sentence agar memutar audio Example Sentence
            var sBtn = sentenceAudioButton != null ? sentenceAudioButton : audioButton;
            if (sBtn != null)
            {
                sBtn.onClick.RemoveListener(PlaySentenceAudio);
                sBtn.onClick.AddListener(PlaySentenceAudio);
            }

            if (saveButton != null) saveButton.onClick.AddListener(Save);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (retakeButton != null) retakeButton.onClick.AddListener(Close);
            if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(Close);
            if (exampleToggle != null)
                exampleToggle.onValueChanged.AddListener(on =>
                {
                    if (exampleLabel != null) exampleLabel.gameObject.SetActive(on);
                });
        }

        private void Update()
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                    {
                        Save();
                    }
                    else if (kb.escapeKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame)
                    {
                        Close();
                    }
                }
#else
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    Save();
                }
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.C))
                {
                    Close();
                }
#endif
            }
        }

        private void OnEnable()
        {
            if (captureSystem == null) captureSystem = FindFirstObjectByType<CameraCaptureSystem>();
            if (captureSystem != null) captureSystem.OnPhotoCaptured += Show;
        }

        private void OnDisable()
        {
            if (captureSystem != null) captureSystem.OnPhotoCaptured -= Show;
        }

        public void Show(PhotoCaptureResult result)
        {
            current = result;
            VocabularyEntry vocab = result.Vocabulary;

            if (photoImage != null) photoImage.texture = result.Snapshot;

            if (dateStampLabel != null)
            {
                dateStampLabel.text = result.TakenAt.ToString("yy MM dd");
            }

            if (categoryLabel != null)
            {
                categoryLabel.text = FormatCategory(vocab != null ? vocab.category : "");
            }

            if (kanjiLabel != null)
            {
                if (vocab != null)
                {
                    kanjiLabel.text = string.IsNullOrEmpty(vocab.kana) 
                        ? vocab.japaneseWord 
                        : $"{vocab.japaneseWord} <size=70%><color=#8C7063>({vocab.kana})</color></size>";
                }
                else kanjiLabel.text = "-";

                UpdateVocabAudioButtonPosition();
            }

            if (romajiLabel != null)
            {
                if (vocab != null)
                {
                    romajiLabel.text = $"<color=#2563EB><b>{vocab.romaji}</b></color> <color=#94A3B8>•</color> <color=#475569>{vocab.translation}</color>";
                }
                else romajiLabel.text = "-";
            }

            if (vocab != null && !string.IsNullOrEmpty(vocab.exampleSentence))
            {
                string[] lines = vocab.exampleSentence.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (exampleJapaneseLabel != null)
                {
                    exampleJapaneseLabel.text = lines.Length > 0 ? lines[0] : "";
                }
                if (exampleTranslationLabel != null)
                {
                    exampleTranslationLabel.text = lines.Length > 1 ? string.Join(" ", lines, 1, lines.Length - 1) : "";
                }
                if (exampleLabel != null)
                {
                    exampleLabel.text = vocab.exampleSentence;
                }
            }

            // New Discovery vs Already Discovered
            if (result.IsNewDiscovery)
            {
                if (bannerImage != null && bannerNewKotobaSprite != null)
                {
                    bannerImage.sprite = bannerNewKotobaSprite;
                    bannerImage.gameObject.SetActive(true);
                }
                if (saveButtonImage != null && saveButtonWithPointsSprite != null)
                {
                    saveButtonImage.sprite = saveButtonWithPointsSprite;
                }
                if (pointBadgeObject != null)
                {
                    pointBadgeObject.SetActive(true);
                }
                if (pointBadgeLabel != null)
                {
                    pointBadgeLabel.text = "+15 Points";
                }
            }
            else
            {
                if (bannerImage != null)
                {
                    if (bannerAlreadyDiscoveredSprite != null)
                        bannerImage.sprite = bannerAlreadyDiscoveredSprite;
                    else if (bannerNewKotobaSprite != null)
                        bannerImage.sprite = bannerNewKotobaSprite; // Temporary fallback per user choice
                    bannerImage.gameObject.SetActive(true);
                }
                if (saveButtonImage != null && saveButtonWithoutPointsSprite != null)
                {
                    saveButtonImage.sprite = saveButtonWithoutPointsSprite;
                }
                if (pointBadgeObject != null)
                {
                    pointBadgeObject.SetActive(false);
                }
            }

            if (captureSystem != null) captureSystem.InputBlocked = true;
            if (cameraMode != null)
            {
                cameraMode.SetControlsLocked(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Periksa ketersediaan audio dan atur status interactable tombol secara terpisah
            UpdateAudioButtonsState(vocab);

            if (panelRoot != null) panelRoot.SetActive(true);
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D UI Sound
        }

        /// <summary>
        /// Memastikan tombol audio Vocabulary di samping Kanji (道) tersedia berupa ikon tombol play bulat biru
        /// sesuai referensi gambar tanpa tulisan atau label di bawah/samping tombol.
        /// </summary>
        public void EnsureVocabAudioButton()
        {
            Transform cardTransform = null;
            if (kanjiLabel != null) cardTransform = kanjiLabel.transform.parent;
            else if (panelRoot != null) cardTransform = panelRoot.transform.Find("Card");

            if (cardTransform == null) return;

            Transform existing = cardTransform.Find("BtnVocabAudio");
            GameObject vocabBtnGo;
            if (existing != null)
            {
                vocabBtnGo = existing.gameObject;
            }
            else
            {
                vocabBtnGo = new GameObject("BtnVocabAudio", typeof(RectTransform));
                vocabBtnGo.transform.SetParent(cardTransform, false);
                vocabBtnGo.layer = cardTransform.gameObject.layer;
            }

            // Pastikan tidak ada label/teks anak di tombol (cukup berupa ikon tombol play bulat biru)
            for (int i = vocabBtnGo.transform.childCount - 1; i >= 0; i--)
            {
                var child = vocabBtnGo.transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            RectTransform rt = vocabBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(34f, 34f);

            var img = vocabBtnGo.GetComponent<Image>() ?? vocabBtnGo.AddComponent<Image>();
            if (vocabPlayCircleSprite == null)
            {
#if UNITY_EDITOR
                vocabPlayCircleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/PlayCircleButton.png");
#endif
            }
            if (vocabPlayCircleSprite != null)
            {
                img.sprite = vocabPlayCircleSprite;
            }
            img.color = Color.white;
            img.preserveAspect = true;

            vocabAudioButton = vocabBtnGo.GetComponent<Button>() ?? vocabBtnGo.AddComponent<Button>();
            if (audioButton != null)
            {
                vocabAudioButton.colors = audioButton.colors;
                vocabAudioButton.transition = audioButton.transition;
            }

            vocabAudioButton.onClick.RemoveListener(PlayVocabAudio);
            vocabAudioButton.onClick.AddListener(PlayVocabAudio);

            UpdateVocabAudioButtonPosition();
        }

        /// <summary>
        /// Menempatkan tombol audio kosakata tepat di samping kanan teks Kanji secara dinamis.
        /// </summary>
        public void UpdateVocabAudioButtonPosition()
        {
            if (vocabAudioButton == null || kanjiLabel == null) return;

            kanjiLabel.ForceMeshUpdate();

            RectTransform kanjiRt = kanjiLabel.rectTransform;
            RectTransform btnRt = vocabAudioButton.GetComponent<RectTransform>();

            float kanjiWidth = kanjiLabel.preferredWidth;
            float spacing = 10f;
            float startX = kanjiRt.anchoredPosition.x;
            float newX = startX + kanjiWidth + spacing;

            // Batasi agar tidak melebihi margin kanan kartu (215f - lebar tombol)
            float maxX = 215f - btnRt.sizeDelta.x;
            if (newX > maxX) newX = maxX;

            btnRt.anchorMin = kanjiRt.anchorMin;
            btnRt.anchorMax = kanjiRt.anchorMax;
            btnRt.pivot = new Vector2(0f, 0.5f);
            float centerY = kanjiRt.anchoredPosition.y - (kanjiRt.sizeDelta.y * 0.5f);
            btnRt.anchoredPosition = new Vector2(newX, centerY);
        }

        /// <summary>
        /// Memperbarui status interactable tombol audio kosakata dan contoh kalimat secara terpisah.
        /// Tombol dinonaktifkan jika file audio terkait tidak tersedia.
        /// </summary>
        public void UpdateAudioButtonsState(VocabularyEntry vocab)
        {
            // Status tombol kosakata baru di samping Kanji
            AudioClip vocabClip = ResolveVocabAudio(vocab);
            bool hasVocab = vocabClip != null;
            if (vocabAudioButton != null)
            {
                vocabAudioButton.interactable = hasVocab;
            }

            // Status tombol contoh kalimat di bawah Example Sentence
            AudioClip sentenceClip = ResolveSentenceAudio(vocab);
            bool hasSentence = sentenceClip != null;
            var sBtn = sentenceAudioButton != null ? sentenceAudioButton : audioButton;
            if (sBtn != null)
            {
                sBtn.interactable = hasSentence;
            }
        }

        /// <summary>
        /// Memutar audio pelafalan kosakata bahasa Jepang dari audioPath / audioClip secara terpisah.
        /// Tombol baru di samping Kanji (道).
        /// Jika audioPath kosong, null, atau gagal dimuat, tampilkan feedback "Audio kosakata tidak tersedia"
        /// tanpa mengganggu tombol Example Sentence.
        /// </summary>
        public void PlayVocabAudio()
        {
            if (current == null || current.Vocabulary == null)
            {
                ShowAudioFeedback(audioErrorMessage);
                return;
            }

            VocabularyEntry vocab = current.Vocabulary;
            AudioClip clip = ResolveVocabAudio(vocab);

            if (clip == null)
            {
                if (vocabAudioButton != null) vocabAudioButton.interactable = false;

                ShowAudioFeedback(audioErrorMessage);
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(clip);
            Debug.Log($"[PolaroidCardUI] Memutar audio kosakata (samping Kanji): '{vocab.DisplayJapanese}' ({clip.name})");
        }

        /// <summary>
        /// Memutar audio contoh kalimat dari sentenceAudioPath / sentenceAudioClip secara terpisah.
        /// Tombol yang sudah ada di bawah Example Sentence.
        /// </summary>
        public void PlaySentenceAudio()
        {
            if (current == null || current.Vocabulary == null)
            {
                ShowAudioFeedback(sentenceAudioErrorMessage);
                return;
            }

            VocabularyEntry vocab = current.Vocabulary;
            AudioClip clip = ResolveSentenceAudio(vocab);

            if (clip == null)
            {
                var sBtn = sentenceAudioButton != null ? sentenceAudioButton : audioButton;
                if (sBtn != null) sBtn.interactable = false;

                ShowAudioFeedback(sentenceAudioErrorMessage);
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(clip);
            Debug.Log($"[PolaroidCardUI] Memutar audio contoh kalimat (tombol bawah): '{clip.name}'");
        }

        /// <summary>
        /// Mempertahankan fungsi tombol audio bawaan di bawah Example Sentence agar kembali memutar audio Example Sentence seperti semula.
        /// </summary>
        public void PlayAudio()
        {
            PlaySentenceAudio();
        }

        private void ShowAudioFeedback(string message)
        {
            if (hud != null)
            {
                hud.ShowMessage(message);
            }
            else
            {
                Debug.LogWarning($"[PolaroidCardUI] {message}");
            }
        }

        /// <summary>
        /// Memuat audio pelafalan kosakata HANYA dari audioClip atau audioPath data kosakata.
        /// Tidak pernah menggunakan audio contoh kalimat sebagai pengganti.
        /// </summary>
        public static AudioClip ResolveVocabAudio(VocabularyEntry vocab)
        {
            if (vocab == null) return null;
            if (vocab.audioClip != null) return vocab.audioClip;
            if (string.IsNullOrWhiteSpace(vocab.audioPath)) return null;

            return LoadAudioFromResources(vocab.audioPath);
        }

        /// <summary>
        /// Memuat audio contoh kalimat HANYA dari sentenceAudioClip atau sentenceAudioPath.
        /// Tidak pernah menggunakan audio kosakata sebagai pengganti.
        /// </summary>
        public static AudioClip ResolveSentenceAudio(VocabularyEntry vocab)
        {
            if (vocab == null) return null;
            if (vocab.sentenceAudioClip != null) return vocab.sentenceAudioClip;
            if (string.IsNullOrWhiteSpace(vocab.sentenceAudioPath)) return null;

            return LoadAudioFromResources(vocab.sentenceAudioPath);
        }

        private static AudioClip LoadAudioFromResources(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            string cleanPath = path.Trim();
            if (cleanPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                cleanPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                cleanPath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(0, cleanPath.LastIndexOf('.'));
            }

            try
            {
                return Resources.Load<AudioClip>(cleanPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PolaroidCardUI] Gagal memuat audio '{cleanPath}': {ex.Message}");
                return null;
            }
        }

        private static string FormatCategory(string cat)
        {
            if (string.IsNullOrEmpty(cat)) return "PLACES (場所)";
            switch (cat.Trim().ToLowerInvariant())
            {
                case "places":
                case "road":
                    return "PLACES (場所)";
                case "transport":
                    return "TRANSPORT (交通)";
                case "sign":
                    return "SIGNS (看板・標識)";
                case "facility":
                    return "FACILITY (施設)";
                case "infrastructure":
                    return "INFRASTRUCTURE (設備)";
                case "nature":
                    return "NATURE (自然)";
                case "residential":
                    return "RESIDENTIAL (住宅)";
                default:
                    return $"{cat.ToUpperInvariant()} (分類)";
            }
        }

        // Untuk menguji tampilan popup tanpa harus membidik objek.
        // Saat Play: klik ikon (tiga titik) di header komponen ini -> "TES: Tampilkan popup (Audio Tersedia - Bicycle)"
        [ContextMenu("TES: Tampilkan popup (Audio Tersedia - Bicycle)")]
        private void ShowTestCardWithAudio()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PolaroidCardUI] Jalankan tes ini saat Play.", this);
                return;
            }
            if (current != null) return;

            var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                    texture.SetPixel(x, y, new Color(0.2f, 0.6f, x / 255f));
            texture.Apply();

            var info = new ObjectInfo
            {
                objectId = "ICK-01",
                objectName = "Bicycle",
                vocabulary = new VocabularyEntry
                {
                    vocabularyId = 1,
                    japaneseWord = "自転車",
                    kana = "じてんしゃ",
                    romaji = "Jitensha",
                    translation = "Bicycle / Sepeda",
                    exampleSentence = "「毎朝、自転車で学校に行きます。」\n(Maiasa, jitensha de gakkou ni ikimasu) — I go to school by bicycle every morning.",
                    audioPath = "Audio/Bicycle - ICK-01/vocab",
                    sentenceAudioPath = "Audio/Bicycle - ICK-01/sentence",
                    category = "Transport"
                }
            };

            testMode = true;
            Show(new PhotoCaptureResult { Info = info, Snapshot = texture, TakenAt = DateTime.Now });
        }

        [ContextMenu("TES: Tampilkan popup (Tanpa Audio / Fallback)")]
        private void ShowTestCardNoAudio()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PolaroidCardUI] Jalankan tes ini saat Play.", this);
                return;
            }
            if (current != null) return;

            var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                    texture.SetPixel(x, y, new Color(x / 255f, y / 255f, 0.6f));
            texture.Apply();

            var info = new ObjectInfo
            {
                objectId = "ICK-03",
                objectName = "Poster",
                vocabulary = new VocabularyEntry
                {
                    vocabularyId = 3,
                    japaneseWord = "ポスター",
                    kana = "ぽすたー",
                    romaji = "Posutaa",
                    translation = "Poster",
                    exampleSentence = "「壁に赤と青のポスターがあります。」\n(Kabe ni aka to ao no posutaa ga arimasu) — There is a red and blue poster on the wall.",
                    audioPath = "", // Kosong untuk menguji fallback
                    sentenceAudioPath = "",
                    category = "Sign"
                }
            };

            testMode = true;
            Show(new PhotoCaptureResult { Info = info, Snapshot = texture, TakenAt = DateTime.Now });
        }

        // BtnSimpan: insert ke tbl_photo dengan album aktif, tutup pop-up, kembali ke mode kamera.
        private void Save()
        {
            if (current == null) return;

            // Mode tes: tidak ada yang ditulis ke disk maupun Dictionary.
            if (testMode)
            {
                if (hud != null) hud.ShowMessage("Mode tes: tidak ada yang disimpan.");
                Close();
                return;
            }

            if (dictionary == null)
            {
                Debug.LogError("[PolaroidCardUI] DictionaryManager belum diisi.", this);
                return;
            }

            PhotoRecord record = PhotoFileStore.Save(current, dictionary.ActiveAlbumId);
            dictionary.AddPhoto(record);
            OnPhotoSaved?.Invoke(record);

            if (hud != null) hud.ShowMessage(savedMessage);
            Close();
        }

        // BtnTutup: foto dibuang, kosakata tetap Discovered (sudah dicatat saat shutter).
        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

            if (current != null && current.Snapshot != null)
            {
                if (Application.isPlaying) Destroy(current.Snapshot);
                else DestroyImmediate(current.Snapshot);
            }
            current = null;
            testMode = false;

            if (captureSystem != null) captureSystem.InputBlocked = false;
            if (cameraMode != null)
            {
                cameraMode.SetControlsLocked(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
