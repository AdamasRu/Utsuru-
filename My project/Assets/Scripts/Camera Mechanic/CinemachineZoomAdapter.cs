using Unity.Cinemachine;
using UnityEngine;

namespace Utsuru.Capture
{
    /// <summary>
    /// OPSIONAL. Untuk proyek yang memakai Cinemachine 3 (CinemachineBrain di kamera utama):
    /// zoom viewfinder dilakukan lewat Lens.FieldOfView milik CinemachineCamera, karena Brain
    /// menimpa FOV kamera biasa tiap frame. Bila error merah menyebut Cinemachine, hapus file ini.
    /// </summary>
    public class CinemachineZoomAdapter : MonoBehaviour
    {
        [SerializeField] private CameraModeController cameraMode;

        [Tooltip("CinemachineCamera yang aktif, mis. PlayerFollowCamera. Kosong = dicari otomatis.")]
        [SerializeField] private CinemachineCamera followCamera;

        [SerializeField] private float viewfinderFov = 25f;
        [SerializeField] private float zoomSpeed = 10f;

        private float normalFov;
        private bool zoomed;

        private void Awake()
        {
            if (followCamera == null) followCamera = FindFirstObjectByType<CinemachineCamera>();
            if (followCamera == null)
            {
                Debug.LogError("[CinemachineZoomAdapter] CinemachineCamera tidak ditemukan.", this);
                enabled = false;
                return;
            }

            normalFov = followCamera.Lens.FieldOfView;
        }

        private void OnEnable()
        {
            if (cameraMode != null) cameraMode.OnCameraModeChanged += HandleModeChanged;
        }

        private void OnDisable()
        {
            if (cameraMode != null) cameraMode.OnCameraModeChanged -= HandleModeChanged;
        }

        private void HandleModeChanged(bool active)
        {
            zoomed = active;
        }

        private void Update()
        {
            LensSettings lens = followCamera.Lens;
            float target = zoomed ? viewfinderFov : normalFov;

            // Saat tidak zoom dan sudah di FOV normal, jangan menimpa apa pun.
            if (!zoomed && Mathf.Abs(lens.FieldOfView - normalFov) < 0.05f) return;

            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, target, 1f - Mathf.Exp(-zoomSpeed * Time.unscaledDeltaTime));
            followCamera.Lens = lens;
        }
    }
}
