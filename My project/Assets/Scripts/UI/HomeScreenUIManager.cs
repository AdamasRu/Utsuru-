using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
            // Allow returning to HomeScreen with the Escape key when in subpanels
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (creditsPanel != null && creditsPanel.activeSelf)
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
        }

        /// <summary>
        /// Activates HomeScreen panel and deactivates other panels.
        /// </summary>
        public void ShowHomeScreen()
        {
            if (homeScreenPanel != null) homeScreenPanel.SetActive(true);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (settingsOptionsPanel != null) settingsOptionsPanel.SetActive(false);

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
            if (!string.IsNullOrEmpty(gameplaySceneName) && Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            else
            {
                Debug.LogWarning($"[HomeScreenUIManager] Scene '{gameplaySceneName}' is not loaded or not in Build Settings.");
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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
