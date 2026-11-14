using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Navigation Tabs")]
    [SerializeField] private Button tabGameplayBtn;
    [SerializeField] private Button tabControlsBtn;
    [SerializeField] private Button tabGraphicsBtn;
    [SerializeField] private Button tabAudioBtn;

    [SerializeField] private TMP_Text tabGameplayText;
    [SerializeField] private TMP_Text tabControlsText;
    [SerializeField] private TMP_Text tabGraphicsText;
    [SerializeField] private TMP_Text tabAudioText;

    [Header("Tab Panels")]
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject audioPanel;

    [Header("Action Buttons")]
    [SerializeField] private Button applyBtn;
    [SerializeField] private Button backBtn;
    [SerializeField] private Button backToTitleBtn;

    [Header("Gameplay Controls")]
    [SerializeField] private Button charSetPrevBtn;
    [SerializeField] private Button charSetNextBtn;
    [SerializeField] private TMP_Text charSetValueText;
    [SerializeField] private Button hideEnglishToggleBtn;
    [SerializeField] private Image hideEnglishDotImg;
    [SerializeField] private Slider fovSlider;
    [SerializeField] private TMP_Text fovValueText;
    [SerializeField] private Button disableOutlineToggleBtn;
    [SerializeField] private Image disableOutlineDotImg;

    [Header("Controls Controls")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValueText;
    [SerializeField] private Button invertXToggleBtn;
    [SerializeField] private Image invertXDotImg;
    [SerializeField] private Button invertYToggleBtn;
    [SerializeField] private Image invertYDotImg;

    [Header("Graphics Controls")]
    [SerializeField] private Button displayModePrevBtn;
    [SerializeField] private Button displayModeNextBtn;
    [SerializeField] private TMP_Text displayModeValueText;

    [SerializeField] private Button resolutionPrevBtn;
    [SerializeField] private Button resolutionNextBtn;
    [SerializeField] private TMP_Text resolutionValueText;

    [SerializeField] private Button qualityPrevBtn;
    [SerializeField] private Button qualityNextBtn;
    [SerializeField] private TMP_Text qualityValueText;

    [SerializeField] private Button fpsLimitPrevBtn;
    [SerializeField] private Button fpsLimitNextBtn;
    [SerializeField] private TMP_Text fpsLimitValueText;

    [SerializeField] private Button aaPrevBtn;
    [SerializeField] private Button aaNextBtn;
    [SerializeField] private TMP_Text aaValueText;

    [SerializeField] private Button vsyncToggleBtn;
    [SerializeField] private Image vsyncDotImg;

    [Header("Audio Controls")]
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private TMP_Text masterVolValueText;
    [SerializeField] private Slider musicVolSlider;
    [SerializeField] private TMP_Text musicVolValueText;
    [SerializeField] private Slider voiceVolSlider;
    [SerializeField] private TMP_Text voiceVolValueText;

    [Header("References")]
    [SerializeField] private FirstPersonController playerController;

    // Data options
    private readonly string[] charSetOptions = { "kanji + furigana", "hiragana only", "romaji only" };
    private readonly string[] displayModeOptions = { "Full Screen", "Windowed", "Borderless" };
    private readonly string[] resolutionOptions = { "1920 x 1080", "1600 x 900", "1366 x 768", "1280 x 720", "2560 x 1440" };
    private readonly string[] qualityOptions = { "Low", "Medium", "High", "Epic" };
    private readonly string[] fpsLimitOptions = { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "Unlimited" };
    private readonly int[] fpsLimitValues = { 30, 60, 120, 144, -1 };
    private readonly string[] aaOptions = { "Off", "FXAA", "SMAA", "TSR" };

    // Current State
    private int curCharSetIndex = 0;
    private bool curHideEnglish = false;
    private float curFov = 90f;
    private bool curDisableOutline = false;

    private float curSensitivity = 90f;
    private bool curInvertX = false;
    private bool curInvertY = false;

    private int curDisplayModeIndex = 0;
    private int curResolutionIndex = 0;
    private int curQualityIndex = 3; // Epic
    private int curFpsLimitIndex = 4; // Unlimited
    private int curAaIndex = 3; // TSR
    private bool curVsync = false;

    private float curMasterVol = 90f;
    private float curMusicVol = 100f;
    private float curVoiceVol = 75f;

    private float lastVoicePreviewTime = 0f;

    // Visual colors
    private readonly Color activeTabColor = new Color32(106, 67, 47, 255);    // #6A432F
    private readonly Color inactiveTabColor = new Color32(165, 138, 125, 255); // #A58A7D
    private readonly Color toggleOnColor = new Color32(106, 67, 47, 255);     // #6A432F
    private readonly Color toggleOffColor = new Color32(213, 199, 188, 255); // Soft grayish-cream dot

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadSettings();
        SetupListeners();
        SwitchTab(0); // Default Gameplay tab
        ApplySettings();
    }

    private void Start()
    {
        if (playerController == null)
            playerController = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>(FindObjectsInactive.Include);

        ApplyControlsSettings();
        ApplyFOV(curFov);
    }

    private void SetupListeners()
    {
        // Tabs
        if (tabGameplayBtn != null) tabGameplayBtn.onClick.AddListener(() => SwitchTab(0));
        if (tabControlsBtn != null) tabControlsBtn.onClick.AddListener(() => SwitchTab(1));
        if (tabGraphicsBtn != null) tabGraphicsBtn.onClick.AddListener(() => SwitchTab(2));
        if (tabAudioBtn != null) tabAudioBtn.onClick.AddListener(() => SwitchTab(3));

        // Action Buttons
        if (applyBtn != null) applyBtn.onClick.AddListener(OnApplyClicked);
        if (backBtn != null) backBtn.onClick.AddListener(OnBackClicked);
        if (backToTitleBtn != null) backToTitleBtn.onClick.AddListener(OnBackToTitleClicked);

        // Gameplay
        if (charSetPrevBtn != null) charSetPrevBtn.onClick.AddListener(() => CycleCharSet(-1));
        if (charSetNextBtn != null) charSetNextBtn.onClick.AddListener(() => CycleCharSet(1));
        if (hideEnglishToggleBtn != null) hideEnglishToggleBtn.onClick.AddListener(() => ToggleHideEnglish());
        if (fovSlider != null) fovSlider.onValueChanged.AddListener(OnFovChanged);
        if (disableOutlineToggleBtn != null) disableOutlineToggleBtn.onClick.AddListener(() => ToggleDisableOutline());

        // Controls
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (invertXToggleBtn != null) invertXToggleBtn.onClick.AddListener(() => ToggleInvertX());
        if (invertYToggleBtn != null) invertYToggleBtn.onClick.AddListener(() => ToggleInvertY());

        // Graphics
        if (displayModePrevBtn != null) displayModePrevBtn.onClick.AddListener(() => CycleDisplayMode(-1));
        if (displayModeNextBtn != null) displayModeNextBtn.onClick.AddListener(() => CycleDisplayMode(1));
        if (resolutionPrevBtn != null) resolutionPrevBtn.onClick.AddListener(() => CycleResolution(-1));
        if (resolutionNextBtn != null) resolutionNextBtn.onClick.AddListener(() => CycleResolution(1));
        if (qualityPrevBtn != null) qualityPrevBtn.onClick.AddListener(() => CycleQuality(-1));
        if (qualityNextBtn != null) qualityNextBtn.onClick.AddListener(() => CycleQuality(1));
        if (fpsLimitPrevBtn != null) fpsLimitPrevBtn.onClick.AddListener(() => CycleFpsLimit(-1));
        if (fpsLimitNextBtn != null) fpsLimitNextBtn.onClick.AddListener(() => CycleFpsLimit(1));
        if (aaPrevBtn != null) aaPrevBtn.onClick.AddListener(() => CycleAA(-1));
        if (aaNextBtn != null) aaNextBtn.onClick.AddListener(() => CycleAA(1));
        if (vsyncToggleBtn != null) vsyncToggleBtn.onClick.AddListener(() => ToggleVsync());

        // Audio
        if (masterVolSlider != null) masterVolSlider.onValueChanged.AddListener(OnMasterVolChanged);
        if (musicVolSlider != null) musicVolSlider.onValueChanged.AddListener(OnMusicVolChanged);
        if (voiceVolSlider != null) voiceVolSlider.onValueChanged.AddListener(OnVoiceVolChanged);
    }

    public void SwitchTab(int tabIndex)
    {
        Debug.Log($"[SettingsManager] Switch tab to: {tabIndex}");
        if (gameplayPanel != null) gameplayPanel.SetActive(tabIndex == 0);
        if (controlsPanel != null) controlsPanel.SetActive(tabIndex == 1);
        if (graphicsPanel != null) graphicsPanel.SetActive(tabIndex == 2);
        if (audioPanel != null) audioPanel.SetActive(tabIndex == 3);

        UpdateTabVisuals(tabIndex);
    }

    private void UpdateTabVisuals(int activeTab)
    {
        SetTabText(tabGameplayText, activeTab == 0);
        SetTabText(tabControlsText, activeTab == 1);
        SetTabText(tabGraphicsText, activeTab == 2);
        SetTabText(tabAudioText, activeTab == 3);
    }

    private void SetTabText(TMP_Text text, bool isActive)
    {
        if (text == null) return;
        text.color = isActive ? activeTabColor : inactiveTabColor;
        text.fontStyle = FontStyles.Normal; // Tetap Normal agar font Baloo tidak rusak/terdistorsi saat dipilih
    }

    // --- State Handlers ---

    private void CycleCharSet(int dir)
    {
        curCharSetIndex = (curCharSetIndex + dir + charSetOptions.Length) % charSetOptions.Length;
        if (charSetValueText != null) charSetValueText.text = charSetOptions[curCharSetIndex];
        PlayerPrefs.SetInt("Setting_CharacterSet", curCharSetIndex);
        Debug.Log($"[SettingsManager] Character Set diubah ke: {charSetOptions[curCharSetIndex]}");
    }

    private void ToggleHideEnglish()
    {
        curHideEnglish = !curHideEnglish;
        UpdateToggleDot(hideEnglishDotImg, curHideEnglish);
        PlayerPrefs.SetInt("Setting_HideEnglish", curHideEnglish ? 1 : 0);
        if (DialogueManager.Instance != null)
        {
            DialogueManager.HideEnglishTranslation = curHideEnglish;
            DialogueManager.Instance.UpdateSettingsVisibility();
        }
    }

    private void OnFovChanged(float val)
    {
        curFov = Mathf.Round(val);
        if (fovValueText != null) fovValueText.text = curFov.ToString("0");
        PlayerPrefs.SetFloat("Setting_FOV", curFov);
        ApplyFOV(curFov);
    }

    private void ToggleDisableOutline()
    {
        curDisableOutline = !curDisableOutline;
        UpdateToggleDot(disableOutlineDotImg, curDisableOutline);
        PlayerPrefs.SetInt("Setting_DisableOutline", curDisableOutline ? 1 : 0);
    }

    private void OnSensitivityChanged(float val)
    {
        curSensitivity = Mathf.Round(val);
        if (sensitivityValueText != null) sensitivityValueText.text = curSensitivity.ToString("0");
        PlayerPrefs.SetFloat("Setting_Sensitivity", curSensitivity);
        ApplyControlsSettings();
    }

    private void ToggleInvertX()
    {
        curInvertX = !curInvertX;
        UpdateToggleDot(invertXDotImg, curInvertX);
        PlayerPrefs.SetInt("Setting_InvertX", curInvertX ? 1 : 0);
        ApplyControlsSettings();
    }

    private void ToggleInvertY()
    {
        curInvertY = !curInvertY;
        UpdateToggleDot(invertYDotImg, curInvertY);
        PlayerPrefs.SetInt("Setting_InvertY", curInvertY ? 1 : 0);
        ApplyControlsSettings();
    }

    private void CycleDisplayMode(int dir)
    {
        curDisplayModeIndex = (curDisplayModeIndex + dir + displayModeOptions.Length) % displayModeOptions.Length;
        if (displayModeValueText != null) displayModeValueText.text = displayModeOptions[curDisplayModeIndex];
        PlayerPrefs.SetInt("Setting_DisplayMode", curDisplayModeIndex);
    }

    private void CycleResolution(int dir)
    {
        curResolutionIndex = (curResolutionIndex + dir + resolutionOptions.Length) % resolutionOptions.Length;
        if (resolutionValueText != null) resolutionValueText.text = resolutionOptions[curResolutionIndex];
        PlayerPrefs.SetInt("Setting_Resolution", curResolutionIndex);
    }

    private void CycleQuality(int dir)
    {
        curQualityIndex = (curQualityIndex + dir + qualityOptions.Length) % qualityOptions.Length;
        if (qualityValueText != null) qualityValueText.text = qualityOptions[curQualityIndex];
        PlayerPrefs.SetInt("Setting_Quality", curQualityIndex);
        ApplyGraphicsQuality();
    }

    private void CycleFpsLimit(int dir)
    {
        curFpsLimitIndex = (curFpsLimitIndex + dir + fpsLimitOptions.Length) % fpsLimitOptions.Length;
        if (fpsLimitValueText != null) fpsLimitValueText.text = fpsLimitOptions[curFpsLimitIndex];
        PlayerPrefs.SetInt("Setting_FPSLimit", curFpsLimitIndex);
        Application.targetFrameRate = fpsLimitValues[curFpsLimitIndex];
    }

    private void CycleAA(int dir)
    {
        curAaIndex = (curAaIndex + dir + aaOptions.Length) % aaOptions.Length;
        if (aaValueText != null) aaValueText.text = aaOptions[curAaIndex];
        PlayerPrefs.SetInt("Setting_AA", curAaIndex);
        ApplyGraphicsQuality();
    }

    private void ToggleVsync()
    {
        curVsync = !curVsync;
        UpdateToggleDot(vsyncDotImg, curVsync);
        PlayerPrefs.SetInt("Setting_VSync", curVsync ? 1 : 0);
        QualitySettings.vSyncCount = curVsync ? 1 : 0;
    }

    private void OnMasterVolChanged(float val)
    {
        curMasterVol = Mathf.Round(val);
        if (masterVolValueText != null) masterVolValueText.text = curMasterVol.ToString("0");
        PlayerPrefs.SetFloat("Setting_MasterVol", curMasterVol);
        AudioListener.volume = curMasterVol / 100f;
    }

    private void OnMusicVolChanged(float val)
    {
        curMusicVol = Mathf.Round(val);
        if (musicVolValueText != null) musicVolValueText.text = curMusicVol.ToString("0");
        PlayerPrefs.SetFloat("Setting_MusicVol", curMusicVol);
    }

    private void OnVoiceVolChanged(float val)
    {
        curVoiceVol = Mathf.Round(val);
        if (voiceVolValueText != null) voiceVolValueText.text = curVoiceVol.ToString("0");
        PlayerPrefs.SetFloat("Setting_VoiceVol", curVoiceVol);

        if (DialogueManager.Instance != null && DialogueManager.Instance.VoiceAudioSource != null)
        {
            DialogueManager.Instance.VoiceAudioSource.volume = curVoiceVol / 100f;
            if (Time.unscaledTime - lastVoicePreviewTime > 0.2f && DialogueManager.Instance.VoiceAudioSource.clip != null)
            {
                lastVoicePreviewTime = Time.unscaledTime;
                DialogueManager.Instance.VoiceAudioSource.PlayOneShot(DialogueManager.Instance.VoiceAudioSource.clip, curVoiceVol / 100f);
            }
        }
    }

    private void UpdateToggleDot(Image dot, bool isOn)
    {
        if (dot != null) dot.color = isOn ? toggleOnColor : toggleOffColor;
    }

    // --- Apply & Save Logic ---

    public void OnApplyClicked()
    {
        SaveSettings();
        ApplySettings();
        Debug.Log("<color=green>[SettingsManager] PENGATURAN BERHASIL DISIMPAN & DITERAPKAN KE GAME!</color>");
    }

    public void OnBackClicked()
    {
        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.CloseSettingsView();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OnBackToTitleClicked()
    {
        Time.timeScale = 1f;
        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.GoToMainMenu();
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public void ApplySettings()
    {
        // 1. Audio
        AudioListener.volume = curMasterVol / 100f;
        if (DialogueManager.Instance != null && DialogueManager.Instance.VoiceAudioSource != null)
        {
            DialogueManager.Instance.VoiceAudioSource.volume = curVoiceVol / 100f;
        }

        // 2. Controls
        ApplyControlsSettings();

        // 3. FOV (Cinemachine Camera)
        ApplyFOV(curFov);

        // 4. Graphics (Display Mode & Resolution)
        ApplyDisplayAndResolution();

        // 5. Graphics Quality, Anti-Aliasing & FPS
        ApplyGraphicsQuality();

        // 6. Dialogue Manager
        if (DialogueManager.Instance != null)
        {
            DialogueManager.HideEnglishTranslation = curHideEnglish;
            DialogueManager.Instance.UpdateSettingsVisibility();
        }
    }

    private void ApplyControlsSettings()
    {
        if (playerController == null)
            playerController = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>(FindObjectsInactive.Include);

        if (playerController != null)
        {
            playerController.RotationSpeed = (curSensitivity / 90f) * 1.0f;
            playerController.InvertX = curInvertX;
            playerController.InvertY = curInvertY;
            Debug.Log($"[SettingsManager] Player sensitivity: {curSensitivity}, InvertX: {curInvertX}, InvertY: {curInvertY}");
        }
    }

    private void ApplyFOV(float fov)
    {
        curFov = fov;

        if (Camera.main != null)
            Camera.main.fieldOfView = fov;

        var vcam = UnityEngine.Object.FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
        if (vcam != null)
        {
            var lens = vcam.Lens;
            lens.FieldOfView = fov;
            vcam.Lens = lens;
            Debug.Log($"[SettingsManager] Cinemachine FOV diubah ke: {fov}");
        }

        if (Utsuru.Capture.CameraModeController.Instance != null)
        {
            Utsuru.Capture.CameraModeController.Instance.SetNormalFov(fov);
        }

        var zoomAdapters = UnityEngine.Object.FindObjectsByType<Utsuru.Capture.CinemachineZoomAdapter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < zoomAdapters.Length; i++)
        {
            if (zoomAdapters[i] != null) zoomAdapters[i].SetNormalFov(fov);
        }
    }

    private void ApplyDisplayAndResolution()
    {
        FullScreenMode mode = FullScreenMode.FullScreenWindow;
        if (curDisplayModeIndex == 1) mode = FullScreenMode.Windowed;
        else if (curDisplayModeIndex == 2) mode = FullScreenMode.FullScreenWindow;
        else mode = FullScreenMode.ExclusiveFullScreen;

        string resStr = resolutionOptions[curResolutionIndex];
        string[] parts = resStr.Split('x');
        if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int w) && int.TryParse(parts[1].Trim(), out int h))
        {
            Screen.SetResolution(w, h, mode);
        }
    }

    private void ApplyGraphicsQuality()
    {
        QualitySettings.SetQualityLevel(Mathf.Clamp(curQualityIndex, 0, QualitySettings.names.Length - 1), true);
        QualitySettings.vSyncCount = curVsync ? 1 : 0;
        Application.targetFrameRate = fpsLimitValues[curFpsLimitIndex];

        // Terapkan Shadow Distance & LOD Bias nyata agar perbedaan grafis langsung terlihat jelas
        switch (curQualityIndex)
        {
            case 0: // Low
                QualitySettings.shadowDistance = 20f;
                QualitySettings.lodBias = 0.5f;
                break;
            case 1: // Medium
                QualitySettings.shadowDistance = 50f;
                QualitySettings.lodBias = 0.8f;
                break;
            case 2: // High
                QualitySettings.shadowDistance = 90f;
                QualitySettings.lodBias = 1.0f;
                break;
            case 3: // Epic
                QualitySettings.shadowDistance = 160f;
                QualitySettings.lodBias = 1.5f;
                break;
        }

        // Terapkan Anti-Aliasing URP langsung pada Camera
        if (Camera.main != null)
        {
            var camData = Camera.main.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                switch (curAaIndex)
                {
                    case 0: camData.antialiasing = AntialiasingMode.None; break;
                    case 1: camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing; break;
                    case 2: camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; break;
                    case 3: camData.antialiasing = AntialiasingMode.TemporalAntiAliasing; break;
                }
            }
        }
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt("Setting_CharacterSet", curCharSetIndex);
        PlayerPrefs.SetInt("Setting_HideEnglish", curHideEnglish ? 1 : 0);
        PlayerPrefs.SetFloat("Setting_FOV", curFov);
        PlayerPrefs.SetInt("Setting_DisableOutline", curDisableOutline ? 1 : 0);

        PlayerPrefs.SetFloat("Setting_Sensitivity", curSensitivity);
        PlayerPrefs.SetInt("Setting_InvertX", curInvertX ? 1 : 0);
        PlayerPrefs.SetInt("Setting_InvertY", curInvertY ? 1 : 0);

        PlayerPrefs.SetInt("Setting_DisplayMode", curDisplayModeIndex);
        PlayerPrefs.SetInt("Setting_Resolution", curResolutionIndex);
        PlayerPrefs.SetInt("Setting_Quality", curQualityIndex);
        PlayerPrefs.SetInt("Setting_FPSLimit", curFpsLimitIndex);
        PlayerPrefs.SetInt("Setting_AA", curAaIndex);
        PlayerPrefs.SetInt("Setting_VSync", curVsync ? 1 : 0);

        PlayerPrefs.SetFloat("Setting_MasterVol", curMasterVol);
        PlayerPrefs.SetFloat("Setting_MusicVol", curMusicVol);
        PlayerPrefs.SetFloat("Setting_VoiceVol", curVoiceVol);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        curCharSetIndex = PlayerPrefs.GetInt("Setting_CharacterSet", 0);
        curHideEnglish = PlayerPrefs.GetInt("Setting_HideEnglish", 0) == 1;
        curFov = PlayerPrefs.GetFloat("Setting_FOV", 90f);
        curDisableOutline = PlayerPrefs.GetInt("Setting_DisableOutline", 0) == 1;

        curSensitivity = PlayerPrefs.GetFloat("Setting_Sensitivity", 90f);
        curInvertX = PlayerPrefs.GetInt("Setting_InvertX", 0) == 1;
        curInvertY = PlayerPrefs.GetInt("Setting_InvertY", 0) == 1;

        curDisplayModeIndex = PlayerPrefs.GetInt("Setting_DisplayMode", 0);
        curResolutionIndex = PlayerPrefs.GetInt("Setting_Resolution", 0);
        curQualityIndex = PlayerPrefs.GetInt("Setting_Quality", 3);
        curFpsLimitIndex = PlayerPrefs.GetInt("Setting_FPSLimit", 4);
        curAaIndex = PlayerPrefs.GetInt("Setting_AA", 3);
        curVsync = PlayerPrefs.GetInt("Setting_VSync", 0) == 1;

        curMasterVol = PlayerPrefs.GetFloat("Setting_MasterVol", 90f);
        curMusicVol = PlayerPrefs.GetFloat("Setting_MusicVol", 100f);
        curVoiceVol = PlayerPrefs.GetFloat("Setting_VoiceVol", 75f);

        UpdateAllUIElements();
    }

    private void UpdateAllUIElements()
    {
        if (charSetValueText != null) charSetValueText.text = charSetOptions[curCharSetIndex];
        UpdateToggleDot(hideEnglishDotImg, curHideEnglish);
        if (fovSlider != null) fovSlider.value = curFov;
        if (fovValueText != null) fovValueText.text = curFov.ToString("0");
        UpdateToggleDot(disableOutlineDotImg, curDisableOutline);

        if (sensitivitySlider != null) sensitivitySlider.value = curSensitivity;
        if (sensitivityValueText != null) sensitivityValueText.text = curSensitivity.ToString("0");
        UpdateToggleDot(invertXDotImg, curInvertX);
        UpdateToggleDot(invertYDotImg, curInvertY);

        if (displayModeValueText != null) displayModeValueText.text = displayModeOptions[curDisplayModeIndex];
        if (resolutionValueText != null) resolutionValueText.text = resolutionOptions[curResolutionIndex];
        if (qualityValueText != null) qualityValueText.text = qualityOptions[curQualityIndex];
        if (fpsLimitValueText != null) fpsLimitValueText.text = fpsLimitOptions[curFpsLimitIndex];
        if (aaValueText != null) aaValueText.text = aaOptions[curAaIndex];
        UpdateToggleDot(vsyncDotImg, curVsync);

        if (masterVolSlider != null) masterVolSlider.value = curMasterVol;
        if (masterVolValueText != null) masterVolValueText.text = curMasterVol.ToString("0");
        if (musicVolSlider != null) musicVolSlider.value = curMusicVol;
        if (musicVolValueText != null) musicVolValueText.text = curMusicVol.ToString("0");
        if (voiceVolSlider != null) voiceVolSlider.value = curVoiceVol;
        if (voiceVolValueText != null) voiceVolValueText.text = curVoiceVol.ToString("0");
    }
}
