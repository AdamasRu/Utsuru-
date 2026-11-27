using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using StarterAssets;
using UnityEngine.InputSystem;
using Utsuru.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utsuru.Capture
{
    /// <summary>
    /// Mengatur Mode Kamera first-person (DPPL UI-03):
    /// tombol C / BtnCamera menampilkan viewfinder + crosshair dan mem-zoom sedikit.
    /// Mendukung langsung Cinemachine 3 (CinemachineCamera) maupun kamera standar Unity.
    /// </summary>
    public class CameraModeController : MonoBehaviour
    {
        public static CameraModeController Instance { get; private set; }

        [Header("Referensi Kamera")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("CinemachineCamera yang aktif (mis. PlayerFollowCamera). Kosong = dicari otomatis.")]
        [SerializeField] private CinemachineCamera virtualCamera;

        [Header("UI & Kontrol")]
        [Tooltip("Root UI viewfinder: overlay, crosshair, dan BtnShutter (opsional).")]
        [SerializeField] private GameObject viewfinderRoot;

        [Tooltip("Script gerak/look player yang dimatikan saat popup Polaroid tampil.")]
        [SerializeField] private Behaviour[] playerControlBehaviours;

        [Header("Zoom Viewfinder Settings")]
        [Tooltip("FOV minimum saat zoom maksimal (mendekat).")]
        [SerializeField] private float minFov = 15f;
        [Tooltip("FOV maksimum saat zoom keluar (sudut lebar).")]
        [SerializeField] private float maxFov = 45f;
        [Tooltip("FOV default saat awal masuk mode kamera.")]
        [SerializeField] private float defaultFov = 30f;
        [Tooltip("Sensitivitas / step zoom saat mouse scroll diputar.")]
        [SerializeField] private float zoomStep = 2.5f;
        [Tooltip("Kecepatan lerp transisi zoom FOV.")]
        [SerializeField] private float zoomSpeed = 12f;

        [Header("UI Slider Zoom")]
        [Tooltip("UI Slider vertikal untuk zoom kamera.")]
        [SerializeField] private UnityEngine.UI.Slider zoomSlider;

        [Header("UI Hints Luar Mode Kamera")]
        [Tooltip("Hint kamera di HUD yang disembunyikan saat mode kamera aktif.")]
        [SerializeField] private GameObject cameraHintObject;

        [Tooltip("Hint dictionary di HUD yang disembunyikan saat mode kamera aktif.")]
        [SerializeField] private GameObject dictionaryHintObject;

        [Tooltip("Hint achievement di HUD yang disembunyikan saat mode kamera aktif.")]
        [SerializeField] private GameObject achievementHintObject;

        [Tooltip("Hint store di HUD yang disembunyikan saat mode kamera aktif.")]
        [SerializeField] private GameObject storeHintObject;

        [Header("Audio Kamera")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip openCameraClip;
        [SerializeField] private AudioClip closeCameraClip;
        [Range(0f, 1f)]
        [SerializeField] private float cameraSfxVolume = 1f;

        public float MinFov => minFov;
        public float MaxFov => maxFov;
        public float DefaultFov => defaultFov;
        public float CurrentFov => currentCameraFov;

        public bool IsCameraActive { get; private set; }
        public Camera PlayerCamera => playerCamera;
        public CinemachineCamera VirtualCamera => virtualCamera;

        /// <summary>Dipanggil tiap Mode Kamera berubah (true = aktif).</summary>
        public event Action<bool> OnCameraModeChanged;

        private float normalFov = 60f;
        private float currentCameraFov = 30f;
        private bool controlsLocked;
        private bool isUpdatingSliderInternally;
        private bool viewfinderHiddenForOverlay;
        private Coroutine viewfinderFadeCoroutine;
        private const float ViewfinderFadeDuration = 0.16f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[CameraModeController] Terdeteksi duplikat pada GameObject '{gameObject.name}'. Komponen duplikat ini dinonaktifkan.", this);
                enabled = false;
                return;
            }
            Instance = this;

            if (playerCamera == null) playerCamera = Camera.main;
            if (virtualCamera == null) virtualCamera = FindFirstObjectByType<CinemachineCamera>();

            if (virtualCamera != null)
            {
                normalFov = virtualCamera.Lens.FieldOfView;
                Debug.Log($"[CameraModeController] Terhubung ke CinemachineCamera '{virtualCamera.name}'. FOV Normal: {normalFov}", virtualCamera);
            }
            else if (playerCamera != null)
            {
                normalFov = playerCamera.fieldOfView;
                Debug.Log($"[CameraModeController] Siap. Kamera yang dipakai: {playerCamera.name}. FOV Normal: {normalFov}", playerCamera);
            }
            else
            {
                Debug.LogError("[CameraModeController] Kamera player belum diisi dan Camera.main / CinemachineCamera tidak ditemukan.", this);
                enabled = false;
                return;
            }

            if (PlayerPrefs.HasKey("Setting_FOV"))
            {
                normalFov = PlayerPrefs.GetFloat("Setting_FOV", normalFov);
            }

            if (viewfinderRoot == null)
            {
                viewfinderRoot = GameObject.Find("ViewfinderRoot");
            }
            if (viewfinderRoot != null)
            {
                UIPanelAnimator.EnsureCanvasGroup(viewfinderRoot);
                SetViewfinderVisible(false, instant: true);
            }

            if (cameraHintObject == null)
            {
                cameraHintObject = GameObject.Find("CameraHint") ?? GameObject.Find("Camera Mechanic/CameraHint");
            }
            if (dictionaryHintObject == null)
            {
                dictionaryHintObject = GameObject.Find("DictionaryHint") ?? GameObject.Find("Camera Mechanic/DictionaryHint");
            }
            if (achievementHintObject == null)
            {
                achievementHintObject = GameObject.Find("AchievementHint") ?? GameObject.Find("Camera Mechanic/AchievementHint");
            }
            if (storeHintObject == null)
            {
                storeHintObject = GameObject.Find("StoreHint") ?? GameObject.Find("Camera Mechanic/StoreHint");
            }

            // Auto-populate playerControlBehaviours jika belum di-assign di Inspector
            if (playerControlBehaviours == null || playerControlBehaviours.Length == 0)
            {
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("PlayerCapsule");
                if (player != null)
                {
                    var list = new List<Behaviour>();
                    var fpc = player.GetComponent<FirstPersonController>();
                    if (fpc != null) list.Add(fpc);
                    var pi = player.GetComponent<PlayerInput>();
                    if (pi != null) list.Add(pi);
                    if (list.Count > 0) playerControlBehaviours = list.ToArray();
                }
            }
            currentCameraFov = defaultFov;
            if (zoomSlider != null)
            {
                zoomSlider.minValue = 0f;
                zoomSlider.maxValue = 1f;
                zoomSlider.onValueChanged.AddListener(OnSliderValueChanged);
                SyncSliderWithFov(currentCameraFov);
            }

            EnsureAudioReady();
        }

        private void Start()
        {
            // Pastikan saat mulai bermain timeScale normal, kontrol player selalu aktif, gravitasi berjalan, dan kursor terkunci
            Time.timeScale = 1f;
            SetControlsLocked(false);
        }

        private void OnDestroy()
        {
            if (zoomSlider != null)
            {
                zoomSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            bool dialogueBlocking = DialogueManager.Instance != null &&
                                    (DialogueManager.Instance.IsDialogueActive || DialogueManager.Instance.WasClosedThisFrame);

            if (!controlsLocked && !dialogueBlocking && !PolaroidCardUI.WasAnyClosedThisFrame && CameraInput.ToggleCameraPressed())
                SetCameraActive(!IsCameraActive);

            if (IsCameraActive && !controlsLocked && !dialogueBlocking)
            {
                float scroll = CameraInput.GetMouseScrollDelta();
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    // Scroll ke atas (> 0) -> Zoom In -> FOV mengecil
                    // Scroll ke bawah (< 0) -> Zoom Out -> FOV membesar
                    currentCameraFov -= scroll * zoomStep;
                    currentCameraFov = Mathf.Clamp(currentCameraFov, minFov, maxFov);
                    SyncSliderWithFov(currentCameraFov);
                }
            }

            float targetFov = IsCameraActive ? currentCameraFov : normalFov;
            float t = 1f - Mathf.Exp(-zoomSpeed * Time.unscaledDeltaTime);

            if (virtualCamera != null)
            {
                LensSettings lens = virtualCamera.Lens;
                lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, t);
                virtualCamera.Lens = lens;
            }

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, t);
            }
        }

        public void SetZoomSlider(UnityEngine.UI.Slider slider)
        {
            if (zoomSlider != null)
            {
                zoomSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
            zoomSlider = slider;
            if (zoomSlider != null)
            {
                zoomSlider.minValue = 0f;
                zoomSlider.maxValue = 1f;
                zoomSlider.onValueChanged.AddListener(OnSliderValueChanged);
                SyncSliderWithFov(currentCameraFov);
            }
        }

        private void SyncSliderWithFov(float fov)
        {
            if (zoomSlider == null) return;
            isUpdatingSliderInternally = true;
            // 0 = maxFov (zoom keluar/bawah), 1 = minFov (zoom dekat/atas)
            zoomSlider.value = Mathf.InverseLerp(maxFov, minFov, fov);
            isUpdatingSliderInternally = false;
        }

        private void OnSliderValueChanged(float sliderValue)
        {
            if (isUpdatingSliderInternally) return;
            currentCameraFov = Mathf.Lerp(maxFov, minFov, sliderValue);
        }

        /// <summary>Dipakai SettingsManager saat player mengubah FOV agar tidak tertarik kembali oleh loop Update kamera.</summary>
        public void SetNormalFov(float fov)
        {
            normalFov = fov;
            if (IsCameraActive) return;

            if (virtualCamera != null)
            {
                LensSettings lens = virtualCamera.Lens;
                lens.FieldOfView = fov;
                virtualCamera.Lens = lens;
            }

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = fov;
            }
        }

        /// <summary>Bisa dipanggil dari BtnCamera (UI Button onClick).</summary>
        public void SetCameraActive(bool active)
        {
            if (controlsLocked || IsCameraActive == active) return;

            IsCameraActive = active;
            viewfinderHiddenForOverlay = false;
            if (active)
            {
                currentCameraFov = defaultFov;
                SyncSliderWithFov(currentCameraFov);
            }

            Debug.Log($"[CameraModeController] Mode OpenCamera {(active ? "AKTIF" : "MATI")}");
            PlayCameraSfx(active ? openCameraClip : closeCameraClip);
            SetViewfinderVisible(active, instant: false);
            if (cameraHintObject != null) cameraHintObject.SetActive(!active);
            if (dictionaryHintObject != null) dictionaryHintObject.SetActive(!active);
            if (achievementHintObject != null) achievementHintObject.SetActive(!active);
            if (storeHintObject != null) storeHintObject.SetActive(!active);
            OnCameraModeChanged?.Invoke(active);
        }

        /// <summary>
        /// Menyembunyikan / memunculkan kembali ViewfinderRoot secara halus saat overlay (mis. Polaroid) tampil.
        /// Status Mode Kamera tetap terjaga agar zoom FOV dan hint HUD tidak berubah.
        /// </summary>
        public void SetViewfinderOverlayHidden(bool hidden)
        {
            viewfinderHiddenForOverlay = hidden;
            if (!IsCameraActive) return;

            SetViewfinderVisible(!hidden, instant: false);
        }

        private void SetViewfinderVisible(bool visible, bool instant)
        {
            if (viewfinderFadeCoroutine != null)
            {
                StopCoroutine(viewfinderFadeCoroutine);
                viewfinderFadeCoroutine = null;
            }

            if (viewfinderRoot == null) return;

            CanvasGroup cg = UIPanelAnimator.EnsureCanvasGroup(viewfinderRoot);
            if (instant || !gameObject.activeInHierarchy)
            {
                cg.alpha = visible ? 1f : 0f;
                cg.interactable = visible;
                cg.blocksRaycasts = visible;
                viewfinderRoot.SetActive(visible);
                return;
            }

            viewfinderFadeCoroutine = StartCoroutine(FadeViewfinderRoutine(visible));
        }

        private IEnumerator FadeViewfinderRoutine(bool show)
        {
            CanvasGroup cg = UIPanelAnimator.EnsureCanvasGroup(viewfinderRoot);
            float startAlpha = viewfinderRoot.activeSelf ? cg.alpha : 0f;
            float targetAlpha = show ? 1f : 0f;

            if (show)
            {
                if (!viewfinderRoot.activeSelf)
                {
                    cg.alpha = 0f;
                    startAlpha = 0f;
                    viewfinderRoot.SetActive(true);
                }
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            else
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;
                if (!viewfinderRoot.activeSelf)
                {
                    cg.alpha = 0f;
                    viewfinderFadeCoroutine = null;
                    yield break;
                }
            }

            float elapsed = 0f;
            while (elapsed < ViewfinderFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ViewfinderFadeDuration);
                float smooth = t * t * (3f - 2f * t);
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, smooth);
                yield return null;
            }

            cg.alpha = targetAlpha;
            if (!show)
            {
                viewfinderRoot.SetActive(false);
            }

            viewfinderFadeCoroutine = null;
        }

        /// <summary>
        /// Dipakai popup Polaroid, Dictionary, dan Dialog NPC: mengunci kontrol player dan memunculkan kursor,
        /// lalu mengembalikannya saat popup/menu ditutup.
        /// </summary>
        public void SetControlsLocked(bool locked)
        {
            if (!locked && DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                locked = true;
            }

            controlsLocked = locked;

            // 1. Nonaktifkan / aktifkan behaviour kontrol player (FirstPersonController, PlayerInput)
            if (playerControlBehaviours != null && playerControlBehaviours.Length > 0)
            {
                foreach (var behaviour in playerControlBehaviours)
                {
                    if (behaviour == null) continue;
                    behaviour.enabled = !locked;
                    if (!locked && behaviour is PlayerInput pi)
                    {
                        pi.ActivateInput();
                    }
                }
            }
            else
            {
                // Fallback jika belum diisi
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("PlayerCapsule");
                if (player != null)
                {
                    var fpc = player.GetComponent<FirstPersonController>();
                    if (fpc != null) fpc.enabled = !locked;
                    var pi = player.GetComponent<PlayerInput>();
                    if (pi != null)
                    {
                        pi.enabled = !locked;
                        if (!locked) pi.ActivateInput();
                    }
                }
            }

            // 2. Kunci / buka input look pada StarterAssetsInputs
            var inputs = FindFirstObjectByType<StarterAssetsInputs>();
            if (inputs != null)
            {
                inputs.cursorInputForLook = !locked;
                inputs.cursorLocked = !locked;
                if (locked)
                {
                    inputs.look = Vector2.zero;
                    inputs.move = Vector2.zero;
                }
            }

            // 3. Atur visibilitas kursor
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        private void EnsureAudioReady()
        {
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            }
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.ignoreListenerPause = true;

#if UNITY_EDITOR
            if (openCameraClip == null)
                openCameraClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Camera/open.wav");
            if (closeCameraClip == null)
                closeCameraClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Camera/close.wav");
#endif
        }

        private void PlayCameraSfx(AudioClip clip)
        {
            if (clip == null) return;
            EnsureAudioReady();
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, cameraSfxVolume);
            }
        }
    }
}
