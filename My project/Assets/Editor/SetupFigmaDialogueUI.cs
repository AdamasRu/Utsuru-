#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using StarterAssets;

public class SetupFigmaDialogueUI : Editor
{
    [InitializeOnLoadMethod]
    private static void AutoApplyOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.isLoaded && scene.name.Contains("Day_Showcase"))
            {
                Canvas canvas = Object.FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform panelTr = canvas.transform.Find("DialogueBoxPanel");
                    if (panelTr != null)
                    {
                        Transform cgTr = panelTr.Find("ContentGroup");
                        // Cek jika ContentGroup belum memakai AutoLayout (VerticalLayoutGroup)
                        if (cgTr == null || cgTr.GetComponent<VerticalLayoutGroup>() == null)
                        {
                            GenerateFigmaUI();
                        }
                    }
                    else
                    {
                        GenerateFigmaUI();
                    }
                }
            }
        };
    }

    [MenuItem("Tools/Utsuru/Setup Figma Dialogue UI (Pixel Perfect)", priority = 1)]
    public static void GenerateFigmaUI()
    {
        Color32 fillColor = new Color32(58, 38, 26, 255);    // Dark brown Figma (#3A261A)
        Color32 strokeColor = new Color32(115, 82, 58, 255); // Subtle border stroke (#73523A)

        // 1. Buat Kapsul 100% Setengah Lingkaran Sempurna untuk NamePill & RolePill (Sesuai Skala Figma Baru)
        GenerateCapsuleTexture("Assets/UI/namepill.png", 120, 36, 18f, fillColor, strokeColor, new Vector4(18, 10, 18, 10));
        GenerateCapsuleTexture("Assets/UI/Pill_Brown_Bg.png", 120, 34, 17f, fillColor, strokeColor, new Vector4(17, 9, 17, 9));

        // 2. Pastikan Texture Importer untuk semua aset UI punya 9-Slice Border yang presisi & Single Sprite Mode
        ConfigureSpriteImporter("Assets/UI/dialogbox.png", new Vector4(24, 24, 24, 24));
        ConfigureSpriteImporter("Assets/UI/Button_Selected.png", new Vector4(20, 20, 20, 20));
        ConfigureSpriteImporter("Assets/UI/Button_Unselected.png", new Vector4(20, 20, 20, 20));
        ConfigureSpriteImporter("Assets/UI/replayaudiobtn.png", new Vector4(14, 14, 14, 14));
        ConfigureSpriteImporter("Assets/UI/AvatarFrame_Bg.png", new Vector4(24, 24, 24, 24));
        ConfigureSpriteImporter("Assets/UI/DialogueBox_Bg.png", new Vector4(24, 24, 24, 24));
        ConfigureSpriteImporter("Assets/UI/Pill_Brown_Bg.png", new Vector4(17, 9, 17, 9));
        ConfigureSpriteImporter("Assets/UI/namepill.png", new Vector4(18, 10, 18, 10));
        ConfigureSpriteImporter("Assets/UI/Pill_Score_Bg.png", new Vector4(16, 14, 16, 14));
        ConfigureSpriteImporter("Assets/UI/Card_Choice1_Bg.png", new Vector4(18, 18, 18, 18));
        ConfigureSpriteImporter("Assets/UI/Card_Choice2_Bg.png", new Vector4(18, 18, 18, 18));
        ConfigureSpriteImporter("Assets/UI/numberbadge.png", new Vector4(15, 15, 15, 15));
        ConfigureSpriteImporter("Assets/UI/Circle_Badge_Bg.png", new Vector4(15, 15, 15, 15));

        // 3. Ambil Sprite Terbaik dari Assets/UI
        Sprite bgDialogue = LoadSprite("Assets/UI/dialogbox.png") ?? LoadSprite("Assets/UI/DialogueBox_Bg.png");
        Sprite bgBtnSelected = LoadSprite("Assets/UI/Button_Selected.png") ?? LoadSprite("Assets/UI/Card_Choice1_Bg.png");
        Sprite bgBtnUnselected = LoadSprite("Assets/UI/Button_Unselected.png") ?? LoadSprite("Assets/UI/Card_Choice2_Bg.png");
        Sprite bgReplayBtn = LoadSprite("Assets/UI/replayaudiobtn.png") ?? LoadSprite("Assets/UI/Button_Replay_Bg.png");
        Sprite bgAvatarFrame = LoadSprite("Assets/UI/AvatarFrame_Bg.png");
        Sprite bgPillBrown = LoadSprite("Assets/UI/Pill_Brown_Bg.png");
        Sprite bgPillDark = LoadSprite("Assets/UI/namepill.png") ?? LoadSprite("Assets/UI/Pill_Dark_Bg.png");
        Sprite bgPillScore = LoadSprite("Assets/UI/Pill_Score_Bg.png");
        Sprite bgCircleBadge = LoadSprite("Assets/UI/numberbadge.png") ?? LoadSprite("Assets/UI/Circle_Badge_Bg.png");
        Sprite speakerIcon = LoadSprite("Assets/UI/SpeakerIcon2.png") ?? LoadSprite("Assets/UI/IconSpeaker.png") ?? LoadSprite("Assets/UI/Icon_Speaker.png");

        // 4. Ambil Font Khusus Figma (Zen Maru Gothic & Nunito)
        TMP_FontAsset jpFont = LoadFont("Assets/Fonts/Nunito,Zen_Maru_Gothic/Zen_Maru_Gothic/ZenMaruGothic-Black SDF.asset")
                            ?? LoadFont("Assets/Fonts/Nunito,Zen_Maru_Gothic/Zen_Maru_Gothic/ZenMaruGothic-Bold SDF.asset")
                            ?? LoadFont("Assets/Fonts/YUGOTHB SDF.asset");

        TMP_FontAsset nunitoBold = LoadFont("Assets/Fonts/Nunito,Zen_Maru_Gothic/Nunito/static/Nunito-Bold SDF.asset")
                                ?? jpFont;
        TMP_FontAsset nunitoSemi = LoadFont("Assets/Fonts/Nunito,Zen_Maru_Gothic/Nunito/static/Nunito-SemiBold SDF.asset")
                                ?? nunitoBold;

        // Fallback Font agar karakter Jepang, Romaji, dan simbol selalu tampil sempurna
        EnsureFontFallbacks(nunitoBold, jpFont);
        EnsureFontFallbacks(nunitoSemi, jpFont);

        // Default Avatar BoZo (Mary = Karakter Hana di Figma dengan rambut pink flare)
        Sprite defaultAvatar = LoadSprite("Assets/BoZo_StylizedModularCharacters/CustomCharacters/Icons/Base/Mary.png")
                            ?? LoadSprite("Assets/BoZo_StylizedModularCharacters/CustomCharacters/Icons/Base/1DefaultFemale.png")
                            ?? LoadSprite("Assets/BoZo_StylizedModularCharacters/CustomCharacters/Icons/Base/Chloe.png");

        // 5. Update ChoiceButtonPrefab dengan AutoLayout (Tinggi 83px, Badge 40x40px, Font Auto-Fit agar tidak bertumpuk!)
        GameObject choiceBtnPrefab = CreateOrUpdateChoiceButtonPrefab(jpFont, nunitoSemi, nunitoBold, bgBtnSelected, bgCircleBadge);

        // 6. Setup Canvas di Scene (Resolusi Figma: 1920 x 1080)
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        // Hapus DialogueBoxPanel lama agar bersih
        Transform oldPanelTr = canvas.transform.Find("DialogueBoxPanel");
        if (oldPanelTr == null) oldPanelTr = canvas.transform.Find("DialoguePanel");
        if (oldPanelTr != null)
        {
            Undo.DestroyObjectImmediate(oldPanelTr.gameObject);
        }

        // --- BUAT DIALOGUE BOX PANEL BARU (Ukuran Persis Figma: 1700 x 388, Jarak Bawah: 38px) ---
        GameObject panelObj = new GameObject("DialogueBoxPanel");
        Undo.RegisterCreatedObjectUndo(panelObj, "Create Figma Dialogue Panel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform pRt = panelObj.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0f);
        pRt.anchorMax = new Vector2(0.5f, 0f);
        pRt.pivot = new Vector2(0.5f, 0f);
        pRt.anchoredPosition = new Vector2(0f, 38f);
        pRt.sizeDelta = new Vector2(1700f, 388f);

        Image pImg = panelObj.AddComponent<Image>();
        pImg.sprite = bgDialogue;
        pImg.type = Image.Type.Sliced;
        pImg.color = Color.white;

        // --- A. AVATAR BADGE (Ukuran Figma: 220 x 219, Overhang di Pojok Kiri Atas Persis Figma) ---
        GameObject avatarBadgeObj = new GameObject("AvatarBadge");
        avatarBadgeObj.transform.SetParent(panelObj.transform, false);
        RectTransform avRt = avatarBadgeObj.AddComponent<RectTransform>();
        avRt.anchorMin = new Vector2(0f, 1f);
        avRt.anchorMax = new Vector2(0f, 1f);
        avRt.pivot = new Vector2(0f, 1f);
        avRt.anchoredPosition = new Vector2(-12f, 60f);
        avRt.sizeDelta = new Vector2(220f, 219f);

        Image avBg = avatarBadgeObj.AddComponent<Image>();
        avBg.sprite = bgAvatarFrame;
        avBg.type = Image.Type.Sliced;
        avBg.color = Color.white;

        // 1. Avatar Image (Foto Wajah: Dirender langsung di atas background krem, ukuran 208px pas & jelas!)
        GameObject avImgObj = new GameObject("AvatarImage");
        avImgObj.transform.SetParent(avatarBadgeObj.transform, false);
        RectTransform aviRt = avImgObj.AddComponent<RectTransform>();
        aviRt.anchorMin = new Vector2(0.5f, 0.5f);
        aviRt.anchorMax = new Vector2(0.5f, 0.5f);
        aviRt.pivot = new Vector2(0.5f, 0.5f);
        aviRt.anchoredPosition = new Vector2(0f, 6f);
        aviRt.sizeDelta = new Vector2(208f, 208f);

        Image avFace = avImgObj.AddComponent<Image>();
        if (defaultAvatar != null) avFace.sprite = defaultAvatar;
        avFace.preserveAspect = true;
        avFace.raycastTarget = false;

        // 2. Name Pill (Kapsul Nama "Hana (ハナ)" di border bawah)
        GameObject namePillObj = new GameObject("NamePill");
        namePillObj.transform.SetParent(avatarBadgeObj.transform, false);
        RectTransform npRt = namePillObj.AddComponent<RectTransform>();
        npRt.anchorMin = new Vector2(0.5f, 0f);
        npRt.anchorMax = new Vector2(0.5f, 0f);
        npRt.pivot = new Vector2(0.5f, 0.5f);
        npRt.anchoredPosition = new Vector2(0f, 0f);
        npRt.sizeDelta = new Vector2(170f, 36f);

        Image npImg = namePillObj.AddComponent<Image>();
        npImg.sprite = bgPillDark;
        npImg.type = Image.Type.Sliced;

        GameObject nameTextObj = new GameObject("NameText");
        nameTextObj.transform.SetParent(namePillObj.transform, false);
        RectTransform ntRt = nameTextObj.AddComponent<RectTransform>();
        ntRt.anchorMin = Vector2.zero;
        ntRt.anchorMax = Vector2.one;
        ntRt.offsetMin = new Vector2(10f, 0f);
        ntRt.offsetMax = new Vector2(-10f, 0f);

        TMP_Text nameTmp = nameTextObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "Hana (ハナ)";
        nameTmp.font = jpFont;
        nameTmp.fontSize = 17f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color = Color.white;
        nameTmp.raycastTarget = false;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;

        // --- B. HEADER ROW (Left = 245px agar presisi di samping Avatar) ---
        GameObject headerObj = new GameObject("HeaderRow");
        headerObj.transform.SetParent(panelObj.transform, false);
        RectTransform hRt = headerObj.AddComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0f, 1f);
        hRt.anchoredPosition = new Vector2(245f, -22f);
        hRt.sizeDelta = new Vector2(-280f, 36f);

        // Role Pill ("Friendly Neighbor" / "Shopkeeper")
        GameObject rolePillObj = new GameObject("RolePill");
        rolePillObj.transform.SetParent(headerObj.transform, false);
        RectTransform rpRt = rolePillObj.AddComponent<RectTransform>();
        rpRt.anchorMin = new Vector2(0f, 0.5f);
        rpRt.anchorMax = new Vector2(0f, 0.5f);
        rpRt.pivot = new Vector2(0f, 0.5f);
        rpRt.anchoredPosition = new Vector2(0f, 0f);
        rpRt.sizeDelta = new Vector2(150f, 34f);

        Image rpImg = rolePillObj.AddComponent<Image>();
        rpImg.sprite = bgPillBrown;
        rpImg.type = Image.Type.Sliced;

        GameObject roleTextObj = new GameObject("RoleText");
        roleTextObj.transform.SetParent(rolePillObj.transform, false);
        RectTransform rtRt = roleTextObj.AddComponent<RectTransform>();
        rtRt.anchorMin = Vector2.zero;
        rtRt.anchorMax = Vector2.one;
        rtRt.sizeDelta = Vector2.zero;

        TMP_Text roleTmp = roleTextObj.AddComponent<TextMeshProUGUI>();
        roleTmp.text = "Friendly Neighbor";
        roleTmp.font = nunitoBold;
        roleTmp.fontSize = 14f;
        roleTmp.fontStyle = FontStyles.Bold;
        roleTmp.alignment = TextAlignmentOptions.Center;
        roleTmp.color = Color.white;

        // JLPT Tag Text ("Formality: Casual & Friendly • JLPT N5")
        GameObject jlptObj = new GameObject("JLPTText");
        jlptObj.transform.SetParent(headerObj.transform, false);
        RectTransform jlptRt = jlptObj.AddComponent<RectTransform>();
        jlptRt.anchorMin = new Vector2(0f, 0.5f);
        jlptRt.anchorMax = new Vector2(0f, 0.5f);
        jlptRt.pivot = new Vector2(0f, 0.5f);
        jlptRt.anchoredPosition = new Vector2(165f, 0f);
        jlptRt.sizeDelta = new Vector2(500f, 32f);

        TMP_Text jlptTmp = jlptObj.AddComponent<TextMeshProUGUI>();
        jlptTmp.text = "Formality: Casual & Friendly • JLPT N5";
        jlptTmp.font = nunitoBold;
        jlptTmp.fontSize = 15f;
        jlptTmp.fontStyle = FontStyles.Bold;
        jlptTmp.color = new Color32(92, 78, 67, 255);
        jlptTmp.alignment = TextAlignmentOptions.Left;

        // Replay Audio Button
        GameObject replayBtnObj = new GameObject("ReplayAudioButton");
        replayBtnObj.transform.SetParent(headerObj.transform, false);
        RectTransform repRt = replayBtnObj.AddComponent<RectTransform>();
        repRt.anchorMin = new Vector2(1f, 0.5f);
        repRt.anchorMax = new Vector2(1f, 0.5f);
        repRt.pivot = new Vector2(1f, 0.5f);
        repRt.anchoredPosition = new Vector2(-42f, 0f);
        repRt.sizeDelta = new Vector2(150f, 34f);

        Image repImg = replayBtnObj.AddComponent<Image>();
        repImg.sprite = bgReplayBtn;
        repImg.type = Image.Type.Sliced;
        Button repBtn = replayBtnObj.AddComponent<Button>();

        // Replay Icon
        GameObject repIconObj = new GameObject("Icon");
        repIconObj.transform.SetParent(replayBtnObj.transform, false);
        RectTransform riRt = repIconObj.AddComponent<RectTransform>();
        riRt.anchorMin = new Vector2(0f, 0.5f);
        riRt.anchorMax = new Vector2(0f, 0.5f);
        riRt.pivot = new Vector2(0f, 0.5f);
        riRt.anchoredPosition = new Vector2(14f, 0f);
        riRt.sizeDelta = new Vector2(18f, 18f);
        Image riImg = repIconObj.AddComponent<Image>();
        riImg.sprite = speakerIcon;
        riImg.preserveAspect = true;
        riImg.color = new Color32(91, 61, 39, 255);

        // Replay Text
        GameObject repTextObj = new GameObject("Text");
        repTextObj.transform.SetParent(replayBtnObj.transform, false);
        RectTransform rptRt = repTextObj.AddComponent<RectTransform>();
        rptRt.anchorMin = new Vector2(0f, 0f);
        rptRt.anchorMax = new Vector2(1f, 1f);
        rptRt.anchoredPosition = new Vector2(14f, 0f);
        rptRt.sizeDelta = Vector2.zero;
        TMP_Text repTmp = repTextObj.AddComponent<TextMeshProUGUI>();
        repTmp.text = "Replay Audio";
        repTmp.font = nunitoBold;
        repTmp.fontSize = 13.5f;
        repTmp.fontStyle = FontStyles.Bold;
        repTmp.alignment = TextAlignmentOptions.Center;
        repTmp.color = new Color32(91, 61, 39, 255);

        // Close Button (X)
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(headerObj.transform, false);
        RectTransform clsRt = closeBtnObj.AddComponent<RectTransform>();
        clsRt.anchorMin = new Vector2(1f, 0.5f);
        clsRt.anchorMax = new Vector2(1f, 0.5f);
        clsRt.pivot = new Vector2(1f, 0.5f);
        clsRt.anchoredPosition = new Vector2(0f, 0f);
        clsRt.sizeDelta = new Vector2(34f, 34f);

        Image clsImg = closeBtnObj.AddComponent<Image>();
        clsImg.sprite = bgPillDark;
        clsImg.type = Image.Type.Sliced;
        Button clsBtn = closeBtnObj.AddComponent<Button>();

        GameObject clsTextObj = new GameObject("Text");
        clsTextObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform clstRt = clsTextObj.AddComponent<RectTransform>();
        clstRt.anchorMin = Vector2.zero;
        clstRt.anchorMax = Vector2.one;
        clstRt.sizeDelta = Vector2.zero;
        TMP_Text clsTmp = clsTextObj.AddComponent<TextMeshProUGUI>();
        clsTmp.text = "X";
        clsTmp.font = nunitoBold;
        clsTmp.fontSize = 18f;
        clsTmp.fontStyle = FontStyles.Bold;
        clsTmp.alignment = TextAlignmentOptions.Center;
        clsTmp.color = Color.white;
        closeBtnObj.SetActive(false);

        // --- C. KONTEN SOAL (AutoLayout Dinamis: VerticalLayoutGroup + ContentSizeFitter) ---
        // Jika teks Jepang turun 2 baris, Romaji & English otomatis bergeser ke bawah tanpa tumpang tindih!
        GameObject contentObj = new GameObject("ContentGroup");
        contentObj.transform.SetParent(panelObj.transform, false);
        RectTransform cRt = contentObj.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0f, 1f);
        cRt.anchoredPosition = new Vector2(245f, -65f);
        cRt.sizeDelta = new Vector2(-280f, 0f);

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 8f;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Baris 1: Jepang (Font Size 42pt, Auto Wrap, Tinggi Otomatis)
        GameObject jTxtObj = new GameObject("TextJapanese");
        jTxtObj.transform.SetParent(contentObj.transform, false);
        RectTransform jRt = jTxtObj.AddComponent<RectTransform>();

        TMP_Text jTmp = jTxtObj.AddComponent<TextMeshProUGUI>();
        jTmp.text = "「このお花、綺麗に咲いたでしょう？」";
        jTmp.font = jpFont;
        jTmp.fontSize = 42f;
        jTmp.fontStyle = FontStyles.Bold;
        jTmp.color = new Color32(28, 22, 17, 255);
        jTmp.alignment = TextAlignmentOptions.TopLeft;
        jTmp.textWrappingMode = TextWrappingModes.Normal;

        ContentSizeFitter jCsf = jTxtObj.AddComponent<ContentSizeFitter>();
        jCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        jCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Baris 2: Romaji (Font Size 24pt, Biru Slate Figma, Auto Wrap, Tinggi Otomatis)
        GameObject rTxtObj = new GameObject("TextRomaji");
        rTxtObj.transform.SetParent(contentObj.transform, false);
        RectTransform rRt = rTxtObj.AddComponent<RectTransform>();

        TMP_Text rTmp = rTxtObj.AddComponent<TextMeshProUGUI>();
        rTmp.text = "“Kono ohana, kirei ni saita deshou?”";
        rTmp.font = nunitoBold;
        rTmp.fontSize = 24f;
        rTmp.fontStyle = FontStyles.Bold;
        rTmp.color = new Color32(43, 91, 132, 255);
        rTmp.alignment = TextAlignmentOptions.TopLeft;
        rTmp.textWrappingMode = TextWrappingModes.Normal;

        ContentSizeFitter rCsf = rTxtObj.AddComponent<ContentSizeFitter>();
        rCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Baris 3: Inggris (Font Size 22pt, Abu Arang Figma, Auto Wrap, Tinggi Otomatis)
        GameObject eTxtObj = new GameObject("TextEnglish");
        eTxtObj.transform.SetParent(contentObj.transform, false);
        RectTransform eRt = eTxtObj.AddComponent<RectTransform>();

        TMP_Text eTmp = eTxtObj.AddComponent<TextMeshProUGUI>();
        eTmp.text = "“These flowers bloomed beautifully, don't you think?”";
        eTmp.font = nunitoSemi;
        eTmp.fontSize = 22f;
        eTmp.color = new Color32(90, 84, 79, 255);
        eTmp.alignment = TextAlignmentOptions.TopLeft;
        eTmp.textWrappingMode = TextWrappingModes.Normal;

        ContentSizeFitter eCsf = eTxtObj.AddComponent<ContentSizeFitter>();
        eCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        eCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- D. CHOOSE PROMPT ("CHOOSE YOUR KAIWA RESPONSE [1 to 3]:") ---
        // Diposisikan stabil di atas ChoicesContainer
        GameObject promptObj = new GameObject("ChoosePromptText");
        promptObj.transform.SetParent(panelObj.transform, false);
        RectTransform cpRt = promptObj.AddComponent<RectTransform>();
        cpRt.anchorMin = new Vector2(0f, 0f);
        cpRt.anchorMax = new Vector2(1f, 0f);
        cpRt.pivot = new Vector2(0f, 0f);
        cpRt.anchoredPosition = new Vector2(245f, 118f);
        cpRt.sizeDelta = new Vector2(-280f, 22f);

        TMP_Text cpTmp = promptObj.AddComponent<TextMeshProUGUI>();
        cpTmp.text = "CHOOSE YOUR KAIWA RESPONSE [1 to 3]:";
        cpTmp.font = nunitoBold;
        cpTmp.fontSize = 13.5f;
        cpTmp.fontStyle = FontStyles.Bold;
        cpTmp.characterSpacing = 2f;
        cpTmp.color = new Color32(123, 115, 107, 255);
        cpTmp.alignment = TextAlignmentOptions.Left;

        // --- E. CHOICES CONTAINER (Tinggi 83px, Spacing 20px, Terpaku Stabil di Bawah Panel) ---
        GameObject choicesContainerObj = new GameObject("ChoicesContainer");
        choicesContainerObj.transform.SetParent(panelObj.transform, false);
        RectTransform ccRt = choicesContainerObj.AddComponent<RectTransform>();
        ccRt.anchorMin = new Vector2(0f, 0f);
        ccRt.anchorMax = new Vector2(1f, 0f);
        ccRt.pivot = new Vector2(0f, 0f);
        ccRt.anchoredPosition = new Vector2(245f, 25f);
        ccRt.sizeDelta = new Vector2(-280f, 83f);

        HorizontalLayoutGroup hlg = choicesContainerObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // --- F. SCORE BADGE PILL (Teks Bersih "+100 Points") ---
        GameObject scoreBadgeObj = new GameObject("ScoreBadgePill");
        scoreBadgeObj.transform.SetParent(panelObj.transform, false);
        RectTransform sbRt = scoreBadgeObj.AddComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 0f);
        sbRt.anchorMax = new Vector2(1f, 0f);
        sbRt.pivot = new Vector2(0f, 0f);
        sbRt.anchoredPosition = new Vector2(245f, 118f);
        sbRt.sizeDelta = new Vector2(240f, 32f);

        Image sbImg = scoreBadgeObj.AddComponent<Image>();
        sbImg.enabled = false;

        GameObject sbTextObj = new GameObject("Text");
        sbTextObj.transform.SetParent(scoreBadgeObj.transform, false);
        RectTransform sbtRt = sbTextObj.AddComponent<RectTransform>();
        sbtRt.anchorMin = Vector2.zero;
        sbtRt.anchorMax = Vector2.one;
        sbtRt.offsetMin = Vector2.zero;
        sbtRt.offsetMax = Vector2.zero;

        TMP_Text sbTmp = sbTextObj.AddComponent<TextMeshProUGUI>();
        sbTmp.text = "+100 Points";
        sbTmp.font = nunitoBold;
        sbTmp.fontSize = 15f;
        sbTmp.fontStyle = FontStyles.Bold;
        sbTmp.color = new Color32(190, 120, 20, 255);
        sbTmp.alignment = TextAlignmentOptions.Left;
        sbTmp.raycastTarget = false;
        scoreBadgeObj.SetActive(false);

        // --- G. SAMBUNGKAN KE DIALOGUEMANAGER ---
        DialogueManager diagMgr = Object.FindAnyObjectByType<DialogueManager>();
        if (diagMgr == null)
        {
            GameObject dmObj = new GameObject("DialogueManager");
            diagMgr = dmObj.AddComponent<DialogueManager>();
        }

        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        AudioSource aSrc = diagMgr.GetComponent<AudioSource>();
        if (aSrc == null) aSrc = diagMgr.gameObject.AddComponent<AudioSource>();
        AudioClip blipVoice = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sans-talk-sound.mp3");
        if (blipVoice != null) aSrc.clip = blipVoice;

        Transform promptTr = canvas.transform.Find("InteractionPrompt");
        GameObject promptObjRef = promptTr != null ? promptTr.gameObject : null;

        SetPrivateField(diagMgr, "dialoguePanel", panelObj);
        SetPrivateField(diagMgr, "interactionPrompt", promptObjRef);
        SetPrivateField(diagMgr, "avatarImage", avFace);
        SetPrivateField(diagMgr, "nameText", nameTmp);
        SetPrivateField(diagMgr, "roleTagText", roleTmp);
        SetPrivateField(diagMgr, "jlptTagText", jlptTmp);
        SetPrivateField(diagMgr, "replayAudioButton", repBtn);
        SetPrivateField(diagMgr, "closeButton", clsBtn);
        SetPrivateField(diagMgr, "textJapanese", jTmp);
        SetPrivateField(diagMgr, "textRomaji", rTmp);
        SetPrivateField(diagMgr, "textEnglish", eTmp);
        SetPrivateField(diagMgr, "choosePromptText", cpTmp);
        SetPrivateField(diagMgr, "choicesContainer", choicesContainerObj.transform);
        SetPrivateField(diagMgr, "choiceButtonPrefab", choiceBtnPrefab);
        SetPrivateField(diagMgr, "scoreBadgePill", scoreBadgeObj);
        SetPrivateField(diagMgr, "scoreBadgeText", sbTmp);
        SetPrivateField(diagMgr, "choiceCardNormalBg", bgBtnUnselected);
        SetPrivateField(diagMgr, "choiceCardSelectedBg", bgBtnSelected);
        SetPrivateField(diagMgr, "audioSource", aSrc);
        SetPrivateField(diagMgr, "playerController", player);

        panelObj.SetActive(false);

        EditorUtility.SetDirty(diagMgr);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green>[Utsuru] SUKSES! AutoLayout Dinamis telah diterapkan (Teks soal & pilihan tidak akan pernah saling menimpa)!</color>");
    }

    private static GameObject CreateOrUpdateChoiceButtonPrefab(TMP_FontAsset jpFont, TMP_FontAsset rmFont, TMP_FontAsset numFont, Sprite bgCard, Sprite bgCircle)
    {
        string prefabPath = "Assets/Prefabs/ChoiceButtonPrefab.prefab";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject root = new GameObject("ChoiceButtonPrefab");
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(430f, 83f);

        Image bgImg = root.AddComponent<Image>();
        bgImg.sprite = bgCard;
        bgImg.type = Image.Type.Sliced;

        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = bgImg;

        ChoiceButtonUI cbUI = root.AddComponent<ChoiceButtonUI>();
        cbUI.button = btn;
        cbUI.backgroundImage = bgImg;

        // Number Badge Circle (40x40 px)
        GameObject badgeObj = new GameObject("NumberBadge");
        badgeObj.transform.SetParent(root.transform, false);
        RectTransform bRt = badgeObj.AddComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0f, 0.5f);
        bRt.anchorMax = new Vector2(0f, 0.5f);
        bRt.pivot = new Vector2(0f, 0.5f);
        bRt.anchoredPosition = new Vector2(16f, 0f);
        bRt.sizeDelta = new Vector2(40f, 40f);

        Image bImg = badgeObj.AddComponent<Image>();
        bImg.sprite = bgCircle;
        bImg.color = new Color32(91, 61, 39, 255);
        cbUI.numberBadgeBackground = bImg;

        GameObject badgeTextObj = new GameObject("BadgeText");
        badgeTextObj.transform.SetParent(badgeObj.transform, false);
        RectTransform btRt = badgeTextObj.AddComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.sizeDelta = Vector2.zero;

        TMP_Text bTmp = badgeTextObj.AddComponent<TextMeshProUGUI>();
        bTmp.text = "1";
        bTmp.font = numFont;
        bTmp.fontSize = 20f;
        bTmp.fontStyle = FontStyles.Bold;
        bTmp.color = Color.white;
        bTmp.alignment = TextAlignmentOptions.Center;
        cbUI.numberBadgeText = bTmp;

        // Content Column dengan VerticalLayoutGroup (AutoLayout agar teks Jepang dan Romaji saling mendorong secara dinamis)
        GameObject colObj = new GameObject("TextColumn");
        colObj.transform.SetParent(root.transform, false);
        RectTransform colRt = colObj.AddComponent<RectTransform>();
        colRt.anchorMin = new Vector2(0f, 0f);
        colRt.anchorMax = new Vector2(1f, 1f);
        colRt.pivot = new Vector2(0f, 0.5f);
        colRt.anchoredPosition = new Vector2(66f, 0f);
        colRt.sizeDelta = new Vector2(-76f, 0f);

        VerticalLayoutGroup vlg = colObj.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(0, 0, 6, 6);

        // Teks Jepang (Auto-Sizing 17-22pt agar teks panjang muat 1 baris, dan jika wrap tetap terdorong rapi)
        GameObject jpTxtObj = new GameObject("JapaneseText");
        jpTxtObj.transform.SetParent(colObj.transform, false);

        TMP_Text jpTmp = jpTxtObj.AddComponent<TextMeshProUGUI>();
        jpTmp.text = "手入れが素晴らしいですね！";
        jpTmp.font = jpFont;
        jpTmp.fontSize = 22f;
        jpTmp.enableAutoSizing = true;
        jpTmp.fontSizeMin = 16f;
        jpTmp.fontSizeMax = 22f;
        jpTmp.fontStyle = FontStyles.Bold;
        jpTmp.color = new Color32(28, 22, 17, 255);
        jpTmp.alignment = TextAlignmentOptions.Left;
        jpTmp.textWrappingMode = TextWrappingModes.Normal;
        cbUI.japaneseText = jpTmp;

        ContentSizeFitter jpCsf = jpTxtObj.AddComponent<ContentSizeFitter>();
        jpCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        jpCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Teks Romaji (Auto-Sizing 12-16pt)
        GameObject roTxtObj = new GameObject("RomajiText");
        roTxtObj.transform.SetParent(colObj.transform, false);

        TMP_Text roTmp = roTxtObj.AddComponent<TextMeshProUGUI>();
        roTmp.text = "Teire ga subarashii desu ne!";
        roTmp.font = rmFont;
        roTmp.fontSize = 16f;
        roTmp.enableAutoSizing = true;
        roTmp.fontSizeMin = 12f;
        roTmp.fontSizeMax = 16f;
        roTmp.color = new Color32(110, 104, 97, 255);
        roTmp.alignment = TextAlignmentOptions.Left;
        roTmp.textWrappingMode = TextWrappingModes.Normal;
        cbUI.romajiText = roTmp;

        ContentSizeFitter roCsf = roTxtObj.AddComponent<ContentSizeFitter>();
        roCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        roCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);
        return prefab;
    }

    private static void GenerateCapsuleTexture(string path, int w, int h, float r, Color32 fillColor, Color32 strokeColor, Vector4 border)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32 transparent = new Color32(0, 0, 0, 0);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float cx = (x < r) ? r : ((x > w - r) ? (w - r) : x);
                float cy = h * 0.5f;
                float px = x + 0.5f;
                float py = y + 0.5f;
                float dist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));

                if (dist <= r - 2.0f)
                {
                    tex.SetPixel(x, y, fillColor);
                }
                else if (dist <= r - 0.5f)
                {
                    float blend = Mathf.Clamp01(dist - (r - 2.0f));
                    Color c = Color.Lerp(fillColor, strokeColor, blend);
                    tex.SetPixel(x, y, c);
                }
                else if (dist <= r + 0.5f)
                {
                    float alpha = Mathf.Clamp01(1f - (dist - (r - 0.5f)));
                    Color c = strokeColor;
                    c.a *= alpha;
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        ConfigureSpriteImporter(path, border);
    }

    private static void ConfigureSpriteImporter(string path, Vector4 borders)
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
            if (importer.spriteBorder != borders)
            {
                importer.spriteBorder = borders;
                changed = true;
            }
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static void EnsureFontFallbacks(TMP_FontAsset baseFont, TMP_FontAsset fallbackFont)
    {
        if (baseFont == null || fallbackFont == null) return;
        if (baseFont.fallbackFontAssetTable == null)
        {
            baseFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
        }
        if (!baseFont.fallbackFontAssetTable.Contains(fallbackFont))
        {
            baseFont.fallbackFontAssetTable.Add(fallbackFont);
            EditorUtility.SetDirty(baseFont);
            AssetDatabase.SaveAssets();
        }
    }

    private static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static TMP_FontAsset LoadFont(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
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
