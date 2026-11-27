using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// <summary>
    /// Memutar SFX klik kayu (chisper-ui-click-wooden-634791.mp3) untuk semua tombol UI di seluruh scene,
    /// kecuali tombol yang sudah memiliki audio khusus (seluruh Dictionary, tombol kamera, pemutar audio/voice).
    /// Tetap berfungsi saat game di-pause (Time.timeScale == 0) dan menangkap tombol yang dibuat dinamis saat runtime.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    public class GlobalButtonSoundManager : MonoBehaviour
    {
        public static GlobalButtonSoundManager Instance { get; private set; }

        [Header("Audio Source & Clip")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickClip;

        [Header("Pengaturan Volume & Variasi")]
        [Range(0f, 1f)]
        [SerializeField] private float clickVolume = 0.85f;
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private float pitchVariation = 0.03f;

        private readonly HashSet<int> boundButtonIds = new HashSet<int>();
        private readonly List<RaycastResult> pointerRaycastResults = new List<RaycastResult>(16);
        private PointerEventData reusablePointerEventData;
        private int lastPlayedFrame = -1;

        private const string PrefabResourcePath = "Audio/GlobalButtonSoundManager";
        private const string ClipAssetPath = "Assets/Sounds/chisper-ui-click-wooden-634791.mp3";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeAtRuntime()
        {
            if (Instance != null) return;

            var existing = FindFirstObjectByType<GlobalButtonSoundManager>(FindObjectsInactive.Include);
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
                instanceGo.name = "[Auto_GlobalButtonSoundManager]";
                DontDestroyOnLoad(instanceGo);
                return;
            }

            var soundObj = new GameObject("[Auto_GlobalButtonSoundManager]");
            soundObj.AddComponent<GlobalButtonSoundManager>();
            DontDestroyOnLoad(soundObj);
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

            EnsureAudioReady();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            BindAllButtonsInLoadedScenes();
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
            BindAllButtonsInLoadedScenes();
        }

        private void Update()
        {
            // Tangkap tombol yang dibuat dinamis saat runtime (mis. pilihan dialog atau item popup) tepat saat ditekan
            if (IsPointerOrSubmitPressedThisFrame())
            {
                TryBindHoveredOrSelectedButton();
            }
        }

        public void BindAllButtonsInLoadedScenes()
        {
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allButtons.Length; i++)
            {
                TryBindButton(allButtons[i]);
            }
        }

        private void TryBindHoveredOrSelectedButton()
        {
            EventSystem es = EventSystem.current;
            if (es == null) return;

            if (es.currentSelectedGameObject != null)
            {
                Button selectedBtn = es.currentSelectedGameObject.GetComponentInParent<Button>();
                if (selectedBtn != null) TryBindButton(selectedBtn);
            }

            Vector2 pointerPos;
            if (!TryGetPointerPosition(out pointerPos)) return;

            if (reusablePointerEventData == null)
                reusablePointerEventData = new PointerEventData(es);

            reusablePointerEventData.position = pointerPos;
            pointerRaycastResults.Clear();
            es.RaycastAll(reusablePointerEventData, pointerRaycastResults);

            for (int i = 0; i < pointerRaycastResults.Count; i++)
            {
                GameObject hitGo = pointerRaycastResults[i].gameObject;
                if (hitGo == null) continue;
                Button hitBtn = hitGo.GetComponentInParent<Button>();
                if (hitBtn != null)
                {
                    TryBindButton(hitBtn);
                }
            }
        }

        private void TryBindButton(Button btn)
        {
            if (btn == null) return;

            int id = btn.GetInstanceID();
            if (!boundButtonIds.Add(id)) return;

            if (IsExcludedButton(btn)) return;

            btn.onClick.AddListener(PlayButtonClick);
        }

        private static bool IsExcludedButton(Button btn)
        {
            // 1. Tombol yang sudah memakai UIButtonSound khusus
            if (btn.GetComponent<UIButtonSound>() != null)
                return true;

            // 2. Seluruh tombol di dalam Dictionary (sudah memiliki DictionarySoundManager sendiri)
            if (btn.GetComponentInParent<Utsuru.DictionaryUI.DictionaryUIController>(true) != null ||
                HasAncestorNamed(btn.transform, "Dictionary"))
                return true;

            // 3. Tombol kamera / pemutar voice & kosakata / backdrop dimmer
            string n = btn.name;
            if (n == "BtnAudio" ||
                n == "ReplayAudioButton" ||
                n == "BtnShutter" ||
                n == "Btn_Shutter" ||
                n == "BtnCamera" ||
                n == "Btn_Camera" ||
                n == "BackdropClose" ||
                n == "Dimmer" ||
                n == "BlurBackdrop")
            {
                return true;
            }

            return false;
        }

        private static bool HasAncestorNamed(Transform tr, string targetName)
        {
            Transform cur = tr;
            while (cur != null)
            {
                if (cur.name == targetName) return true;
                cur = cur.parent;
            }
            return false;
        }

        public void PlayButtonClick()
        {
            if (Time.frameCount == lastPlayedFrame) return;
            EnsureAudioReady();
            if (buttonClickClip == null || audioSource == null) return;

            lastPlayedFrame = Time.frameCount;

            if (randomizePitch)
                audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            else
                audioSource.pitch = 1f;

            audioSource.PlayOneShot(buttonClickClip, clickVolume);
        }

        private void EnsureAudioReady()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.ignoreListenerPause = true;

#if UNITY_EDITOR
            if (buttonClickClip == null)
            {
                buttonClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipAssetPath);
            }
#endif
        }

        private static bool IsPointerOrSubmitPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
            if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame ||
                                             Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                                             Keyboard.current.spaceKey.wasPressedThisFrame))
                return true;
#endif
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        }

        private static bool TryGetPointerPosition(out Vector2 pos)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                pos = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null)
            {
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
#endif
            pos = Input.mousePosition;
            return true;
        }

#if UNITY_EDITOR
        public void SetClickClipEditor(AudioClip clip, AudioSource source)
        {
            buttonClickClip = clip;
            audioSource = source;
        }
#endif
    }
}

