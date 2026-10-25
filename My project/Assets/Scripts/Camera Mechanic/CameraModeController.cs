using System;
using UnityEngine;
using Unity.Cinemachine;

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

        [Header("Zoom viewfinder")]
        [SerializeField] private float viewfinderFov = 30f;
        [SerializeField] private float zoomSpeed = 10f;

        public bool IsCameraActive { get; private set; }
        public Camera PlayerCamera => playerCamera;
        public CinemachineCamera VirtualCamera => virtualCamera;

        /// <summary>Dipanggil tiap Mode Kamera berubah (true = aktif).</summary>
        public event Action<bool> OnCameraModeChanged;

        private float normalFov = 60f;
        private bool controlsLocked;

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

            if (viewfinderRoot == null)
            {
                viewfinderRoot = GameObject.Find("ViewfinderRoot");
            }
            if (viewfinderRoot != null) viewfinderRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!controlsLocked && CameraInput.ToggleCameraPressed())
                SetCameraActive(!IsCameraActive);

            float targetFov = IsCameraActive ? viewfinderFov : normalFov;
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

        /// <summary>Bisa dipanggil dari BtnCamera (UI Button onClick).</summary>
        public void SetCameraActive(bool active)
        {
            if (controlsLocked || IsCameraActive == active) return;

            IsCameraActive = active;
            Debug.Log($"[CameraModeController] Mode OpenCamera {(active ? "AKTIF" : "MATI")}");
            if (viewfinderRoot != null) viewfinderRoot.SetActive(active);
            OnCameraModeChanged?.Invoke(active);
        }

        /// <summary>
        /// Dipakai popup Polaroid: mengunci kontrol player dan memunculkan kursor,
        /// lalu mengembalikannya saat popup ditutup.
        /// </summary>
        public void SetControlsLocked(bool locked)
        {
            controlsLocked = locked;

            if (playerControlBehaviours != null)
            {
                foreach (var behaviour in playerControlBehaviours)
                {
                    if (behaviour != null) behaviour.enabled = !locked;
                }
            }

            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }
    }
}
