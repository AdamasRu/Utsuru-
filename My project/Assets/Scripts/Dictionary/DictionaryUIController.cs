using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    public enum DictionaryTab
    {
        Vocabulary = 0,
        Course = 1,
        Album = 2,
        Decoration = 3
    }

    /// <summary>
    /// Controller utama buku Dictionary (DPPL UI-05).
    /// Skrip ini berada di GameObject yang SELALU AKTIF di bawah Canvas agar selalu mendengarkan input keyboard 'B'.
    /// Yang diaktifkan/dinonaktifkan adalah viewRoot (panel buku dan dimmer).
    /// </summary>
    public class DictionaryUIController : MonoBehaviour
    {
        [Header("Tampilan Visual (Yang di-toggle)")]
        [Tooltip("Kontainer seluruh visual Dictionary (Dimmer + BookContainer). Dinonaktifkan saat tertutup.")]
        [SerializeField] private GameObject viewRoot;

        [Header("Kontrol Buka / Tutup")]
        [SerializeField] private Button closeButton;
        [SerializeField] private KeyCode toggleKey = KeyCode.B;

        [Header("Marker Tab Buttons (Samping Kiri Buku)")]
        [SerializeField] private Button vocabTabButton;
        [SerializeField] private Button courseTabButton;
        [SerializeField] private Button albumTabButton;
        [SerializeField] private Button decorTabButton;

        [Header("Tab Panels")]
        [SerializeField] private GameObject vocabPanel;
        [SerializeField] private GameObject coursePanel; // Placeholder
        [SerializeField] private GameObject albumPanel;
        [SerializeField] private GameObject decorPanel;  // Placeholder

        [Header("Sub-Controllers")]
        [SerializeField] private DictionaryVocabularyTabUI vocabTabController;
        [SerializeField] private DictionaryAlbumTabUI albumTabController;

        [Header("Visual Tab (Aktif vs Tidak Aktif)")]
        [SerializeField] private Color activeTabColor = new Color(0.95f, 0.92f, 0.85f, 1f);
        [SerializeField] private Color inactiveTabColor = new Color(0.55f, 0.5f, 0.45f, 1f);

        public bool IsOpen => viewRoot != null && viewRoot.activeSelf;

        private DictionaryTab currentTab = DictionaryTab.Album;
        private float prevTimeScale = 1f;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);

            if (vocabTabButton != null) vocabTabButton.onClick.AddListener(() => SwitchTab(DictionaryTab.Vocabulary));
            if (courseTabButton != null) courseTabButton.onClick.AddListener(() => SwitchTab(DictionaryTab.Course));
            if (albumTabButton != null) albumTabButton.onClick.AddListener(() => SwitchTab(DictionaryTab.Album));
            if (decorTabButton != null) decorTabButton.onClick.AddListener(() => SwitchTab(DictionaryTab.Decoration));

            // Default: tampilan buku tertutup saat mulai, tapi GameObject ini tetap aktif mendengarkan input
            if (viewRoot != null) viewRoot.SetActive(false);
        }

        private void Update()
        {
            if (IsTogglePressed())
            {
                Toggle();
            }
            else if (IsOpen && IsEscapePressed())
            {
                Close();
            }
        }

        private bool IsTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.bKey.wasPressedThisFrame) return true;
#endif
            try
            {
                if (Input.GetKeyDown(toggleKey)) return true;
            }
            catch {}
            return false;
        }

        private bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) return true;
            }
            catch {}
            return false;
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open(DictionaryTab startingTab = DictionaryTab.Album)
        {
            if (IsOpen) return;

            // Blok pemotretan kamera saat dictionary terbuka
            if (CameraCaptureSystem.Instance != null)
            {
                CameraCaptureSystem.Instance.InputBlocked = true;
            }

            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            var cameraMode = FindFirstObjectByType<CameraModeController>();
            if (cameraMode != null)
            {
                cameraMode.SetControlsLocked(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (viewRoot != null) viewRoot.SetActive(true);

            SwitchTab(startingTab);
            Debug.Log("[DictionaryUIController] Dictionary DIBUKA.");
        }

        public void Close()
        {
            if (!IsOpen) return;

            if (viewRoot != null) viewRoot.SetActive(false);

            Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;

            if (CameraCaptureSystem.Instance != null)
            {
                CameraCaptureSystem.Instance.InputBlocked = false;
            }

            // Kembalikan kontrol player dan kunci kursor kembali
            var cameraMode = FindFirstObjectByType<CameraModeController>();
            if (cameraMode != null)
            {
                cameraMode.SetControlsLocked(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Debug.Log("[DictionaryUIController] Dictionary DITUTUP.");
        }

        public void SwitchTab(DictionaryTab tab)
        {
            currentTab = tab;

            if (vocabPanel != null) vocabPanel.SetActive(tab == DictionaryTab.Vocabulary);
            if (coursePanel != null) coursePanel.SetActive(tab == DictionaryTab.Course);
            if (albumPanel != null) albumPanel.SetActive(tab == DictionaryTab.Album);
            if (decorPanel != null) decorPanel.SetActive(tab == DictionaryTab.Decoration);

            UpdateTabButtonVisual(vocabTabButton, tab == DictionaryTab.Vocabulary);
            UpdateTabButtonVisual(courseTabButton, tab == DictionaryTab.Course);
            UpdateTabButtonVisual(albumTabButton, tab == DictionaryTab.Album);
            UpdateTabButtonVisual(decorTabButton, tab == DictionaryTab.Decoration);

            if (tab == DictionaryTab.Vocabulary && vocabTabController != null)
            {
                vocabTabController.RefreshUI();
            }
            else if (tab == DictionaryTab.Album && albumTabController != null)
            {
                albumTabController.RefreshUI();
            }
        }

        private void UpdateTabButtonVisual(Button btn, bool isActive)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? activeTabColor : inactiveTabColor;
            }
        }
    }
}
