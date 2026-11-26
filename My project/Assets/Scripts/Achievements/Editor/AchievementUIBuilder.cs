using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Utsuru.Achievements.Editor
{
    /// <summary>
    /// Automatically builds and wires the Achievements UI & AchievementManager into Day_Showcase.unity,
    /// matching the Utsuru reference visual design and enabling keybind 'Q' + background Gaussian blur.
    /// </summary>
    [InitializeOnLoad]
    public static class AchievementUIBuilder
    {
        private const string DayShowcaseScenePath = "Assets/Japanese_Street/Scenes/Day_Showcase.unity";

        static AchievementUIBuilder()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureSetupOnEditorLoad;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (Application.isPlaying) return;
            if (scene.path == DayShowcaseScenePath)
            {
                EnsureAchievementsInActiveScene(saveScene: false);
            }
        }

        private static void EnsureSetupOnEditorLoad()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            AchievementAssetImporter.ConfigureAssets();

            Scene active = EditorSceneManager.GetActiveScene();
            if (active.IsValid() && active.path == DayShowcaseScenePath)
            {
                EnsureAchievementsInActiveScene(saveScene: !active.isDirty);
                return;
            }

            // If Day_Showcase.unity hasn't had Achievements built into it yet and active scene has no unsaved edits,
            // open Day_Showcase briefly, inject Achievements cleanly, save it, and restore the previous scene.
            string fullScenePath = Path.GetFullPath(DayShowcaseScenePath);
            if (File.Exists(fullScenePath) && !active.isDirty)
            {
                string yaml = File.ReadAllText(fullScenePath);
                if (!yaml.Contains("AchievementUIController"))
                {
                    string prevPath = active.path;
                    Scene dayScene = EditorSceneManager.OpenScene(DayShowcaseScenePath, OpenSceneMode.Single);
                    if (dayScene.IsValid())
                    {
                        EnsureAchievementsInActiveScene(saveScene: true);
                        if (!string.IsNullOrEmpty(prevPath) && prevPath != DayShowcaseScenePath && File.Exists(Path.GetFullPath(prevPath)))
                        {
                            EditorSceneManager.OpenScene(prevPath, OpenSceneMode.Single);
                        }
                    }
                }
            }
        }

        [MenuItem("Tools/Utsuru/Build Achievements UI")]
        public static void BuildUIMenuItem()
        {
            AchievementAssetImporter.ConfigureAssets();
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.path != DayShowcaseScenePath && File.Exists(Path.GetFullPath(DayShowcaseScenePath)))
            {
                if (active.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                EditorSceneManager.OpenScene(DayShowcaseScenePath, OpenSceneMode.Single);
            }
            BuildUI(saveScene: true);
        }

        private static void EnsureAchievementsInActiveScene(bool saveScene)
        {
            if (Object.FindFirstObjectByType<AchievementUIController>() != null &&
                Object.FindFirstObjectByType<AchievementManager>() != null)
            {
                return;
            }
            BuildUI(saveScene);
        }

        public static void BuildUI(bool saveScene = true)
        {
            AchievementAssetImporter.ConfigureAssets();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[AchievementUIBuilder] Canvas not found in active scene.");
                return;
            }

            // 1. Ensure AchievementManager exists in scene
            AchievementManager manager = Object.FindFirstObjectByType<AchievementManager>();
            if (manager == null)
            {
                GameObject mgrGo = new GameObject("AchievementManager");
                Undo.RegisterCreatedObjectUndo(mgrGo, "Create AchievementManager");
                manager = mgrGo.AddComponent<AchievementManager>();
            }

            // 2. Load Sprites
            Sprite panelBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Achievements-Panel-Bg.png");
            Sprite boxSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Achievements-Box.png");
            Sprite medalBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Achievements-Medal-Rectangle-Background.png");
            Sprite medalIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Achievements-Medal-Icon.png");
            Sprite greenCheckSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Green-Check-Icon.png");
            Sprite dividerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Brown-Vertical-Line.png");
            Sprite starsSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Stars-Point-Icon.png");
            Sprite closeBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Close-Button-Shape.png");
            Sprite collectBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Collect-Button-Shape.png");
            Sprite scrollTrackSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Achievements-Scrollbar-Track.png");
            Sprite scrollHandleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Achievements-Scrollbar-Handle.png");
            Material blurMat = AchievementAssetImporter.EnsureBlurMaterial();

            // 3. Load Fonts
            TMP_FontAsset balooExtraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Baloo_2/static/Baloo2-ExtraBold SDF.asset")
                ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Baloo-Regular SDF 1.asset")
                ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Baloo-Regular SDF.asset");
            TMP_FontAsset balooBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Baloo_2/static/Baloo2-Bold SDF.asset")
                ?? balooExtraBold;
            TMP_FontAsset nunitoBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset")
                ?? balooBold;

            if (balooExtraBold != null) balooExtraBold.ReadFontAssetDefinition();
            if (balooBold != null) balooBold.ReadFontAssetDefinition();
            if (nunitoBold != null) nunitoBold.ReadFontAssetDefinition();

            Color darkBrown = new Color(0.373f, 0.224f, 0.137f, 1f);    // #5F3923
            Color mediumBrown = new Color(0.471f, 0.322f, 0.231f, 1f);  // #78523B

            // 4. Rebuild Canvas/Achievements cleanly
            Transform existingRoot = canvas.transform.Find("Achievements");
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot.gameObject);
            }

            GameObject achRoot = CreateUIObject("Achievements", canvas.transform);
            Undo.RegisterCreatedObjectUndo(achRoot, "Create Achievements UI");
            SetStretch(achRoot.GetComponent<RectTransform>());

            // Position sibling right before PauseMenuPanel (so PauseMenu stays on top if ever opened, and Achievements covers HUD)
            Transform pauseTr = canvas.transform.Find("PauseMenuPanel");
            if (pauseTr != null)
            {
                achRoot.transform.SetSiblingIndex(pauseTr.GetSiblingIndex());
            }

            AchievementUIController controller = achRoot.AddComponent<AchievementUIController>();

            // 5. AchievementsView (Toggled active/inactive)
            GameObject viewGo = CreateUIObject("AchievementsView", achRoot.transform);
            SetStretch(viewGo.GetComponent<RectTransform>());
            CanvasGroup viewCg = viewGo.AddComponent<CanvasGroup>();
            viewCg.alpha = 0f;

            // 5.1 BlurBackdrop (RawImage showing multi-pass Gaussian blurred 3D background)
            GameObject blurGo = CreateUIObject("BlurBackdrop", viewGo.transform);
            SetStretch(blurGo.GetComponent<RectTransform>());
            RawImage blurRaw = blurGo.AddComponent<RawImage>();
            blurRaw.color = Color.white;
            blurRaw.raycastTarget = true;
            blurRaw.enabled = false;

            // 5.2 TintOverlay (subtle dark warm overlay for depth)
            GameObject tintGo = CreateUIObject("TintOverlay", viewGo.transform);
            SetStretch(tintGo.GetComponent<RectTransform>());
            Image tintImg = tintGo.AddComponent<Image>();
            tintImg.color = new Color(0.08f, 0.05f, 0.03f, 0.22f);
            tintImg.raycastTarget = true;

            // 5.3 MainPanel (1300 x 820 centered rounded cream card)
            GameObject panelGo = CreateUIObject("MainPanel", viewGo.transform);
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(1300f, 820f);
            panelRt.anchoredPosition = Vector2.zero;

            Image panelImg = panelGo.AddComponent<Image>();
            if (panelBgSprite != null)
            {
                panelImg.sprite = panelBgSprite;
                panelImg.type = Image.Type.Sliced;
                panelImg.color = Color.white;
            }
            else
            {
                panelImg.color = new Color(0.98f, 0.957f, 0.922f, 1f);
            }
            panelImg.raycastTarget = true;

            // 5.4 HeaderTitle ("ACHIEVEMENT")
            GameObject headerGo = CreateUIObject("HeaderTitle", panelGo.transform);
            RectTransform headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.5f, 1f);
            headerRt.anchorMax = new Vector2(0.5f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(800f, 84f);
            headerRt.anchoredPosition = new Vector2(0f, -34f);

            TextMeshProUGUI headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
            if (balooExtraBold != null) headerTmp.font = balooExtraBold;
            headerTmp.text = "ACHIEVEMENT";
            headerTmp.fontSize = 62f;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color = darkBrown;
            headerTmp.characterSpacing = 2f;

            Shadow headerShadow = headerGo.AddComponent<Shadow>();
            headerShadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            headerShadow.effectDistance = new Vector2(2f, -4f);

            // 5.5 ScrollView
            GameObject scrollGo = CreateUIObject("ScrollView", panelGo.transform);
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.sizeDelta = new Vector2(1130f, 548f);
            scrollRt.anchoredPosition = new Vector2(12f, 10f);

            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Viewport
            GameObject viewportGo = CreateUIObject("Viewport", scrollGo.transform);
            RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(0f, 0f);
            viewportRt.offsetMax = new Vector2(-36f, 0f);
            viewportGo.AddComponent<RectMask2D>();
            Image viewportRayImg = viewportGo.AddComponent<Image>();
            viewportRayImg.color = new Color(0f, 0f, 0f, 0f);
            viewportRayImg.raycastTarget = true;

            // Content
            GameObject contentGo = CreateUIObject("Content", viewportGo.transform);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Vertical Scrollbar
            GameObject scrollbarGo = CreateUIObject("Scrollbar Vertical", scrollGo.transform);
            RectTransform sbRt = scrollbarGo.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0.5f);
            sbRt.anchorMax = new Vector2(1f, 0.5f);
            sbRt.pivot = new Vector2(1f, 0.5f);
            sbRt.sizeDelta = new Vector2(12f, 500f);
            sbRt.anchoredPosition = new Vector2(-4f, 0f);

            Image sbTrackImg = scrollbarGo.AddComponent<Image>();
            if (scrollTrackSprite != null)
            {
                sbTrackImg.sprite = scrollTrackSprite;
                sbTrackImg.type = Image.Type.Sliced;
                sbTrackImg.color = Color.white;
            }
            else
            {
                sbTrackImg.color = new Color(0.89f, 0.85f, 0.79f, 0.9f);
            }

            Scrollbar scrollbar = scrollbarGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            GameObject slidingGo = CreateUIObject("Sliding Area", scrollbarGo.transform);
            SetStretch(slidingGo.GetComponent<RectTransform>());

            GameObject handleGo = CreateUIObject("Handle", slidingGo.transform);
            SetStretch(handleGo.GetComponent<RectTransform>());
            Image handleImg = handleGo.AddComponent<Image>();
            if (scrollHandleSprite != null)
            {
                handleImg.sprite = scrollHandleSprite;
                handleImg.type = Image.Type.Sliced;
                handleImg.color = Color.white;
            }
            else
            {
                handleImg.color = new Color(0.45f, 0.35f, 0.25f, 1f);
            }
            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect = handleGo.GetComponent<RectTransform>();

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            // 5.6 Achievement Item Template (inside Content, inactive)
            GameObject itemGo = CreateUIObject("AchievementItemTemplate", contentGo.transform);
            RectTransform itemRt = itemGo.GetComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(1020f, 166f);

            Image itemBgImg = itemGo.AddComponent<Image>();
            if (boxSprite != null)
            {
                itemBgImg.sprite = boxSprite;
                itemBgImg.type = Image.Type.Sliced;
                itemBgImg.color = Color.white;
            }
            else
            {
                itemBgImg.color = new Color(0.95f, 0.89f, 0.81f, 1f);
            }

            AchievementItemUI itemUI = itemGo.AddComponent<AchievementItemUI>();

            // Medal Frame (left square)
            GameObject medalFrameGo = CreateUIObject("MedalFrame", itemGo.transform);
            RectTransform medalFrameRt = medalFrameGo.GetComponent<RectTransform>();
            medalFrameRt.anchorMin = new Vector2(0.5f, 0.5f);
            medalFrameRt.anchorMax = new Vector2(0.5f, 0.5f);
            medalFrameRt.pivot = new Vector2(0.5f, 0.5f);
            medalFrameRt.sizeDelta = new Vector2(130f, 130f);
            medalFrameRt.anchoredPosition = new Vector2(-410f, 0f);
            Image medalFrameImg = medalFrameGo.AddComponent<Image>();
            if (medalBgSprite != null) medalFrameImg.sprite = medalBgSprite;
            medalFrameImg.preserveAspect = true;

            // Medal Icon
            GameObject medalIconGo = CreateUIObject("MedalIcon", medalFrameGo.transform);
            RectTransform medalIconRt = medalIconGo.GetComponent<RectTransform>();
            medalIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            medalIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            medalIconRt.pivot = new Vector2(0.5f, 0.5f);
            medalIconRt.sizeDelta = new Vector2(78f, 110f);
            medalIconRt.anchoredPosition = Vector2.zero;
            Image medalIconImg = medalIconGo.AddComponent<Image>();
            if (medalIconSprite != null) medalIconImg.sprite = medalIconSprite;
            medalIconImg.preserveAspect = true;

            // Green Checkmark (shown when unlocked)
            GameObject checkGo = CreateUIObject("GreenCheck", medalFrameGo.transform);
            RectTransform checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.5f, 0.5f);
            checkRt.anchorMax = new Vector2(0.5f, 0.5f);
            checkRt.pivot = new Vector2(0.5f, 0.5f);
            checkRt.sizeDelta = new Vector2(104f, 102f);
            checkRt.anchoredPosition = new Vector2(4f, 0f);
            Image checkImg = checkGo.AddComponent<Image>();
            if (greenCheckSprite != null) checkImg.sprite = greenCheckSprite;
            checkImg.preserveAspect = true;
            checkGo.SetActive(false);

            // Title Text
            GameObject titleGo = CreateUIObject("TitleText", itemGo.transform);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(510f, 44f);
            titleRt.anchoredPosition = new Vector2(-64f, 38f);
            TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            if (balooExtraBold != null) titleTmp.font = balooExtraBold;
            titleTmp.text = "Achievement Title";
            titleTmp.fontSize = 34f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.color = darkBrown;

            // Description Text
            GameObject descGo = CreateUIObject("DescriptionText", itemGo.transform);
            RectTransform descRt = descGo.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.5f, 0.5f);
            descRt.anchorMax = new Vector2(0.5f, 0.5f);
            descRt.pivot = new Vector2(0.5f, 0.5f);
            descRt.sizeDelta = new Vector2(510f, 70f);
            descRt.anchoredPosition = new Vector2(-64f, -22f);
            TextMeshProUGUI descTmp = descGo.AddComponent<TextMeshProUGUI>();
            if (nunitoBold != null) descTmp.font = nunitoBold;
            descTmp.text = "Achievement description text.";
            descTmp.fontSize = 20f;
            descTmp.fontStyle = FontStyles.Bold;
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.color = mediumBrown;
            descTmp.textWrappingMode = TextWrappingModes.Normal;

            // Brown Vertical Divider Line
            GameObject dividerGo = CreateUIObject("DividerLine", itemGo.transform);
            RectTransform dividerRt = dividerGo.GetComponent<RectTransform>();
            dividerRt.anchorMin = new Vector2(0.5f, 0.5f);
            dividerRt.anchorMax = new Vector2(0.5f, 0.5f);
            dividerRt.pivot = new Vector2(0.5f, 0.5f);
            dividerRt.sizeDelta = new Vector2(5f, 116f);
            dividerRt.anchoredPosition = new Vector2(224f, 0f);
            Image dividerImg = dividerGo.AddComponent<Image>();
            if (dividerSprite != null) dividerImg.sprite = dividerSprite;
            else dividerImg.color = mediumBrown;

            // Stars Icon
            GameObject starsGo = CreateUIObject("StarsIcon", itemGo.transform);
            RectTransform starsRt = starsGo.GetComponent<RectTransform>();
            starsRt.anchorMin = new Vector2(0.5f, 0.5f);
            starsRt.anchorMax = new Vector2(0.5f, 0.5f);
            starsRt.pivot = new Vector2(0.5f, 0.5f);
            starsRt.sizeDelta = new Vector2(144f, 78f);
            starsRt.anchoredPosition = new Vector2(368f, 18f);
            Image starsImg = starsGo.AddComponent<Image>();
            if (starsSprite != null) starsImg.sprite = starsSprite;
            starsImg.preserveAspect = true;

            // Green Vector Checkmark over Stars (shown after points are collected)
            GameObject starsCheckGo = CreateUIObject("StarsCheck", starsGo.transform);
            RectTransform starsCheckRt = starsCheckGo.GetComponent<RectTransform>();
            starsCheckRt.anchorMin = new Vector2(0.5f, 0.5f);
            starsCheckRt.anchorMax = new Vector2(0.5f, 0.5f);
            starsCheckRt.pivot = new Vector2(0.5f, 0.5f);
            starsCheckRt.sizeDelta = new Vector2(84f, 82f);
            starsCheckRt.anchoredPosition = new Vector2(4f, 0f);
            Image starsCheckImg = starsCheckGo.AddComponent<Image>();
            if (greenCheckSprite != null) starsCheckImg.sprite = greenCheckSprite;
            starsCheckImg.preserveAspect = true;
            starsCheckGo.SetActive(false);

            // Points Text
            GameObject ptsGo = CreateUIObject("PointsText", itemGo.transform);
            RectTransform ptsRt = ptsGo.GetComponent<RectTransform>();
            ptsRt.anchorMin = new Vector2(0.5f, 0.5f);
            ptsRt.anchorMax = new Vector2(0.5f, 0.5f);
            ptsRt.pivot = new Vector2(0.5f, 0.5f);
            ptsRt.sizeDelta = new Vector2(160f, 40f);
            ptsRt.anchoredPosition = new Vector2(368f, -38f);
            TextMeshProUGUI ptsTmp = ptsGo.AddComponent<TextMeshProUGUI>();
            if (balooExtraBold != null) ptsTmp.font = balooExtraBold;
            ptsTmp.text = "100";
            ptsTmp.fontSize = 32f;
            ptsTmp.fontStyle = FontStyles.Bold;
            ptsTmp.alignment = TextAlignmentOptions.Center;
            ptsTmp.color = darkBrown;

            // Collect Button (shown when achievement completed but reward not yet claimed)
            GameObject collectGo = CreateUIObject("CollectButton", itemGo.transform);
            RectTransform collectRt = collectGo.GetComponent<RectTransform>();
            collectRt.anchorMin = new Vector2(0.5f, 0.5f);
            collectRt.anchorMax = new Vector2(0.5f, 0.5f);
            collectRt.pivot = new Vector2(0.5f, 0.5f);
            collectRt.sizeDelta = new Vector2(174f, 56f);
            collectRt.anchoredPosition = new Vector2(368f, -28f);

            Image collectImg = collectGo.AddComponent<Image>();
            if (collectBtnSprite != null)
            {
                collectImg.sprite = collectBtnSprite;
                collectImg.type = Image.Type.Simple;
                collectImg.preserveAspect = true;
                collectImg.color = Color.white;
            }
            else
            {
                collectImg.color = new Color(0.298f, 0.651f, 0.282f, 1f);
            }

            Button collectBtn = collectGo.AddComponent<Button>();
            collectBtn.targetGraphic = collectImg;
            ColorBlock cb = collectBtn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.92f, 1f, 0.92f, 1f);
            cb.pressedColor = new Color(0.78f, 0.9f, 0.78f, 1f);
            collectBtn.colors = cb;

            GameObject collectTxtGo = CreateUIObject("Text", collectGo.transform);
            SetStretch(collectTxtGo.GetComponent<RectTransform>());
            TextMeshProUGUI collectTmp = collectTxtGo.AddComponent<TextMeshProUGUI>();
            if (balooBold != null) collectTmp.font = balooBold;
            collectTmp.text = "Collect";
            collectTmp.fontSize = 23f;
            collectTmp.fontStyle = FontStyles.Bold;
            collectTmp.alignment = TextAlignmentOptions.Center;
            collectTmp.color = Color.white;
            collectGo.SetActive(false);

            itemUI.ConfigureReferences(titleTmp, descTmp, ptsTmp, medalFrameImg, medalIconImg, checkGo, starsImg, starsCheckGo, collectBtn);
            itemGo.SetActive(false);

            // 5.7 Close Button
            GameObject closeGo = CreateUIObject("CloseButton", panelGo.transform);
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.sizeDelta = new Vector2(190f, 84f);
            closeRt.anchoredPosition = new Vector2(0f, 34f);

            Image closeImg = closeGo.AddComponent<Image>();
            if (closeBtnSprite != null)
            {
                closeImg.sprite = closeBtnSprite;
                closeImg.preserveAspect = true;
            }
            else
            {
                closeImg.color = new Color(0.29f, 0.18f, 0.106f, 1f);
            }
            Button closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;

            GameObject closeTxtGo = CreateUIObject("Text", closeGo.transform);
            SetStretch(closeTxtGo.GetComponent<RectTransform>());
            TextMeshProUGUI closeTmp = closeTxtGo.AddComponent<TextMeshProUGUI>();
            if (balooBold != null) closeTmp.font = balooBold;
            closeTmp.text = "Close";
            closeTmp.fontSize = 30f;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;

            viewGo.SetActive(false);

            // 6. Wire Serialized Fields on AchievementUIController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("viewRoot").objectReferenceValue = viewGo;
            so.FindProperty("viewCanvasGroup").objectReferenceValue = viewCg;
            so.FindProperty("mainPanelRt").objectReferenceValue = panelRt;
            so.FindProperty("blurBackdrop").objectReferenceValue = blurRaw;
            so.FindProperty("tintOverlay").objectReferenceValue = tintImg;
            so.FindProperty("blurMaterial").objectReferenceValue = blurMat;
            so.FindProperty("contentContainer").objectReferenceValue = contentGo.transform;
            so.FindProperty("itemTemplate").objectReferenceValue = itemUI;
            so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();

            Scene currentScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(currentScene);
            if (saveScene)
            {
                EditorSceneManager.SaveScene(currentScene);
                Debug.Log("[AchievementUIBuilder] Achievements UI built, wired, and scene saved.");
            }
            else
            {
                Debug.Log("[AchievementUIBuilder] Achievements UI built and wired into active scene.");
            }
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
