#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utsuru.Capture;
using Utsuru.Course;
using Utsuru.DictionaryUI;

namespace Utsuru.Course.Editor
{
    public static class DictionaryCourseUIBuilder
    {
        private static Sprite defaultSprite;
        private static TMP_FontAsset defaultFont;

        [MenuItem("Utsuru/Build Course UI (Option C - Magazine)")]
        public static void BuildCourseUI()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                string showcasePath = "Assets/Japanese_Street/Scenes/Day_Showcase.unity";
                if (File.Exists(showcasePath))
                {
                    activeScene = EditorSceneManager.OpenScene(showcasePath);
                    canvasGo = GameObject.Find("Canvas");
                }
            }

            if (canvasGo == null)
            {
                Debug.LogError("[DictionaryCourseUIBuilder] GameObject 'Canvas' not found in scene!");
                return;
            }

            Transform dictTransform = canvasGo.transform.Find("Dictionary");
            if (dictTransform == null)
            {
                Debug.LogError("[DictionaryCourseUIBuilder] GameObject 'Dictionary' not found under Canvas!");
                return;
            }

            Transform viewRoot = dictTransform.Find("DictionaryView");
            if (viewRoot == null)
            {
                Debug.LogError("[DictionaryCourseUIBuilder] GameObject 'DictionaryView' not found!");
                return;
            }

            Transform book = viewRoot.Find("BookContainer");
            if (book == null)
            {
                Debug.LogError("[DictionaryCourseUIBuilder] GameObject 'BookContainer' not found!");
                return;
            }

            defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            
            // Setup font with Japanese support (Dynamic atlas) & Fallback font table
            defaultFont = SetupJapaneseFontSupport();

            // 1. Ensure CourseDatabase is on Managers GameObject
            GameObject managersGo = GameObject.Find("Managers");
            CourseDatabase courseDb = null;
            if (managersGo != null)
            {
                courseDb = managersGo.GetComponent<CourseDatabase>();
                if (courseDb == null)
                {
                    courseDb = Undo.AddComponent<CourseDatabase>(managersGo);
                    Debug.Log("[DictionaryCourseUIBuilder] Added CourseDatabase to 'Managers' GameObject.");
                }
            }
            else
            {
                courseDb = dictTransform.GetComponent<CourseDatabase>();
                if (courseDb == null)
                {
                    courseDb = Undo.AddComponent<CourseDatabase>(dictTransform.gameObject);
                }
            }

            // 2. Prepare Tab_Course_Panel
            Transform coursePanelTransform = book.Find("Tab_Course_Panel");
            GameObject coursePanel;
            if (coursePanelTransform == null)
            {
                coursePanel = CreateUIObject("Tab_Course_Panel", book);
            }
            else
            {
                coursePanel = coursePanelTransform.gameObject;
                int childCount = coursePanel.transform.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(coursePanel.transform.GetChild(i).gameObject);
                }
            }

            RectTransform cpRt = coursePanel.GetComponent<RectTransform>();
            SetStretchMargins(cpRt, 20f, 20f, 20f, 20f);

            var courseTabUI = coursePanel.GetComponent<DictionaryCourseTabUI>();
            if (courseTabUI == null)
            {
                courseTabUI = Undo.AddComponent<DictionaryCourseTabUI>(coursePanel);
            }

            // 3. Build Quiz Modal under viewRoot
            Transform oldQuizModal = viewRoot.Find("Modal_CourseQuiz");
            if (oldQuizModal != null)
            {
                Undo.DestroyObjectImmediate(oldQuizModal.gameObject);
            }
            GameObject quizModalGo = BuildQuizModal(viewRoot);
            var quizUI = quizModalGo.GetComponent<CourseQuizUI>();

            // 4. Build Magazine Spread layout in Tab_Course_Panel
            BuildMagazineSpread(coursePanel, courseTabUI, courseDb, quizUI);

            // 5. Connect references to DictionaryUIController
            var dictCtrl = dictTransform.GetComponent<DictionaryUIController>();
            if (dictCtrl != null)
            {
                SerializedObject soCtrl = new SerializedObject(dictCtrl);
                var propCoursePanel = soCtrl.FindProperty("coursePanel");
                if (propCoursePanel != null) propCoursePanel.objectReferenceValue = coursePanel;

                var propCourseTabCtrl = soCtrl.FindProperty("courseTabController");
                if (propCourseTabCtrl != null) propCourseTabCtrl.objectReferenceValue = courseTabUI;

                soCtrl.ApplyModifiedProperties();
                Debug.Log("[DictionaryCourseUIBuilder] Connected to DictionaryUIController.");
            }

            coursePanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[DictionaryCourseUIBuilder] Course UI (Option C - Magazine Spread) BUILT SUCCESSFULLY!");
        }

        private static TMP_FontAsset SetupJapaneseFontSupport()
        {
            TMP_FontAsset baseTmpFont = TMP_Settings.defaultFontAsset;
            string fontAssetPath = "Assets/Fonts/NotoSansJP SDF.asset";
            string ttfPath = "Assets/Fonts/NotoSansJP.ttf";

            TMP_FontAsset jpFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);

            if (jpFontAsset == null)
            {
                // Ensure TTF exists
                if (!File.Exists(ttfPath))
                {
                    string winFont = @"C:\Windows\Fonts\NotoSansJP-VF.ttf";
                    if (File.Exists(winFont))
                    {
                        if (!Directory.Exists("Assets/Fonts")) Directory.CreateDirectory("Assets/Fonts");
                        File.Copy(winFont, ttfPath, true);
                        AssetDatabase.ImportAsset(ttfPath, ImportAssetOptions.ForceSynchronousImport);
                    }
                }

                Font ttfFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
                if (ttfFont != null)
                {
                    jpFontAsset = TMP_FontAsset.CreateFontAsset(ttfFont);
                    if (jpFontAsset != null)
                    {
                        jpFontAsset.name = "NotoSansJP SDF";
                        jpFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                        AssetDatabase.CreateAsset(jpFontAsset, fontAssetPath);
                        AssetDatabase.SaveAssets();
                        Debug.Log("[DictionaryCourseUIBuilder] Created Dynamic TMP Font Asset 'NotoSansJP SDF'.");
                    }
                }
            }

            // Register as Fallback font in defaultFontAsset so all TMP texts can render Japanese characters
            if (jpFontAsset != null && baseTmpFont != null)
            {
                if (baseTmpFont.fallbackFontAssetTable == null)
                {
                    baseTmpFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }

                if (!baseTmpFont.fallbackFontAssetTable.Contains(jpFontAsset))
                {
                    baseTmpFont.fallbackFontAssetTable.Add(jpFontAsset);
                    EditorUtility.SetDirty(baseTmpFont);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[DictionaryCourseUIBuilder] Registered NotoSansJP as Fallback Font in {baseTmpFont.name}.");
                }

                return jpFontAsset;
            }

            return baseTmpFont;
        }

        private static void BuildMagazineSpread(GameObject coursePanel, DictionaryCourseTabUI tabUI, CourseDatabase db, CourseQuizUI quizUI)
        {
            SerializedObject so = new SerializedObject(tabUI);

            var dictManager = Object.FindFirstObjectByType<DictionaryManager>();
            so.FindProperty("database").objectReferenceValue = db;
            so.FindProperty("dictionaryManager").objectReferenceValue = dictManager;
            so.FindProperty("quizUI").objectReferenceValue = quizUI;

            // --- TOP BAR: Overall Progress ---
            GameObject topBar = CreateUIObject("TopBar", coursePanel.transform);
            RectTransform tbRt = topBar.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.sizeDelta = new Vector2(0f, 36f);
            tbRt.anchoredPosition = new Vector2(0f, -4f);

            GameObject progressTextGo = CreateText("OverallProgressText", topBar.transform, "Course Progress: 0 / 79 Activities (0%)", 16, TextAlignmentOptions.Right, new Color(0.2f, 0.35f, 0.5f));
            RectTransform ptRt = progressTextGo.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0.5f, 0.5f);
            ptRt.anchorMax = new Vector2(1f, 0.5f);
            ptRt.pivot = new Vector2(1f, 0.5f);
            ptRt.anchoredPosition = new Vector2(-28f, 0f);
            ptRt.sizeDelta = new Vector2(400f, 32f);
            so.FindProperty("overallProgressLabel").objectReferenceValue = progressTextGo.GetComponent<TMP_Text>();

            // --- LEFT PAGE (Lesson Info & Reference Notes) ---
            GameObject leftPage = CreateUIObject("LeftPage_Course", coursePanel.transform);
            RectTransform lpRt = leftPage.GetComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0f, 0f);
            lpRt.anchorMax = new Vector2(0.5f, 1f);
            lpRt.offsetMin = new Vector2(25f, 60f);
            lpRt.offsetMax = new Vector2(-15f, -44f);

            var leftScroll = leftPage.AddComponent<ScrollRect>();
            leftScroll.horizontal = false;
            leftScroll.vertical = true;
            leftScroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject leftViewport = CreateUIObject("Viewport", leftPage.transform);
            SetStretch(leftViewport.GetComponent<RectTransform>());
            leftViewport.AddComponent<RectMask2D>();

            GameObject leftContent = CreateUIObject("Content", leftViewport.transform);
            RectTransform lcRt = leftContent.GetComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(0f, 1f);
            lcRt.anchorMax = new Vector2(1f, 1f);
            lcRt.pivot = new Vector2(0.5f, 1f);
            lcRt.sizeDelta = new Vector2(0f, 600f);

            var lcLayout = leftContent.AddComponent<VerticalLayoutGroup>();
            lcLayout.padding = new RectOffset(16, 20, 10, 20);
            lcLayout.spacing = 10f;
            lcLayout.childControlWidth = true;
            lcLayout.childControlHeight = false;
            lcLayout.childForceExpandWidth = true;
            lcLayout.childForceExpandHeight = false;

            var lcFitter = leftContent.AddComponent<ContentSizeFitter>();
            lcFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            lcFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            leftScroll.viewport = leftViewport.GetComponent<RectTransform>();
            leftScroll.content = lcRt;

            // 1. Topic Badge
            GameObject topicBadgeGo = CreateText("TopicBadge", leftContent.transform, "TOPIC 1: はじめての日本語 (Starting to Speak Japanese)", 13, TextAlignmentOptions.Left, new Color(0.45f, 0.4f, 0.35f));
            so.FindProperty("topicBadgeLabel").objectReferenceValue = topicBadgeGo.GetComponent<TMP_Text>();

            // 2. Lesson Title
            GameObject lessonTitleGo = CreateText("LessonTitle", leftContent.transform, "Lesson 1: おはようございます\n<size=75%><color=#555555>Good morning!</color></size>", 22, TextAlignmentOptions.Left, new Color(0.15f, 0.15f, 0.2f));
            so.FindProperty("lessonTitleLabel").objectReferenceValue = lessonTitleGo.GetComponent<TMP_Text>();

            // 3. Lesson Objective
            GameObject lessonObjGo = CreateText("LessonObjective", leftContent.transform, "<b>Learning Objectives:</b>\nUse basic expressions to begin and end simple interactions...", 15, TextAlignmentOptions.Left, new Color(0.3f, 0.35f, 0.4f));
            so.FindProperty("lessonObjectiveLabel").objectReferenceValue = lessonObjGo.GetComponent<TMP_Text>();

            // 4. Divider Line
            GameObject dividerGo = CreateUIObject("Divider", leftContent.transform);
            var divImg = dividerGo.AddComponent<Image>();
            divImg.color = new Color(0.82f, 0.78f, 0.72f, 0.8f);
            var divLe = dividerGo.AddComponent<LayoutElement>();
            divLe.preferredHeight = 2f;

            // 5. Writing System (Aksara) Note Section
            GameObject aksaraSecGo = CreateUIObject("AksaraSection", leftContent.transform);
            aksaraSecGo.AddComponent<VerticalLayoutGroup>();
            GameObject aksaraTextGo = CreateText("AksaraLabel", aksaraSecGo.transform, "<b>Writing System:</b>\nHiragana introduction...", 14, TextAlignmentOptions.Left, new Color(0.2f, 0.25f, 0.3f));
            so.FindProperty("aksaraNoteSection").objectReferenceValue = aksaraSecGo;
            so.FindProperty("aksaraNoteLabel").objectReferenceValue = aksaraTextGo.GetComponent<TMP_Text>();

            // 6. Grammar Notes Section
            GameObject grammarSecGo = CreateUIObject("GrammarSection", leftContent.transform);
            grammarSecGo.AddComponent<VerticalLayoutGroup>();
            GameObject grammarTextGo = CreateText("GrammarLabel", grammarSecGo.transform, "<b>Grammar Notes:</b>\n• ...", 14, TextAlignmentOptions.Left, new Color(0.18f, 0.28f, 0.38f));
            so.FindProperty("grammarNotesSection").objectReferenceValue = grammarSecGo;
            so.FindProperty("grammarNotesLabel").objectReferenceValue = grammarTextGo.GetComponent<TMP_Text>();

            // 7. Culture & Context Section
            GameObject cultureSecGo = CreateUIObject("CulturalSection", leftContent.transform);
            cultureSecGo.AddComponent<VerticalLayoutGroup>();
            GameObject cultureTextGo = CreateText("CulturalLabel", cultureSecGo.transform, "<b>Culture & Context:</b>\nThe etiquette of bowing...", 14, TextAlignmentOptions.Left, new Color(0.38f, 0.28f, 0.18f));
            so.FindProperty("culturalNoteSection").objectReferenceValue = cultureSecGo;
            so.FindProperty("culturalNoteLabel").objectReferenceValue = cultureTextGo.GetComponent<TMP_Text>();

            // 8. New Kanji Words Section
            GameObject kanjiSecGo = CreateUIObject("KanjiSection", leftContent.transform);
            kanjiSecGo.AddComponent<VerticalLayoutGroup>();
            GameObject kanjiTextGo = CreateText("KanjiLabel", kanjiSecGo.transform, "<b>New Kanji:</b> 名前 • 国 • 私", 15, TextAlignmentOptions.Left, new Color(0.2f, 0.35f, 0.25f));
            so.FindProperty("kanjiWordsSection").objectReferenceValue = kanjiSecGo;
            so.FindProperty("kanjiWordsLabel").objectReferenceValue = kanjiTextGo.GetComponent<TMP_Text>();

            // --- RIGHT PAGE (Activity & Practice) ---
            GameObject rightPage = CreateUIObject("RightPage_Course", coursePanel.transform);
            RectTransform rpRt = rightPage.GetComponent<RectTransform>();
            rpRt.anchorMin = new Vector2(0.5f, 0f);
            rpRt.anchorMax = new Vector2(1f, 1f);
            rpRt.offsetMin = new Vector2(15f, 60f);
            rpRt.offsetMax = new Vector2(-25f, -44f);

            var rightScroll = rightPage.AddComponent<ScrollRect>();
            rightScroll.horizontal = false;
            rightScroll.vertical = true;
            rightScroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject rightViewport = CreateUIObject("Viewport", rightPage.transform);
            SetStretch(rightViewport.GetComponent<RectTransform>());
            rightViewport.AddComponent<RectMask2D>();

            GameObject rightContent = CreateUIObject("Content", rightViewport.transform);
            RectTransform rcRt = rightContent.GetComponent<RectTransform>();
            rcRt.anchorMin = new Vector2(0f, 1f);
            rcRt.anchorMax = new Vector2(1f, 1f);
            rcRt.pivot = new Vector2(0.5f, 1f);
            rcRt.sizeDelta = new Vector2(0f, 600f);

            var rcLayout = rightContent.AddComponent<VerticalLayoutGroup>();
            rcLayout.padding = new RectOffset(20, 16, 10, 20);
            rcLayout.spacing = 10f;
            rcLayout.childControlWidth = true;
            rcLayout.childControlHeight = false;
            rcLayout.childForceExpandWidth = true;
            rcLayout.childForceExpandHeight = false;

            var rcFitter = rightContent.AddComponent<ContentSizeFitter>();
            rcFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rcFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            rightScroll.viewport = rightViewport.GetComponent<RectTransform>();
            rightScroll.content = rcRt;

            // 1. Activity Header
            GameObject actHeaderGo = CreateText("ActivityHeader", rightContent.transform, "ACTIVITY 1 OF 4", 13, TextAlignmentOptions.Left, new Color(0.45f, 0.45f, 0.5f));
            so.FindProperty("activityHeaderLabel").objectReferenceValue = actHeaderGo.GetComponent<TMP_Text>();

            // 2. Activity Title
            GameObject actTitleGo = CreateText("ActivityTitle", rightContent.transform, "こんにちは\n<size=75%><color=#445566>Greeting someone</color></size>", 22, TextAlignmentOptions.Left, new Color(0.12f, 0.16f, 0.24f));
            so.FindProperty("activityTitleLabel").objectReferenceValue = actTitleGo.GetComponent<TMP_Text>();

            // 3. Illustration Box
            GameObject illustBoxGo = CreateUIObject("IllustrationBox", rightContent.transform);
            var ibImg = illustBoxGo.AddComponent<Image>();
            ibImg.sprite = defaultSprite;
            ibImg.type = Image.Type.Sliced;
            ibImg.color = new Color(0.92f, 0.90f, 0.86f, 1f);
            var ibLe = illustBoxGo.AddComponent<LayoutElement>();
            ibLe.preferredHeight = 220f;

            GameObject rawImgGo = CreateUIObject("RawImage", illustBoxGo.transform);
            RectTransform rawRt = rawImgGo.GetComponent<RectTransform>();
            SetStretchMargins(rawRt, 6f, 6f, 6f, 6f);
            var rawImg = rawImgGo.AddComponent<RawImage>();
            so.FindProperty("illustrationContainer").objectReferenceValue = illustBoxGo;
            so.FindProperty("illustrationImage").objectReferenceValue = rawImg;

            // 4. Explanation Text
            GameObject explGo = CreateText("ExplanationText", rightContent.transform, "Activity explanation text...", 15, TextAlignmentOptions.Left, new Color(0.25f, 0.28f, 0.32f));
            so.FindProperty("explanationLabel").objectReferenceValue = explGo.GetComponent<TMP_Text>();

            // 5. Key Phrases Section
            GameObject keyPhrasesSecGo = CreateUIObject("KeyPhrasesSection", rightContent.transform);
            var kpImg = keyPhrasesSecGo.AddComponent<Image>();
            kpImg.sprite = defaultSprite;
            kpImg.type = Image.Type.Sliced;
            kpImg.color = new Color(0.93f, 0.92f, 0.88f, 1f);

            var kpLayout = keyPhrasesSecGo.AddComponent<VerticalLayoutGroup>();
            kpLayout.padding = new RectOffset(14, 14, 10, 10);
            kpLayout.childControlWidth = true;
            kpLayout.childControlHeight = true;
            kpLayout.childForceExpandWidth = true;
            kpLayout.childForceExpandHeight = false;
            keyPhrasesSecGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject kpTextGo = CreateText("KeyPhrasesLabel", keyPhrasesSecGo.transform, "• おはようございます\n• こんにちは", 16, TextAlignmentOptions.Left, new Color(0.1f, 0.15f, 0.25f));
            so.FindProperty("keyPhrasesSection").objectReferenceValue = keyPhrasesSecGo;
            so.FindProperty("keyPhrasesLabel").objectReferenceValue = kpTextGo.GetComponent<TMP_Text>();

            // 6. Action / Quiz Row
            GameObject quizRowGo = CreateUIObject("QuizRow", rightContent.transform);
            var qrLayout = quizRowGo.AddComponent<HorizontalLayoutGroup>();
            qrLayout.spacing = 16f;
            qrLayout.childAlignment = TextAnchor.MiddleLeft;
            qrLayout.childControlWidth = false;
            qrLayout.childControlHeight = false;
            var qrLe = quizRowGo.AddComponent<LayoutElement>();
            qrLe.preferredHeight = 44f;

            // Completed Badge
            GameObject badgeGo = CreateUIObject("CompletedBadge", quizRowGo.transform);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(140f, 38f);
            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.sprite = defaultSprite;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = new Color(0.25f, 0.65f, 0.35f, 1f);
            GameObject badgeTextGo = CreateText("BadgeText", badgeGo.transform, "✔ COMPLETED", 14, TextAlignmentOptions.Center, Color.white);
            SetStretch(badgeTextGo.GetComponent<RectTransform>());
            so.FindProperty("completedBadgeObject").objectReferenceValue = badgeGo;

            // Quiz Button
            GameObject btnQuizGo = CreateButton("Btn_Quiz", quizRowGo.transform, "Take Mini-Quiz", new Vector2(170f, 40f), new Color(0.22f, 0.52f, 0.72f, 1f));
            so.FindProperty("quizButton").objectReferenceValue = btnQuizGo.GetComponent<Button>();
            so.FindProperty("quizButtonText").objectReferenceValue = btnQuizGo.GetComponentInChildren<TMP_Text>();

            // 7. Activity Sub-Pagination
            GameObject actPagerGo = CreateUIObject("ActivityPager", rightContent.transform);
            var apLayout = actPagerGo.AddComponent<HorizontalLayoutGroup>();
            apLayout.spacing = 12f;
            apLayout.childAlignment = TextAnchor.MiddleCenter;
            apLayout.childControlWidth = false;
            apLayout.childControlHeight = false;
            var apLe = actPagerGo.AddComponent<LayoutElement>();
            apLe.preferredHeight = 40f;

            GameObject btnPrevAct = CreateButton("Btn_PrevAct", actPagerGo.transform, "<", new Vector2(44f, 34f), new Color(0.45f, 0.42f, 0.4f, 1f));
            GameObject actIndGo = CreateText("ActIndicator", actPagerGo.transform, "1 / 4", 16, TextAlignmentOptions.Center, new Color(0.2f, 0.2f, 0.2f));
            RectTransform aiRt = actIndGo.GetComponent<RectTransform>();
            aiRt.sizeDelta = new Vector2(80f, 34f);
            GameObject btnNextAct = CreateButton("Btn_NextAct", actPagerGo.transform, ">", new Vector2(44f, 34f), new Color(0.45f, 0.42f, 0.4f, 1f));

            so.FindProperty("prevActivityButton").objectReferenceValue = btnPrevAct.GetComponent<Button>();
            so.FindProperty("nextActivityButton").objectReferenceValue = btnNextAct.GetComponent<Button>();
            so.FindProperty("activityIndicatorLabel").objectReferenceValue = actIndGo.GetComponent<TMP_Text>();

            // --- BOTTOM BAR: Lesson Pagination ---
            GameObject lessonNavBar = CreateUIObject("LessonNavBar", coursePanel.transform);
            RectTransform lnbRt = lessonNavBar.GetComponent<RectTransform>();
            lnbRt.anchorMin = new Vector2(0f, 0f);
            lnbRt.anchorMax = new Vector2(1f, 0f);
            lnbRt.pivot = new Vector2(0.5f, 0f);
            lnbRt.sizeDelta = new Vector2(0f, 48f);
            lnbRt.anchoredPosition = new Vector2(0f, 8f);

            GameObject btnPrevLes = CreateButton("Btn_PrevLesson", lessonNavBar.transform, "< Previous Lesson", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(180f, 38f), new Vector2(40f, 0f), new Color(0.35f, 0.35f, 0.35f));
            GameObject lesIndGo = CreateText("LessonIndicator", lessonNavBar.transform, "Lesson 1 / 18", 18, TextAlignmentOptions.Center, new Color(0.2f, 0.2f, 0.2f));
            RectTransform liRt = lesIndGo.GetComponent<RectTransform>();
            liRt.anchorMin = new Vector2(0.5f, 0.5f);
            liRt.anchorMax = new Vector2(0.5f, 0.5f);
            liRt.pivot = new Vector2(0.5f, 0.5f);
            liRt.sizeDelta = new Vector2(240f, 40f);
            liRt.anchoredPosition = Vector2.zero;

            GameObject btnNextLes = CreateButton("Btn_NextLesson", lessonNavBar.transform, "Next Lesson >", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(180f, 38f), new Vector2(-40f, 0f), new Color(0.35f, 0.35f, 0.35f));

            so.FindProperty("prevLessonButton").objectReferenceValue = btnPrevLes.GetComponent<Button>();
            so.FindProperty("nextLessonButton").objectReferenceValue = btnNextLes.GetComponent<Button>();
            so.FindProperty("lessonIndicatorLabel").objectReferenceValue = lesIndGo.GetComponent<TMP_Text>();

            so.ApplyModifiedProperties();
        }

        private static GameObject BuildQuizModal(Transform parent)
        {
            GameObject modal = CreateUIObject("Modal_CourseQuiz", parent);
            SetStretch(modal.GetComponent<RectTransform>());

            var dimImg = modal.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.75f);
            var backdropBtn = modal.AddComponent<Button>();

            // Dialog Window
            GameObject dialog = CreateUIObject("DialogWindow", modal.transform);
            RectTransform dlgRt = dialog.GetComponent<RectTransform>();
            dlgRt.anchorMin = new Vector2(0.5f, 0.5f);
            dlgRt.anchorMax = new Vector2(0.5f, 0.5f);
            dlgRt.pivot = new Vector2(0.5f, 0.5f);
            dlgRt.sizeDelta = new Vector2(620f, 540f);

            var dlgImg = dialog.AddComponent<Image>();
            dlgImg.sprite = defaultSprite;
            dlgImg.type = Image.Type.Sliced;
            dlgImg.color = new Color(0.96f, 0.94f, 0.90f, 1f);

            // Title
            GameObject titleGo = CreateText("Title", dialog.transform, "Mini Quiz: こんにちは (Greeting someone)", 20, TextAlignmentOptions.Left, new Color(0.15f, 0.15f, 0.2f));
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(30f, -25f);
            titleRt.sizeDelta = new Vector2(-100f, 30f);

            // Question Counter
            GameObject counterGo = CreateText("QuestionCounter", dialog.transform, "Question 1 of 2", 15, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.55f));
            RectTransform cntRt = counterGo.GetComponent<RectTransform>();
            cntRt.anchorMin = new Vector2(0f, 1f);
            cntRt.anchorMax = new Vector2(1f, 1f);
            cntRt.pivot = new Vector2(0f, 1f);
            cntRt.anchoredPosition = new Vector2(30f, -55f);
            cntRt.sizeDelta = new Vector2(-100f, 25f);

            // Close Button (X)
            GameObject closeBtnGo = CreateButton("Btn_Close", dialog.transform, "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(34f, 34f), new Vector2(-20f, -20f), new Color(0.85f, 0.25f, 0.2f, 1f));

            // Question Box
            GameObject qBoxGo = CreateUIObject("QuestionBox", dialog.transform);
            RectTransform qbRt = qBoxGo.GetComponent<RectTransform>();
            qbRt.anchorMin = new Vector2(0.5f, 1f);
            qbRt.anchorMax = new Vector2(0.5f, 1f);
            qbRt.pivot = new Vector2(0.5f, 1f);
            qbRt.anchoredPosition = new Vector2(0f, -95f);
            qbRt.sizeDelta = new Vector2(560f, 90f);

            var qbImg = qBoxGo.AddComponent<Image>();
            qbImg.sprite = defaultSprite;
            qbImg.type = Image.Type.Sliced;
            qbImg.color = new Color(0.90f, 0.88f, 0.84f, 1f);

            GameObject qTextGo = CreateText("QuestionText", qBoxGo.transform, "Which Japanese expression is appropriate for: 'Greeting someone'?", 17, TextAlignmentOptions.Center, new Color(0.1f, 0.15f, 0.25f));
            SetStretchMargins(qTextGo.GetComponent<RectTransform>(), 12f, 12f, 8f, 8f);

            // 4 Option Buttons Container
            GameObject optContainer = CreateUIObject("OptionContainer", dialog.transform);
            RectTransform ocRt = optContainer.GetComponent<RectTransform>();
            ocRt.anchorMin = new Vector2(0.5f, 1f);
            ocRt.anchorMax = new Vector2(0.5f, 1f);
            ocRt.pivot = new Vector2(0.5f, 1f);
            ocRt.anchoredPosition = new Vector2(0f, -205f);
            ocRt.sizeDelta = new Vector2(560f, 230f);

            var ocLayout = optContainer.AddComponent<VerticalLayoutGroup>();
            ocLayout.spacing = 10f;
            ocLayout.childControlWidth = true;
            ocLayout.childControlHeight = true;
            ocLayout.childForceExpandWidth = true;
            ocLayout.childForceExpandHeight = false;

            Button[] optionBtns = new Button[4];
            TMP_Text[] optionLabels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject optBtnGo = CreateButton($"Option_{i}", optContainer.transform, $"Option {i + 1}", new Vector2(0f, 44f), new Color(0.92f, 0.90f, 0.85f, 1f));
                var le = optBtnGo.AddComponent<LayoutElement>();
                le.preferredHeight = 44f;
                optionBtns[i] = optBtnGo.GetComponent<Button>();
                var txt = optBtnGo.GetComponentInChildren<TMP_Text>();
                txt.color = new Color(0.15f, 0.15f, 0.2f);
                txt.fontSize = 16f;
                optionLabels[i] = txt;
            }

            // Feedback Label
            GameObject feedGo = CreateText("FeedbackLabel", dialog.transform, "", 17, TextAlignmentOptions.Center, new Color(0.2f, 0.65f, 0.35f));
            RectTransform feedRt = feedGo.GetComponent<RectTransform>();
            feedRt.anchorMin = new Vector2(0.5f, 0f);
            feedRt.anchorMax = new Vector2(0.5f, 0f);
            feedRt.pivot = new Vector2(0.5f, 0f);
            feedRt.anchoredPosition = new Vector2(0f, 25f);
            feedRt.sizeDelta = new Vector2(560f, 35f);

            var quizUI = modal.AddComponent<CourseQuizUI>();
            SerializedObject so = new SerializedObject(quizUI);
            so.FindProperty("modalRoot").objectReferenceValue = modal;
            so.FindProperty("backdropCloseButton").objectReferenceValue = backdropBtn;
            so.FindProperty("closeButton").objectReferenceValue = closeBtnGo.GetComponent<Button>();
            so.FindProperty("titleLabel").objectReferenceValue = titleGo.GetComponent<TMP_Text>();
            so.FindProperty("questionCounterLabel").objectReferenceValue = counterGo.GetComponent<TMP_Text>();
            so.FindProperty("questionText").objectReferenceValue = qTextGo.GetComponent<TMP_Text>();
            so.FindProperty("feedbackLabel").objectReferenceValue = feedGo.GetComponent<TMP_Text>();

            SerializedProperty optsProp = so.FindProperty("optionButtons");
            optsProp.arraySize = 4;
            for (int i = 0; i < 4; i++) optsProp.GetArrayElementAtIndex(i).objectReferenceValue = optionBtns[i];

            SerializedProperty labelsProp = so.FindProperty("optionLabels");
            labelsProp.arraySize = 4;
            for (int i = 0; i < 4; i++) labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = optionLabels[i];

            so.ApplyModifiedProperties();
            modal.SetActive(false);

            return modal;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Vector2 size, Color color)
        {
            return CreateButton(name, parent, text, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero, color);
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos, Color color)
        {
            GameObject btnGo = CreateUIObject(name, parent);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var img = btnGo.AddComponent<Image>();
            img.sprite = defaultSprite;
            img.type = Image.Type.Sliced;
            img.color = color;

            btnGo.AddComponent<Button>();

            GameObject textGo = CreateText("Text", btnGo.transform, text, 15, TextAlignmentOptions.Center, Color.white);
            SetStretch(textGo.GetComponent<RectTransform>());

            return btnGo;
        }

        private static GameObject CreateText(string name, Transform parent, string content, float fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject textGo = CreateUIObject(name, parent);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return textGo;
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
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetStretchMargins(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
#endif
