using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Utsuru.UI
{
    /// <summary>
    /// Central manager for the HomeScreen scene UI.
    /// Manages panel switching between HomeScreen, Credits, and Settings-Options,
    /// button interactions, scene transitions with white fade, application exit,
    /// and staggered menu button entrance animation.
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

        [Header("Staggered Menu Button Entrance")]
        [SerializeField] private float buttonUpwardOffset = 40f;
        [SerializeField] private float buttonDuration = 0.35f;
        [SerializeField] private float buttonDelay = 0.12f;

        private struct ButtonAnimState
        {
            public Button button;
            public RectTransform rect;
            public CanvasGroup canvasGroup;
            public Vector2 targetPos;
        }

        private List<ButtonAnimState> menuButtons = new List<ButtonAnimState>();
        private Coroutine staggeredEntranceCoroutine;
        private bool isFirstStart = true;

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
            SetupStaggeredMenuButtons();
            EnsureSakuraEmitter();

            // Briefly awaken settings panel if needed so SettingsManager applies initial audio/graphics settings
            if (settingsOptionsPanel != null && !settingsOptionsPanel.activeSelf)
            {
                settingsOptionsPanel.SetActive(true);
                var sm = settingsOptionsPanel.GetComponent<SettingsManager>();
                if (sm != null) sm.Initialize();
                settingsOptionsPanel.SetActive(false);
            }

            ShowHomeScreenDirect();
        }

        private void Start()
        {
            if (isFirstStart)
            {
                isFirstStart = false;
                // Run staggered entrance when first loading HomeScreen
                PlayStaggeredMenuEntrance();
            }
        }

        private void Update()
        {
            // Allow returning to HomeScreen with the Escape key when in subpanels
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (creditsPanel != null && creditsPanel.activeSelf)
                {
                    ReturnToHomeScreen();
                }
                else if (settingsOptionsPanel != null && settingsOptionsPanel.activeSelf)
                {
                    ReturnToHomeScreen();
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
                creditsBackButton.onClick.AddListener(ReturnToHomeScreen);
            }

            if (settingsBackButton != null)
            {
                settingsBackButton.onClick.RemoveAllListeners();
                settingsBackButton.onClick.AddListener(ReturnToHomeScreen);
            }
        }

        private void SetupStaggeredMenuButtons()
        {
            menuButtons.Clear();
            Button[] btnList = new Button[] { playButton, optionsButton, creditsButton, exitGameButton };

            foreach (var btn in btnList)
            {
                if (btn == null) continue;

                var rt = btn.GetComponent<RectTransform>();
                var cg = btn.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = btn.gameObject.AddComponent<CanvasGroup>();
                }

                menuButtons.Add(new ButtonAnimState
                {
                    button = btn,
                    rect = rt,
                    canvasGroup = cg,
                    targetPos = rt.anchoredPosition
                });
            }
        }

        private void EnsureSakuraEmitter()
        {
            if (homeScreenPanel != null) SakuraSceneIntegrator.EnsureEmitterForPanel(homeScreenPanel.transform);
            if (creditsPanel != null) SakuraSceneIntegrator.EnsureEmitterForPanel(creditsPanel.transform);
            if (settingsOptionsPanel != null) SakuraSceneIntegrator.EnsureEmitterForPanel(settingsOptionsPanel.transform);
        }

        /// <summary>
        /// Direct activation of HomeScreen without transition (e.g. at startup).
        /// </summary>
        public void ShowHomeScreenDirect()
        {
            if (homeScreenPanel != null) homeScreenPanel.SetActive(true);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (settingsOptionsPanel != null) settingsOptionsPanel.SetActive(false);

            UpdateLogoutButtonVisibility();
        }

        /// <summary>
        /// Activates HomeScreen panel with smooth white fade transition and sequential button entrance.
        /// </summary>
        public void ShowHomeScreen()
        {
            ReturnToHomeScreen();
        }

        /// <summary>
        /// Returns from Options or Credits to HomeScreen with white fade and staggered entrance.
        /// </summary>
        public void ReturnToHomeScreen()
        {
            if (ScreenFadeTransition.Instance != null)
            {
                ScreenFadeTransition.Instance.DoTransition(
                    onCovered: () =>
                    {
                        ShowHomeScreenDirect();
                        PrepareButtonsForStaggeredEntrance();
                    },
                    onComplete: () =>
                    {
                        PlayStaggeredMenuEntrance();
                    }
                );
            }
            else
            {
                ShowHomeScreenDirect();
                PlayStaggeredMenuEntrance();
            }
        }

        /// <summary>
        /// Prepares the four menu buttons by positioning them slightly below and setting alpha to 0.
        /// </summary>
        private void PrepareButtonsForStaggeredEntrance()
        {
            if (staggeredEntranceCoroutine != null)
            {
                StopCoroutine(staggeredEntranceCoroutine);
                staggeredEntranceCoroutine = null;
            }

            for (int i = 0; i < menuButtons.Count; i++)
            {
                var b = menuButtons[i];
                b.rect.anchoredPosition = new Vector2(b.targetPos.x, b.targetPos.y - buttonUpwardOffset);
                b.canvasGroup.alpha = 0f;
                b.canvasGroup.interactable = false;
                b.canvasGroup.blocksRaycasts = false;
                if (b.button != null) b.button.interactable = false;
            }
        }

        /// <summary>
        /// Plays sequential upward entrance animation for Play, Options, Credits, Exit Game.
        /// </summary>
        public void PlayStaggeredMenuEntrance()
        {
            if (staggeredEntranceCoroutine != null)
            {
                StopCoroutine(staggeredEntranceCoroutine);
            }
            staggeredEntranceCoroutine = StartCoroutine(StaggeredEntranceRoutine());
        }

        private IEnumerator StaggeredEntranceRoutine()
        {
            // Ensure initial offset
            PrepareButtonsForStaggeredEntrance();

            for (int i = 0; i < menuButtons.Count; i++)
            {
                int idx = i;
                StartCoroutine(AnimateSingleButton(menuButtons[idx]));
                yield return new WaitForSecondsRealtime(buttonDelay);
            }

            // Wait until the last button finishes
            float remaining = Mathf.Max(0f, buttonDuration - buttonDelay);
            yield return new WaitForSecondsRealtime(remaining);

            // Re-enable interactions
            for (int i = 0; i < menuButtons.Count; i++)
            {
                var b = menuButtons[i];
                b.rect.anchoredPosition = b.targetPos;
                b.canvasGroup.alpha = 1f;
                b.canvasGroup.interactable = true;
                b.canvasGroup.blocksRaycasts = true;
                if (b.button != null) b.button.interactable = true;
            }

            staggeredEntranceCoroutine = null;
        }

        private IEnumerator AnimateSingleButton(ButtonAnimState b)
        {
            Vector2 startPos = new Vector2(b.targetPos.x, b.targetPos.y - buttonUpwardOffset);
            Vector2 endPos = b.targetPos;

            float elapsed = 0f;
            while (elapsed < buttonDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / buttonDuration);
                // Ease Out Cubic
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);

                b.rect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, easeOut);
                b.canvasGroup.alpha = easeOut;
                yield return null;
            }

            b.rect.anchoredPosition = endPos;
            b.canvasGroup.alpha = 1f;
            b.canvasGroup.interactable = true;
            b.canvasGroup.blocksRaycasts = true;
            if (b.button != null) b.button.interactable = true;
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
        /// Activates Credits panel with smooth white fade transition.
        /// </summary>
        public void ShowCredits()
        {
            if (ScreenFadeTransition.Instance != null)
            {
                ScreenFadeTransition.Instance.DoTransition(
                    onCovered: () =>
                    {
                        if (homeScreenPanel != null) homeScreenPanel.SetActive(false);
                        if (creditsPanel != null) creditsPanel.SetActive(true);
                        if (settingsOptionsPanel != null) settingsOptionsPanel.SetActive(false);
                    }
                );
            }
            else
            {
                if (homeScreenPanel != null) homeScreenPanel.SetActive(false);
                if (creditsPanel != null) creditsPanel.SetActive(true);
                if (settingsOptionsPanel != null) settingsOptionsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Activates Settings-Options panel with smooth white fade transition.
        /// </summary>
        public void ShowSettingsOptions()
        {
            if (ScreenFadeTransition.Instance != null)
            {
                ScreenFadeTransition.Instance.DoTransition(
                    onCovered: () =>
                    {
                        if (homeScreenPanel != null) homeScreenPanel.SetActive(false);
                        if (creditsPanel != null) creditsPanel.SetActive(false);
                        if (settingsOptionsPanel != null) settingsOptionsPanel.SetActive(true);
                    }
                );
            }
            else
            {
                if (homeScreenPanel != null) homeScreenPanel.SetActive(false);
                if (creditsPanel != null) creditsPanel.SetActive(false);
                if (settingsOptionsPanel != null) settingsOptionsPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Handler for Play Button click. Fades out to pure white, then loads the target gameplay scene.
        /// </summary>
        public void OnPlayButtonClicked()
        {
            Debug.Log($"[HomeScreenUIManager] Play button clicked. Loading scene '{gameplaySceneName}' with white fade...");
            if (ScreenFadeTransition.Instance != null)
            {
                ScreenFadeTransition.Instance.DoTransition(
                    onCovered: () =>
                    {
                        if (!string.IsNullOrEmpty(gameplaySceneName) && Application.CanStreamedLevelBeLoaded(gameplaySceneName))
                        {
                            SceneManager.LoadScene(gameplaySceneName);
                        }
                        else
                        {
                            Debug.LogWarning($"[HomeScreenUIManager] Scene '{gameplaySceneName}' is not loaded or not in Build Settings.");
                        }
                    }
                );
            }
            else
            {
                if (!string.IsNullOrEmpty(gameplaySceneName) && Application.CanStreamedLevelBeLoaded(gameplaySceneName))
                {
                    SceneManager.LoadScene(gameplaySceneName);
                }
            }
        }

        /// <summary>
        /// Handler for Exit Game Button click. Smooth white fade-out before quitting.
        /// </summary>
        public void OnExitGameClicked()
        {
            Debug.Log("[HomeScreenUIManager] Exit Game clicked with white fade.");
            if (ScreenFadeTransition.Instance != null)
            {
                ScreenFadeTransition.Instance.DoExitTransition(() =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        /// <summary>
        /// Handler for Logout Button click with white fade transition.
        /// </summary>
        public void OnLogoutButtonClicked()
        {
            Debug.Log("[HomeScreenUIManager] Logout button clicked with white fade.");
            if (ScreenFadeTransition.Instance != null)
            {
                ScreenFadeTransition.Instance.DoTransition(
                    onCovered: () =>
                    {
                        PerformLogout();
                        SceneManager.LoadScene("LoginPage");
                    }
                );
            }
            else
            {
                PerformLogout();
                SceneManager.LoadScene("LoginPage");
            }
        }

        private void PerformLogout()
        {
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
