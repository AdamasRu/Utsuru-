using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utsuru.Achievements.Editor;

namespace Utsuru.Store.Editor
{
    /// <summary>
    /// Membangun dan menghubungkan UI Store Tema Kamus & StoreThemeManager ke Day_Showcase.unity,
    /// mengikuti desain modal krem + blur latar 3D + keybind 'F'.
    /// </summary>
    [InitializeOnLoad]
    public static class StoreUIBuilder
    {
        private const string DayShowcaseScenePath = "Assets/Japanese_Street/Scenes/Day_Showcase.unity";

        static StoreUIBuilder()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureSetupOnEditorLoad;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (Application.isPlaying) return;
            if (scene.path == DayShowcaseScenePath)
            {
                EnsureStoreInActiveScene(saveScene: false);
            }
        }

        private static void EnsureSetupOnEditorLoad()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene active = EditorSceneManager.GetActiveScene();
            if (active.IsValid() && active.path == DayShowcaseScenePath)
            {
                EnsureStoreInActiveScene(saveScene: !active.isDirty);
            }
        }

        [MenuItem("Tools/Utsuru/Build Store UI")]
        public static void BuildUIMenuItem()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.path != DayShowcaseScenePath && File.Exists(Path.GetFullPath(DayShowcaseScenePath)))
            {
                if (active.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                EditorSceneManager.OpenScene(DayShowcaseScenePath, OpenSceneMode.Single);
            }
            BuildUI(saveScene: true);
        }

        private static void EnsureStoreInActiveScene(bool saveScene)
        {
            if (Object.FindFirstObjectByType<StoreUIController>(FindObjectsInactive.Include) != null &&
                Object.FindFirstObjectByType<StoreThemeManager>(FindObjectsInactive.Include) != null)
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
                Debug.LogWarning("[StoreUIBuilder] Canvas not found in active scene.");
                return;
            }

            // 1. Pastikan StoreThemeManager ada di scene
            StoreThemeManager manager = Object.FindFirstObjectByType<StoreThemeManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                GameObject mgrGo = new GameObject("StoreThemeManager");
                Undo.RegisterCreatedObjectUndo(mgrGo, "Create StoreThemeManager");
                manager = mgrGo.AddComponent<StoreThemeManager>();
            }

            // 2. Muat Sprites & Material
            Sprite panelBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Achievements-Panel-Bg.png");
            Sprite boxSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Achievements-Box.png");
            Sprite starsSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Stars-Point-Icon.png");
            Sprite closeBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Achievements/Close-Button-Shape.png");
            Sprite pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Category Pill.png");
            Material blurMat = AchievementAssetImporter.EnsureBlurMaterial();

            // 3. Muat Fonts
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

            Color darkBrown = new Color(0.373f, 0.224f, 0.137f, 1f);
            Color mediumBrown = new Color(0.471f, 0.322f, 0.231f, 1f);

            // 4. Rebuild Canvas/Store secara bersih
            Transform existingRoot = canvas.transform.Find("Store");
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot.gameObject);
            }

            GameObject storeRoot = CreateUIObject("Store", canvas.transform);
            Undo.RegisterCreatedObjectUndo(storeRoot, "Create Store UI");
            SetStretch(storeRoot.GetComponent<RectTransform>());

            Transform pauseTr = canvas.transform.Find("PauseMenuPanel");
            if (pauseTr != null)
            {
                storeRoot.transform.SetSiblingIndex(pauseTr.GetSiblingIndex());
            }

            StoreUIController controller = storeRoot.AddComponent<StoreUIController>();

            // 5. StoreView
            GameObject viewGo = CreateUIObject("StoreView", storeRoot.transform);
            SetStretch(viewGo.GetComponent<RectTransform>());
            CanvasGroup viewCg = viewGo.AddComponent<CanvasGroup>();
            viewCg.alpha = 0f;

            // 5.1 BlurBackdrop
            GameObject blurGo = CreateUIObject("BlurBackdrop", viewGo.transform);
            SetStretch(blurGo.GetComponent<RectTransform>());
            RawImage blurRaw = blurGo.AddComponent<RawImage>();
            blurRaw.color = Color.white;
            blurRaw.raycastTarget = true;
            blurRaw.enabled = false;

            // 5.2 TintOverlay
            GameObject tintGo = CreateUIObject("TintOverlay", viewGo.transform);
            SetStretch(tintGo.GetComponent<RectTransform>());
            Image tintImg = tintGo.AddComponent<Image>();
            tintImg.color = new Color(0.08f, 0.05f, 0.03f, 0.22f);
            tintImg.raycastTarget = true;

            // 5.3 MainPanel (1140 x 880 sesuai proporsi mockup Store)
            GameObject panelGo = CreateUIObject("MainPanel", viewGo.transform);
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(1140f, 880f);
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

            // 5.4 Header ("STORE" di kiri + badge poin di kanan + garis pemisah)
            GameObject headerTitleGo = CreateUIObject("HeaderTitle", panelGo.transform);
            RectTransform headerRt = headerTitleGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(0f, 1f);
            headerRt.pivot = new Vector2(0f, 1f);
            headerRt.sizeDelta = new Vector2(420f, 84f);
            headerRt.anchoredPosition = new Vector2(140f, -46f);

            TextMeshProUGUI headerTmp = headerTitleGo.AddComponent<TextMeshProUGUI>();
            if (balooExtraBold != null) headerTmp.font = balooExtraBold;
            headerTmp.text = "STORE";
            headerTmp.fontSize = 62f;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.alignment = TextAlignmentOptions.Left;
            headerTmp.color = darkBrown;
            headerTmp.characterSpacing = 2f;

            // Pill Saldo Poin di kanan atas
            GameObject pointsBadgeGo = CreateUIObject("PointsBalanceBadge", panelGo.transform);
            RectTransform pointsBadgeRt = pointsBadgeGo.GetComponent<RectTransform>();
            pointsBadgeRt.anchorMin = new Vector2(1f, 1f);
            pointsBadgeRt.anchorMax = new Vector2(1f, 1f);
            pointsBadgeRt.pivot = new Vector2(1f, 1f);
            pointsBadgeRt.sizeDelta = new Vector2(210f, 64f);
            pointsBadgeRt.anchoredPosition = new Vector2(-140f, -54f);

            Image pointsBadgeImg = pointsBadgeGo.AddComponent<Image>();
            if (pillSprite != null)
            {
                pointsBadgeImg.sprite = pillSprite;
                pointsBadgeImg.type = Image.Type.Sliced;
                pointsBadgeImg.pixelsPerUnitMultiplier = 0.8f;
            }
            pointsBadgeImg.color = new Color(0.86f, 0.80f, 0.72f, 1f);

            Outline badgeOutline = pointsBadgeGo.AddComponent<Outline>();
            badgeOutline.effectColor = mediumBrown;
            badgeOutline.effectDistance = new Vector2(2f, -2f);

            GameObject headerStarGo = CreateUIObject("StarIcon", pointsBadgeGo.transform);
            RectTransform headerStarRt = headerStarGo.GetComponent<RectTransform>();
            headerStarRt.anchorMin = new Vector2(0f, 0.5f);
            headerStarRt.anchorMax = new Vector2(0f, 0.5f);
            headerStarRt.pivot = new Vector2(0f, 0.5f);
            headerStarRt.sizeDelta = new Vector2(40f, 40f);
            headerStarRt.anchoredPosition = new Vector2(20f, 0f);
            Image headerStarImg = headerStarGo.AddComponent<Image>();
            if (starsSprite != null) headerStarImg.sprite = starsSprite;
            headerStarImg.preserveAspect = true;

            GameObject pointsTextGo = CreateUIObject("PointsBalanceText", pointsBadgeGo.transform);
            RectTransform pointsTextRt = pointsTextGo.GetComponent<RectTransform>();
            pointsTextRt.anchorMin = Vector2.zero;
            pointsTextRt.anchorMax = Vector2.one;
            pointsTextRt.offsetMin = new Vector2(64f, 0f);
            pointsTextRt.offsetMax = new Vector2(-16f, 0f);
            TextMeshProUGUI pointsBalanceTmp = pointsTextGo.AddComponent<TextMeshProUGUI>();
            if (balooExtraBold != null) pointsBalanceTmp.font = balooExtraBold;
            pointsBalanceTmp.text = "0";
            pointsBalanceTmp.fontSize = 36f;
            pointsBalanceTmp.fontStyle = FontStyles.Bold;
            pointsBalanceTmp.alignment = TextAlignmentOptions.Center;
            pointsBalanceTmp.color = darkBrown;

            // Garis pemisah horizontal di bawah header
            GameObject dividerGo = CreateUIObject("HeaderDivider", panelGo.transform);
            RectTransform dividerRt = dividerGo.GetComponent<RectTransform>();
            dividerRt.anchorMin = new Vector2(0.5f, 1f);
            dividerRt.anchorMax = new Vector2(0.5f, 1f);
            dividerRt.pivot = new Vector2(0.5f, 1f);
            dividerRt.sizeDelta = new Vector2(860f, 4f);
            dividerRt.anchoredPosition = new Vector2(0f, -142f);
            Image dividerImg = dividerGo.AddComponent<Image>();
            dividerImg.color = mediumBrown;

            // 5.5 Grid Kartu Tema (3 atas, 2 bawah — total 5 kartu)
            Vector2[] cardPositions = new Vector2[]
            {
                new Vector2(-280f, 118f), // Row 1 Left (Sakura)
                new Vector2(0f, 118f),    // Row 1 Center (Botanical)
                new Vector2(280f, 118f),  // Row 1 Right (Autumn)
                new Vector2(-140f, -172f),// Row 2 Left (Midnight)
                new Vector2(140f, -172f)  // Row 2 Right (Summer)
            };

            List<StoreThemeCardUI> cards = new List<StoreThemeCardUI>();
            for (int i = 0; i < cardPositions.Length; i++)
            {
                StoreThemeCardUI card = CreateThemeCard(
                    $"ThemeCard_{i + 1}",
                    panelGo.transform,
                    cardPositions[i],
                    boxSprite,
                    pillSprite,
                    starsSprite,
                    balooExtraBold,
                    nunitoBold,
                    darkBrown,
                    mediumBrown
                );
                cards.Add(card);
            }

            // 5.6 Feedback Banner (muncul sementara saat beli sukses / poin kurang)
            GameObject feedbackGo = CreateUIObject("FeedbackBanner", panelGo.transform);
            RectTransform feedbackRt = feedbackGo.GetComponent<RectTransform>();
            feedbackRt.anchorMin = new Vector2(0.5f, 0f);
            feedbackRt.anchorMax = new Vector2(0.5f, 0f);
            feedbackRt.pivot = new Vector2(0.5f, 0f);
            feedbackRt.sizeDelta = new Vector2(760f, 46f);
            feedbackRt.anchoredPosition = new Vector2(0f, 112f);

            Image feedbackBg = feedbackGo.AddComponent<Image>();
            if (pillSprite != null)
            {
                feedbackBg.sprite = pillSprite;
                feedbackBg.type = Image.Type.Sliced;
                feedbackBg.pixelsPerUnitMultiplier = 0.9f;
            }
            feedbackBg.color = new Color(0.20f, 0.56f, 0.28f, 0.96f);

            GameObject feedbackTxtGo = CreateUIObject("Text", feedbackGo.transform);
            SetStretch(feedbackTxtGo.GetComponent<RectTransform>());
            TextMeshProUGUI feedbackTmp = feedbackTxtGo.AddComponent<TextMeshProUGUI>();
            if (nunitoBold != null) feedbackTmp.font = nunitoBold;
            feedbackTmp.text = "";
            feedbackTmp.fontSize = 20f;
            feedbackTmp.fontStyle = FontStyles.Bold;
            feedbackTmp.alignment = TextAlignmentOptions.Center;
            feedbackTmp.color = Color.white;
            feedbackGo.SetActive(false);

            // 5.7 Close Button
            GameObject closeGo = CreateUIObject("CloseButton", panelGo.transform);
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.sizeDelta = new Vector2(180f, 68f);
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
            closeTmp.fontSize = 28f;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;

            viewGo.SetActive(false);

            // 6. Wire Serialized Fields on StoreUIController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("viewRoot").objectReferenceValue = viewGo;
            so.FindProperty("viewCanvasGroup").objectReferenceValue = viewCg;
            so.FindProperty("mainPanelRt").objectReferenceValue = panelRt;
            so.FindProperty("blurBackdrop").objectReferenceValue = blurRaw;
            so.FindProperty("tintOverlay").objectReferenceValue = tintImg;
            so.FindProperty("blurMaterial").objectReferenceValue = blurMat;
            so.FindProperty("pointsBalanceText").objectReferenceValue = pointsBalanceTmp;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("feedbackBanner").objectReferenceValue = feedbackGo;
            so.FindProperty("feedbackBackground").objectReferenceValue = feedbackBg;
            so.FindProperty("feedbackText").objectReferenceValue = feedbackTmp;

            SerializedProperty cardsProp = so.FindProperty("themeCards");
            cardsProp.arraySize = cards.Count;
            for (int i = 0; i < cards.Count; i++)
            {
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }

            so.ApplyModifiedProperties();

            Scene currentScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(currentScene);
            if (saveScene)
            {
                EditorSceneManager.SaveScene(currentScene);
                Debug.Log("[StoreUIBuilder] Store UI built, wired, and scene saved.");
            }
            else
            {
                Debug.Log("[StoreUIBuilder] Store UI built and wired into active scene.");
            }
        }

        private static StoreThemeCardUI CreateThemeCard(
            string name,
            Transform parent,
            Vector2 anchoredPos,
            Sprite boxSprite,
            Sprite pillSprite,
            Sprite starsSprite,
            TMP_FontAsset balooExtraBold,
            TMP_FontAsset nunitoBold,
            Color darkBrown,
            Color mediumBrown)
        {
            GameObject cardGo = CreateUIObject(name, parent);
            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(246f, 266f);
            cardRt.anchoredPosition = anchoredPos;

            Image cardBg = cardGo.AddComponent<Image>();
            if (boxSprite != null)
            {
                cardBg.sprite = boxSprite;
                cardBg.type = Image.Type.Sliced;
                cardBg.color = Color.white;
            }
            else
            {
                cardBg.color = new Color(0.93f, 0.87f, 0.78f, 1f);
            }

            Outline cardOutline = cardGo.AddComponent<Outline>();
            cardOutline.effectColor = mediumBrown;
            cardOutline.effectDistance = new Vector2(2f, -2f);

            StoreThemeCardUI cardUI = cardGo.AddComponent<StoreThemeCardUI>();

            // Swatch Preview Square
            GameObject previewGo = CreateUIObject("SwatchPreview", cardGo.transform);
            RectTransform previewRt = previewGo.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0.5f, 1f);
            previewRt.anchorMax = new Vector2(0.5f, 1f);
            previewRt.pivot = new Vector2(0.5f, 1f);
            previewRt.sizeDelta = new Vector2(168f, 144f);
            previewRt.anchoredPosition = new Vector2(0f, -18f);

            RawImage previewRaw = previewGo.AddComponent<RawImage>();
            previewRaw.color = Color.white;
            Outline previewOutline = previewGo.AddComponent<Outline>();
            previewOutline.effectColor = new Color(0.75f, 0.68f, 0.60f, 0.75f);
            previewOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Title Label
            GameObject titleGo = CreateUIObject("TitleText", cardGo.transform);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0f);
            titleRt.anchorMax = new Vector2(0.5f, 0f);
            titleRt.pivot = new Vector2(0.5f, 0f);
            titleRt.sizeDelta = new Vector2(226f, 34f);
            titleRt.anchoredPosition = new Vector2(0f, 64f);

            TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            if (nunitoBold != null) titleTmp.font = nunitoBold;
            titleTmp.text = "Theme Name";
            titleTmp.fontSize = 21f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = darkBrown;

            // Price / Owned Pill Button
            GameObject btnGo = CreateUIObject("PriceButton", cardGo.transform);
            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(174f, 44f);
            btnRt.anchoredPosition = new Vector2(0f, 14f);

            Image btnImg = btnGo.AddComponent<Image>();
            if (pillSprite != null)
            {
                btnImg.sprite = pillSprite;
                btnImg.type = Image.Type.Sliced;
                btnImg.pixelsPerUnitMultiplier = 0.85f;
            }
            btnImg.color = new Color(0.86f, 0.80f, 0.72f, 1f);

            Outline btnOutline = btnGo.AddComponent<Outline>();
            btnOutline.effectColor = mediumBrown;
            btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            GameObject starGo = CreateUIObject("StarIcon", btnGo.transform);
            RectTransform starRt = starGo.GetComponent<RectTransform>();
            starRt.anchorMin = new Vector2(0f, 0.5f);
            starRt.anchorMax = new Vector2(0f, 0.5f);
            starRt.pivot = new Vector2(0f, 0.5f);
            starRt.sizeDelta = new Vector2(28f, 28f);
            starRt.anchoredPosition = new Vector2(22f, 0f);
            Image starImg = starGo.AddComponent<Image>();
            if (starsSprite != null) starImg.sprite = starsSprite;
            starImg.preserveAspect = true;

            GameObject labelGo = CreateUIObject("ActionLabel", btnGo.transform);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(16f, 0f);
            labelRt.offsetMax = new Vector2(-12f, 0f);

            TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            if (balooExtraBold != null) labelTmp.font = balooExtraBold;
            labelTmp.text = "300";
            labelTmp.fontSize = 24f;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = darkBrown;

            cardUI.ConfigureReferences(cardBg, previewRaw, titleTmp, btn, btnImg, starGo, labelTmp);
            return cardUI;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
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
