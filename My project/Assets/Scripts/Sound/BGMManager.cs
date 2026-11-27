using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utsuru.Sound
{
    /// <summary>
    /// Mengelola pemutaran BGM secara persisten di seluruh scene:
    /// - BGM_MainMenu diputar di scene menu (LoginPage, HomeScreen).
    /// - BGM_InGame diputar di scene gameplay (Day_Showcase, Nigth_Showcase, dll).
    /// Transisi antar BGM menggunakan crossfade halus dan volume dijaga lembut agar tidak terlalu keras.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class BGMManager : MonoBehaviour
    {
        public static BGMManager Instance { get; private set; }

        [Header("Audio Clips BGM")]
        [SerializeField] private AudioClip mainMenuBgm;
        [SerializeField] private AudioClip inGameBgm;
        [SerializeField] private AudioClip inGameNightBgm;

        [Header("Pengaturan Volume & Crossfade")]
        [Tooltip("Volume maksimum BGM saat slider Music Settings di posisi 100% (dijaga lembut).")]
        [Range(0f, 1f)]
        [SerializeField] private float maxBgmVolume = 0.22f;
        [SerializeField] private float crossfadeDuration = 1.0f;

        [Header("Audio Sources (Otomatis dibuat bila kosong)")]
        [SerializeField] private AudioSource primarySource;
        [SerializeField] private AudioSource secondarySource;

        private AudioSource activeSource;
        private AudioClip currentTargetClip;
        private float currentActiveFactor = 1f;
        private bool isNightActive = false;
        private Coroutine fadeCoroutine;

        private const string PrefabResourcePath = "Audio/BGMManager";
        private const string MainMenuClipPath = "Assets/Sounds/BGM_MainMenu.mp3";
        private const string InGameClipPath = "Assets/Sounds/BGM_InGame.mp3";
        private const string InGameNightClipPath = "Assets/Sounds/snoozybeats-warm-breeze-lofi-music-chill-lofi-344259.mp3";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeAtRuntime()
        {
            if (Instance != null) return;

            var existing = FindFirstObjectByType<BGMManager>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Instance = existing;
                DontDestroyOnLoad(existing.gameObject);
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab != null)
            {
                GameObject instanceGo = Instantiate(prefab);
                instanceGo.name = "[Auto_BGMManager]";
                DontDestroyOnLoad(instanceGo);
                return;
            }

            var bgmObj = new GameObject("[Auto_BGMManager]");
            bgmObj.AddComponent<BGMManager>();
            DontDestroyOnLoad(bgmObj);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureSourcesAndClips();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            UpdateBgmForScene(SceneManager.GetActiveScene().name);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateBgmForScene(scene.name);
        }

        public void SetTimeOfDay(bool isNight)
        {
            isNightActive = isNight;
            UpdateBgmForScene(SceneManager.GetActiveScene().name, syncFromDayNightManager: false);
        }

        public void UpdateBgmForScene(string sceneName, bool syncFromDayNightManager = true)
        {
            EnsureSourcesAndClips();

            AudioClip desiredClip;
            if (IsMainMenuScene(sceneName))
            {
                isNightActive = false;
                desiredClip = mainMenuBgm;
            }
            else
            {
                if (syncFromDayNightManager)
                {
                    var dnm = FindFirstObjectByType<DayNightManager>();
                    if (dnm != null)
                    {
                        isNightActive = (dnm.currentState == TimeOfDayState.Night);
                    }
                    else
                    {
                        isNightActive = !string.IsNullOrEmpty(sceneName) &&
                            (sceneName.IndexOf("Night", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             sceneName.IndexOf("Nigth", StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                }

                desiredClip = (isNightActive && inGameNightBgm != null) ? inGameNightBgm : inGameBgm;
            }

            if (desiredClip == null) return;

            if (currentTargetClip == desiredClip && activeSource != null && activeSource.isPlaying)
            {
                RefreshVolume();
                return;
            }

            PlayClipWithCrossfade(desiredClip);
        }

        public void RefreshVolume()
        {
            if (fadeCoroutine != null) return;
            if (activeSource != null)
            {
                activeSource.volume = GetTargetVolume() * currentActiveFactor;
            }
        }

        private void PlayClipWithCrossfade(AudioClip nextClip)
        {
            currentTargetClip = nextClip;

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (activeSource == null || !activeSource.isPlaying || crossfadeDuration <= 0f)
            {
                AudioSource target = activeSource ?? primarySource;
                AudioSource other = target == primarySource ? secondarySource : primarySource;
                if (other != null)
                {
                    other.Stop();
                    other.volume = 0f;
                }

                target.clip = nextClip;
                target.volume = 0f;
                target.Play();
                activeSource = target;
                fadeCoroutine = StartCoroutine(FadeInSingleSourceRoutine(target, Mathf.Min(0.6f, crossfadeDuration)));
                return;
            }

            AudioSource outgoing = activeSource;
            AudioSource incoming = outgoing == primarySource ? secondarySource : primarySource;

            incoming.clip = nextClip;
            incoming.volume = 0f;
            incoming.Play();
            activeSource = incoming;

            fadeCoroutine = StartCoroutine(CrossfadeRoutine(outgoing, incoming, crossfadeDuration));
        }

        private IEnumerator FadeInSingleSourceRoutine(AudioSource target, float duration)
        {
            currentActiveFactor = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                currentActiveFactor = Mathf.SmoothStep(0f, 1f, t);
                target.volume = GetTargetVolume() * currentActiveFactor;
                yield return null;
            }

            currentActiveFactor = 1f;
            target.volume = GetTargetVolume();
            fadeCoroutine = null;
        }

        private IEnumerator CrossfadeRoutine(AudioSource outgoing, AudioSource incoming, float duration)
        {
            float outStartVol = outgoing.volume;
            float elapsed = 0f;
            currentActiveFactor = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float smooth = Mathf.SmoothStep(0f, 1f, t);
                currentActiveFactor = smooth;

                float targetVol = GetTargetVolume();
                outgoing.volume = Mathf.Lerp(outStartVol, 0f, smooth);
                incoming.volume = targetVol * smooth;
                yield return null;
            }

            outgoing.Stop();
            outgoing.volume = 0f;
            currentActiveFactor = 1f;
            incoming.volume = GetTargetVolume();
            fadeCoroutine = null;
        }

        private float GetTargetVolume()
        {
            float settingMusic = PlayerPrefs.GetFloat("Setting_MusicVol", 100f) / 100f;
            return Mathf.Clamp01(settingMusic) * maxBgmVolume;
        }

        private static bool IsMainMenuScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;

            return string.Equals(sceneName, "HomeScreen", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(sceneName, "LoginPage", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(sceneName, "MainMenu", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(sceneName, "Title", StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureSourcesAndClips()
        {
            if (primarySource == null)
            {
                AudioSource[] existing = GetComponents<AudioSource>();
                primarySource = existing.Length > 0 ? existing[0] : gameObject.AddComponent<AudioSource>();
            }
            if (secondarySource == null)
            {
                AudioSource[] existing = GetComponents<AudioSource>();
                secondarySource = existing.Length > 1 ? existing[1] : gameObject.AddComponent<AudioSource>();
            }

            ConfigureBgmSource(primarySource);
            ConfigureBgmSource(secondarySource);

#if UNITY_EDITOR
            if (mainMenuBgm == null)
                mainMenuBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(MainMenuClipPath);
            if (inGameBgm == null)
                inGameBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(InGameClipPath);
            if (inGameNightBgm == null)
                inGameNightBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(InGameNightClipPath);
#endif
        }

        private static void ConfigureBgmSource(AudioSource src)
        {
            if (src == null) return;
            src.playOnAwake = false;
            src.loop = true;
            src.ignoreListenerPause = true;
            src.spatialBlend = 0f;
        }

#if UNITY_EDITOR
        public void SetBgmClipsEditor(AudioClip menuClip, AudioClip gameClip, AudioClip nightClip, AudioSource srcA, AudioSource srcB)
        {
            mainMenuBgm = menuClip;
            inGameBgm = gameClip;
            inGameNightBgm = nightClip;
            primarySource = srcA;
            secondarySource = srcB;
        }
#endif
    }
}

