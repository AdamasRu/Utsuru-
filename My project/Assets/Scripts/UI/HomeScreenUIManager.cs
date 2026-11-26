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

        private UISakuraRain sakuraRain;
        private Coroutine panelTransitionCoroutine;
        private Coroutine guestPopupCoroutine;
        private GameObject currentActivePanel;
        private bool initialEntrancePlayed;
        private bool isStartingGameplay;

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

            isStartingGameplay = false;
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

            InitializeVisualEffects();
            ShowHomeScreen();
        }

        private void InitializeVisualEffects()
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            UIPanelAnimator.EnsureSharedBackground(canvas, GetAllMenuPanels());

            sakuraRain = UISakuraRain.EnsureForCanvas(canvas);
            sakuraRain?.AttachToCanvasBackgroundLayer(canvas, true);

            UIPanelAnimator.AttachButtonFeedbackInChildren(homeScreenPanel);
            UIPanelAnimator.AttachButtonFeedbackInChildren(creditsPanel);
            UIPanelAnimator.AttachButtonFeedbackInChildren(settingsOptionsPanel);
            UIPanelAnimator.AttachButtonFeedbackInChildren(guestSavePopup);
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
        /// Activates HomeScreen panel and deactivates other panels with smooth animation.
        /// </summary>
        public void ShowHomeScreen()
        {
            HideGuestSavePopup(!initialEntrancePlayed);
            SwitchMenuPanel(homeScreenPanel, slideDirection: -1, enableSakura: true);
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
        /// Activates Credits panel and deactivates other panels with smooth animation.
        /// </summary>
        public void ShowCredits()
        {
            SwitchMenuPanel(creditsPanel, slideDirection: 1, enableSakura: true);
        }

        /// <summary>
        /// Activates Settings-Options panel and hides sakura rain (excluded on Options/Settings).
        /// </summary>
        public void ShowSettingsOptions()
        {
            SwitchMenuPanel(settingsOptionsPanel, slideDirection: 1, enableSakura: false);
        }

        private GameObject[] GetAllMenuPanels()
        {
            return new GameObject[] { homeScreenPanel, creditsPanel, settingsOptionsPanel };
        }

        private void SwitchMenuPanel(GameObject targetPanel, int slideDirection, bool enableSakura)
        {
            if (targetPanel == null) return;

            GameObject[] allPanels = GetAllMenuPanels();

            if (!initialEntrancePlayed)
            {
                initialEntrancePlayed = true;
                for (int i = 0; i < allPanels.Length; i++)
                {
                    if (allPanels[i] != null && allPanels[i] != targetPanel)
                    {
                        UIPanelAnimator.ResetPanelVisuals(allPanels[i], 1f);
                        allPanels[i].SetActive(false);
                    }
                }

                currentActivePanel = targetPanel;
                sakuraRain?.SetVisible(enableSakura, immediate: true);
                panelTransitionCoroutine = StartCoroutine(UIPanelAnimator.PlayPanelEntranceRoutine(targetPanel));
                return;
            }

            if (currentActivePanel == targetPanel && targetPanel.activeSelf)
            {
                sakuraRain?.SetVisible(enableSakura);
                return;
            }

            GameObject previousPanel = currentActivePanel;

            if (panelTransitionCoroutine != null)
            {
                StopCoroutine(panelTransitionCoroutine);
                for (int i = 0; i < allPanels.Length; i++)
                {
                    if (allPanels[i] != null && allPanels[i] != previousPanel)
                    {
                        UIPanelAnimator.ResetPanelVisuals(allPanels[i], 1f);
                        allPanels[i].SetActive(false);
                    }
                }
            }

            sakuraRain?.SetVisible(enableSakura);

            currentActivePanel = targetPanel;
            panelTransitionCoroutine = StartCoroutine(RunMenuPanelSwitch(previousPanel, targetPanel, allPanels, slideDirection));
        }

        private IEnumerator RunMenuPanelSwitch(GameObject fromPanel, GameObject toPanel, GameObject[] allPanels, int direction)
        {
            yield return UIPanelAnimator.SwitchPanelRoutine(fromPanel, toPanel, allPanels, direction);
            panelTransitionCoroutine = null;
        }

        /// <summary>
        /// Handler for Play Button click. Loads the target gameplay scene with fade.
        /// </summary>
        public void OnPlayButtonClicked()
        {
            if (isStartingGameplay || UISceneFader.IsTransitioning) return;
            isStartingGameplay = true;

            Debug.Log($"[HomeScreenUIManager] Play button clicked. Loading scene '{gameplaySceneName}'...");
            try
            {
                Time.timeScale = 1f;
                UISceneFader.LoadSceneWithFade(gameplaySceneName);
            }
            catch (System.Exception ex)
            {
                isStartingGameplay = false;
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
            if (isStartingGameplay || UISceneFader.IsTransitioning) return;
            isStartingGameplay = true;

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

            UISceneFader.LoadSceneWithFade("LoginPage");
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
                if (guestPopupCoroutine != null) StopCoroutine(guestPopupCoroutine);
                guestPopupCoroutine = StartCoroutine(AnimateGuestPopupRoutine(show: true));
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
            HideGuestSavePopup(immediate: false);
        }

        private void HideGuestSavePopup(bool immediate)
        {
            if (guestSavePopup == null) return;

            if (guestPopupCoroutine != null)
            {
                StopCoroutine(guestPopupCoroutine);
                guestPopupCoroutine = null;
            }

            if (immediate || !guestSavePopup.activeSelf)
            {
                UIPanelAnimator.ResetPanelVisuals(guestSavePopup, 1f);
                guestSavePopup.SetActive(false);
                return;
            }

            guestPopupCoroutine = StartCoroutine(AnimateGuestPopupRoutine(show: false));
        }

        private IEnumerator AnimateGuestPopupRoutine(bool show)
        {
            CanvasGroup cg = UIPanelAnimator.EnsureCanvasGroup(guestSavePopup);
            Transform card = guestSavePopup.transform.Find("Card");

            if (show)
            {
                guestSavePopup.SetActive(true);
                cg.alpha = 0f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
                if (card != null) card.localScale = new Vector3(0.94f, 0.94f, 1f);

                float elapsed = 0f;
                const float dur = 0.2f;
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    cg.alpha = eased;
                    if (card != null) card.localScale = Vector3.Lerp(new Vector3(0.94f, 0.94f, 1f), Vector3.one, eased);
                    yield return null;
                }

                cg.alpha = 1f;
                if (card != null) card.localScale = Vector3.one;
            }
            else
            {
                cg.interactable = true;
                cg.blocksRaycasts = false;
                float startAlpha = cg.alpha;
                float elapsed = 0f;
                const float dur = 0.14f;
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    cg.alpha = Mathf.Lerp(startAlpha, 0f, t);
                    if (card != null) card.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.95f, 0.95f, 1f), t);
                    yield return null;
                }

                UIPanelAnimator.ResetPanelVisuals(guestSavePopup, 1f);
                if (card != null) card.localScale = Vector3.one;
                guestSavePopup.SetActive(false);
            }

            guestPopupCoroutine = null;
        }

        /// <summary>
        /// Menangani klik tombol Login di dalam popup Guest: mengarahkan pemain ke halaman LoginPage.
        /// </summary>
        public void OnGuestPopupLoginClicked()
        {
            HideGuestSavePopup(immediate: true);
            Debug.Log("[HomeScreenUIManager] Navigating to LoginPage from Guest Save Popup...");
            UISceneFader.LoadSceneWithFade("LoginPage");
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
