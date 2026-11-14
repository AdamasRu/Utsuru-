using System.Collections;
using UnityEngine;
using StarterAssets; // Untuk mengakses FirstPersonController bawaan aset

public enum TimeOfDayState
{
    Day,
    Night
}

public class DayNightManager : MonoBehaviour
{
    [Header("Waktu Siklus")]
    [Tooltip("Durasi siang/malam dalam menit (contoh: 5 untuk 5 menit, atau 0.2 untuk tes cepat)")]
    [SerializeField] private float durationInMinutes = 5f;
    public TimeOfDayState currentState = TimeOfDayState.Day;

    [Header("Referensi UI Cutscene")]
    [SerializeField] private CutsceneTransitionUI cutsceneUI;

    [Header("Referensi Player")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private StarterAssetsInputs playerInputs;

    [Header("Lampu Siang & Malam")]
    [SerializeField] private GameObject lightDayObject;
    [SerializeField] private GameObject lightNightObject;

    [Header("Skybox Materials")]
    [SerializeField] private Material daySkybox;
    [SerializeField] private Material nightSkybox;

    private float timer;
    private bool isTransitioning = false;

    private void Start()
    {
        // Konversi menit ke detik
        timer = durationInMinutes * 60f;

        // Pastikan kondisi visual awal sesuai state (Siang)
        ApplyEnvironment(currentState);
    }

    private void Update()
    {
        // Jangan jalankan timer saat sedang dalam cutscene transisi
        if (isTransitioning) return;

        // PAUSE TIMER: Jangan kurangi waktu atau mulai cutscene jika player sedang ngobrol dengan NPC!
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            StartCoroutine(StartTransitionRoutine());
        }
    }

    private IEnumerator StartTransitionRoutine()
    {
        isTransitioning = true;

        // 1. Kunci pergerakan player
        SetPlayerControl(false);

        // Tentukan waktu berikutnya
        bool targetIsNight = (currentState == TimeOfDayState.Day);

        // 2. Jalankan Cutscene Transisi
        yield return StartCoroutine(cutsceneUI.PlayTransitionRoutine(targetIsNight, () =>
        {
            // BAGIAN INI DIEKSEKUSI TEPAT SAAT LAYAR SEDANG HITAM PEKAT
            currentState = targetIsNight ? TimeOfDayState.Night : TimeOfDayState.Day;
            ApplyEnvironment(currentState);

            // Kocok ulang NPC dengan model karakter baru saat layar sedang hitam!
            if (NPCSpawner.Instance != null)
            {
                NPCSpawner.Instance.SpawnRandomNPCs();
            }
        }));

        // 3. Transisi selesai, reset timer dan kembalikan kontrol player
        timer = durationInMinutes * 60f;
        SetPlayerControl(true);
        isTransitioning = false;
    }

    private void ApplyEnvironment(TimeOfDayState state)
    {
        bool isNight = (state == TimeOfDayState.Night);

        // 1. Aktifkan / Matikan GameObject Lampu
        if (lightDayObject != null)
            lightDayObject.SetActive(!isNight);

        if (lightNightObject != null)
            lightNightObject.SetActive(isNight);

        // 2. Ganti Skybox Material
        if (isNight && nightSkybox != null)
        {
            RenderSettings.skybox = nightSkybox;
            RenderSettings.ambientIntensity = 0.83f;
        }
        else if (!isNight && daySkybox != null)
        {
            RenderSettings.skybox = daySkybox;
            RenderSettings.ambientIntensity = 0.93f;
        }

        // 3. Perbarui pencahayaan global URP
        DynamicGI.UpdateEnvironment();
    }

    private void SetPlayerControl(bool isEnabled)
    {
        if (playerController != null)
        {
            playerController.enabled = isEnabled;

            PlayerFootsteps footsteps = playerController.GetComponent<PlayerFootsteps>();
            if (footsteps != null)
                footsteps.enabled = isEnabled;
        }

        // Netralkan input arah agar karakter tidak meluncur sendiri saat kontrol dimatikan
        if (playerInputs != null && !isEnabled)
            playerInputs.MoveInput(Vector2.zero);
    }
}
