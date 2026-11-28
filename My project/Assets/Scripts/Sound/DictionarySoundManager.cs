using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Utsuru.Sound
{
    public enum UISoundType
    {
        TabSwitch,
        PageTurn,
        CardFlip,
        BookOpen,
        BookClose,
        Custom
    }

    /// <summary>
    /// Sound Manager mandiri untuk audio Dictionary & Scrapbook (Zero-Impact Architecture):
    /// - 100% Aman: Tidak mengubah satu baris pun kode script UI yang sudah ada.
    /// - 100% Otomatis: Menginisialisasi dirinya sendiri saat Play mode dimulai dan otomatis
    ///   mendaftarkan event suara ke tombol-tombol buku Dictionary di scene.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class DictionarySoundManager : MonoBehaviour
    {
        public static DictionarySoundManager Instance { get; private set; }

        [Header("Audio Source")]
        [SerializeField] private AudioSource audioSource;

        [Header("Volume & Variasi Pitch")]
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private float pitchVariation = 0.05f;

        [Header("Audio Clips Dictionary")]
        [SerializeField] private AudioClip[] openingClips;
        [SerializeField] private AudioClip[] closingClips;
        [SerializeField] private AudioClip pageTurnClip;
        [SerializeField] private AudioClip[] cardFlipClips;
        [SerializeField] private AudioClip tabSwitchClip;

        private const string PrefabResourcePath = "Audio/DictionarySoundManager";

        private Utsuru.DictionaryUI.DictionaryUIController _dictController;
        private readonly HashSet<int> _boundButtonIds = new HashSet<int>();
        private bool _wasOpen = false;
        private bool _justPlayedClose = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeAtRuntime()
        {
            if (Instance != null) return;

            var existing = FindFirstObjectByType<DictionarySoundManager>(FindObjectsInactive.Include);
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
                instanceGo.name = "[Auto_DictionarySoundManager]";
                DontDestroyOnLoad(instanceGo);
                return;
            }

            var soundObj = new GameObject("[Auto_DictionarySoundManager]");
            soundObj.AddComponent<DictionarySoundManager>();
            DontDestroyOnLoad(soundObj);
            Debug.Log("<color=cyan>[DictionarySoundManager] Otomatis aktif & siap memutar SFX Dictionary!</color>");
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

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.ignoreListenerPause = true; // Tetap bunyi saat game pause (Time.timeScale = 0)

#if UNITY_EDITOR
            if (openingClips == null || openingClips.Length == 0 || openingClips[0] == null)
            {
                AutoAssignClips();
            }
#endif
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _dictController = null;
            _wasOpen = false;
            _justPlayedClose = false;
            _boundButtonIds.Clear();
            BindAllDictionaryUI();
        }

        private void Start()
        {
            BindAllDictionaryUI();
        }

        private void Update()
        {
            if (_dictController == null)
            {
                _dictController = FindFirstObjectByType<Utsuru.DictionaryUI.DictionaryUIController>(FindObjectsInactive.Include);
                if (_dictController == null) return;
                _wasOpen = _dictController.IsOpen;
                BindAllDictionaryUI();
            }

            bool isOpen = _dictController.IsOpen;

            // Deteksi perubahan status buka/tutup buku
            if (isOpen != _wasOpen)
            {
                _wasOpen = isOpen;
                if (isOpen)
                {
                    _justPlayedClose = false;
                    PlayBookOpen();
                }
                else
                {
                    if (!_justPlayedClose)
                    {
                        PlayBookClose();
                    }
                    _justPlayedClose = false;
                }
            }

            // Deteksi tombol Escape saat buku terbuka untuk respon audio instan
            if (isOpen)
            {
                bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) escPressed = true;
#else
                if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
#endif
                if (escPressed)
                {
                    _justPlayedClose = true;
                    PlayBookClose();
                }
            }
        }

        /// <summary>
        /// Mengaitkan listener audio secara non-invasif (tanpa mengubah script target)
        /// </summary>
        public void BindAllDictionaryUI()
        {
            if (_dictController == null)
            {
                _dictController = FindFirstObjectByType<Utsuru.DictionaryUI.DictionaryUIController>(FindObjectsInactive.Include);
            }

            if (_dictController != null)
            {
                // 1. Tab Bookmark Atas (Album, Like, Favorite, Quiz, Hiragana, Module, Theme, Exit)
                BindButton(_dictController, "tabAlbumButton", () => PlaySound(UISoundType.TabSwitch));
                BindButton(_dictController, "tabLikeButton", () => PlaySound(UISoundType.TabSwitch));
                BindButton(_dictController, "tabFavoriteButton", () => PlaySound(UISoundType.TabSwitch));
                BindButton(_dictController, "tabQuizButton", () => PlaySound(UISoundType.TabSwitch));
                BindButton(_dictController, "tabHiraganaButton", () => PlaySound(UISoundType.TabSwitch));
                BindButton(_dictController, "tabModuleButton", () => PlaySound(UISoundType.TabSwitch));
                BindButton(_dictController, "tabThemeButton", () => PlaySound(UISoundType.TabSwitch));
                BindButton(_dictController, "tabExitButton", () =>
                {
                    _justPlayedClose = true;
                    PlayBookClose();
                });
            }

            // 2. Klik Balik Halaman Buku Kiri/Kanan & Kartu Polaroid
            var bookSpread = FindFirstObjectByType<Utsuru.DictionaryUI.DictionaryBookSpreadUI>(FindObjectsInactive.Include);
            if (bookSpread != null)
            {
                BindButton(bookSpread, "leftPageClickArea", () => PlaySound(UISoundType.PageTurn));
                BindButton(bookSpread, "rightPageClickArea", () => PlaySound(UISoundType.PageTurn));

                // Bind kartu polaroid di book spread
                var slotsField = typeof(Utsuru.DictionaryUI.DictionaryBookSpreadUI).GetField("polaroidSlots", BindingFlags.Instance | BindingFlags.NonPublic);
                if (slotsField != null)
                {
                    var slots = slotsField.GetValue(bookSpread) as Utsuru.DictionaryUI.DictionaryPolaroidCardUI[];
                    if (slots != null)
                    {
                        foreach (var slot in slots)
                        {
                            if (slot != null) BindPolaroidCard(slot);
                        }
                    }
                }
            }

            // Bind semua polaroid card yang mungkin ada di scene
            var allPolaroids = FindObjectsByType<Utsuru.DictionaryUI.DictionaryPolaroidCardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var card in allPolaroids)
            {
                if (card != null) BindPolaroidCard(card);
            }

            // 3. Sub-tab Lembar Kana
            var kanaSpread = FindFirstObjectByType<Utsuru.DictionaryUI.DictionaryKanaSpreadUI>(FindObjectsInactive.Include);
            if (kanaSpread != null)
            {
                BindButtonArray(kanaSpread, "tabGojuonButtons", () => PlaySound(UISoundType.TabSwitch));
                BindButtonArray(kanaSpread, "tabDakuonButtons", () => PlaySound(UISoundType.TabSwitch));
                BindButtonArray(kanaSpread, "tabYoonButtons", () => PlaySound(UISoundType.TabSwitch));
            }
        }

        private void BindPolaroidCard(Utsuru.DictionaryUI.DictionaryPolaroidCardUI card)
        {
            BindButton(card, "frontClickButton", () => PlaySound(UISoundType.CardFlip));
            BindButton(card, "backClickButton", () => PlaySound(UISoundType.CardFlip));
            BindButton(card, "cardClickButton", () => PlaySound(UISoundType.CardFlip));
        }

        private void BindButton(object target, string fieldName, Action onPlaySound)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null) return;

            var btn = field.GetValue(target) as Button;
            if (btn != null && _boundButtonIds.Add(btn.GetInstanceID()))
            {
                btn.onClick.AddListener(() => onPlaySound?.Invoke());
            }
        }

        private void BindButtonArray(object target, string fieldName, Action onPlaySound)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null) return;

            var btns = field.GetValue(target) as Button[];
            if (btns != null)
            {
                foreach (var btn in btns)
                {
                    if (btn != null && _boundButtonIds.Add(btn.GetInstanceID()))
                    {
                        btn.onClick.AddListener(() => onPlaySound?.Invoke());
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // --- Static API ---

        public static void Play(UISoundType soundType) => Instance?.PlaySound(soundType);
        public static void PlayOpen() => Instance?.PlayBookOpen();
        public static void PlayClose() => Instance?.PlayBookClose();
        public static void PlayPageTurn() => Instance?.PlaySound(UISoundType.PageTurn);
        public static void PlayCardFlip() => Instance?.PlaySound(UISoundType.CardFlip);
        public static void PlayTabSwitch() => Instance?.PlaySound(UISoundType.TabSwitch);

        // --- Public Playback Methods ---

        public void PlaySound(UISoundType soundType)
        {
            switch (soundType)
            {
                case UISoundType.TabSwitch:
                    PlaySingleClip(tabSwitchClip);
                    break;
                case UISoundType.PageTurn:
                    PlaySingleClip(pageTurnClip);
                    break;
                case UISoundType.CardFlip:
                    PlayRandomClip(cardFlipClips);
                    break;
                case UISoundType.BookOpen:
                    PlayRandomClip(openingClips);
                    break;
                case UISoundType.BookClose:
                    PlayRandomClip(closingClips);
                    break;
            }
        }

        public void PlayBookOpen()
        {
            PlayRandomClip(openingClips);
        }

        public void PlayBookClose()
        {
            PlayRandomClip(closingClips);
        }

        public void PlayClipDirectly(AudioClip clip)
        {
            if (clip != null) PlaySingleClip(clip);
        }

        private void PlaySingleClip(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;

            if (randomizePitch)
            {
                audioSource.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            }
            else
            {
                audioSource.pitch = 1f;
            }

            audioSource.PlayOneShot(clip, sfxVolume);
        }

        private void PlayRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;

            int randomIndex = UnityEngine.Random.Range(0, clips.Length);
            AudioClip selected = clips[randomIndex];

            if (selected != null)
            {
                PlaySingleClip(selected);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Clips from Assets/Sounds/Dictionary")]
        public void AutoAssignClips()
        {
            openingClips = new AudioClip[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Dictionary/opening_1.MP3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Dictionary/opening_2.MP3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Dictionary/opening_3.MP3")
            };

            closingClips = new AudioClip[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Dictionary/closing_1.MP3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Dictionary/closing_2.MP3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Dictionary/closing_3.MP3")
            };

            pageTurnClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Dictionary/page_turn.MP3");

            cardFlipClips = new AudioClip[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Dictionary/polaroid_card_flipping_1.MP3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Dictionary/polaroid_card_flipping_2.MP3")
            };

            tabSwitchClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Dictionary/tab_switching.MP3");

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            EditorUtility.SetDirty(this);
            Debug.Log("[DictionarySoundManager] Berhasil memuat klip audio dari Assets/Sounds/Dictionary!", this);
        }

        private void Reset()
        {
            AutoAssignClips();
        }
#endif
    }
}
