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
        [SerializeField] private string audioErrorMessage = "File audio tidak tersedia untuk kata ini.";
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

            if (audioButton != null) audioButton.onClick.AddListener(PlayAudio);
            if (saveButton != null) saveButton.onClick.AddListener(Save);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (retakeButton != null) retakeButton.onClick.AddListener(Close);
            if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(Close);
            if (exampleToggle != null)
                exampleToggle.onValueChanged.AddListener(on => exampleLabel.gameObject.SetActive(on));
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

            if (panelRoot != null) panelRoot.SetActive(true);
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
        // Saat Play: klik ikon (tiga titik) di header komponen ini -> "TES: tampilkan popup contoh".
        [ContextMenu("TES: tampilkan popup contoh")]
        private void ShowTestCard()
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
                objectId = "TEST_001",
                objectName = "Contoh",
                vocabulary = new VocabularyEntry
                {
                    vocabularyId = 999,
                    japaneseWord = "猫",
                    kana = "ねこ",
                    romaji = "Neko",
                    translation = "Kucing",
                    exampleSentence = "猫がいます。",
                    category = "Animal"
                }
            };

            testMode = true;
            Show(new PhotoCaptureResult { Info = info, Snapshot = texture, TakenAt = DateTime.Now });
        }

        private void PlayAudio()
        {
            if (current == null) return;

            VocabularyEntry vocab = current.Vocabulary;
            if (vocab == null) return;

            AudioClip clip = null;
            // Jika kalimat contoh sedang aktif/terbuka, dahulukan audio kalimat jika ada
            if (exampleToggle != null && exampleToggle.isOn && (!string.IsNullOrEmpty(vocab.sentenceAudioPath) || vocab.sentenceAudioClip != null))
            {
                clip = vocab.sentenceAudioClip != null ? vocab.sentenceAudioClip : Resources.Load<AudioClip>(vocab.sentenceAudioPath);
            }

            if (clip == null)
            {
                clip = ResolveAudio(vocab);
            }

            if (clip == null || audioSource == null)
            {
                if (hud != null) hud.ShowMessage(audioErrorMessage);
                return;
            }
            audioSource.PlayOneShot(clip);
        }

        // AudioClip langsung bila ada; kalau tidak, muat dari folder Resources lewat audioPath.
        private static AudioClip ResolveAudio(VocabularyEntry vocab)
        {
            if (vocab.audioClip != null) return vocab.audioClip;
            if (string.IsNullOrEmpty(vocab.audioPath)) return null;
            return Resources.Load<AudioClip>(vocab.audioPath);
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
