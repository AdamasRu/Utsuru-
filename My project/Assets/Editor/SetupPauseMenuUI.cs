#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using StarterAssets;
using Unity.Cinemachine;

public class SetupPauseMenuUI : Editor
{
    [MenuItem("Tools/Utsuru/Setup Pause Menu (ESC to Open)", priority = 2)]
    public static void GeneratePauseMenuUI()
    {
        // 1. Sprite & Font
        string logoPath = "Assets/UI/logoutsuru.png";
        ConfigureSpriteImporter(logoPath);
        Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);

        Sprite btnBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Button_Unselected.png")
                    ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Card_Choice2_Bg.png");

        Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Circle_Badge_Bg.png");

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito,Zen_Maru_Gothic/Nunito/static/Nunito-Bold SDF.asset")
                          ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito,Zen_Maru_Gothic/Zen_Maru_Gothic/ZenMaruGothic-Bold SDF.asset")
                          ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/YUGOTHB SDF.asset");

        // 2. Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SetupPauseMenuUI] Tidak ditemukan Canvas di Scene!");
            return;
        }

        // Hapus PauseMenuPanel lama jika ada agar bersih dan ter-update
        Transform oldPanel = canvas.transform.Find("PauseMenuPanel");
        if (oldPanel != null)
        {
            Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        Color32 darkBrown = new Color32(74, 46, 27, 255); // #4A2E1B
        Color32 mutedBrown = new Color32(140, 115, 98, 255); // #8C7362

        // 3. Panel Overlay Fullscreen
        GameObject pausePanelObj = new GameObject("PauseMenuPanel", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(pausePanelObj, "Create Pause Menu Panel");
        pausePanelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = pausePanelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image panelImg = pausePanelObj.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.08f, 0.06f, 0.65f); // Redup hangat #1E140F
        panelImg.raycastTarget = true;

        // ==========================================
        // A. MAIN PAUSE CONTENT (2 Tombol: Settings & Back To Menu)
        // ==========================================
        GameObject mainContentObj = new GameObject("MainPauseContent", typeof(RectTransform));
        mainContentObj.transform.SetParent(pausePanelObj.transform, false);
        RectTransform mcRt = mainContentObj.GetComponent<RectTransform>();
        mcRt.anchorMin = new Vector2(0.5f, 0.5f);
        mcRt.anchorMax = new Vector2(0.5f, 0.5f);
        mcRt.pivot = new Vector2(0.5f, 0.5f);
        mcRt.anchoredPosition = Vector2.zero;
        mcRt.sizeDelta = new Vector2(500f, 600f);

        CreateLogoObject(mainContentObj.transform, "UtsuruLogo", logoSprite, new Vector2(0f, 95f), new Vector2(395f, 202f));

        GameObject btnsGroupObj = new GameObject("ButtonsGroup", typeof(RectTransform));
        btnsGroupObj.transform.SetParent(mainContentObj.transform, false);
        RectTransform bgRt = btnsGroupObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = new Vector2(0f, -80f);
        bgRt.sizeDelta = new Vector2(400f, 160f);

        VerticalLayoutGroup vlg = btnsGroupObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;

        Button settingsBtn = CreatePillButton(btnsGroupObj.transform, "SettingsButton", "Settings", new Vector2(380f, 58f), btnBg, font, darkBrown, 24f);
        Button backToMenuBtn = CreatePillButton(btnsGroupObj.transform, "BackToMenuButton", "Back To Menu", new Vector2(380f, 58f), btnBg, font, darkBrown, 24f);

        // ==========================================
        // B. SETTINGS PANEL (Tampilan 4 Tab Lengkap Sesuai Figma)
        // ==========================================
        GameObject settingsPanelObj = new GameObject("SettingsPanel", typeof(RectTransform));
        settingsPanelObj.transform.SetParent(pausePanelObj.transform, false);
        RectTransform spRt = settingsPanelObj.GetComponent<RectTransform>();
        spRt.anchorMin = Vector2.zero;
        spRt.anchorMax = Vector2.one;
        spRt.offsetMin = Vector2.zero;
        spRt.offsetMax = Vector2.zero;

        Button backToTitleBtn = CreateTopLeftBackButton(settingsPanelObj.transform, "BackToTitleButton", "<  Back to Title", btnBg, font, darkBrown);

        GameObject scObj = new GameObject("SettingsCenterContent", typeof(RectTransform));
        scObj.transform.SetParent(settingsPanelObj.transform, false);
        RectTransform scRt = scObj.GetComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0.5f, 0.5f);
        scRt.anchorMax = new Vector2(0.5f, 0.5f);
        scRt.pivot = new Vector2(0.5f, 0.5f);
        scRt.anchoredPosition = new Vector2(0f, 20f);
        scRt.sizeDelta = new Vector2(1000f, 720f);

        CreateLogoObject(scObj.transform, "UtsuruLogo_Settings", logoSprite, new Vector2(0f, 260f), new Vector2(300f, 153f));

        // Tab Bar Kapsul (Gameplay | Controls | Graphics | Audio)
        GameObject tabBarObj = new GameObject("TabBar", typeof(RectTransform));
        tabBarObj.transform.SetParent(scObj.transform, false);
        RectTransform tbRt = tabBarObj.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0.5f, 0.5f);
        tbRt.anchorMax = new Vector2(0.5f, 0.5f);
        tbRt.pivot = new Vector2(0.5f, 0.5f);
        tbRt.anchoredPosition = new Vector2(0f, 150f);
        tbRt.sizeDelta = new Vector2(960f, 50f);

        Image tbImg = tabBarObj.AddComponent<Image>();
        tbImg.sprite = btnBg;
        tbImg.type = Image.Type.Sliced;
        tbImg.color = Color.white;

        HorizontalLayoutGroup tbHlg = tabBarObj.AddComponent<HorizontalLayoutGroup>();
        tbHlg.childControlWidth = true;
        tbHlg.childControlHeight = true;
        tbHlg.childForceExpandWidth = true;
        tbHlg.childForceExpandHeight = true;

        (Button tabGameplayBtn, TMP_Text tabGameplayTxt) = CreateTabItem(tabBarObj.transform, "Tab_Gameplay", "Gameplay", font, darkBrown, true);
        (Button tabControlsBtn, TMP_Text tabControlsTxt) = CreateTabItem(tabBarObj.transform, "Tab_Controls", "Controls", font, mutedBrown, false);
        (Button tabGraphicsBtn, TMP_Text tabGraphicsTxt) = CreateTabItem(tabBarObj.transform, "Tab_Graphics", "Graphics", font, mutedBrown, false);
        (Button tabAudioBtn, TMP_Text tabAudioTxt) = CreateTabItem(tabBarObj.transform, "Tab_Audio", "Audio", font, mutedBrown, false);

        // Tab Content Panels Container
        GameObject panelsContainer = new GameObject("TabPanelsContainer", typeof(RectTransform));
        panelsContainer.transform.SetParent(scObj.transform, false);
        RectTransform pcRt = panelsContainer.GetComponent<RectTransform>();
        pcRt.anchorMin = new Vector2(0.5f, 0.5f);
        pcRt.anchorMax = new Vector2(0.5f, 0.5f);
        pcRt.pivot = new Vector2(0.5f, 0.5f);
        pcRt.anchoredPosition = new Vector2(0f, -60f);
        pcRt.sizeDelta = new Vector2(960f, 360f);

        // 1. GAMEPLAY PANEL
        GameObject gpObj = CreateTabContentPanel(panelsContainer.transform, "GameplayPanel", 12f);
        (Button charPrev, Button charNext, TMP_Text charTxt) = CreateSelectorRow(gpObj.transform, "CharacterSetRow", "Character Set", "kanji + furigana", btnBg, font, darkBrown);
        (Button hideEnBtn, Image hideEnDot) = CreateToggleRow(gpObj.transform, "HideEnglishRow", "Hide English Translation On Cards", false, btnBg, circleSprite, font, darkBrown);
        (Slider fovSld, TMP_Text fovTxt) = CreateSliderRow(gpObj.transform, "FOVRow", "FOV (Field of View)", 60f, 110f, 90f, btnBg, circleSprite, font, darkBrown);
        (Button outlineBtn, Image outlineDot) = CreateToggleRow(gpObj.transform, "DisableOutlineRow", "Disable Subject Outline", false, btnBg, circleSprite, font, darkBrown);

        // 2. CONTROLS PANEL
        GameObject cpObj = CreateTabContentPanel(panelsContainer.transform, "ControlsPanel", 14f);
        (Slider sensSld, TMP_Text sensTxt) = CreateSliderRow(cpObj.transform, "SensitivityRow", "Mouse Look Sensitivity", 10f, 200f, 90f, btnBg, circleSprite, font, darkBrown);
        (Button invXBtn, Image invXDot) = CreateToggleRow(cpObj.transform, "InvertXRow", "Invert X-Axis", false, btnBg, circleSprite, font, darkBrown);
        (Button invYBtn, Image invYDot) = CreateToggleRow(cpObj.transform, "InvertYRow", "Invert Y-Axis", false, btnBg, circleSprite, font, darkBrown);

        // 3. GRAPHICS PANEL
        GameObject grpObj = CreateTabContentPanel(panelsContainer.transform, "GraphicsPanel", 8f);
        (Button dispPrev, Button dispNext, TMP_Text dispTxt) = CreateSelectorRow(grpObj.transform, "DisplayModeRow", "Display Mode", "Full Screen", btnBg, font, darkBrown);
        (Button resPrev, Button resNext, TMP_Text resTxt) = CreateSelectorRow(grpObj.transform, "ResolutionRow", "Screen Resolution", "1920 x 1080", btnBg, font, darkBrown);
        (Button qualPrev, Button qualNext, TMP_Text qualTxt) = CreateSelectorRow(grpObj.transform, "QualityRow", "Graphics Quality", "Epic", btnBg, font, darkBrown);
        (Button fpsPrev, Button fpsNext, TMP_Text fpsTxt) = CreateSelectorRow(grpObj.transform, "FPSLimitRow", "Frame Rate Limit", "Unlimited", btnBg, font, darkBrown);
        (Button aaPrev, Button aaNext, TMP_Text aaTxt) = CreateSelectorRow(grpObj.transform, "AntiAliasingRow", "Anti-Aliasing Method", "TSR", btnBg, font, darkBrown);
        (Button vsyncBtn, Image vsyncDot) = CreateToggleRow(grpObj.transform, "VSyncRow", "V-Sync", false, btnBg, circleSprite, font, darkBrown);

        // 4. AUDIO PANEL
        GameObject apObj = CreateTabContentPanel(panelsContainer.transform, "AudioPanel", 14f);
        (Slider mVolSld, TMP_Text mVolTxt) = CreateSliderRow(apObj.transform, "MasterVolRow", "Master Volume", 0f, 100f, 90f, btnBg, circleSprite, font, darkBrown);
        (Slider bgmSld, TMP_Text bgmTxt) = CreateSliderRow(apObj.transform, "MusicVolRow", "Music Volume", 0f, 100f, 100f, btnBg, circleSprite, font, darkBrown);
        (Slider vVolSld, TMP_Text vVolTxt) = CreateSliderRow(apObj.transform, "VoiceVolRow", "Voice Lines Volume", 0f, 100f, 75f, btnBg, circleSprite, font, darkBrown);

        // Bottom Action Buttons: [ Apply ] [ Back ]
        GameObject actionBtnsObj = new GameObject("ActionButtonsBar", typeof(RectTransform));
        actionBtnsObj.transform.SetParent(scObj.transform, false);
        RectTransform abRt = actionBtnsObj.GetComponent<RectTransform>();
        abRt.anchorMin = new Vector2(0.5f, 0.5f);
        abRt.anchorMax = new Vector2(0.5f, 0.5f);
        abRt.pivot = new Vector2(0.5f, 0.5f);
        abRt.anchoredPosition = new Vector2(0f, -290f);
        abRt.sizeDelta = new Vector2(960f, 50f);

        HorizontalLayoutGroup abHlg = actionBtnsObj.AddComponent<HorizontalLayoutGroup>();
        abHlg.spacing = 20f;
        abHlg.childControlWidth = true;
        abHlg.childControlHeight = true;
        abHlg.childForceExpandWidth = true;
        abHlg.childForceExpandHeight = true;

        Button applyBtn = CreatePillButton(actionBtnsObj.transform, "ApplyButton", "Apply", Vector2.zero, btnBg, font, darkBrown, 22f);
        Button backBtn = CreatePillButton(actionBtnsObj.transform, "BackButton", "Back", Vector2.zero, btnBg, font, darkBrown, 22f);

        // ==========================================
        // C. HUBUNGKAN LOGIKA MANAGER (PauseMenuManager & SettingsManager)
        // ==========================================
        PauseMenuManager pauseMgr = Object.FindAnyObjectByType<PauseMenuManager>();
        if (pauseMgr == null)
        {
            GameObject mgrObj = new GameObject("PauseMenuManager");
            pauseMgr = mgrObj.AddComponent<PauseMenuManager>();
        }

        SettingsManager settingsMgr = Object.FindAnyObjectByType<SettingsManager>();
        if (settingsMgr == null)
        {
            GameObject smObj = new GameObject("SettingsManager");
            settingsMgr = smObj.AddComponent<SettingsManager>();
        }

        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>(FindObjectsInactive.Include);
        StarterAssetsInputs inputs = Object.FindAnyObjectByType<StarterAssetsInputs>(FindObjectsInactive.Include);

        // Wire PauseMenuManager
        SetPrivateField(pauseMgr, "pausePanel", pausePanelObj);
        SetPrivateField(pauseMgr, "settingsButton", settingsBtn);
        SetPrivateField(pauseMgr, "backToMenuButton", backToMenuBtn);
        SetPrivateField(pauseMgr, "mainPauseContent", mainContentObj);
        SetPrivateField(pauseMgr, "settingsPanel", settingsPanelObj);
        SetPrivateField(pauseMgr, "playerController", player);
        SetPrivateField(pauseMgr, "playerInputs", inputs);

        // Wire SettingsManager
        SetPrivateField(settingsMgr, "gameplayPanel", gpObj);
        SetPrivateField(settingsMgr, "controlsPanel", cpObj);
        SetPrivateField(settingsMgr, "graphicsPanel", grpObj);
        SetPrivateField(settingsMgr, "audioPanel", apObj);

        SetPrivateField(settingsMgr, "tabGameplayBtn", tabGameplayBtn);
        SetPrivateField(settingsMgr, "tabControlsBtn", tabControlsBtn);
        SetPrivateField(settingsMgr, "tabGraphicsBtn", tabGraphicsBtn);
        SetPrivateField(settingsMgr, "tabAudioBtn", tabAudioBtn);

        SetPrivateField(settingsMgr, "tabGameplayText", tabGameplayTxt);
        SetPrivateField(settingsMgr, "tabControlsText", tabControlsTxt);
        SetPrivateField(settingsMgr, "tabGraphicsText", tabGraphicsTxt);
        SetPrivateField(settingsMgr, "tabAudioText", tabAudioTxt);

        SetPrivateField(settingsMgr, "applyBtn", applyBtn);
        SetPrivateField(settingsMgr, "backBtn", backBtn);
        SetPrivateField(settingsMgr, "backToTitleBtn", backToTitleBtn);

        // Gameplay
        SetPrivateField(settingsMgr, "charSetPrevBtn", charPrev);
        SetPrivateField(settingsMgr, "charSetNextBtn", charNext);
        SetPrivateField(settingsMgr, "charSetValueText", charTxt);
        SetPrivateField(settingsMgr, "hideEnglishToggleBtn", hideEnBtn);
        SetPrivateField(settingsMgr, "hideEnglishDotImg", hideEnDot);
        SetPrivateField(settingsMgr, "fovSlider", fovSld);
        SetPrivateField(settingsMgr, "fovValueText", fovTxt);
        SetPrivateField(settingsMgr, "disableOutlineToggleBtn", outlineBtn);
        SetPrivateField(settingsMgr, "disableOutlineDotImg", outlineDot);

        // Controls
        SetPrivateField(settingsMgr, "sensitivitySlider", sensSld);
        SetPrivateField(settingsMgr, "sensitivityValueText", sensTxt);
        SetPrivateField(settingsMgr, "invertXToggleBtn", invXBtn);
        SetPrivateField(settingsMgr, "invertXDotImg", invXDot);
        SetPrivateField(settingsMgr, "invertYToggleBtn", invYBtn);
        SetPrivateField(settingsMgr, "invertYDotImg", invYDot);

        // Graphics
        SetPrivateField(settingsMgr, "displayModePrevBtn", dispPrev);
        SetPrivateField(settingsMgr, "displayModeNextBtn", dispNext);
        SetPrivateField(settingsMgr, "displayModeValueText", dispTxt);
        SetPrivateField(settingsMgr, "resolutionPrevBtn", resPrev);
        SetPrivateField(settingsMgr, "resolutionNextBtn", resNext);
        SetPrivateField(settingsMgr, "resolutionValueText", resTxt);
        SetPrivateField(settingsMgr, "qualityPrevBtn", qualPrev);
        SetPrivateField(settingsMgr, "qualityNextBtn", qualNext);
        SetPrivateField(settingsMgr, "qualityValueText", qualTxt);
        SetPrivateField(settingsMgr, "fpsLimitPrevBtn", fpsPrev);
        SetPrivateField(settingsMgr, "fpsLimitNextBtn", fpsNext);
        SetPrivateField(settingsMgr, "fpsLimitValueText", fpsTxt);
        SetPrivateField(settingsMgr, "aaPrevBtn", aaPrev);
        SetPrivateField(settingsMgr, "aaNextBtn", aaNext);
        SetPrivateField(settingsMgr, "aaValueText", aaTxt);
        SetPrivateField(settingsMgr, "vsyncToggleBtn", vsyncBtn);
        SetPrivateField(settingsMgr, "vsyncDotImg", vsyncDot);

        // Audio
        SetPrivateField(settingsMgr, "masterVolSlider", mVolSld);
        SetPrivateField(settingsMgr, "masterVolValueText", mVolTxt);
        SetPrivateField(settingsMgr, "musicVolSlider", bgmSld);
        SetPrivateField(settingsMgr, "musicVolValueText", bgmTxt);
        SetPrivateField(settingsMgr, "voiceVolSlider", vVolSld);
        SetPrivateField(settingsMgr, "voiceVolValueText", vVolTxt);

        SetPrivateField(settingsMgr, "playerController", player);

        // Initial Visibility
        settingsPanelObj.SetActive(false);
        mainContentObj.SetActive(true);
        pausePanelObj.SetActive(false);

        EditorUtility.SetDirty(pauseMgr);
        EditorUtility.SetDirty(settingsMgr);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green>[Utsuru] SUKSES! Pause Menu & Settings telah diperbarui dengan layout sempurna & siap dimainkan!</color>");
    }

    // ==========================================
    // HELPER CREATION METHODS
    // ==========================================

    private static void CreateLogoObject(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject logoObj = new GameObject(name, typeof(RectTransform));
        logoObj.transform.SetParent(parent, false);
        RectTransform rt = logoObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = logoObj.AddComponent<Image>();
        if (sprite != null) img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    private static (Button, TMP_Text) CreateTabItem(Transform parent, string name, string label, TMP_FontAsset font, Color color, bool isBold)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        // Graphic transparan agar seluruh area tab bisa diklik
        Image tabImg = obj.AddComponent<Image>();
        tabImg.color = new Color(0f, 0f, 0f, 0.001f);
        tabImg.raycastTarget = true;

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = tabImg;

        // Teks berada di child terpisah agar tidak bentrok dengan Image di parent
        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(obj.transform, false);
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        TMP_Text tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = font;
        tmp.fontSize = 22f;
        tmp.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return (btn, tmp);
    }

    private static GameObject CreateTabContentPanel(Transform parent, string name, float spacing)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        return panel;
    }

    private static (Button prev, Button next, TMP_Text val) CreateSelectorRow(Transform parent, string name, string label, string defaultVal, Sprite bg, TMP_FontAsset font, Color textColor)
    {
        GameObject row = CreateBaseRow(parent, name, label, bg, font, textColor);

        GameObject widget = new GameObject("SelectorWidget", typeof(RectTransform));
        widget.transform.SetParent(row.transform, false);
        RectTransform wrt = widget.GetComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0.55f, 0f);
        wrt.anchorMax = new Vector2(1f, 1f);
        wrt.offsetMin = Vector2.zero;
        wrt.offsetMax = new Vector2(-25f, 0f);

        // Prev Arrow Button (<)
        GameObject prevObj = new GameObject("PrevBtn", typeof(RectTransform));
        prevObj.transform.SetParent(widget.transform, false);
        RectTransform prt = prevObj.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0.5f);
        prt.anchorMax = new Vector2(0f, 0.5f);
        prt.pivot = new Vector2(0f, 0.5f);
        prt.sizeDelta = new Vector2(40f, 40f);
        prt.anchoredPosition = Vector2.zero;

        TMP_Text pTxt = prevObj.AddComponent<TextMeshProUGUI>();
        pTxt.text = "<";
        pTxt.font = font;
        pTxt.fontSize = 24f;
        pTxt.fontStyle = FontStyles.Bold;
        pTxt.color = textColor;
        pTxt.alignment = TextAlignmentOptions.Center;
        pTxt.raycastTarget = true; // TMP Text bertindak sebagai Graphic penangkap klik

        Button prevBtn = prevObj.AddComponent<Button>();
        prevBtn.targetGraphic = pTxt;

        // Next Arrow Button (>)
        GameObject nextObj = new GameObject("NextBtn", typeof(RectTransform));
        nextObj.transform.SetParent(widget.transform, false);
        RectTransform nrt = nextObj.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(1f, 0.5f);
        nrt.anchorMax = new Vector2(1f, 0.5f);
        nrt.pivot = new Vector2(1f, 0.5f);
        nrt.sizeDelta = new Vector2(40f, 40f);
        nrt.anchoredPosition = Vector2.zero;

        TMP_Text nTxt = nextObj.AddComponent<TextMeshProUGUI>();
        nTxt.text = ">";
        nTxt.font = font;
        nTxt.fontSize = 24f;
        nTxt.fontStyle = FontStyles.Bold;
        nTxt.color = textColor;
        nTxt.alignment = TextAlignmentOptions.Center;
        nTxt.raycastTarget = true; // TMP Text bertindak sebagai Graphic penangkap klik

        Button nextBtn = nextObj.AddComponent<Button>();
        nextBtn.targetGraphic = nTxt;

        // Value Text
        GameObject valObj = new GameObject("ValueText", typeof(RectTransform));
        valObj.transform.SetParent(widget.transform, false);
        RectTransform vrt = valObj.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0f, 0f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.offsetMin = new Vector2(45f, 0f);
        vrt.offsetMax = new Vector2(-45f, 0f);

        TMP_Text vTxt = valObj.AddComponent<TextMeshProUGUI>();
        vTxt.text = defaultVal;
        vTxt.font = font;
        vTxt.fontSize = 20f;
        vTxt.fontStyle = FontStyles.Bold;
        vTxt.color = textColor;
        vTxt.alignment = TextAlignmentOptions.Center;
        vTxt.raycastTarget = false;

        return (prevBtn, nextBtn, vTxt);
    }

    private static (Button toggleBtn, Image dot) CreateToggleRow(Transform parent, string name, string label, bool defaultOn, Sprite bg, Sprite circle, TMP_FontAsset font, Color textColor)
    {
        GameObject row = CreateBaseRow(parent, name, label, bg, font, textColor);

        Image rowImg = row.GetComponent<Image>();
        Button btn = row.AddComponent<Button>();
        btn.targetGraphic = rowImg;

        GameObject dotObj = new GameObject("ToggleDot", typeof(RectTransform));
        dotObj.transform.SetParent(row.transform, false);
        RectTransform drt = dotObj.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(1f, 0.5f);
        drt.anchorMax = new Vector2(1f, 0.5f);
        drt.pivot = new Vector2(1f, 0.5f);
        drt.anchoredPosition = new Vector2(-30f, 0f);
        drt.sizeDelta = new Vector2(22f, 22f);

        Image dotImg = dotObj.AddComponent<Image>();
        dotImg.sprite = circle;
        dotImg.color = defaultOn ? textColor : new Color32(213, 199, 188, 255);
        dotImg.raycastTarget = false;

        return (btn, dotImg);
    }

    private static (Slider slider, TMP_Text valText) CreateSliderRow(Transform parent, string name, string label, float min, float max, float defaultVal, Sprite bg, Sprite circle, TMP_FontAsset font, Color textColor)
    {
        GameObject row = CreateBaseRow(parent, name, label, bg, font, textColor);

        // Value Text on the far right
        GameObject valObj = new GameObject("ValueText", typeof(RectTransform));
        valObj.transform.SetParent(row.transform, false);
        RectTransform vrt = valObj.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(1f, 0.5f);
        vrt.anchorMax = new Vector2(1f, 0.5f);
        vrt.pivot = new Vector2(1f, 0.5f);
        vrt.anchoredPosition = new Vector2(-25f, 0f);
        vrt.sizeDelta = new Vector2(55f, 40f);

        TMP_Text vTxt = valObj.AddComponent<TextMeshProUGUI>();
        vTxt.text = defaultVal.ToString("0");
        vTxt.font = font;
        vTxt.fontSize = 20f;
        vTxt.fontStyle = FontStyles.Bold;
        vTxt.color = textColor;
        vTxt.alignment = TextAlignmentOptions.Right;
        vTxt.raycastTarget = false;

        // Slider Object
        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform));
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform srt = sliderObj.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(1f, 0.5f);
        srt.anchorMax = new Vector2(1f, 0.5f);
        srt.pivot = new Vector2(1f, 0.5f);
        srt.anchoredPosition = new Vector2(-90f, 0f);
        srt.sizeDelta = new Vector2(220f, 30f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultVal;

        // Background Track (Crisp 5px bar)
        GameObject bgTrack = new GameObject("Background", typeof(RectTransform));
        bgTrack.transform.SetParent(sliderObj.transform, false);
        RectTransform bgTrt = bgTrack.GetComponent<RectTransform>();
        bgTrt.anchorMin = new Vector2(0f, 0.5f);
        bgTrt.anchorMax = new Vector2(1f, 0.5f);
        bgTrt.pivot = new Vector2(0.5f, 0.5f);
        bgTrt.offsetMin = new Vector2(0f, -2.5f);
        bgTrt.offsetMax = new Vector2(0f, 2.5f);
        Image bgImg = bgTrack.AddComponent<Image>();
        bgImg.color = new Color32(205, 191, 180, 255);

        // Fill Area & Fill
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fart = fillArea.GetComponent<RectTransform>();
        fart.anchorMin = new Vector2(0f, 0.5f);
        fart.anchorMax = new Vector2(1f, 0.5f);
        fart.pivot = new Vector2(0.5f, 0.5f);
        fart.offsetMin = new Vector2(0f, -2.5f);
        fart.offsetMax = new Vector2(0f, 2.5f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = textColor;

        // Handle Slide Area & Handle (Perfect 18x18 circle)
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform hart = handleArea.GetComponent<RectTransform>();
        hart.anchorMin = Vector2.zero;
        hart.anchorMax = Vector2.one;
        hart.offsetMin = new Vector2(10f, 0f);
        hart.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(18f, 18f);
        Image hImg = handle.AddComponent<Image>();
        hImg.sprite = circle;
        hImg.color = textColor;

        slider.fillRect = frt;
        slider.handleRect = hrt;
        slider.targetGraphic = hImg;
        slider.direction = Slider.Direction.LeftToRight;

        return (slider, vTxt);
    }

    private static GameObject CreateBaseRow(Transform parent, string name, string label, Sprite bg, TMP_FontAsset font, Color textColor)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(960f, 48f);

        Image img = row.AddComponent<Image>();
        img.sprite = bg;
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        // Label Text on Left
        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(row.transform, false);
        RectTransform lrt = labelObj.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(0.55f, 1f);
        lrt.offsetMin = new Vector2(25f, 0f);
        lrt.offsetMax = Vector2.zero;

        TMP_Text tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = font;
        tmp.fontSize = 22f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;

        return row;
    }

    private static Button CreateTopLeftBackButton(Transform parent, string name, string label, Sprite bg, TMP_FontAsset font, Color textColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(40f, -40f);
        rt.sizeDelta = new Vector2(190f, 48f);

        Image img = btnObj.AddComponent<Image>();
        img.sprite = bg;
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        TMP_Text tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = font;
        tmp.fontSize = 20f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    private static Button CreatePillButton(Transform parent, string objName, string label, Vector2 size, Sprite bgSprite, TMP_FontAsset font, Color textColor, float fontSize)
    {
        GameObject btnObj = new GameObject(objName, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        if (size != Vector2.zero) rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.sprite = bgSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color32(255, 245, 230, 255);
        cb.pressedColor = new Color32(230, 215, 195, 255);
        cb.selectedColor = Color.white;
        btn.colors = cb;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;

        TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    private static void ConfigureSpriteImporter(string path)
    {
        if (!File.Exists(path)) return;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
#endif
