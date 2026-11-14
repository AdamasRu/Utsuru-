using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Daftar file suara langkah kaki")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip landingClip;

    [Header("Pengaturan Irama Langkah (Distance-Based)")]
    [Tooltip("Jarak langkah saat jalan santai (meter). Standar: 1.8m")]
    [SerializeField] private float walkStepDistance = 1.8f;

    [Tooltip("Jarak langkah saat sprint/lari (meter). Standar: 2.3m")]
    [SerializeField] private float sprintStepDistance = 2.3f;

    [Range(0f, 1f)]
    [SerializeField] private float footstepVolume = 0.35f;

    private CharacterController controller;
    private FirstPersonController playerController;
    private AudioSource audioSource;

    private float accumulatedDistance = 0f;
    private float airTime = 0f;
    private bool wasGrounded = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<FirstPersonController>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D Audio di telinga player

        EnsureFootstepClipsLoaded();
    }

    private void EnsureFootstepClipsLoaded()
    {
#if UNITY_EDITOR
        bool hasClips = footstepClips != null && footstepClips.Length > 0;
        if (!hasClips)
        {
            var loaded = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= 10; i++)
            {
                string path = $"Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_{i:00}.wav";
                AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) loaded.Add(clip);
            }
            if (loaded.Count > 0)
            {
                footstepClips = loaded.ToArray();
                footstepVolume = Mathf.Max(footstepVolume, 0.4f);
            }
        }

        if (landingClip == null)
        {
            landingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Land.wav");
        }
#endif
    }

    private void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
        accumulatedDistance = 0f;
    }

    private void Update()
    {
        // 1. Jangan bunyikan saat kontrol player mati (cutscene atau dialog)
        if (playerController != null && !playerController.enabled)
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
            accumulatedDistance = 0f;
            return;
        }

        // 2. Deteksi Ground yang Kebal Medan Tidak Rata / Tanjakan Trotoar
        // Toleransi raycast 0.35m ke bawah mencegah 'isGrounded' kedip saat melewati sambungan paving/trotoar
        bool isGrounded = controller.isGrounded || Physics.Raycast(transform.position, Vector3.down, 0.35f, ~0, QueryTriggerInteraction.Ignore);

        if (!isGrounded)
        {
            airTime += Time.deltaTime;
            wasGrounded = false;
            return; // Sedang di udara / lompat
        }

        // 3. Suara Mendarat (Hanya jika benar-benar lompat/jatuh > 0.2 detik, bukan undakan kecil trotoar)
        if (!wasGrounded)
        {
            if (airTime > 0.22f && landingClip != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(landingClip, footstepVolume);
            }
            airTime = 0f;
            wasGrounded = true;
            accumulatedDistance = 0f; // Reset agar langkah pertama pas menapak rapi
        }

        // 4. Hitung Kecepatan Bergerak Horizontal
        Vector3 horizontalVel = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float currentSpeed = horizontalVel.magnitude;

        // Player sedang bergerak aktif (> 0.5 m/s)
        if (currentSpeed > 0.5f)
        {
            // Akumulasi jarak tempuh fisik kaki
            accumulatedDistance += currentSpeed * Time.deltaTime;

            // Tentukan jeda jarak langkah sesuai ritme jalan vs sprint
            float targetDistance = (currentSpeed > 4.5f) ? sprintStepDistance : walkStepDistance;

            if (accumulatedDistance >= targetDistance)
            {
                PlayRandomFootstep();
                accumulatedDistance = 0f;
            }
        }
        else
        {
            // Saat berhenti sesaat, tahan akumulasi agar saat jalan lagi langkah pertama tidak delay
            accumulatedDistance = Mathf.Min(accumulatedDistance, walkStepDistance * 0.7f);
        }
    }

    private void PlayRandomFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.pitch = Random.Range(0.92f, 1.08f);
        audioSource.PlayOneShot(clip, footstepVolume);
    }
}
