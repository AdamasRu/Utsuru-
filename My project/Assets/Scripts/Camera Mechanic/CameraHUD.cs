using System.Collections;
using TMPro;
using UnityEngine;

namespace Utsuru.Capture
{
    /// <summary>
    /// HUD mode OpenCamera: nama objek dekat crosshair, pesan singkat, efek flash,
    /// dan label poin. Tampilan teks bisa disesuaikan dengan art style lewat font/warna TMP.
    /// </summary>
    public class CameraHUD : MonoBehaviour
    {
        [SerializeField] private InteractableDetector detector;
        [SerializeField] private PointRewardSystem points;

        [Header("UI")]
        [Tooltip("Nama objek yang dibidik, diletakkan dekat crosshair.")]
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text pointsLabel;

        [Tooltip("Image putih layar penuh + CanvasGroup (alpha 0, Blocks Raycasts mati).")]
        [SerializeField] private CanvasGroup flashGroup;

        [Header("Pengaturan")]
        [SerializeField] private float flashDuration = 0.25f;
        [SerializeField] private float messageDuration = 2f;

        private Coroutine messageRoutine;
        private Coroutine flashRoutine;

        private void Awake()
        {
            if (detector == null) detector = FindFirstObjectByType<InteractableDetector>();
            if (points == null) points = FindFirstObjectByType<PointRewardSystem>();

            if (hintLabel != null) hintLabel.gameObject.SetActive(false);
            if (messageLabel != null) messageLabel.gameObject.SetActive(false);
            if (flashGroup != null) flashGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            if (detector != null) detector.OnTargetChanged += HandleTargetChanged;
            if (points != null) points.OnPointsChanged += UpdatePoints;
        }

        private void Start()
        {
            if (points != null) UpdatePoints(points.Points);
        }

        private void OnDisable()
        {
            if (detector != null) detector.OnTargetChanged -= HandleTargetChanged;
            if (points != null) points.OnPointsChanged -= UpdatePoints;
        }

        private void HandleTargetChanged(InteractableObject target)
        {
            if (hintLabel == null) return;

            hintLabel.gameObject.SetActive(target != null);
            if (target != null && detector.CurrentInfo != null) hintLabel.text = detector.CurrentInfo.objectName;
        }

        private void UpdatePoints(int total)
        {
            if (pointsLabel != null) pointsLabel.text = total.ToString();
        }

        public void ShowMessage(string message)
        {
            if (messageLabel == null) return;

            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(message));
        }

        public void PlayFlash()
        {
            if (flashGroup == null) return;

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator MessageRoutine(string message)
        {
            messageLabel.text = message;
            messageLabel.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(messageDuration);
            messageLabel.gameObject.SetActive(false);
        }

        private IEnumerator FlashRoutine()
        {
            flashGroup.alpha = 1f;
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                flashGroup.alpha = 1f - Mathf.Clamp01(elapsed / flashDuration);
                yield return null;
            }
            flashGroup.alpha = 0f;
        }
    }
}
