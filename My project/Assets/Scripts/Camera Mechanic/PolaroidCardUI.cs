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
            if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(Close);
            if (exampleToggle != null)
                exampleToggle.onValueChanged.AddListener(on => exampleLabel.gameObject.SetActive(on));
        }

        private void Update()
        {
            // Izinkan menutup kartu pop-up dengan tombol Escape, C, atau Space agar pemain tidak terjebak
            if (panelRoot != null && panelRoot.activeSelf)
            {
#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                {
                    Close();
                }
#else
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Space))
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

        private void Show(PhotoCaptureResult result)
        {
            current = result;
            VocabularyEntry vocab = result.Vocabulary;

            if (photoImage != null) photoImage.texture = result.Snapshot;
            if (kanjiLabel != null) kanjiLabel.text = vocab != null ? vocab.DisplayJapanese : "-";
            if (romajiLabel != null) romajiLabel.text = vocab != null ? vocab.romaji : "-";
            if (meaningLabel != null) meaningLabel.text = vocab != null ? vocab.translation : "-";
            if (exampleLabel != null)
            {
                exampleLabel.text = vocab != null ? vocab.exampleSentence : "";
                exampleLabel.gameObject.SetActive(false);
            }

            // Kalimat contoh opsional lewat toggle: default tersembunyi.
            if (exampleToggle != null)
            {
                exampleToggle.SetIsOnWithoutNotify(false);
            }

            if (statusBadgeLabel != null)
            {
                statusBadgeLabel.text = result.IsNewDiscovery ? "✨ Kosakata Baru Ditemukan! (+10 Poin)" : "📖 Kaji Ulang (Sudah Ada di Album)";
                statusBadgeLabel.color = result.IsNewDiscovery ? new Color(0.2f, 0.85f, 0.4f) : new Color(1f, 0.75f, 0.2f);
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

            AudioClip clip = ResolveAudio(current.Vocabulary);
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
        private void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

            if (current != null && current.Snapshot != null) Destroy(current.Snapshot);
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
