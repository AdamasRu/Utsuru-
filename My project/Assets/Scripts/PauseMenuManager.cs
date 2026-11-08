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
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (pausePanel != null)
            pausePanel.SetActive(false);

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
            // Jangan buka pause menu jika pemain sedang berada di dalam dialog NPC
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
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

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (mainPauseContent != null)
            mainPauseContent.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

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

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

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
    }

    public void OpenSettingsView()
    {
        if (mainPauseContent != null)
            mainPauseContent.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettingsView()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainPauseContent != null)
            mainPauseContent.SetActive(true);
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
        Time.timeScale = 1f;
        Debug.Log("[PauseMenuManager] Tombol 'Back To Menu' diklik!");

        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning($"[PauseMenuManager] Scene '{mainMenuSceneName}' belum ada di Build Settings. Kembali ke scene index 0.");
            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                SceneManager.LoadScene(0);
            }
        }
    }
}
