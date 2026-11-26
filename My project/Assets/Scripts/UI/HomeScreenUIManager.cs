using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Utsuru.UI
{
    /// <summary>
    /// Central manager for the HomeScreen scene UI.
    /// Manages panel switching between HomeScreen, Credits, and Settings-Options,
    /// button interactions, scene transitions, and application exit.
    /// </summary>
    public class HomeScreenUIManager : MonoBehaviour
    {
        public static HomeScreenUIManager Instance { get; private set; }

        [Header("Panels")]
        [Tooltip("The main home screen panel containing menu buttons.")]
        [SerializeField] private GameObject homeScreenPanel;

        [Tooltip("The credits panel.")]
        [SerializeField] private GameObject creditsPanel;

        [Tooltip("The settings and options panel.")]
        [SerializeField] private GameObject settingsOptionsPanel;

        [Header("HomeScreen Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button exitGameButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button saveToCloudButton;
        [SerializeField] private TMP_Text saveToCloudText;

        [Header("Guest Save Popup")]
        [Tooltip("Popup modal yang muncul saat guest menekan tombol Save to Cloud.")]
        [SerializeField] private GameObject guestSavePopup;
        [SerializeField] private Button guestPopupLoginButton;
        [SerializeField] private Button guestPopupCloseButton;

        [Header("Credits Buttons")]
        [SerializeField] private Button creditsBackButton;

        [Header("Settings-Options Buttons")]
        [SerializeField] private Button settingsBackButton;

        [Header("Scene Transition")]
        [Tooltip("Name of the gameplay scene to load when Play is clicked.")]
        [SerializeField] private string gameplaySceneName = "Day_Showcase";

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            SetupListeners();

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Briefly awaken settings panel if needed so SettingsManager applies initial audio/graphics settings
            if (settingsOptionsPanel != null && !settingsOptionsPanel.activeSelf)
            {
                settingsOptionsPanel.SetActive(true);
                var sm = settingsOptionsPanel.GetComponent<SettingsManager>();
                if (sm != null) sm.Initialize();
                settingsOptionsPanel.SetActive(false);
            }

            ShowHomeScreen();
        }

        private void Update()
        {
            // Allow returning to HomeScreen with the Escape key when in subpanels or popups
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (guestSavePopup != null && guestSavePopup.activeSelf)
                {
                    HideGuestSavePopup();
                }
                else if (creditsPanel != null && creditsPanel.activeSelf)
                {
                    ShowHomeScreen();
                }
                else if (settingsOptionsPanel != null && settingsOptionsPanel.activeSelf)
                {
                    ShowHomeScreen();
                }
            }
        }

        private void SetupListeners()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(OnPlayButtonClicked);
            }

            if (optionsButton != null)
            {
                optionsButton.onClick.RemoveAllListeners();
                optionsButton.onClick.AddListener(ShowSettingsOptions);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveAllListeners();
                creditsButton.onClick.AddListener(ShowCredits);
            }

            if (exitGameButton != null)
            {
                exitGameButton.onClick.RemoveAllListeners();
                exitGameButton.onClick.AddListener(OnExitGameClicked);
            }

            if (logoutButton == null && homeScreenPanel != null)
            {
                var t = homeScreenPanel.transform.Find("LogOutButton") ?? homeScreenPanel.transform.Find("LogoutButton");
                if (t != null) logoutButton = t.GetComponent<Button>();
            }

            if (logoutButton != null)
            {
                logoutButton.onClick.RemoveAllListeners();
                logoutButton.onClick.AddListener(OnLogoutButtonClicked);
            }

            if (saveToCloudButton == null && homeScreenPanel != null)
            {
                var t = homeScreenPanel.transform.Find("SaveToCloudButton");
                if (t != null) saveToCloudButton = t.GetComponent<Button>();
            }

            if (saveToCloudButton != null)
            {
                if (saveToCloudText == null)
                {
                    saveToCloudText = saveToCloudButton.GetComponentInChildren<TMP_Text>();
                }
                saveToCloudButton.onClick.RemoveAllListeners();
                saveToCloudButton.onClick.AddListener(OnSaveToCloudClicked);
            }

            if (creditsBackButton != null)
            {
                creditsBackButton.onClick.RemoveAllListeners();
                creditsBackButton.onClick.AddListener(ShowHomeScreen);
            }

            if (settingsBackButton != null)
            {
                settingsBackButton.onClick.RemoveAllListeners();
                settingsBackButton.onClick.AddListener(ShowHomeScreen);
            }

            // Setup Guest Save Popup references and buttons
            if (guestSavePopup == null)
            {
                var canvas = transform.parent != null ? transform.parent : transform;
                var popupT = canvas.Find("GuestSavePopup");
                if (popupT != null) guestSavePopup = popupT.gameObject;
            }

            if (guestSavePopup != null)
            {
                if (guestPopupLoginButton == null)
                {
                    var loginT = guestSavePopup.transform.Find("Card/Buttons/LoginButton") 
                                 ?? guestSavePopup.transform.Find("Card/LoginButton")
                                 ?? guestSavePopup.transform.Find("LoginButton");
                    if (loginT != null) guestPopupLoginButton = loginT.GetComponent<Button>();
                }

                if (guestPopupCloseButton == null)
                {
                    var closeT = guestSavePopup.transform.Find("Card/Buttons/CloseButton") 
                                 ?? guestSavePopup.transform.Find("Card/CloseButton")
                                 ?? guestSavePopup.transform.Find("CloseButton");
                    if (closeT != null) guestPopupCloseButton = closeT.GetComponent<Button>();
                }

                if (guestPopupLoginButton != null)
                {
                    guestPopupLoginButton.onClick.RemoveAllListeners();
                    guestPopupLoginButton.onClick.AddListener(OnGuestPopupLoginClicked);
                }

                if (guestPopupCloseButton != null)
                {
                    guestPopupCloseButton.onClick.RemoveAllListeners();
                    guestPopupCloseButton.onClick.AddListener(HideGuestSavePopup);
                }

                // If popup has a backdrop button, dismiss on backdrop click
                var backdropBtn = guestSavePopup.GetComponent<Button>();
                if (backdropBtn != null)
                {
                    backdropBtn.onClick.RemoveAllListeners();
                    backdropBtn.onClick.AddListener(HideGuestSavePopup);
                }
            }
        }

        /// <summary>
        /// Activates HomeScreen panel and deactivates other panels.
        /// </summary>
        public void ShowHomeScreen()
        {
            if (homeScreenPanel != null) homeScreenPanel.SetActive(true);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (settingsOptionsPanel != null) settingsOptionsPanel.SetActive(false);
            HideGuestSavePopup();

            UpdateLogoutButtonVisibility();
        }

        /// <summary>
        /// Updates the visibility of the logout button based on login mode.
        /// If the player entered as Guest, the logout button is hidden.
        /// </summary>
        public void UpdateLogoutButtonVisibility()
        {
            if (logoutButton == null && homeScreenPanel != null)
            {
                var t = homeScreenPanel.transform.Find("LogOutButton") ?? homeScreenPanel.transform.Find("LogoutButton");
                if (t != null) logoutButton = t.GetComponent<Button>();
            }

            if (logoutButton != null)
            {
                bool isGuest = false;
                if (Utsuru.Authentication.SessionManager.Instance != null)
                {
                    isGuest = Utsuru.Authentication.SessionManager.Instance.IsGuest;
                }
                else
                {
                    var profile = Utsuru.Authentication.Storage.LocalAccountStorage.LoadProfile();
                    if (profile != null && profile.login_mode == "Guest")
                    {
                        isGuest = true;
                    }
                }

                logoutButton.gameObject.SetActive(!isGuest);
                Debug.Log($"[HomeScreenUIManager] Logout button visibility set to {!isGuest} (IsGuest: {isGuest})");
            }
        }

        /// <summary>
        /// Activates Credits panel and deactivates other panels.
        /// </summary>
        public void ShowCredits()
        {
            if (homeScreenPanel != null) homeScreenPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(true);
            if (settingsOptionsPanel != null) settingsOptionsPanel.SetActive(false);
        }

        /// <summary>
        /// Activates Settings-Options panel and deactivates other panels.
        /// </summary>
        public void ShowSettingsOptions()
        {
            if (homeScreenPanel != null) homeScreenPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (settingsOptionsPanel != null) settingsOptionsPanel.SetActive(true);
        }

        /// <summary>
        /// Handler for Play Button click. Loads the target gameplay scene.
        /// </summary>
        public void OnPlayButtonClicked()
        {
            Debug.Log($"[HomeScreenUIManager] Play button clicked. Loading scene '{gameplaySceneName}'...");
            try
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(gameplaySceneName);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HomeScreenUIManager] Error loading scene '{gameplaySceneName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Handler for Exit Game Button click. Quits the application.
        /// </summary>
        public void OnExitGameClicked()
        {
            Debug.Log("[HomeScreenUIManager] Exit Game clicked.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Handler for Logout Button click.
        /// Clears persistent remembered-account marker and encrypted tokens,
        /// returns player to LoginPage. Preserves local game progress.
        /// </summary>
        public void OnLogoutButtonClicked()
        {
            Debug.Log("[HomeScreenUIManager] Logout button clicked.");
            if (Utsuru.Authentication.SessionManager.Instance != null)
            {
                Utsuru.Authentication.SessionManager.Instance.Logout();
            }
            else
            {
                Utsuru.Authentication.Storage.LocalAccountStorage.ClearRememberedAccount();
                Utsuru.Authentication.Security.SecureCredentialStorage.ClearCredentials();
                if (Utsuru.Authentication.AuthManager.Instance != null)
                {
                    Utsuru.Authentication.AuthManager.Instance.ClearSession();
                }
            }

            SceneManager.LoadScene("LoginPage");
        }

        private Coroutine cloudFeedbackRoutine;

        /// <summary>
        /// Handler for Save to Cloud button click.
        /// Triggers manual, on-demand synchronization of points, discoveries, and photos to Supabase.
        /// </summary>
        public void OnSaveToCloudClicked()
        {
            Debug.Log("[HomeScreenUIManager] Save to Cloud button clicked.");

            if (cloudFeedbackRoutine != null)
            {
                StopCoroutine(cloudFeedbackRoutine);
            }

            var syncManager = Utsuru.CloudSync.SupabaseCloudSyncManager.EnsureInstance();

            // Cek apakah pemain bermain sebagai Guest atau belum memiliki sesi login akun terdaftar
            if (!syncManager.IsUserLoggedIn(out _, out _))
            {
                Debug.Log("[HomeScreenUIManager] Player is in Guest mode / not logged in. Showing 'Login to save to cloud' popup.");
                ShowGuestSavePopup();
                return;
            }

            if (saveToCloudButton != null) saveToCloudButton.interactable = false;
            if (saveToCloudText != null) saveToCloudText.text = "Saving...";

            syncManager.SaveAllProgressToCloud((success, message) =>
            {
                if (this == null) return;
                string feedback;
                if (success)
                {
                    feedback = "Saved!";
                }
                else if (message != null && (message.Contains("login") || message.Contains("Login")))
                {
                    ShowGuestSavePopup();
                    feedback = "Login Required!";
                }
                else
                {
                    feedback = "Save Failed";
                }
                cloudFeedbackRoutine = StartCoroutine(ShowCloudFeedbackRoutine(feedback, 2.5f));
            });
        }

        /// <summary>
        /// Menampilkan notifikasi popup modal saat pemain Guest ingin menyimpan ke Cloud.
        /// </summary>
        public void ShowGuestSavePopup()
        {
            if (guestSavePopup != null)
            {
                guestSavePopup.SetActive(true);
            }
            else
            {
                // Fallback jika GameObject popup belum di-assign
                cloudFeedbackRoutine = StartCoroutine(ShowCloudFeedbackRoutine("Login Required!", 2.5f));
            }
        }

        /// <summary>
        /// Menutup notifikasi popup modal Guest.
        /// </summary>
        public void HideGuestSavePopup()
        {
            if (guestSavePopup != null)
            {
                guestSavePopup.SetActive(false);
            }
        }

        /// <summary>
        /// Menangani klik tombol Login di dalam popup Guest: mengarahkan pemain ke halaman LoginPage.
        /// </summary>
        public void OnGuestPopupLoginClicked()
        {
            HideGuestSavePopup();
            Debug.Log("[HomeScreenUIManager] Navigating to LoginPage from Guest Save Popup...");
            SceneManager.LoadScene("LoginPage");
        }

        private IEnumerator ShowCloudFeedbackRoutine(string message, float duration)
        {
            if (saveToCloudText != null) saveToCloudText.text = message;
            if (saveToCloudButton != null) saveToCloudButton.interactable = false;

            yield return new WaitForSecondsRealtime(duration);

            if (saveToCloudText != null) saveToCloudText.text = "Save to Cloud";
            if (saveToCloudButton != null) saveToCloudButton.interactable = true;
            cloudFeedbackRoutine = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
