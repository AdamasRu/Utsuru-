using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utsuru.Audio
{
    /// <summary>
    /// Singleton AudioManager (DontDestroyOnLoad) untuk mengelola pemutaran BGM secara terpusat.
    /// Memastikan BGM loop, berganti secara otomatis sesuai scene, volume tersinkronisasi dengan PlayerPrefs / SettingsManager,
    /// serta mencegah audio overlap maupun duplikasi instance.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<AudioManager>();
                }
                return instance;
            }
        }

        [Header("BGM Clips")]
        [SerializeField] private AudioClip bgmMainMenu;
        [SerializeField] private AudioClip bgmInGame;

        [Header("Audio Source")]
        [SerializeField] private AudioSource bgmSource;

        private float currentMusicVolume01 = 1.0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject audioMgrGo = new GameObject("AudioManager");
                audioMgrGo.AddComponent<AudioManager>();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.Log($"[AudioManager] Instance duplikat terdeteksi pada '{gameObject.name}'. Menghancurkan objek ini.");
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSource();
            LoadBgmClipsIfNeeded();
            LoadVolumeSettings();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            // Tangani scene awal saat game pertama kali dijalankan
            UpdateBGMForScene(SceneManager.GetActiveScene().name);
        }

        private void EnsureAudioSource()
        {
            if (bgmSource == null)
            {
                bgmSource = GetComponent<AudioSource>();
                if (bgmSource == null)
                {
                    bgmSource = gameObject.AddComponent<AudioSource>();
                }
            }

            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f; // 2D Stereo Background Music
            bgmSource.ignoreListenerPause = true; // Musik tetap mengalun saat jeda (Pause) jika diinginkan
        }

        private void LoadBgmClipsIfNeeded()
        {
            if (bgmMainMenu == null)
            {
#if UNITY_EDITOR
                bgmMainMenu = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/BGM_MainMenu.mp3");
#endif
                if (bgmMainMenu == null)
                {
                    bgmMainMenu = Resources.Load<AudioClip>("Audio/BGM/BGM_MainMenu");
                }
            }

            if (bgmInGame == null)
            {
#if UNITY_EDITOR
                bgmInGame = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/BGM_InGame.mp3");
#endif
                if (bgmInGame == null)
                {
                    bgmInGame = Resources.Load<AudioClip>("Audio/BGM/BGM_InGame");
                }
            }
        }

        private void LoadVolumeSettings()
        {
            float musicVolPref = PlayerPrefs.GetFloat("Setting_MusicVol", 100f);
            SetMusicVolume(musicVolPref / 100f);
        }

        /// <summary>
        /// Mengatur volume BGM dengan input skala 0.0f - 1.0f (dari skala 0-100 di UI).
        /// </summary>
        public void SetMusicVolume(float volume01)
        {
            currentMusicVolume01 = Mathf.Clamp01(volume01);
            if (bgmSource != null)
            {
                bgmSource.volume = currentMusicVolume01;
            }
        }

        /// <summary>
        /// Mengatur volume BGM langsung menggunakan nilai slider (skala 0 - 100).
        /// </summary>
        public void SetMusicVolumeFromSlider(float sliderValue0To100)
        {
            SetMusicVolume(sliderValue0To100 / 100f);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateBGMForScene(scene.name);
        }

        /// <summary>
        /// Memilih dan memutar BGM yang sesuai berdasarkan nama scene.
        /// Tidak me-restart musik jika klip target sama dengan yang sedang diputar (contoh: LoginPage ke HomeScreen).
        /// </summary>
        public void UpdateBGMForScene(string sceneName)
        {
            AudioClip targetClip = null;

            if (sceneName.Equals("LoginPage", StringComparison.OrdinalIgnoreCase) ||
                sceneName.Equals("HomeScreen", StringComparison.OrdinalIgnoreCase))
            {
                targetClip = bgmMainMenu;
            }
            else if (sceneName.Equals("Day_Showcase", StringComparison.OrdinalIgnoreCase))
            {
                targetClip = bgmInGame;
            }

            if (targetClip != null)
            {
                PlayBGM(targetClip);
            }
            else
            {
                Debug.Log($"[AudioManager] Scene '{sceneName}' tidak memiliki pemetaan BGM spesifik.");
            }
        }

        /// <summary>
        /// Memutar klip BGM. Jika klip yang diminta sudah aktif diputar, pemutaran dilanjutkan tanpa terputus.
        /// </summary>
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;

            EnsureAudioSource();

            // Jika musik yang sama sedang diputar, lanjutkan tanpa me-restart
            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.volume = currentMusicVolume01;
            bgmSource.loop = true;
            bgmSource.Play();
            Debug.Log($"[AudioManager] Memutar BGM: {clip.name} (Volume: {currentMusicVolume01 * 100f:0}%)");
        }

        /// <summary>
        /// Menghentikan pemutaran BGM jika diperlukan.
        /// </summary>
        public void StopBGM()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }
        }
    }
}
