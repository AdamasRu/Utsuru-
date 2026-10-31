using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utsuru.Capture
{
    /// <summary>
    /// Deteksi objek saat mode OpenCamera aktif. Memakai SphereCast (raycast "tebal")
    /// dari tengah layar sehingga objek tetap terdeteksi walau bidikan tidak presisi.
    /// ID objek yang terdeteksi dicocokkan ke database; hanya objek yang ID-nya ada di
    /// database yang dianggap target (diberi outline dan bisa difoto).
    /// </summary>
    public class InteractableDetector : MonoBehaviour
    {
        [SerializeField] private CameraModeController cameraMode;

        [Tooltip("Database objek/kosakata (InspectorObjectDatabase sementara, atau skrip database asli).")]
        [SerializeField] private ObjectDatabaseProvider database;

        [Header("Deteksi")]
        [Tooltip("Jarak maksimal deteksi (DPPL: 3 unit).")]
        [SerializeField] private float maxDistance = 3f;

        [Tooltip("Radius 'lunak' di sekitar raycast. Makin besar = makin mudah membidik objek.")]
        [SerializeField] private float aimRadius = 0.2f;

        [Tooltip("Tag yang harus dimiliki objek yang bisa difoto.")]
        [SerializeField] private string requiredTag = "Interactable";

        [Tooltip("Keluarkan layer Player dari mask ini bila player punya collider.")]
        [SerializeField] private LayerMask raycastMask = ~0;

        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Highlight")]
        [Tooltip("Material dengan shader Utsuru/OutlineHull.")]
        [SerializeField] private Material outlineMaterial;

        public InteractableObject CurrentTarget { get; private set; }

        /// <summary>Data database untuk target saat ini (null bila tidak ada target).</summary>
        public ObjectInfo CurrentInfo { get; private set; }

        /// <summary>Dipanggil saat target berubah (null = tidak ada target).</summary>
        public event Action<InteractableObject> OnTargetChanged;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[32];
        private readonly Dictionary<Collider, InteractableObject> resolveCache = new Dictionary<Collider, InteractableObject>();
        private readonly HashSet<string> warnedKeys = new HashSet<string>();

        private void Awake()
        {
            if (cameraMode == null) cameraMode = GetComponent<CameraModeController>() ?? FindFirstObjectByType<CameraModeController>();
            if (database == null) database = GetComponent<ObjectDatabaseProvider>() ?? FindFirstObjectByType<ObjectDatabaseProvider>();
        }

        private void Start()
        {
            if (database == null)
                Debug.LogWarning("[InteractableDetector] Database objek belum diisi, detektor akan menggunakan data fallback.", this);
            if (outlineMaterial == null)
                Debug.LogWarning("[InteractableDetector] Outline material belum diisi, highlight tidak akan tampil.", this);
        }

        private void Update()
        {
            RefreshNow();
        }

        /// <summary>Jalankan deteksi sekarang dan kembalikan targetnya.</summary>
        public InteractableObject RefreshNow()
        {
            if (cameraMode != null && cameraMode.IsCameraActive) Detect();
            else SetTarget(null, null);

            return CurrentTarget;
        }

        /// <summary>Sembunyikan/tampilkan outline sementara, mis. saat mengambil snapshot.</summary>
        public void SuppressHighlight(bool suppress)
        {
            if (CurrentTarget != null)
                CurrentTarget.SetHighlighted(!suppress, outlineMaterial);
        }

        private void Detect()
        {
            Camera cam = (cameraMode != null && cameraMode.PlayerCamera != null) ? cameraMode.PlayerCamera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float searchDist = Mathf.Max(maxDistance, 10f); // Pastikan jangkauan cukup dari kamera third-person
            int count = Physics.SphereCastNonAlloc(ray, aimRadius, hitBuffer, searchDist, raycastMask, triggerInteraction);

            InteractableObject best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hitBuffer[i];
                InteractableObject candidate = Resolve(hit.collider);
                if (candidate == null) continue;

                Vector3 targetPos = hit.point != Vector3.zero ? hit.point : candidate.transform.position;
                Vector3 toPoint = targetPos - ray.origin;
                float along = Vector3.Dot(toPoint, ray.direction);
                float offCenter = (toPoint - ray.direction * along).magnitude;
                float score = offCenter + (hit.distance > 0 ? hit.distance : along) * 0.01f;

                if (score >= bestScore) continue;
                if (IsOccluded(ray.origin, targetPos, candidate)) continue;

                best = candidate;
                bestScore = score;
            }

            // Raycast garis lurus tengah layar jika SphereCast belum menangkap
            if (best == null)
            {
                if (Physics.Raycast(ray, out RaycastHit centerHit, searchDist, raycastMask, triggerInteraction))
                {
                    InteractableObject candidate = Resolve(centerHit.collider);
                    if (candidate != null && !IsOccluded(ray.origin, centerHit.point, candidate))
                    {
                        best = candidate;
                    }
                }
            }

            // Pencocokan ke database hanya untuk kandidat terbaik
            ObjectInfo info = null;
            if (best != null && !TryGetInfo(best, out info)) best = null;

            SetTarget(best, info);
        }

        private bool TryGetInfo(InteractableObject candidate, out ObjectInfo info)
        {
            info = null;
            string id = candidate.ObjectId;

            if (string.IsNullOrEmpty(id))
            {
                WarnOnce("#" + candidate.GetInstanceID(), $"Objek '{candidate.name}' belum punya Object Id.", candidate);
                return false;
            }

            // Target yang sama dari frame sebelumnya
            if (candidate == CurrentTarget && CurrentInfo != null)
            {
                info = CurrentInfo;
                return true;
            }

            if (database != null && database.TryGetObject(id, out info) && info != null) return true;

            // Fallback otomatis jika ID belum terdaftar di database
            info = new ObjectInfo
            {
                objectId = id,
                objectName = candidate.name,
                vocabulary = new VocabularyEntry
                {
                    vocabularyId = Mathf.Abs(candidate.GetInstanceID()),
                    japaneseWord = candidate.name,
                    kana = "",
                    romaji = id,
                    translation = candidate.name,
                    exampleSentence = ""
                }
            };
            return true;
        }

        private void WarnOnce(string key, string message, UnityEngine.Object context)
        {
            if (warnedKeys.Add(key)) Debug.LogWarning("[InteractableDetector] " + message, context);
        }

        // Pastikan tidak ada dinding/benda lain yang menghalangi antara kamera dan objek (abaikan pemain sendiri).
        private bool IsOccluded(Vector3 origin, Vector3 point, InteractableObject target)
        {
            Vector3 end = Vector3.Lerp(origin, point, 0.95f);
            Vector3 dir = end - origin;
            float dist = dir.magnitude;
            if (dist <= 0.001f) return false;
            dir.Normalize();

            RaycastHit[] hits = Physics.RaycastAll(origin, dir, dist, raycastMask, triggerInteraction);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit blocker = hits[i];
                if (blocker.collider == null) continue;

                // Abaikan pemain sendiri
                if (blocker.collider.CompareTag("Player") || blocker.collider.transform.root.CompareTag("Player"))
                    continue;

                InteractableObject blockerTarget = Resolve(blocker.collider);
                if (blockerTarget != target)
                    return true;
            }

            return false;
        }

        private InteractableObject Resolve(Collider col)
        {
            if (col == null) return null;
            if (resolveCache.TryGetValue(col, out InteractableObject cached)) return cached;

            InteractableObject result = null;
            var interactable = col.GetComponentInParent<InteractableObject>();
            if (interactable == null)
            {
                interactable = col.GetComponentInParent<DataVocabularyObject>();
            }

            if (interactable != null)
            {
                bool hasVocab = col.GetComponentInParent<DataVocabularyObject>() != null || interactable is DataVocabularyObject;
                bool tagOk = string.IsNullOrEmpty(requiredTag)
                    || col.CompareTag(requiredTag)
                    || interactable.gameObject.CompareTag(requiredTag)
                    || hasVocab;

                if (tagOk) result = interactable;
            }

            resolveCache[col] = result;
            return result;
        }

        private void SetTarget(InteractableObject next, ObjectInfo info)
        {
            if (next == CurrentTarget) return;

            if (CurrentTarget != null) CurrentTarget.SetHighlighted(false, outlineMaterial);
            CurrentTarget = next;
            CurrentInfo = next != null ? info : null;
            if (CurrentTarget != null) CurrentTarget.SetHighlighted(true, outlineMaterial);

            OnTargetChanged?.Invoke(CurrentTarget);
        }

        // Pilih objek ini di Hierarchy untuk melihat garis dan bola deteksi di Scene view.
        private void OnDrawGizmosSelected()
        {
            if (cameraMode == null || cameraMode.PlayerCamera == null) return;

            Transform cam = cameraMode.PlayerCamera.transform;
            Vector3 end = cam.position + cam.forward * maxDistance;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(cam.position, end);
            Gizmos.DrawWireSphere(end, aimRadius);
        }
    }
}
