using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utsuru.Store;

namespace Utsuru.DictionaryUI.Editor
{
    /// <summary>
    /// Membangun tab Theme dan halaman Apply Theme di dalam Dictionary tanpa merusak layout Dictionary yang sudah ada.
    /// Juga menghubungkan DictionaryThemeApplier dengan BookContainer & sprite Book_*.png.
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

            var controller = Object.FindFirstObjectByType<DictionaryUIController>(FindObjectsInactive.Include);
            var bookSpread = Object.FindFirstObjectByType<DictionaryBookSpreadUI>(FindObjectsInactive.Include);
            if (controller == null || bookSpread == null)
            {
                Debug.LogWarning("[DictionaryThemeUIBuilder] DictionaryUIController atau DictionaryBookSpreadUI tidak ditemukan di scene.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(controller.gameObject, "Build Dictionary Theme UI");

            var zenFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ZenMaruGothic-Bold SDF.asset");
            var nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");
            if (zenFont != null) zenFont.ReadFontAssetDefinition();
            if (nunitoFont != null) nunitoFont.ReadFontAssetDefinition();

            Sprite cardBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Card Background.png");
            Sprite pillBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Category Pill.png");
            Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Button_Unselected.png");
            Sprite tabBookmarkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_theme.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_module.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_hiragana.png");

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
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(allThemes[i].bookAssetPath);
                bindings.Add(new DictionaryThemeApplier.ThemeSpriteBinding
                {
                    themeId = allThemes[i].id,
                    bookSprite = s
                });

                if (allThemes[i].id == StoreThemeCatalog.DefaultThemeId && s != null && bookCoverImg != null)
                {
                    // Set default editor cover sprite ke Plain
                    bookCoverImg.sprite = s;
                }
            }
            applier.ConfigureReferences(bookCoverImg, bindings);

            // Serialize bindings agar tersimpan di scene
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

            // Halaman Kiri (Plain, Sakura, Botanical)
            GameObject leftPage = CreateUIObject("LeftThemePage", themeRoot.transform);
            RectTransform leftRt = leftPage.GetComponent<RectTransform>();
            SetPageTransform(leftRt, isLeft: true);
            leftRt.anchoredPosition = new Vector2(56f, 20f);
            leftRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            CreatePageHeader(leftPage.transform, "BOOK THEMES", "Pilih dan terapkan tema buku kamusmu", zenFont, nunitoFont);

            GameObject noticeGo = CreateUIObject("StatusNotice", leftPage.transform);
            RectTransform noticeRt = noticeGo.GetComponent<RectTransform>();
            noticeRt.anchorMin = new Vector2(0.5f, 1f);
            noticeRt.anchorMax = new Vector2(0.5f, 1f);
            noticeRt.pivot = new Vector2(0.5f, 1f);
            noticeRt.sizeDelta = new Vector2(660f, 36f);
            noticeRt.anchoredPosition = new Vector2(0f, -126f);
            var noticeTmp = noticeGo.AddComponent<TextMeshProUGUI>();
            noticeTmp.font = nunitoFont ?? zenFont;
            noticeTmp.fontSize = 16.5f;
            noticeTmp.fontStyle = FontStyles.Bold;
            noticeTmp.alignment = TextAlignmentOptions.Center;
            noticeTmp.color = new Color(0.32f, 0.27f, 0.45f, 1f);
            noticeTmp.text = "Beli tema tambahan di Store dengan menekan F saat eksplorasi.";

            // Halaman Kanan (Autumn, Midnight, Summer + Tombol Open Store)
            GameObject rightPage = CreateUIObject("RightThemePage", themeRoot.transform);
            RectTransform rightRt = rightPage.GetComponent<RectTransform>();
            SetPageTransform(rightRt, isLeft: false);
            rightRt.anchoredPosition = new Vector2(-56f, 20f);
            rightRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            CreatePageHeader(rightPage.transform, "COLLECTION", "Tema yang sudah dibeli siap dipakai kapan saja", zenFont, nunitoFont);

            // Tombol Shortcut ke Store di header kanan
            GameObject storeBtnGo = CreateUIObject("BtnOpenStore", rightPage.transform);
            RectTransform storeBtnRt = storeBtnGo.GetComponent<RectTransform>();
            storeBtnRt.anchorMin = new Vector2(0.5f, 1f);
            storeBtnRt.anchorMax = new Vector2(0.5f, 1f);
            storeBtnRt.pivot = new Vector2(0.5f, 1f);
            storeBtnRt.sizeDelta = new Vector2(260f, 40f);
            storeBtnRt.anchoredPosition = new Vector2(0f, -124f);
            Image storeBtnImg = storeBtnGo.AddComponent<Image>();
            if (pillBgSprite != null)
            {
                storeBtnImg.sprite = pillBgSprite;
                storeBtnImg.type = Image.Type.Sliced;
                storeBtnImg.pixelsPerUnitMultiplier = 0.85f;
            }
            storeBtnImg.color = new Color(0.373f, 0.224f, 0.137f, 1f);
            Button openStoreBtn = storeBtnGo.AddComponent<Button>();
            openStoreBtn.targetGraphic = storeBtnImg;

            GameObject storeTxtGo = CreateUIObject("Text", storeBtnGo.transform);
            SetStretch(storeTxtGo.GetComponent<RectTransform>());
            var storeBtnTmp = storeTxtGo.AddComponent<TextMeshProUGUI>();
            storeBtnTmp.font = zenFont ?? nunitoFont;
            storeBtnTmp.fontSize = 17f;
            storeBtnTmp.fontStyle = FontStyles.Bold;
            storeBtnTmp.alignment = TextAlignmentOptions.Center;
            storeBtnTmp.color = Color.white;
            storeBtnTmp.text = "Open Store (F)";

            // 6 Slot Tema (3 di Halaman Kiri, 3 di Halaman Kanan)
            var slots = new List<DictionaryThemeSpreadUI.ThemeOptionSlot>();
            float[] rowYs = new float[] { -180f, -380f, -580f };

            for (int i = 0; i < allThemes.Count; i++)
            {
                Transform pageParent = i < 3 ? leftPage.transform : rightPage.transform;
                float posY = rowYs[i % 3];
                slots.Add(CreateThemeOptionRow(
                    $"ThemeSlot_{allThemes[i].id}",
                    pageParent,
                    posY,
                    cardBgSprite,
                    pillBgSprite,
                    btnSprite,
                    zenFont,
                    nunitoFont
                ));
            }

            // Wire DictionaryThemeSpreadUI
            themeSpreadUI.ConfigureReferences(applier, noticeTmp, openStoreBtn, slots);
            var spreadUiSo = new SerializedObject(themeSpreadUI);
            spreadUiSo.FindProperty("themeApplier").objectReferenceValue = applier;
            spreadUiSo.FindProperty("statusNoticeText").objectReferenceValue = noticeTmp;
            spreadUiSo.FindProperty("openStoreShortcutButton").objectReferenceValue = openStoreBtn;
            var optionSlotsProp = spreadUiSo.FindProperty("optionSlots");
            optionSlotsProp.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
            {
                var elem = optionSlotsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("cardBackground").objectReferenceValue = slots[i].cardBackground;
                elem.FindPropertyRelative("bookPreviewImage").objectReferenceValue = slots[i].bookPreviewImage;
                elem.FindPropertyRelative("swatchAccent").objectReferenceValue = slots[i].swatchAccent;
                elem.FindPropertyRelative("titleText").objectReferenceValue = slots[i].titleText;
                elem.FindPropertyRelative("statusBadgeText").objectReferenceValue = slots[i].statusBadgeText;
                elem.FindPropertyRelative("actionButton").objectReferenceValue = slots[i].actionButton;
                elem.FindPropertyRelative("actionButtonBg").objectReferenceValue = slots[i].actionButtonBg;
                elem.FindPropertyRelative("actionButtonText").objectReferenceValue = slots[i].actionButtonText;
            }
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
                Debug.Log("[DictionaryThemeUIBuilder] Tab Theme & DictionaryThemeApplier berhasil dibangun dan disimpan.");
            }
            else
            {
                Debug.Log("[DictionaryThemeUIBuilder] Tab Theme & DictionaryThemeApplier berhasil dibangun ke scene aktif.");
            }
        }

        private static DictionaryThemeSpreadUI.ThemeOptionSlot CreateThemeOptionRow(
            string name,
            Transform parent,
            float posY,
            Sprite cardBgSprite,
            Sprite pillSprite,
            Sprite btnSprite,
            TMP_FontAsset zenFont,
            TMP_FontAsset nunitoFont)
        {
            GameObject rowGo = CreateUIObject(name, parent);
            RectTransform rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 1f);
            rowRt.anchorMax = new Vector2(0.5f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.sizeDelta = new Vector2(660f, 176f);
            rowRt.anchoredPosition = new Vector2(0f, posY);

            Image rowBg = rowGo.AddComponent<Image>();
            if (cardBgSprite != null)
            {
                rowBg.sprite = cardBgSprite;
                rowBg.type = Image.Type.Sliced;
            }
            rowBg.color = new Color(0.99f, 0.97f, 0.93f, 1f);

            Outline rowOutline = rowGo.AddComponent<Outline>();
            rowOutline.effectColor = new Color(0.75f, 0.68f, 0.60f, 0.65f);
            rowOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Color Accent Bar di sisi kiri kartu
            GameObject accentGo = CreateUIObject("SwatchAccent", rowGo.transform);
            RectTransform accentRt = accentGo.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 0f);
            accentRt.anchorMax = new Vector2(0f, 1f);
            accentRt.pivot = new Vector2(0f, 0.5f);
            accentRt.sizeDelta = new Vector2(14f, -16f);
            accentRt.anchoredPosition = new Vector2(8f, 0f);
            Image accentImg = accentGo.AddComponent<Image>();
            accentImg.color = Color.white;

            // Book Preview Thumbnail
            GameObject previewGo = CreateUIObject("BookPreview", rowGo.transform);
            RectTransform previewRt = previewGo.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0f, 0.5f);
            previewRt.anchorMax = new Vector2(0f, 0.5f);
            previewRt.pivot = new Vector2(0f, 0.5f);
            previewRt.sizeDelta = new Vector2(170f, 118f);
            previewRt.anchoredPosition = new Vector2(34f, 0f);
            Image previewImg = previewGo.AddComponent<Image>();
            previewImg.preserveAspect = true;
            previewImg.color = Color.white;

            // Title Text
            GameObject titleGo = CreateUIObject("TitleText", rowGo.transform);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.sizeDelta = new Vector2(250f, 40f);
            titleRt.anchoredPosition = new Vector2(218f, 22f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.font = zenFont ?? nunitoFont;
            titleTmp.fontSize = 23f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.color = new Color(0.24f, 0.18f, 0.14f, 1f);
            titleTmp.text = "Theme";

            // Status Badge Text
            GameObject statusGo = CreateUIObject("StatusText", rowGo.transform);
            RectTransform statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0f, 0.5f);
            statusRt.anchorMax = new Vector2(0f, 0.5f);
            statusRt.pivot = new Vector2(0f, 0.5f);
            statusRt.sizeDelta = new Vector2(250f, 32f);
            statusRt.anchoredPosition = new Vector2(218f, -20f);
            var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.font = nunitoFont ?? zenFont;
            statusTmp.fontSize = 17f;
            statusTmp.fontStyle = FontStyles.Bold;
            statusTmp.alignment = TextAlignmentOptions.Left;
            statusTmp.color = new Color(0.35f, 0.30f, 0.45f, 1f);
            statusTmp.text = "Dimiliki";

            // Action Button (Apply / Active / Buy)
            GameObject btnGo = CreateUIObject("ActionButton", rowGo.transform);
            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.sizeDelta = new Vector2(148f, 52f);
            btnRt.anchoredPosition = new Vector2(-22f, 0f);

            Image btnImg = btnGo.AddComponent<Image>();
            Sprite resolvedBtnSprite = pillSprite != null ? pillSprite : btnSprite;
            if (resolvedBtnSprite != null)
            {
                btnImg.sprite = resolvedBtnSprite;
                btnImg.type = Image.Type.Sliced;
                btnImg.pixelsPerUnitMultiplier = 0.85f;
            }
            btnImg.color = new Color(0.36f, 0.25f, 0.58f, 1f);

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            GameObject btnTxtGo = CreateUIObject("Text", btnGo.transform);
            SetStretch(btnTxtGo.GetComponent<RectTransform>());
            var btnTmp = btnTxtGo.AddComponent<TextMeshProUGUI>();
            btnTmp.font = zenFont ?? nunitoFont;
            btnTmp.fontSize = 18.5f;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            btnTmp.text = "Apply";

            return new DictionaryThemeSpreadUI.ThemeOptionSlot
            {
                cardBackground = rowBg,
                bookPreviewImage = previewImg,
                swatchAccent = accentImg,
                titleText = titleTmp,
                statusBadgeText = statusTmp,
                actionButton = btn,
                actionButtonBg = btnImg,
                actionButtonText = btnTmp
            };
        }

        private static void CreatePageHeader(Transform parent, string title, string subtitle, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont)
        {
            GameObject titleGo = CreateUIObject("PageTitle", parent);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(680f, 44f);
            titleRt.anchoredPosition = new Vector2(0f, -44f);

            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.font = zenFont;
            titleTmp.fontSize = 28f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.28f, 0.22f, 0.55f, 1f);
            titleTmp.text = title;

            GameObject subGo = CreateUIObject("PageSubtitle", parent);
            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 1f);
            subRt.anchorMax = new Vector2(0.5f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.sizeDelta = new Vector2(680f, 32f);
            subRt.anchoredPosition = new Vector2(0f, -86f);

            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.font = nunitoFont;
            subTmp.fontSize = 16.5f;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.42f, 0.38f, 0.54f, 1f);
            subTmp.text = subtitle;
        }

        private static void SetPageTransform(RectTransform rt, bool isLeft)
        {
            rt.anchorMin = isLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            rt.anchorMax = isLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            rt.pivot = isLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(740f, 820f);
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
