using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button backToMenuButton;

    [Header("Sub Panels")]
    [SerializeField] private GameObject mainPauseContent;
    [SerializeField] private GameObject settingsPanel;

    [Header("Player References")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private StarterAssetsInputs playerInputs;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "HomeScreen";

    public bool IsPaused { get; private set; } = false;

    private Coroutine pauseVisibilityCoroutine;
    private Coroutine subPanelCoroutine;
    private GameObject currentSubPanel;
    private bool isNavigatingToMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        IsPaused = false;
        isNavigatingToMenu = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            Utsuru.UI.UIPanelAnimator.AttachButtonFeedbackInChildren(pausePanel);
            pausePanel.SetActive(false);
        }

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }
    }

    private void Start()
    {
        if (!IsPaused)
        {
            Time.timeScale = 1f;
        }

        if (playerController == null)
            playerController = Object.FindAnyObjectByType<FirstPersonController>();

        if (playerInputs == null)
            playerInputs = Object.FindAnyObjectByType<StarterAssetsInputs>();
    }

    private void Update()
    {
        bool escPressed = Input.GetKeyDown(KeyCode.Escape);

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            escPressed = true;
#endif

        if (escPressed)
        {
            // Jangan buka pause menu jika pemain sedang berada di dalam dialog NPC atau menu Achievements
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                return;

            if (Utsuru.Achievements.AchievementUIController.Instance != null && Utsuru.Achievements.AchievementUIController.Instance.IsOpen)
                return;

            if (Utsuru.Store.StoreUIController.Instance != null && Utsuru.Store.StoreUIController.Instance.IsOpen)
                return;

            // Jika sedang membuka panel Settings, tekan ESC akan kembali ke Main Pause Content
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettingsView();
                return;
            }

            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        IsPaused = true;
        isNavigatingToMenu = false;

        if (pauseVisibilityCoroutine != null)
        {
            StopCoroutine(pauseVisibilityCoroutine);
            pauseVisibilityCoroutine = null;
        }
        if (subPanelCoroutine != null)
        {
            StopCoroutine(subPanelCoroutine);
            subPanelCoroutine = null;
        }

        if (mainPauseContent != null)
        {
            Utsuru.UI.UIPanelAnimator.ResetPanelVisuals(mainPauseContent, 1f);
            mainPauseContent.SetActive(true);
        }

        if (settingsPanel != null)
        {
            Utsuru.UI.UIPanelAnimator.ResetPanelVisuals(settingsPanel, 1f);
            settingsPanel.SetActive(false);
        }

        currentSubPanel = mainPauseContent;

        if (pausePanel != null)
        {
            pauseVisibilityCoroutine = StartCoroutine(PlayPauseOpenRoutine());
        }

        Time.timeScale = 0f;

        if (playerController != null)
            playerController.enabled = false;

        if (playerInputs != null)
        {
            playerInputs.MoveInput(Vector2.zero);
            playerInputs.cursorLocked = false;
            playerInputs.cursorInputForLook = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        IsPaused = false;

        if (pauseVisibilityCoroutine != null)
        {
            StopCoroutine(pauseVisibilityCoroutine);
            pauseVisibilityCoroutine = null;
        }
        if (subPanelCoroutine != null)
        {
            StopCoroutine(subPanelCoroutine);
            subPanelCoroutine = null;
        }

        Time.timeScale = 1f;

        if (playerController != null)
            playerController.enabled = true;

        if (playerInputs != null)
        {
            playerInputs.cursorLocked = true;
            playerInputs.cursorInputForLook = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pausePanel != null && pausePanel.activeSelf)
        {
            pauseVisibilityCoroutine = StartCoroutine(PlayPauseCloseRoutine());
        }
        else if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OpenSettingsView()
    {
        if (settingsPanel == null) return;
        if (currentSubPanel == settingsPanel && settingsPanel.activeSelf) return;

        SwitchPauseSubPanel(mainPauseContent, settingsPanel, slideDirection: 1);
    }

    public void CloseSettingsView()
    {
        if (mainPauseContent == null) return;
        if (currentSubPanel == mainPauseContent && mainPauseContent.activeSelf) return;

        SwitchPauseSubPanel(settingsPanel, mainPauseContent, slideDirection: -1);
    }

    private void SwitchPauseSubPanel(GameObject fromPanel, GameObject toPanel, int slideDirection)
    {
        if (subPanelCoroutine != null)
        {
            StopCoroutine(subPanelCoroutine);
            subPanelCoroutine = null;
        }

        GameObject[] subPanels = new GameObject[] { mainPauseContent, settingsPanel };
        currentSubPanel = toPanel;
        subPanelCoroutine = StartCoroutine(RunPauseSubPanelSwitch(fromPanel, toPanel, subPanels, slideDirection));
    }

    private System.Collections.IEnumerator PlayPauseOpenRoutine()
    {
        yield return Utsuru.UI.UIPanelAnimator.PlayPanelEntranceRoutine(pausePanel, 0.20f);
        pauseVisibilityCoroutine = null;
    }

    private System.Collections.IEnumerator PlayPauseCloseRoutine()
    {
        yield return Utsuru.UI.UIPanelAnimator.PlayPanelExitRoutine(pausePanel, 0.16f);
        if (settingsPanel != null)
        {
            Utsuru.UI.UIPanelAnimator.ResetPanelVisuals(settingsPanel, 1f);
            settingsPanel.SetActive(false);
        }
        pauseVisibilityCoroutine = null;
    }

    private System.Collections.IEnumerator RunPauseSubPanelSwitch(GameObject fromPanel, GameObject toPanel, GameObject[] subPanels, int direction)
    {
        yield return Utsuru.UI.UIPanelAnimator.SwitchPanelRoutine(fromPanel, toPanel, subPanels, direction);
        subPanelCoroutine = null;
    }

    public void GoToMainMenu()
    {
        OnBackToMenuClicked();
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[PauseMenuManager] Tombol 'Settings' diklik!");
        OpenSettingsView();
    }

    private void OnBackToMenuClicked()
    {
        if (isNavigatingToMenu) return;
        isNavigatingToMenu = true;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pausePanel != null)
        {
            CanvasGroup cg = Utsuru.UI.UIPanelAnimator.EnsureCanvasGroup(pausePanel);
            cg.blocksRaycasts = false;
        }
        Debug.Log("[PauseMenuManager] Tombol 'Back To Menu' diklik! Memuat HomeScreen...");

        try
        {
            string target = Application.CanStreamedLevelBeLoaded(mainMenuSceneName) ? mainMenuSceneName : "HomeScreen";
            Utsuru.UI.UISceneFader.LoadSceneWithFade(target);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PauseMenuManager] Gagal memuat scene '{mainMenuSceneName}': {ex.Message}. Kembali ke HomeScreen.");
            SceneManager.LoadScene("HomeScreen");
        }
    }
}
