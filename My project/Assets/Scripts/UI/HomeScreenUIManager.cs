using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Utsuru.Authentication;

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

        [Header("Admin Account Management (Local CRUD)")]
        [Tooltip("Button on HomeScreen that only appears if logged in as Admin.")]
        [SerializeField] private Button adminAccountsButton;

        [Tooltip("Modal window displaying the list of all registered accounts.")]
        [SerializeField] private GameObject adminAccountsModal;

        [Tooltip("Container scroll rect content for account items.")]
        [SerializeField] private Transform accountsListContainer;

        [Tooltip("Prefab/Template for an account row.")]
        [SerializeField] private GameObject accountRowTemplate;

        [Tooltip("Close button for the admin modal.")]
        [SerializeField] private Button closeAdminModalButton;

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
            // Allow returning to HomeScreen with the Escape key when in subpanels or modal
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (adminAccountsModal != null && adminAccountsModal.activeSelf)
                {
                    CloseAdminAccountsModal();
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

            if (adminAccountsButton == null && homeScreenPanel != null)
            {
                var t = homeScreenPanel.transform.Find("AdminAccountsButton") ?? homeScreenPanel.transform.Find("Btn_AdminAccounts");
                if (t != null) adminAccountsButton = t.GetComponent<Button>();
            }

            if (adminAccountsButton != null)
            {
                adminAccountsButton.onClick.RemoveAllListeners();
                adminAccountsButton.onClick.AddListener(OpenAdminAccountsModal);
            }

            if (closeAdminModalButton != null)
            {
                closeAdminModalButton.onClick.RemoveAllListeners();
                closeAdminModalButton.onClick.AddListener(CloseAdminAccountsModal);
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
            if (adminAccountsModal != null) adminAccountsModal.SetActive(false);

            UpdateLogoutButtonVisibility();
            UpdateAdminButtonVisibility();
        }

        /// <summary>
        /// Updates the visibility of the admin management button based on the active account role.
        /// Strictly visible ONLY when logged in as Admin.
        /// </summary>
        public void UpdateAdminButtonVisibility()
        {
            if (adminAccountsButton == null && homeScreenPanel != null)
            {
                var t = homeScreenPanel.transform.Find("AdminAccountsButton") ?? homeScreenPanel.transform.Find("Btn_AdminAccounts");
                if (t != null) adminAccountsButton = t.GetComponent<Button>();
            }

            if (adminAccountsButton != null)
            {
                bool isAdmin = Utsuru.Authentication.LocalAccountManager.Instance != null &&
                               Utsuru.Authentication.LocalAccountManager.Instance.IsAdminLoggedIn;
                adminAccountsButton.gameObject.SetActive(isAdmin);
                Debug.Log($"[HomeScreenUIManager] Admin button visibility set to {isAdmin} (IsAdmin: {isAdmin})");
            }
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

        // ==========================================
        // Local Account Admin Modal (CRUD Read & Delete)
        // ==========================================

        /// <summary>
        /// Opens the Admin Account Management modal and populates the account list.
        /// </summary>
        public void OpenAdminAccountsModal()
        {
            if (adminAccountsModal != null)
            {
                adminAccountsModal.SetActive(true);
                RefreshAccountsList();
            }
        }

        /// <summary>
        /// Closes the Admin Account Management modal.
        /// </summary>
        public void CloseAdminAccountsModal()
        {
            if (adminAccountsModal != null)
            {
                adminAccountsModal.SetActive(false);
            }
        }

        /// <summary>
        /// Reads all accounts from LocalAccountManager and populates rows in the scroll view.
        /// </summary>
        public void RefreshAccountsList()
        {
            if (accountsListContainer == null) return;

            // Clear previous instantiated items (except template)
            foreach (Transform child in accountsListContainer)
            {
                if (accountRowTemplate != null && child.gameObject == accountRowTemplate)
                {
                    continue;
                }
                Destroy(child.gameObject);
            }

            if (accountRowTemplate != null)
            {
                accountRowTemplate.SetActive(false);
            }

            var accounts = LocalAccountManager.EnsureInstance().GetAllAccounts();
            foreach (var acc in accounts)
            {
                if (accountRowTemplate == null) break;

                GameObject row = Instantiate(accountRowTemplate, accountsListContainer);
                row.SetActive(true);

                // Setup labels
                var infoText = row.transform.Find("AccountInfoText") != null 
                    ? row.transform.Find("AccountInfoText").GetComponent<TMP_Text>()
                    : row.GetComponentInChildren<TMP_Text>();

                if (infoText != null)
                {
                    string roleBadge = acc.role == "admin" 
                        ? "<color=#D35400><b>[ADMIN]</b></color>" 
                        : "<color=#2980B9><b>[PEMAIN]</b></color>";

                    string subInfo = acc.username.Equals(acc.email, System.StringComparison.OrdinalIgnoreCase)
                        ? $"Terdaftar: {acc.createdAt}"
                        : $"{acc.email} • {acc.createdAt}";

                    infoText.text = $"{roleBadge} <b>{acc.username}</b>\n<size=75%><color=#7F8C8D>{subInfo}</color></size>";
                }

                // Setup delete button
                var deleteBtn = row.transform.Find("DeleteButton") != null
                    ? row.transform.Find("DeleteButton").GetComponent<Button>()
                    : row.GetComponentInChildren<Button>();

                if (deleteBtn != null)
                {
                    // Admin account cannot be deleted!
                    if (acc.role == "admin" || acc.username.Equals("admin", System.StringComparison.OrdinalIgnoreCase))
                    {
                        deleteBtn.gameObject.SetActive(false);
                    }
                    else
                    {
                        deleteBtn.gameObject.SetActive(true);
                        string accId = acc.accountId;
                        string accName = acc.username;
                        deleteBtn.onClick.RemoveAllListeners();
                        deleteBtn.onClick.AddListener(() => OnDeleteAccountClicked(accId, accName));
                    }
                }
            }
        }

        private void OnDeleteAccountClicked(string accountId, string username)
        {
            if (LocalAccountManager.Instance == null) return;

            bool success = LocalAccountManager.Instance.DeleteAccount(accountId, out string error);
            if (success)
            {
                Debug.Log($"[HomeScreenUIManager] Akun '{username}' berhasil dihapus.");
                RefreshAccountsList();
            }
            else
            {
                Debug.LogWarning($"[HomeScreenUIManager] Gagal menghapus akun: {error}");
            }
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
