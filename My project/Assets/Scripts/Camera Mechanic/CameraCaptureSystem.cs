using System;
using UnityEngine;

namespace Utsuru.Capture
{
    /// <summary>Hasil satu kali pemotretan. Snapshot dimiliki penerima event (hapus bila tidak dipakai).</summary>
    public class PhotoCaptureResult
    {
        public InteractableObject Target;
        public ObjectInfo Info;
        public Texture2D Snapshot;
        public DateTime TakenAt;
        public bool IsNewDiscovery = true;

        public VocabularyEntry Vocabulary => Info != null ? Info.vocabulary : null;
    }

    /// <summary>
    /// Menangani shutter (DPPL: CameraCaptureSystem). Klik kiri di mode OpenCamera:
    /// cek target -> cek DictionaryManager -> (objek baru) tambah poin + snapshot -> buka UI-04.
    /// Objek lama tetap membuka kartu polaroid untuk kaji ulang (review) tanpa menambah poin ganda.
    /// </summary>
    public class CameraCaptureSystem : MonoBehaviour
    {
        [Header("Referensi")]
        [SerializeField] private CameraModeController cameraMode;
        [SerializeField] private InteractableDetector detector;
        [SerializeField] private DictionaryManager dictionary;
        [SerializeField] private PointRewardSystem points;
        [SerializeField] private CameraHUD hud;

        [Header("Pengaturan Shutter")]
        [Tooltip("Jika aktif, objek yang sudah pernah difoto tetap membuka kartu Polaroid untuk kaji ulang kosakata (+0 poin).")]
        [SerializeField] private bool allowReviewCaptured = true;

        [Header("Snapshot")]
        [Tooltip("Ukuran sisi foto persegi (piksel).")]
        [SerializeField] private int photoSize = 512;
        [SerializeField] private float cooldown = 0.6f;

        [Header("Audio (opsional)")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip shutterClip;

        [Header("Pesan (DPPL UI-03)")]
        [SerializeField] private string alreadyPhotoMessage = "Objek ini sudah pernah difoto sebelumnya (Kaji Ulang).";
        [Tooltip("{0} = nama kata, {1} = jumlah poin")]
        [SerializeField] private string newWordMessageFormat = "Kosakata baru ditemukan: {0}! +{1} Poin";

        /// <summary>Dipicu setelah foto berhasil diambil untuk objek baru.</summary>
        public event Action<PhotoCaptureResult> OnPhotoCaptured;

        /// <summary>Diisi popup Polaroid agar shutter tidak jalan saat popup terbuka.</summary>
        public bool InputBlocked { get; set; }

        public static CameraCaptureSystem Instance { get; private set; }

        private float nextAllowedTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[CameraCaptureSystem] Terdeteksi duplikat pada GameObject '{gameObject.name}'. Komponen duplikat ini dinonaktifkan.", this);
                enabled = false;
                return;
            }
            Instance = this;

            // Auto-resolve referensi bila belum di-assign di Inspector
            if (cameraMode == null) cameraMode = GetComponent<CameraModeController>() ?? FindFirstObjectByType<CameraModeController>();
            if (detector == null) detector = GetComponent<InteractableDetector>() ?? FindFirstObjectByType<InteractableDetector>();
            if (dictionary == null) dictionary = GetComponent<DictionaryManager>() ?? FindFirstObjectByType<DictionaryManager>();
            if (points == null) points = GetComponent<PointRewardSystem>() ?? FindFirstObjectByType<PointRewardSystem>();
            if (hud == null) hud = GetComponent<CameraHUD>() ?? FindFirstObjectByType<CameraHUD>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Shutter hanya diproses jika mode OpenCamera sedang aktif
            if (cameraMode != null && cameraMode.IsCameraActive && CameraInput.ShutterPressed())
            {
                TryCapture();
            }
        }

        /// <summary>Bisa juga dipanggil dari BtnShutter (UI Button onClick).</summary>
        public void TryCapture()
        {
            if (InputBlocked || cameraMode == null || !cameraMode.IsCameraActive || Time.unscaledTime < nextAllowedTime) return;

            if (dictionary == null)
            {
                Debug.LogError("[CameraCaptureSystem] DictionaryManager belum diisi.", this);
                return;
            }

            var target = detector.RefreshNow();
            ObjectInfo info = detector.CurrentInfo;
            if (target == null || info == null) return;

            if (info.vocabulary == null)
            {
                Debug.LogWarning($"[CameraCaptureSystem] Objek '{info.objectId}' di database belum punya data kosakata.", target);
                return;
            }

            bool isNew = !dictionary.IsObjectDiscovered(target.ObjectId);

            if (!isNew && !allowReviewCaptured)
            {
                if (hud != null) hud.ShowMessage(alreadyPhotoMessage);
                return;
            }

            nextAllowedTime = Time.unscaledTime + cooldown;

            // Outline disembunyikan sebentar supaya tidak ikut masuk ke foto.
            detector.SuppressHighlight(true);
            Texture2D snapshot = TakeSnapshot();
            detector.SuppressHighlight(false);

            int earned = 0;
            if (isNew)
            {
                dictionary.MarkDiscovered(target.ObjectId, info.vocabulary.vocabularyId);
                earned = points != null ? points.PointsPerNewWord : 0;
                if (points != null) points.EarnPoints(earned);
            }

            if (sfxSource != null && shutterClip != null) sfxSource.PlayOneShot(shutterClip);
            if (hud != null)
            {
                hud.PlayFlash();
                if (isNew)
                    hud.ShowMessage(string.Format(newWordMessageFormat, info.vocabulary.japaneseWord, earned));
                else
                    hud.ShowMessage(alreadyPhotoMessage);
            }

            OnPhotoCaptured?.Invoke(new PhotoCaptureResult
            {
                Target = target,
                Info = info,
                Snapshot = snapshot,
                TakenAt = DateTime.Now,
                IsNewDiscovery = isNew
            });
        }

        // Render kamera pemain ke RenderTexture persegi, tanpa UI (overlay tidak ikut ter-render).
        private Texture2D TakeSnapshot()
        {
            Camera cam = (cameraMode != null && cameraMode.PlayerCamera != null) ? cameraMode.PlayerCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogError("[CameraCaptureSystem] Kamera untuk snapshot tidak ditemukan.");
                return new Texture2D(photoSize, photoSize);
            }
            RenderTexture rt = RenderTexture.GetTemporary(photoSize, photoSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = cam.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var texture = new Texture2D(photoSize, photoSize, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, photoSize, photoSize), 0, 0);
            texture.Apply();

            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);

            return texture;
        }
    }
}
