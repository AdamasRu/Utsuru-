using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utsuru.Store;
using Utsuru.Store.Editor;

namespace Utsuru.DictionaryUI.Editor
{
    /// <summary>
    /// Membangun tab Theme model slide per tema di dalam Dictionary:
    /// - Page Kiri kosong agar fokus ke live preview sampul buku.
    /// - Page Kanan berisi Header bergaya Quiz (nama tema + slide), deskripsi,
    ///   tombol status (Apply / Applied / Locked), dan tombol hint buka Store jika terkunci.
    /// </summary>
    [InitializeOnLoad]
    public static class DictionaryThemeUIBuilder
    {
        private const string DayShowcaseScenePath = "Assets/Japanese_Street/Scenes/Day_Showcase.unity";

        static DictionaryThemeUIBuilder()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureSetupOnEditorLoad;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (Application.isPlaying) return;
            if (scene.path == DayShowcaseScenePath)
            {
                EnsureThemeUIInActiveScene(saveScene: false);
            }
        }

        private static void EnsureSetupOnEditorLoad()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene active = EditorSceneManager.GetActiveScene();
            if (active.IsValid() && active.path == DayShowcaseScenePath)
            {
                EnsureThemeUIInActiveScene(saveScene: !active.isDirty);
            }
        }

        private static void EnsureThemeUIInActiveScene(bool saveScene)
        {
            var controller = Object.FindFirstObjectByType<DictionaryUIController>(FindObjectsInactive.Include);
            var spread = Object.FindFirstObjectByType<DictionaryBookSpreadUI>(FindObjectsInactive.Include);
            if (controller == null || spread == null) return;

            bool hasThemeTab = spread.transform.Find("ThemeSpreadRoot") != null &&
                               Object.FindFirstObjectByType<DictionaryThemeApplier>(FindObjectsInactive.Include) != null;
            if (hasThemeTab) return;

            BuildThemeUI(saveScene);
        }

        [MenuItem("Tools/Utsuru/Build Dictionary Theme UI")]
        public static void BuildThemeUIMenuItem()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.path != DayShowcaseScenePath && File.Exists(Path.GetFullPath(DayShowcaseScenePath)))
            {
                if (active.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                EditorSceneManager.OpenScene(DayShowcaseScenePath, OpenSceneMode.Single);
            }
            BuildThemeUI(saveScene: true);
        }

        public static void BuildThemeUI(bool saveScene = true)
        {
            DictionaryAssetImporter.ConfigureSprites();
            StoreUIBuilder.ConfigureStoreSprites();

            var controller = Object.FindFirstObjectByType<DictionaryUIController>(FindObjectsInactive.Include);
            var bookSpread = Object.FindFirstObjectByType<DictionaryBookSpreadUI>(FindObjectsInactive.Include);
            if (controller == null || bookSpread == null)
            {
                Debug.LogWarning("[DictionaryThemeUIBuilder] DictionaryUIController atau DictionaryBookSpreadUI tidak ditemukan di scene.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(controller.gameObject, "Build Dictionary Theme UI");

            // Font
            var zenFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ZenMaruGothic-Bold SDF.asset");
            var nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");
            var balooExtraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Baloo_2/static/Baloo2-ExtraBold SDF.asset")
                ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Baloo-Regular SDF 1.asset")
                ?? zenFont;

            if (zenFont != null) zenFont.ReadFontAssetDefinition();
            if (nunitoFont != null) nunitoFont.ReadFontAssetDefinition();
            if (balooExtraBold != null) balooExtraBold.ReadFontAssetDefinition();

            // Sprites
            Sprite pillHeaderSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Quiz/Pill_Header.png");
            Sprite bgActionBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Store/background_btn_buy.png");
            Sprite tabBookmarkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_theme.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_module.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_hiragana.png");

            Color darkBrown = new Color(0.373f, 0.224f, 0.137f, 1f);
            Color quizBrown = new Color(0.24f, 0.16f, 0.13f, 1f);
            Color descBrown = new Color(0.36f, 0.26f, 0.21f, 1f);

            // 1. Pastikan DictionaryThemeApplier terpasang dan tersambung ke BookContainer + semua Book_*.png
            Transform dictView = controller.transform.Find("DictionaryView");
            Transform bookContainerTr = dictView != null ? dictView.Find("BookContainer") : null;
            Image bookCoverImg = bookContainerTr != null ? bookContainerTr.GetComponent<Image>() : null;

            var applier = controller.GetComponent<DictionaryThemeApplier>();
            if (applier == null)
            {
                applier = controller.gameObject.AddComponent<DictionaryThemeApplier>();
            }

            var bindings = new List<DictionaryThemeApplier.ThemeSpriteBinding>();
            IReadOnlyList<StoreThemeDefinition> allThemes = StoreThemeCatalog.All;

            for (int i = 0; i < allThemes.Count; i++)
            {
                Sprite bookSprite = AssetDatabase.LoadAssetAtPath<Sprite>(allThemes[i].bookAssetPath);
                bindings.Add(new DictionaryThemeApplier.ThemeSpriteBinding
                {
                    themeId = allThemes[i].id,
                    bookSprite = bookSprite
                });

                if (allThemes[i].id == StoreThemeCatalog.DefaultThemeId && bookSprite != null && bookCoverImg != null)
                {
                    bookCoverImg.sprite = bookSprite;
                }
            }
            applier.ConfigureReferences(bookCoverImg, bindings);

            var applierSo = new SerializedObject(applier);
            applierSo.FindProperty("bookCoverImage").objectReferenceValue = bookCoverImg;
            var spritesProp = applierSo.FindProperty("themeSprites");
            spritesProp.arraySize = bindings.Count;
            for (int i = 0; i < bindings.Count; i++)
            {
                var elem = spritesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("themeId").stringValue = bindings[i].themeId;
                elem.FindPropertyRelative("bookSprite").objectReferenceValue = bindings[i].bookSprite;
            }
            applierSo.ApplyModifiedPropertiesWithoutUndo();

            // 2. Buat atau perbarui Tab_Theme di TopTabs
            Button tabThemeBtn = null;
            Transform topTabsTr = bookContainerTr != null ? bookContainerTr.Find("TopTabs") : null;
            if (topTabsTr != null)
            {
                Transform oldTab = topTabsTr.Find("Tab_Theme");
                if (oldTab != null) Object.DestroyImmediate(oldTab.gameObject);

                GameObject tabGo = CreateUIObject("Tab_Theme", topTabsTr);
                RectTransform tabRt = tabGo.GetComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(1f, 1f);
                tabRt.anchorMax = new Vector2(1f, 1f);
                tabRt.pivot = new Vector2(0.5f, 1f);
                tabRt.sizeDelta = new Vector2(86f, 149f);
                tabRt.anchoredPosition = new Vector2(-470f, -29.36f);
                tabRt.localScale = new Vector3(0.9f, 0.9f, 1f);

                Image tabImg = tabGo.AddComponent<Image>();
                tabImg.sprite = tabBookmarkSprite;
                tabImg.preserveAspect = true;
                tabImg.color = Color.white;

                tabThemeBtn = tabGo.AddComponent<Button>();
                tabThemeBtn.targetGraphic = tabImg;
            }

            // 3. Buat atau perbarui ThemeSpreadRoot di BookSpread
            Transform spreadTrans = bookSpread.transform;
            Transform oldRoot = spreadTrans.Find("ThemeSpreadRoot");
            if (oldRoot != null) Object.DestroyImmediate(oldRoot.gameObject);

            GameObject themeRoot = CreateUIObject("ThemeSpreadRoot", spreadTrans);
            SetStretch(themeRoot.GetComponent<RectTransform>());
            var themeSpreadUI = themeRoot.AddComponent<DictionaryThemeSpreadUI>();

            // Halaman Kiri: KOSONG sesuai desain (fokus preview sampul buku)
            GameObject leftPage = CreateUIObject("LeftThemePage", themeRoot.transform);
            RectTransform leftRt = leftPage.GetComponent<RectTransform>();
            SetPageTransform(leftRt, isLeft: true);

            // Halaman Kanan: Header (gaya Quiz), Deskripsi, Tombol Status, Tombol Hint Store
            GameObject rightPage = CreateUIObject("RightThemePage", themeRoot.transform);
            RectTransform rightRt = rightPage.GetComponent<RectTransform>();
            SetPageTransform(rightRt, isLeft: false);
            CanvasGroup rightCg = rightPage.AddComponent<CanvasGroup>();

            // A. Header Pill Badge (gaya Quiz) - Menampilkan Nama Tema
            GameObject headerBadgeGo = CreateUIObject("HeaderBadge", rightPage.transform);
            RectTransform headerBadgeRt = headerBadgeGo.GetComponent<RectTransform>();
            headerBadgeRt.anchorMin = new Vector2(0.5f, 1f);
            headerBadgeRt.anchorMax = new Vector2(0.5f, 1f);
            headerBadgeRt.pivot = new Vector2(0.5f, 1f);
            headerBadgeRt.sizeDelta = new Vector2(496f, 63f);
            headerBadgeRt.anchoredPosition = new Vector2(0f, -49.56f);

            var headerImg = headerBadgeGo.AddComponent<Image>();
            if (pillHeaderSprite != null)
            {
                headerImg.sprite = pillHeaderSprite;
                headerImg.type = Image.Type.Sliced;
            }
            headerImg.color = pillHeaderSprite != null ? Color.white : quizBrown;

            GameObject headerTextGo = CreateUIObject("Text", headerBadgeGo.transform);
            SetStretch(headerTextGo.GetComponent<RectTransform>());
            var headerTmp = headerTextGo.AddComponent<TextMeshProUGUI>();
            headerTmp.font = zenFont;
            headerTmp.fontSize = 28f;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color = Color.white;
            headerTmp.text = "Plain Theme";

            // B. Subtitle Slide Indicator (gaya Quiz PromptText)
            GameObject promptGo = CreateUIObject("PromptText", rightPage.transform);
            RectTransform promptRt = promptGo.GetComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0.5f, 1f);
            promptRt.anchorMax = new Vector2(0.5f, 1f);
            promptRt.pivot = new Vector2(0.5f, 1f);
            promptRt.sizeDelta = new Vector2(748.3f, 32f);
            promptRt.anchoredPosition = new Vector2(0f, -116.1f);

            var promptTmp = promptGo.AddComponent<TextMeshProUGUI>();
            promptTmp.font = nunitoFont;
            promptTmp.fontSize = 30f;
            promptTmp.fontStyle = FontStyles.Bold;
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.color = quizBrown;
            promptTmp.text = "Theme 1 / 6";

            // C. Tombol Status (Apply / Applied / Locked) - Gaya Pill Store
            GameObject statusBtnGo = CreateUIObject("StatusActionButton", rightPage.transform);
            RectTransform statusBtnRt = statusBtnGo.GetComponent<RectTransform>();
            statusBtnRt.anchorMin = new Vector2(0.5f, 1f);
            statusBtnRt.anchorMax = new Vector2(0.5f, 1f);
            statusBtnRt.pivot = new Vector2(0.5f, 1f);
            statusBtnRt.sizeDelta = new Vector2(230f, 78f);
            statusBtnRt.anchoredPosition = new Vector2(0f, -280f);

            Image statusBtnImg = statusBtnGo.AddComponent<Image>();
            if (bgActionBtnSprite != null)
            {
                statusBtnImg.sprite = bgActionBtnSprite;
                statusBtnImg.type = Image.Type.Simple;
                statusBtnImg.preserveAspect = true;
            }
            statusBtnImg.color = Color.white;

            Button statusBtn = statusBtnGo.AddComponent<Button>();
            statusBtn.targetGraphic = statusBtnImg;

            GameObject statusTxtGo = CreateUIObject("Text", statusBtnGo.transform);
            SetStretch(statusTxtGo.GetComponent<RectTransform>());
            var statusBtnTmp = statusTxtGo.AddComponent<TextMeshProUGUI>();
            statusBtnTmp.font = balooExtraBold ?? zenFont;
            statusBtnTmp.fontSize = 28f;
            statusBtnTmp.fontStyle = FontStyles.Bold;
            statusBtnTmp.alignment = TextAlignmentOptions.Center;
            statusBtnTmp.color = darkBrown;
            statusBtnTmp.text = "Applied";

            // D. Tombol Hint Buka Store (hanya muncul jika tema BELUM dimiliki)
            GameObject storeHintGo = CreateUIObject("BtnOpenStoreHint", rightPage.transform);
            RectTransform storeHintRt = storeHintGo.GetComponent<RectTransform>();
            storeHintRt.anchorMin = new Vector2(0.5f, 1f);
            storeHintRt.anchorMax = new Vector2(0.5f, 1f);
            storeHintRt.pivot = new Vector2(0.5f, 1f);
            storeHintRt.sizeDelta = new Vector2(360f, 56f);
            storeHintRt.anchoredPosition = new Vector2(0f, -385f);

            Image storeHintImg = storeHintGo.AddComponent<Image>();
            if (pillHeaderSprite != null)
            {
                storeHintImg.sprite = pillHeaderSprite;
                storeHintImg.type = Image.Type.Sliced;
            }
            storeHintImg.color = pillHeaderSprite != null ? Color.white : quizBrown;

            Button storeHintBtn = storeHintGo.AddComponent<Button>();
            storeHintBtn.targetGraphic = storeHintImg;

            GameObject storeHintTxtGo = CreateUIObject("Text", storeHintGo.transform);
            SetStretch(storeHintTxtGo.GetComponent<RectTransform>());
            var storeHintTmp = storeHintTxtGo.AddComponent<TextMeshProUGUI>();
            storeHintTmp.font = zenFont ?? nunitoFont;
            storeHintTmp.fontSize = 20f;
            storeHintTmp.fontStyle = FontStyles.Bold;
            storeHintTmp.alignment = TextAlignmentOptions.Center;
            storeHintTmp.color = Color.white;
            storeHintTmp.text = "Open Store (F)  •  300 Points";
            storeHintGo.SetActive(false);

            // E. Feedback Notice di bawah tombol
            GameObject noticeGo = CreateUIObject("StatusNotice", rightPage.transform);
            RectTransform noticeRt = noticeGo.GetComponent<RectTransform>();
            noticeRt.anchorMin = new Vector2(0.5f, 1f);
            noticeRt.anchorMax = new Vector2(0.5f, 1f);
            noticeRt.pivot = new Vector2(0.5f, 1f);
            noticeRt.sizeDelta = new Vector2(540f, 44f);
            noticeRt.anchoredPosition = new Vector2(0f, -470f);

            var noticeTmp = noticeGo.AddComponent<TextMeshProUGUI>();
            noticeTmp.font = nunitoFont ?? zenFont;
            noticeTmp.fontSize = 20f;
            noticeTmp.fontStyle = FontStyles.Bold;
            noticeTmp.alignment = TextAlignmentOptions.Center;
            noticeTmp.color = quizBrown;
            noticeTmp.text = "";

            // Wire DictionaryThemeSpreadUI
            themeSpreadUI.ConfigureReferences(
                applier,
                bookSpread,
                leftPage,
                rightPage,
                rightCg,
                headerTmp,
                promptTmp,
                statusBtn,
                statusBtnImg,
                statusBtnTmp,
                storeHintBtn,
                storeHintTmp,
                noticeTmp
            );

            var spreadUiSo = new SerializedObject(themeSpreadUI);
            spreadUiSo.FindProperty("themeApplier").objectReferenceValue = applier;
            spreadUiSo.FindProperty("bookSpread").objectReferenceValue = bookSpread;
            spreadUiSo.FindProperty("leftPageRoot").objectReferenceValue = leftPage;
            spreadUiSo.FindProperty("rightPageRoot").objectReferenceValue = rightPage;
            spreadUiSo.FindProperty("rightPageCanvasGroup").objectReferenceValue = rightCg;
            spreadUiSo.FindProperty("themeNameHeaderText").objectReferenceValue = headerTmp;
            spreadUiSo.FindProperty("slideSubtitleText").objectReferenceValue = promptTmp;
            spreadUiSo.FindProperty("statusActionButton").objectReferenceValue = statusBtn;
            spreadUiSo.FindProperty("statusActionButtonBg").objectReferenceValue = statusBtnImg;
            spreadUiSo.FindProperty("statusActionButtonText").objectReferenceValue = statusBtnTmp;
            spreadUiSo.FindProperty("storeHintButton").objectReferenceValue = storeHintBtn;
            spreadUiSo.FindProperty("storeHintButtonText").objectReferenceValue = storeHintTmp;
            spreadUiSo.FindProperty("statusNoticeText").objectReferenceValue = noticeTmp;
            spreadUiSo.ApplyModifiedPropertiesWithoutUndo();

            themeRoot.SetActive(false);

            // 4. Wire ke DictionaryBookSpreadUI & DictionaryUIController
            var bookSpreadSo = new SerializedObject(bookSpread);
            bookSpreadSo.FindProperty("themeSpreadRoot").objectReferenceValue = themeRoot;
            bookSpreadSo.FindProperty("themeSpreadUI").objectReferenceValue = themeSpreadUI;
            bookSpreadSo.ApplyModifiedPropertiesWithoutUndo();

            var ctrlSo = new SerializedObject(controller);
            if (tabThemeBtn != null)
                ctrlSo.FindProperty("tabThemeButton").objectReferenceValue = tabThemeBtn;
            ctrlSo.FindProperty("themeApplier").objectReferenceValue = applier;
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();

            Scene currentScene = EditorSceneManager.GetActiveScene();
            EditorUtility.SetDirty(controller.gameObject);
            EditorSceneManager.MarkSceneDirty(currentScene);
            if (saveScene)
            {
                EditorSceneManager.SaveScene(currentScene);
                Debug.Log("[DictionaryThemeUIBuilder] Tab Theme (Slide Live Preview + Fade) berhasil dibangun dan disimpan.");
            }
            else
            {
                Debug.Log("[DictionaryThemeUIBuilder] Tab Theme (Slide Live Preview + Fade) berhasil dibangun ke scene aktif.");
            }
        }

        private static void SetPageTransform(RectTransform rt, bool isLeft)
        {
            if (isLeft)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(740f, 820f);
                rt.anchoredPosition = new Vector2(17f, 0f);
                rt.localScale = Vector3.one;
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(740f, 820f);
                rt.anchoredPosition = new Vector2(0f, 0f);
                rt.localScale = Vector3.one;
            }
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
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
