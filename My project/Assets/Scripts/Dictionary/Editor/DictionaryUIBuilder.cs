using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI.Editor
{
    public static class DictionaryUIBuilder
    {
        [MenuItem("Tools/Utsuru/Build Dictionary Book UI")]
        public static void BuildUI()
        {
            // 1. Pastikan sprite sudah terkonfigurasi
            DictionaryAssetImporter.ConfigureSprites();

            // 2. Muat aset Sprite
            var bgDictSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/BG_dictionary.png");
            var bookPlainSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Book/Book_Plain.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Book/Book_Purple.png");
            var coverCounterPillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Book/Book_CoverCounterPill.png");

            var bmAlbumSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_home.png");
            var bmLikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_like.png");
            var bmFavoriteSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_favorite.png");
            var bmQuizSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_quiz.png");
            var bmHiraganaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_hiragana.png");
            var bmModuleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_module.png");
            var bmExitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_exit.png");
            var btnPrevSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_previous.png");
            var btnNextSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_next.png");

            var polaroidBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Polaroid/Polaroid.png");
            var tapeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Polaroid/Tape_Washi.png");

            var likeOffSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Polaroid/Btn_Like_Off.png");
            var likeOnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Polaroid/Btn_Like_On.png");
            var favOffSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Polaroid/Btn_Favorite_Off.png");
            var favOnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Polaroid/Btn_Favorite_On.png");

            // 3. Muat Font TMP SDF
            var zenFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ZenMaruGothic-Bold SDF.asset");
            var nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");
            if (zenFont != null) zenFont.ReadFontAssetDefinition();
            if (nunitoFont != null) nunitoFont.ReadFontAssetDefinition();

            // 4. Cari Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[DictionaryUIBuilder] Canvas tidak ditemukan di scene!");
                return;
            }

            Undo.RegisterCompleteObjectUndo(canvas.gameObject, "Build Dictionary Book UI");

            // 5. Cari atau Buat Canvas/Dictionary
            Transform dictTrans = canvas.transform.Find("Dictionary");
            GameObject dictGo;
            if (dictTrans == null)
            {
                dictGo = CreateUIObject("Dictionary", canvas.transform);
                var dictRt = dictGo.GetComponent<RectTransform>();
                SetStretch(dictRt);
            }
            else
            {
                dictGo = dictTrans.gameObject;
            }

            var controller = dictGo.GetComponent<DictionaryUIController>();
            if (controller == null) controller = dictGo.AddComponent<DictionaryUIController>();

            // 6. Buat atau Reset DictionaryView
            Transform oldViewTrans = dictGo.transform.Find("DictionaryView");
            if (oldViewTrans != null)
            {
                Object.DestroyImmediate(oldViewTrans.gameObject);
            }

            GameObject viewGo = CreateUIObject("DictionaryView", dictGo.transform);
            RectTransform viewRt = viewGo.GetComponent<RectTransform>();
            SetStretch(viewRt);
            var viewCg = viewGo.AddComponent<CanvasGroup>();
            viewCg.alpha = 1f;

            // 6.1 FadeBackground (BG_dictionary.png)
            GameObject bgGo = CreateUIObject("FadeBackground", viewGo.transform);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            SetStretch(bgRt);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = bgDictSprite;
            bgImg.color = Color.white;
            bgImg.raycastTarget = true; // Blok klik tembus ke dunia 3D

            // 6.2 BookContainer (Book_Purple.png)
            GameObject bookGo = CreateUIObject("BookContainer", viewGo.transform);
            RectTransform bookRt = bookGo.GetComponent<RectTransform>();
            bookRt.anchorMin = new Vector2(0.5f, 0.5f);
            bookRt.anchorMax = new Vector2(0.5f, 0.5f);
            bookRt.pivot = new Vector2(0.5f, 0.5f);
            bookRt.sizeDelta = new Vector2(1720f, 940f);
            bookRt.anchoredPosition = new Vector2(0f, -25f);
            var bookImg = bookGo.AddComponent<Image>();
            bookImg.sprite = bookPlainSprite;
            bookImg.preserveAspect = false;

            // 6.3 TopTabs (Bookmark Tabs di Atas Buku)
            GameObject tabsGo = CreateUIObject("TopTabs", bookGo.transform);
            RectTransform tabsRt = tabsGo.GetComponent<RectTransform>();
            tabsRt.anchorMin = new Vector2(0.5f, 1f);
            tabsRt.anchorMax = new Vector2(0.5f, 1f);
            tabsRt.pivot = new Vector2(0.5f, 0f);
            tabsRt.sizeDelta = new Vector2(1600f, 120f);
            tabsRt.anchoredPosition = new Vector2(0f, -50f);

            // Tab Kiri: Top-Left (Scale 0.9)
            Button tabAlbum = CreateTabButton("Tab_Album", tabsGo.transform, bmAlbumSprite, new Vector2(86f, 149f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(75.35f, -29.36f), 0.9f);
            Button tabLike = CreateTabButton("Tab_Like", tabsGo.transform, bmLikeSprite, new Vector2(86f, 149f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(165.35f, -29.36f), 0.9f);
            Button tabFav = CreateTabButton("Tab_Favorite", tabsGo.transform, bmFavoriteSprite, new Vector2(86f, 119f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(255.35f, -29.36f), 0.9f);
            Button tabQuiz = CreateTabButton("Tab_Quiz", tabsGo.transform, bmQuizSprite, new Vector2(86f, 119f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(345.35f, -29.36f), 0.9f);

            // Tab Kanan: Top-Right (Scale 0.9) - 2 Tab (Hiragana, Module) + Theme + Exit
            Button tabHiragana = CreateTabButton("Tab_Hiragana", tabsGo.transform, bmHiraganaSprite, new Vector2(86f, 119f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-200f, -29.36f), 0.9f);
            Button tabModule = CreateTabButton("Tab_Module", tabsGo.transform, bmModuleSprite, new Vector2(86f, 119f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-290f, -29.36f), 0.9f);
            Button tabExit = CreateTabButton("Tab_Exit", tabsGo.transform, bmExitSprite, new Vector2(84f, 85f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-45.45f, -84f), 0.9f);

            // 6.4 BookSpread (Area Isi Buku 2 Halaman)
            GameObject spreadGo = CreateUIObject("BookSpread", bookGo.transform);
            RectTransform spreadRt = spreadGo.GetComponent<RectTransform>();
            spreadRt.anchorMin = new Vector2(0.5f, 0.5f);
            spreadRt.anchorMax = new Vector2(0.5f, 0.5f);
            spreadRt.pivot = new Vector2(0.5f, 0.5f);
            spreadRt.sizeDelta = new Vector2(1640f, 860f);
            spreadRt.anchoredPosition = new Vector2(0f, -10f);

            var bookSpread = spreadGo.AddComponent<DictionaryBookSpreadUI>();

            // 6.4.1 PhotoSpreadRoot
            GameObject photoSpreadRoot = CreateUIObject("PhotoSpreadRoot", spreadGo.transform);
            RectTransform photoSpreadRt = photoSpreadRoot.GetComponent<RectTransform>();
            SetStretch(photoSpreadRt);
            var photoSpreadCg = photoSpreadRoot.AddComponent<CanvasGroup>();
            photoSpreadCg.alpha = 1f;

            // Halaman Kiri (Middle-Left, Pos X = 17)
            GameObject leftPageGo = CreateUIObject("LeftPage", photoSpreadRoot.transform);
            RectTransform leftPageRt = leftPageGo.GetComponent<RectTransform>();
            leftPageRt.anchorMin = new Vector2(0f, 0.5f);
            leftPageRt.anchorMax = new Vector2(0f, 0.5f);
            leftPageRt.pivot = new Vector2(0f, 0.5f);
            leftPageRt.sizeDelta = new Vector2(740f, 820f);
            leftPageRt.anchoredPosition = new Vector2(17f, 0f);

            // Tombol Klik Halaman Kiri (Previous Spread)
            GameObject leftClickGo = CreateUIObject("Btn_PreviousPage", leftPageGo.transform);
            RectTransform leftClickRt = leftClickGo.GetComponent<RectTransform>();
            leftClickRt.anchorMin = new Vector2(0f, 0f);
            leftClickRt.anchorMax = new Vector2(0f, 1f);
            leftClickRt.pivot = new Vector2(0f, 0.5f);
            leftClickRt.sizeDelta = new Vector2(75f, 0f);
            leftClickRt.anchoredPosition = Vector2.zero;
            var leftClickImg = leftClickGo.AddComponent<Image>();
            if (btnPrevSprite != null)
            {
                leftClickImg.sprite = btnPrevSprite;
                leftClickImg.preserveAspect = true;
                leftClickImg.color = Color.white;
            }
            else
            {
                leftClickImg.color = new Color(0f, 0f, 0f, 0.001f);
            }
            var leftClickBtn = leftClickGo.AddComponent<Button>();
            leftClickBtn.targetGraphic = leftClickImg;

            // SlotGrid Kiri
            GameObject leftGridGo = CreateUIObject("SlotGrid_Left", leftPageGo.transform);
            RectTransform leftGridRt = leftGridGo.GetComponent<RectTransform>();
            SetStretch(leftGridRt);

            // Halaman Kanan (Middle-Right, Pos X = -100, Pivot = (0, 0.5))
            GameObject rightPageGo = CreateUIObject("RightPage", photoSpreadRoot.transform);
            RectTransform rightPageRt = rightPageGo.GetComponent<RectTransform>();
            rightPageRt.anchorMin = new Vector2(1f, 0.5f);
            rightPageRt.anchorMax = new Vector2(1f, 0.5f);
            rightPageRt.pivot = new Vector2(0f, 0.5f);
            rightPageRt.sizeDelta = new Vector2(740f, 820f);
            rightPageRt.anchoredPosition = new Vector2(-100f, 0f);

            // Tombol Klik Halaman Kanan (Next Spread)
            GameObject rightClickGo = CreateUIObject("Btn_NextPage", rightPageGo.transform);
            RectTransform rightClickRt = rightClickGo.GetComponent<RectTransform>();
            rightClickRt.anchorMin = new Vector2(0f, 0f);
            rightClickRt.anchorMax = new Vector2(0f, 1f);
            rightClickRt.pivot = new Vector2(0f, 0.5f);
            rightClickRt.sizeDelta = new Vector2(75f, 0f);
            rightClickRt.anchoredPosition = new Vector2(25f, 0f);
            var rightClickImg = rightClickGo.AddComponent<Image>();
            if (btnNextSprite != null)
            {
                rightClickImg.sprite = btnNextSprite;
                rightClickImg.preserveAspect = true;
                rightClickImg.color = Color.white;
            }
            else
            {
                rightClickImg.color = new Color(0f, 0f, 0f, 0.001f);
            }
            var rightClickBtn = rightClickGo.AddComponent<Button>();
            rightClickBtn.targetGraphic = rightClickImg;

            // SlotGrid Kanan
            GameObject rightGridGo = CreateUIObject("SlotGrid_Right", rightPageGo.transform);
            RectTransform rightGridRt = rightGridGo.GetComponent<RectTransform>();
            SetStretch(rightGridRt);

            // Pagination Number Text (Kiri & Kanan Bawah)
            GameObject leftNumGo = CreateUIObject("LeftPageNum", leftPageGo.transform);
            RectTransform leftNumRt = leftNumGo.GetComponent<RectTransform>();
            leftNumRt.anchorMin = new Vector2(0f, 0f);
            leftNumRt.anchorMax = new Vector2(0f, 0f);
            leftNumRt.pivot = new Vector2(0f, 0f);
            leftNumRt.sizeDelta = new Vector2(100f, 40f);
            leftNumRt.anchoredPosition = new Vector2(30f, 15f);
            var leftNumTmp = leftNumGo.AddComponent<TextMeshProUGUI>();
            leftNumTmp.font = nunitoFont;
            leftNumTmp.fontSize = 26;
            leftNumTmp.fontStyle = FontStyles.Bold;
            leftNumTmp.color = new Color(0.48f, 0.42f, 0.52f, 0.8f);
            leftNumTmp.text = "1";
            leftNumTmp.raycastTarget = false;

            GameObject rightNumGo = CreateUIObject("RightPageNum", rightPageGo.transform);
            RectTransform rightNumRt = rightNumGo.GetComponent<RectTransform>();
            rightNumRt.anchorMin = new Vector2(1f, 0f);
            rightNumRt.anchorMax = new Vector2(1f, 0f);
            rightNumRt.pivot = new Vector2(1f, 0f);
            rightNumRt.sizeDelta = new Vector2(100f, 40f);
            rightNumRt.anchoredPosition = new Vector2(-30f, 15f);
            var rightNumTmp = rightNumGo.AddComponent<TextMeshProUGUI>();
            rightNumTmp.font = nunitoFont;
            rightNumTmp.fontSize = 26;
            rightNumTmp.fontStyle = FontStyles.Bold;
            rightNumTmp.alignment = TextAlignmentOptions.Right;
            rightNumTmp.color = new Color(0.48f, 0.42f, 0.52f, 0.8f);
            rightNumTmp.text = "2";
            rightNumTmp.raycastTarget = false;

            // 6.4.2 Buat 8 Slot Polaroid
            var polaroidSlots = new DictionaryPolaroidCardUI[8];
            Vector2 anchorML = new Vector2(0f, 0.5f);
            Vector2 anchorMR = new Vector2(1f, 0.5f);

            // Halaman Kiri (Slot 1-4: Middle-Left)
            polaroidSlots[0] = CreatePolaroidSlot("Slot_1", leftGridGo.transform, anchorML, anchorML, new Vector2(225f, 185f), 0, polaroidBgSprite, tapeSprite, likeOffSprite, likeOnSprite, favOffSprite, favOnSprite, zenFont, nunitoFont);
            polaroidSlots[1] = CreatePolaroidSlot("Slot_2", leftGridGo.transform, anchorML, anchorML, new Vector2(525f, 185f), 1, polaroidBgSprite, tapeSprite, likeOffSprite, likeOnSprite, favOffSprite, favOnSprite, zenFont, nunitoFont);
            polaroidSlots[2] = CreatePolaroidSlot("Slot_3", leftGridGo.transform, anchorML, anchorML, new Vector2(225f, -175f), 2, polaroidBgSprite, tapeSprite, likeOffSprite, likeOnSprite, favOffSprite, favOnSprite, zenFont, nunitoFont);
            polaroidSlots[3] = CreatePolaroidSlot("Slot_4", leftGridGo.transform, anchorML, anchorML, new Vector2(525f, -175f), 3, polaroidBgSprite, tapeSprite, likeOffSprite, likeOnSprite, favOffSprite, favOnSprite, zenFont, nunitoFont);

            // Halaman Kanan (Slot 5-8: Middle-Left)
            polaroidSlots[4] = CreatePolaroidSlot("Slot_5", rightGridGo.transform, anchorML, anchorML, new Vector2(-490f, 185f), 4, polaroidBgSprite, tapeSprite, likeOffSprite, likeOnSprite, favOffSprite, favOnSprite, zenFont, nunitoFont);
            polaroidSlots[5] = CreatePolaroidSlot("Slot_6", rightGridGo.transform, anchorML, anchorML, new Vector2(-190f, 185f), 5, polaroidBgSprite, tapeSprite, likeOffSprite, likeOnSprite, favOffSprite, favOnSprite, zenFont, nunitoFont);
            polaroidSlots[6] = CreatePolaroidSlot("Slot_7", rightGridGo.transform, anchorML, anchorML, new Vector2(-490f, -175f), 6, polaroidBgSprite, tapeSprite, likeOffSprite, likeOnSprite, favOffSprite, favOnSprite, zenFont, nunitoFont);
            polaroidSlots[7] = CreatePolaroidSlot("Slot_8", rightGridGo.transform, anchorML, anchorML, new Vector2(-190f, -175f), 7, polaroidBgSprite, tapeSprite, likeOffSprite, likeOnSprite, favOffSprite, favOnSprite, zenFont, nunitoFont);

            // 6.4.3 EmptyStateNotice
            GameObject emptyGo = CreateUIObject("EmptyStateNotice", spreadGo.transform);
            RectTransform emptyRt = emptyGo.GetComponent<RectTransform>();
            emptyRt.anchorMin = new Vector2(0.5f, 0.5f);
            emptyRt.anchorMax = new Vector2(0.5f, 0.5f);
            emptyRt.pivot = new Vector2(0.5f, 0.5f);
            emptyRt.sizeDelta = new Vector2(750f, 220f);
            emptyRt.anchoredPosition = new Vector2(0f, 0f);
            var emptyTmp = emptyGo.AddComponent<TextMeshProUGUI>();
            emptyTmp.font = nunitoFont;
            emptyTmp.fontSize = 26;
            emptyTmp.alignment = TextAlignmentOptions.Center;
            emptyTmp.color = new Color(0.45f, 0.4f, 0.5f, 0.9f);
            emptyTmp.textWrappingMode = TextWrappingModes.Normal;
            emptyTmp.text = "No photos captured yet.\nExplore the town and snap photos of objects to fill your dictionary!";
            emptyGo.SetActive(false);

            // 6.4.4 PlaceholderRoot (Quiz, Hiragana, Module)
            GameObject placeGo = CreateUIObject("PlaceholderRoot", spreadGo.transform);
            RectTransform placeRt = placeGo.GetComponent<RectTransform>();
            placeRt.anchorMin = new Vector2(0.5f, 0.5f);
            placeRt.anchorMax = new Vector2(0.5f, 0.5f);
            placeRt.pivot = new Vector2(0.5f, 0.5f);
            placeRt.sizeDelta = new Vector2(900f, 400f);
            placeRt.anchoredPosition = new Vector2(0f, 0f);

            GameObject placeTitleGo = CreateUIObject("PlaceholderTitle", placeGo.transform);
            RectTransform placeTitleRt = placeTitleGo.GetComponent<RectTransform>();
            placeTitleRt.anchorMin = new Vector2(0.5f, 1f);
            placeTitleRt.anchorMax = new Vector2(0.5f, 1f);
            placeTitleRt.pivot = new Vector2(0.5f, 1f);
            placeTitleRt.sizeDelta = new Vector2(800f, 60f);
            placeTitleRt.anchoredPosition = new Vector2(0f, -40f);
            var placeTitleTmp = placeTitleGo.AddComponent<TextMeshProUGUI>();
            placeTitleTmp.font = zenFont;
            placeTitleTmp.fontSize = 38;
            placeTitleTmp.fontStyle = FontStyles.Bold;
            placeTitleTmp.alignment = TextAlignmentOptions.Center;
            placeTitleTmp.color = new Color(0.24f, 0.2f, 0.28f);
            placeTitleTmp.text = "QUIZ CHALLENGE";

            GameObject placeDescGo = CreateUIObject("PlaceholderDesc", placeGo.transform);
            RectTransform placeDescRt = placeDescGo.GetComponent<RectTransform>();
            placeDescRt.anchorMin = new Vector2(0.5f, 1f);
            placeDescRt.anchorMax = new Vector2(0.5f, 1f);
            placeDescRt.pivot = new Vector2(0.5f, 1f);
            placeDescRt.sizeDelta = new Vector2(750f, 120f);
            placeDescRt.anchoredPosition = new Vector2(0f, -120f);
            var placeDescTmp = placeDescGo.AddComponent<TextMeshProUGUI>();
            placeDescTmp.font = nunitoFont;
            placeDescTmp.fontSize = 26;
            placeDescTmp.alignment = TextAlignmentOptions.Center;
            placeDescTmp.color = new Color(0.4f, 0.35f, 0.45f);
            placeDescTmp.textWrappingMode = TextWrappingModes.Normal;
            placeDescTmp.text = "Feature under construction.\nComing soon in next update!";

            placeGo.SetActive(false);

            // 6.5 Bottom Spine Discovery Counter Clasp Badge (X / 72)
            GameObject progressBadgeGo = CreateUIObject("DiscoveryProgressBadge", bookGo.transform);
            RectTransform progressBadgeRt = progressBadgeGo.GetComponent<RectTransform>();
            progressBadgeRt.anchorMin = new Vector2(0.5f, 0f);
            progressBadgeRt.anchorMax = new Vector2(0.5f, 0f);
            progressBadgeRt.pivot = new Vector2(0.5f, 0.5f);
            progressBadgeRt.sizeDelta = new Vector2(224f, 62f);
            progressBadgeRt.anchoredPosition = new Vector2(-17f, 64f);
            var progressBadgeImg = progressBadgeGo.AddComponent<Image>();
            progressBadgeImg.sprite = coverCounterPillSprite;
            progressBadgeImg.type = Image.Type.Sliced;
            progressBadgeImg.color = Color.white;
            progressBadgeImg.raycastTarget = false;

            GameObject progressHdrGo = CreateUIObject("ProgressHeaderText", progressBadgeGo.transform);
            RectTransform progressHdrRt = progressHdrGo.GetComponent<RectTransform>();
            progressHdrRt.anchorMin = new Vector2(0f, 0.5f);
            progressHdrRt.anchorMax = new Vector2(1f, 1f);
            progressHdrRt.offsetMin = new Vector2(16f, -3f);
            progressHdrRt.offsetMax = new Vector2(-16f, -7f);
            var progressHdrTmp = progressHdrGo.AddComponent<TextMeshProUGUI>();
            progressHdrTmp.font = nunitoFont;
            progressHdrTmp.fontStyle = FontStyles.Bold;
            progressHdrTmp.fontSize = 10.5f;
            progressHdrTmp.characterSpacing = 14f;
            progressHdrTmp.alignment = TextAlignmentOptions.Center;
            progressHdrTmp.color = new Color(0.92f, 0.83f, 0.75f, 0.92f);
            progressHdrTmp.text = "DISCOVERED";
            progressHdrTmp.raycastTarget = false;

            GameObject progressCntGo = CreateUIObject("ProgressCountText", progressBadgeGo.transform);
            RectTransform progressCntRt = progressCntGo.GetComponent<RectTransform>();
            progressCntRt.anchorMin = new Vector2(0f, 0f);
            progressCntRt.anchorMax = new Vector2(1f, 0.65f);
            progressCntRt.offsetMin = new Vector2(16f, 6f);
            progressCntRt.offsetMax = new Vector2(-16f, -2f);
            var progressCntTmp = progressCntGo.AddComponent<TextMeshProUGUI>();
            progressCntTmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Baloo_2/static/Baloo2-ExtraBold SDF.asset") ?? nunitoFont;
            progressCntTmp.fontStyle = FontStyles.Bold;
            progressCntTmp.fontSize = 24f;
            progressCntTmp.enableAutoSizing = true;
            progressCntTmp.fontSizeMin = 16f;
            progressCntTmp.fontSizeMax = 24f;
            progressCntTmp.textWrappingMode = TextWrappingModes.NoWrap;
            progressCntTmp.alignment = TextAlignmentOptions.Center;
            progressCntTmp.color = Color.white;
            progressCntTmp.text = "<color=#FFF8EE><b>0</b></color> <color=#DFC6AE><size=80%>/ 72</size></color>";
            progressCntTmp.raycastTarget = false;

            // Wire DictionaryBookSpreadUI fields via SerializedObject
            var spreadSo = new SerializedObject(bookSpread);
            var slotsProp = spreadSo.FindProperty("polaroidSlots");
            slotsProp.arraySize = 8;
            for (int i = 0; i < 8; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = polaroidSlots[i];
            }
            spreadSo.FindProperty("leftPageClickArea").objectReferenceValue = leftClickBtn;
            spreadSo.FindProperty("rightPageClickArea").objectReferenceValue = rightClickBtn;
            spreadSo.FindProperty("leftPageNumberText").objectReferenceValue = leftNumTmp;
            spreadSo.FindProperty("rightPageNumberText").objectReferenceValue = rightNumTmp;
            spreadSo.FindProperty("discoveryProgressText").objectReferenceValue = progressCntTmp;
            spreadSo.FindProperty("photoSpreadRoot").objectReferenceValue = photoSpreadRoot;
            spreadSo.FindProperty("placeholderRoot").objectReferenceValue = placeGo;
            spreadSo.FindProperty("placeholderTitleText").objectReferenceValue = placeTitleTmp;
            spreadSo.FindProperty("placeholderDescText").objectReferenceValue = placeDescTmp;
            spreadSo.FindProperty("emptyStateNotice").objectReferenceValue = emptyGo;
            spreadSo.FindProperty("emptyStateText").objectReferenceValue = emptyTmp;
            spreadSo.FindProperty("dictionaryManager").objectReferenceValue = Object.FindFirstObjectByType<DictionaryManager>();
            spreadSo.FindProperty("dominoStaggerDelay").floatValue = 0.045f;
            spreadSo.FindProperty("dominoFadeOutDuration").floatValue = 0.10f;
            spreadSo.FindProperty("dominoPopInDuration").floatValue = 0.22f;
            spreadSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire DictionaryUIController fields via SerializedObject
            var ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("viewRoot").objectReferenceValue = viewGo;
            ctrlSo.FindProperty("viewCanvasGroup").objectReferenceValue = viewCg;
            ctrlSo.FindProperty("bookContainerRt").objectReferenceValue = bookRt;
            ctrlSo.FindProperty("tabAlbumButton").objectReferenceValue = tabAlbum;
            ctrlSo.FindProperty("tabLikeButton").objectReferenceValue = tabLike;
            ctrlSo.FindProperty("tabFavoriteButton").objectReferenceValue = tabFav;
            ctrlSo.FindProperty("tabQuizButton").objectReferenceValue = tabQuiz;
            ctrlSo.FindProperty("tabHiraganaButton").objectReferenceValue = tabHiragana;
            ctrlSo.FindProperty("tabModuleButton").objectReferenceValue = tabModule;
            ctrlSo.FindProperty("tabExitButton").objectReferenceValue = tabExit;
            ctrlSo.FindProperty("bookSpread").objectReferenceValue = bookSpread;
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();

            // Hubungkan juga Btn_DictionaryHUD (di Canvas/Camera Mechanic) jika ada
            var hudBtnGo = GameObject.Find("Canvas/Camera Mechanic/Btn_DictionaryHUD");
            if (hudBtnGo != null)
            {
                var hudBtn = hudBtnGo.GetComponent<Button>();
                if (hudBtn != null)
                {
                    // Bersihkan listener lama dan sambungkan ke controller.Toggle
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(hudBtn.onClick, 0);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(hudBtn.onClick, controller.Toggle);
                }
            }

            // Bangun juga Tab Hiragana dan Tab Module secara otomatis
            DictionaryLearnUIBuilder.BuildLearnUI();

            // Bangun Tab Quiz Challenge secara otomatis
            DictionaryQuizUIBuilder.BuildQuizUI();

            // Bangun Tab Theme & sambungkan DictionaryThemeApplier
            DictionaryThemeUIBuilder.BuildThemeUI(saveScene: false);

            // Awalnya tutup viewRoot
            viewGo.SetActive(false);

            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(dictGo);
                EditorUtility.SetDirty(canvas.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            }

            Debug.Log("[DictionaryUIBuilder] Berhasil membangun UI Dictionary Rework di Canvas/Dictionary!");
        }

        [MenuItem("Tools/Utsuru/Update Dictionary Page Buttons")]
        public static void UpdateDictionaryPageButtons()
        {
            DictionaryAssetImporter.ConfigureSprites();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            Transform photoRoot = canvas.transform.Find("Dictionary/DictionaryView/BookContainer/BookSpread/PhotoSpreadRoot");
            if (photoRoot == null)
            {
                Debug.LogWarning("[DictionaryUIBuilder] PhotoSpreadRoot tidak ditemukan di scene aktif.");
                return;
            }

            var btnPrevSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_previous.png");
            var btnNextSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/btn_next.png");

            Undo.RegisterCompleteObjectUndo(photoRoot.gameObject, "Update Dictionary Page Buttons");

            Transform leftBtnTr = photoRoot.Find("LeftPage/LeftPageClickArea") ?? photoRoot.Find("LeftPage/Btn_PreviousPage");
            if (leftBtnTr != null)
            {
                leftBtnTr.name = "Btn_PreviousPage";
                Transform oldArrow = leftBtnTr.Find("ArrowIcon");
                if (oldArrow != null) Object.DestroyImmediate(oldArrow.gameObject);
                if (leftBtnTr.TryGetComponent<Image>(out var img) && btnPrevSprite != null)
                {
                    img.sprite = btnPrevSprite;
                    img.preserveAspect = true;
                    img.color = Color.white;
                }
                if (leftBtnTr.TryGetComponent<Button>(out var btn))
                {
                    btn.transition = Selectable.Transition.ColorTint;
                    btn.targetGraphic = leftBtnTr.GetComponent<Image>();
                }
            }

            Transform rightBtnTr = photoRoot.Find("RightPage/RightPageClickArea") ?? photoRoot.Find("RightPage/Btn_NextPage");
            if (rightBtnTr != null)
            {
                rightBtnTr.name = "Btn_NextPage";
                Transform oldArrow = rightBtnTr.Find("ArrowIcon");
                if (oldArrow != null) Object.DestroyImmediate(oldArrow.gameObject);
                if (rightBtnTr.TryGetComponent<Image>(out var img) && btnNextSprite != null)
                {
                    img.sprite = btnNextSprite;
                    img.preserveAspect = true;
                    img.color = Color.white;
                }
                if (rightBtnTr.TryGetComponent<Button>(out var btn))
                {
                    btn.transition = Selectable.Transition.ColorTint;
                    btn.targetGraphic = rightBtnTr.GetComponent<Image>();
                }
            }

            EditorUtility.SetDirty(photoRoot.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(photoRoot.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(photoRoot.gameObject.scene);
            Debug.Log("[DictionaryUIBuilder] Btn_PreviousPage & Btn_NextPage berhasil diperbarui dan disimpan.");
        }

        [MenuItem("Tools/Utsuru/Update PhotoSpread Card Layout")]
        public static void UpdatePhotoSpreadCardLayout()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            Transform photoRoot = canvas.transform.Find("Dictionary/DictionaryView/BookContainer/BookSpread/PhotoSpreadRoot");
            if (photoRoot == null)
            {
                Debug.LogWarning("[DictionaryUIBuilder] PhotoSpreadRoot tidak ditemukan di scene aktif.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(photoRoot.gameObject, "Update PhotoSpread Card Layout");
            int updated = 0;

            foreach (var card in photoRoot.GetComponentsInChildren<DictionaryPolaroidCardUI>(true))
            {
                Transform container = card.transform.Find("CardContainer") ?? card.transform.Find("PolaroidCard");
                if (container == null) continue;
                Transform front = container.Find("CardFace_Front");
                if (front == null) continue;

                Transform kanji = front.Find("KanjiLabel");
                if (kanji != null)
                {
                    if (kanji.TryGetComponent<RectTransform>(out var kRt))
                        kRt.anchoredPosition = new Vector2(-111f, -194f);
                    if (kanji.TryGetComponent<TextMeshProUGUI>(out var kTmp))
                    {
                        kTmp.enableAutoSizing = true;
                        kTmp.fontSizeMin = 12f;
                        kTmp.fontSizeMax = 20f;
                        kTmp.fontSize = 20f;
                        EditorUtility.SetDirty(kTmp);
                    }
                }

                Transform mean = front.Find("MeaningLabel");
                if (mean != null)
                {
                    if (mean.TryGetComponent<RectTransform>(out var mRt))
                        mRt.anchoredPosition = new Vector2(-111f, -248f);
                    if (mean.TryGetComponent<TextMeshProUGUI>(out var mTmp))
                    {
                        mTmp.enableAutoSizing = true;
                        mTmp.fontSizeMin = 10.5f;
                        mTmp.fontSizeMax = 16f;
                        mTmp.fontSize = 16f;
                        EditorUtility.SetDirty(mTmp);
                    }
                }

                Transform like = front.Find("Btn_Like");
                if (like != null && like.TryGetComponent<RectTransform>(out var lRt))
                    lRt.anchoredPosition = new Vector2(-77f, 56.6f);

                Transform fav = front.Find("Btn_Favorite");
                if (fav != null && fav.TryGetComponent<RectTransform>(out var fRt))
                    fRt.anchoredPosition = new Vector2(-45.45f, 57.7f);

                updated++;
            }

            EditorUtility.SetDirty(photoRoot.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(photoRoot.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(photoRoot.gameObject.scene);
            Debug.Log($"[DictionaryUIBuilder] Berhasil memperbarui layout {updated} kartu di PhotoSpreadRoot.");
        }

        private static Button CreateTabButton(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, float scale = 0.9f)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            rt.localScale = new Vector3(scale, scale, 1f);

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            return btn;
        }

        private static DictionaryPolaroidCardUI CreatePolaroidSlot(
            string name, 
            Transform parent, 
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos, 
            int slotIdx,
            Sprite bgSprite, 
            Sprite tapeSprite,
            Sprite likeOff, 
            Sprite likeOn, 
            Sprite favOff, 
            Sprite favOn,
            TMP_FontAsset zenFont, 
            TMP_FontAsset nunitoFont)
        {
            GameObject slotGo = CreateUIObject(name, parent);
            RectTransform slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.anchorMin = anchorMin;
            slotRt.anchorMax = anchorMax;
            slotRt.pivot = new Vector2(0.5f, 0.5f);
            slotRt.sizeDelta = new Vector2(300f, 365f);
            slotRt.anchoredPosition = anchoredPos;

            var cardUI = slotGo.AddComponent<DictionaryPolaroidCardUI>();

            // CardContainer (yang berputar saat flip)
            GameObject containerGo = CreateUIObject("CardContainer", slotGo.transform);
            RectTransform containerRt = containerGo.GetComponent<RectTransform>();
            SetStretch(containerRt);
            var containerCg = containerGo.AddComponent<CanvasGroup>();

            // Washi Tape (Pita perekat atas)
            GameObject tapeGo = CreateUIObject("TapeGraphic", containerGo.transform);
            RectTransform tapeRt = tapeGo.GetComponent<RectTransform>();
            tapeRt.anchorMin = new Vector2(0.5f, 1f);
            tapeRt.anchorMax = new Vector2(0.5f, 1f);
            tapeRt.pivot = new Vector2(0.5f, 0.5f);
            tapeRt.sizeDelta = new Vector2(85f, 22f);
            tapeRt.anchoredPosition = new Vector2(0f, -4f);
            var tapeImg = tapeGo.AddComponent<Image>();
            tapeImg.sprite = tapeSprite;
            tapeImg.raycastTarget = false;

            // ================= FRONT FACE =================
            GameObject frontGo = CreateUIObject("CardFace_Front", containerGo.transform);
            RectTransform frontRt = frontGo.GetComponent<RectTransform>();
            SetStretch(frontRt);

            // Background Kartu Polaroid dengan bayangan halus
            var frontBg = frontGo.AddComponent<Image>();
            frontBg.sprite = bgSprite;
            frontBg.raycastTarget = false; // Biarkan CardClickTrigger menangani klik kartu penuh

            // Tombol Klik Badan Kartu (untuk trigger flip) - mencakup 100% kartu penuh
            GameObject clickGo = CreateUIObject("CardClickTrigger", frontGo.transform);
            RectTransform clickRt = clickGo.GetComponent<RectTransform>();
            SetStretch(clickRt);
            var clickImg = clickGo.AddComponent<Image>();
            clickImg.color = new Color(0, 0, 0, 0.001f);
            clickImg.raycastTarget = true;
            var cardClickBtn = clickGo.AddComponent<Button>();
            cardClickBtn.transition = Selectable.Transition.None;

            // PhotoFrame (Rasio 4:3, padding rapi di kertas polaroid)
            GameObject photoFrameGo = CreateUIObject("PhotoFrame", frontGo.transform);
            RectTransform photoFrameRt = photoFrameGo.GetComponent<RectTransform>();
            photoFrameRt.anchorMin = new Vector2(0.5f, 1f);
            photoFrameRt.anchorMax = new Vector2(0.5f, 1f);
            photoFrameRt.pivot = new Vector2(0.5f, 1f);
            photoFrameRt.sizeDelta = new Vector2(244f, 166f);
            photoFrameRt.anchoredPosition = new Vector2(0f, -20f);

            // RawImage untuk foto snapshot (raycastTarget = false agar klik tembus ke CardClickTrigger)
            GameObject photoGo = CreateUIObject("PhotoImage", photoFrameGo.transform);
            RectTransform photoRt = photoGo.GetComponent<RectTransform>();
            SetStretch(photoRt);
            var photoRaw = photoGo.AddComponent<RawImage>();
            photoRaw.color = Color.white;
            photoRaw.raycastTarget = false;

            // Kanji & Furigana Label (NoWrap + auto-fit agar sejajar persis dengan foto & tampak rapi 1 baris)
            GameObject kanjiGo = CreateUIObject("KanjiLabel", frontGo.transform);
            RectTransform kanjiRt = kanjiGo.GetComponent<RectTransform>();
            kanjiRt.anchorMin = new Vector2(0.5f, 1f);
            kanjiRt.anchorMax = new Vector2(0.5f, 1f);
            kanjiRt.pivot = new Vector2(0f, 1f);
            kanjiRt.sizeDelta = new Vector2(244f, 38f);
            kanjiRt.anchoredPosition = new Vector2(-122f, -195f);
            var kanjiTmp = kanjiGo.AddComponent<TextMeshProUGUI>();
            kanjiTmp.font = zenFont;
            kanjiTmp.fontSize = 24f;
            kanjiTmp.enableAutoSizing = true;
            kanjiTmp.fontSizeMin = 10.5f;
            kanjiTmp.fontSizeMax = 24f;
            kanjiTmp.lineSpacing = 0f;
            kanjiTmp.textWrappingMode = TextWrappingModes.NoWrap;
            kanjiTmp.overflowMode = TextOverflowModes.Ellipsis;
            kanjiTmp.alignment = TextAlignmentOptions.MidlineLeft;
            kanjiTmp.fontStyle = FontStyles.Normal;
            kanjiTmp.color = new Color(0.12f, 0.10f, 0.10f, 1f);
            kanjiTmp.text = "<b>桜</b> <size=64%><color=#5B5555>(さくら)</color></size>";
            kanjiTmp.raycastTarget = false;

            // Meaning & Romaji Label (Biru tegas rata kiri di bawah Kanji)
            GameObject meanGo = CreateUIObject("MeaningLabel", frontGo.transform);
            RectTransform meanRt = meanGo.GetComponent<RectTransform>();
            meanRt.anchorMin = new Vector2(0.5f, 1f);
            meanRt.anchorMax = new Vector2(0.5f, 1f);
            meanRt.pivot = new Vector2(0f, 1f);
            meanRt.sizeDelta = new Vector2(244f, 44f);
            meanRt.anchoredPosition = new Vector2(-122f, -234f);
            var meanTmp = meanGo.AddComponent<TextMeshProUGUI>();
            meanTmp.font = nunitoFont;
            meanTmp.fontSize = 14.5f;
            meanTmp.enableAutoSizing = true;
            meanTmp.fontSizeMin = 9.5f;
            meanTmp.fontSizeMax = 14.5f;
            meanTmp.lineSpacing = 2f;
            meanTmp.overflowMode = TextOverflowModes.Ellipsis;
            meanTmp.alignment = TextAlignmentOptions.TopLeft;
            meanTmp.fontStyle = FontStyles.Bold;
            meanTmp.color = new Color(0.118f, 0.388f, 0.914f, 1f);
            meanTmp.textWrappingMode = TextWrappingModes.Normal;
            meanTmp.text = "sakura • Cherry Blossom";
            meanTmp.raycastTarget = false;

            // Tombol Like (Hati) di pojok kanan bawah (aman di atas border bawah kertas Polaroid)
            GameObject likeGo = CreateUIObject("Btn_Like", frontGo.transform);
            RectTransform likeRt = likeGo.GetComponent<RectTransform>();
            likeRt.anchorMin = new Vector2(1f, 0f);
            likeRt.anchorMax = new Vector2(1f, 0f);
            likeRt.pivot = new Vector2(0.5f, 0.5f);
            likeRt.sizeDelta = new Vector2(26f, 26f);
            likeRt.anchoredPosition = new Vector2(-77f, 56.6f);
            var likeBtn = likeGo.AddComponent<Button>();
            var likeImg = likeGo.AddComponent<Image>();
            likeImg.sprite = likeOff;
            likeImg.preserveAspect = true;
            likeImg.raycastTarget = true;
            likeBtn.targetGraphic = likeImg;

            // Tombol Favorite (Bintang) di pojok kanan bawah
            GameObject favGo = CreateUIObject("Btn_Favorite", frontGo.transform);
            RectTransform favRt = favGo.GetComponent<RectTransform>();
            favRt.anchorMin = new Vector2(1f, 0f);
            favRt.anchorMax = new Vector2(1f, 0f);
            favRt.pivot = new Vector2(0.5f, 0.5f);
            favRt.sizeDelta = new Vector2(28f, 28f);
            favRt.anchoredPosition = new Vector2(-45.45f, 57.7f);
            var favBtn = favGo.AddComponent<Button>();
            var favImg = favGo.AddComponent<Image>();
            favImg.sprite = favOff;
            favImg.preserveAspect = true;
            favImg.raycastTarget = true;
            favBtn.targetGraphic = favImg;

            // ================= BACK FACE (Flip Side) =================
            GameObject backGo = CreateUIObject("CardFace_Back", containerGo.transform);
            RectTransform backRt = backGo.GetComponent<RectTransform>();
            SetStretch(backRt);
            var backBg = backGo.AddComponent<Image>();
            backBg.sprite = bgSprite;
            backBg.preserveAspect = true;
            backBg.raycastTarget = false;

            // Tombol Klik Badan Belakang (Trigger flip kembali ke depan)
            GameObject backClickGo = CreateUIObject("BackClickTrigger", backGo.transform);
            RectTransform backClickRt = backClickGo.GetComponent<RectTransform>();
            SetStretch(backClickRt);
            var backClickImg = backClickGo.AddComponent<Image>();
            backClickImg.color = new Color(0, 0, 0, 0.001f);
            backClickImg.raycastTarget = true;
            var backClickBtn = backClickGo.AddComponent<Button>();
            backClickBtn.transition = Selectable.Transition.None;

            // Judul Kata Belakang (disembunyikan agar tampilan belakang persis kartu catatan kalimat bersih)
            GameObject backTitleGo = CreateUIObject("BackKanjiLabel", backGo.transform);
            RectTransform backTitleRt = backTitleGo.GetComponent<RectTransform>();
            backTitleRt.anchorMin = new Vector2(0.5f, 1f);
            backTitleRt.anchorMax = new Vector2(0.5f, 1f);
            backTitleRt.pivot = new Vector2(0.5f, 1f);
            backTitleRt.sizeDelta = new Vector2(244f, 42f);
            backTitleRt.anchoredPosition = new Vector2(0f, -22f);
            var backTitleTmp = backTitleGo.AddComponent<TextMeshProUGUI>();
            backTitleTmp.font = zenFont;
            backTitleTmp.fontSize = 23f;
            backTitleTmp.enableAutoSizing = true;
            backTitleTmp.fontSizeMin = 15f;
            backTitleTmp.fontSizeMax = 23f;
            backTitleTmp.textWrappingMode = TextWrappingModes.NoWrap;
            backTitleTmp.overflowMode = TextOverflowModes.Ellipsis;
            backTitleTmp.fontStyle = FontStyles.Bold;
            backTitleTmp.alignment = TextAlignmentOptions.Center;
            backTitleTmp.color = new Color(0.2f, 0.16f, 0.18f);
            backTitleTmp.text = "道";
            backTitleTmp.raycastTarget = false;
            backTitleGo.SetActive(false);

            // Kotak Contoh Kalimat (Rata kiri atas agar persis seperti kartu catatan polaroid)
            GameObject sentBoxGo = CreateUIObject("SentenceContainer", backGo.transform);
            RectTransform sentBoxRt = sentBoxGo.GetComponent<RectTransform>();
            sentBoxRt.anchorMin = new Vector2(0.5f, 1f);
            sentBoxRt.anchorMax = new Vector2(0.5f, 1f);
            sentBoxRt.pivot = new Vector2(0.5f, 1f);
            sentBoxRt.sizeDelta = new Vector2(236f, 265f);
            sentBoxRt.anchoredPosition = new Vector2(0f, -34f);

            var sentVlg = sentBoxGo.AddComponent<VerticalLayoutGroup>();
            sentVlg.padding = new RectOffset(6, 6, 4, 4);
            sentVlg.spacing = 12f;
            sentVlg.childAlignment = TextAnchor.UpperLeft;
            sentVlg.childControlWidth = true;
            sentVlg.childControlHeight = true;
            sentVlg.childForceExpandWidth = true;
            sentVlg.childForceExpandHeight = false;

            GameObject sentJpGo = CreateUIObject("SentenceJapanese", sentBoxGo.transform);
            var sentJpLe = sentJpGo.AddComponent<LayoutElement>();
            sentJpLe.flexibleHeight = 0f;
            var sentJpTmp = sentJpGo.AddComponent<TextMeshProUGUI>();
            sentJpTmp.font = zenFont;
            sentJpTmp.fontStyle = FontStyles.Normal;
            sentJpTmp.fontSize = 18.5f;
            sentJpTmp.enableAutoSizing = true;
            sentJpTmp.fontSizeMin = 12f;
            sentJpTmp.fontSizeMax = 18.5f;
            sentJpTmp.lineSpacing = 6f;
            sentJpTmp.overflowMode = TextOverflowModes.Ellipsis;
            sentJpTmp.alignment = TextAlignmentOptions.TopLeft;
            sentJpTmp.color = new Color(0.14f, 0.12f, 0.12f, 1f);
            sentJpTmp.textWrappingMode = TextWrappingModes.Normal;
            sentJpTmp.text = "「この道をまっすぐ\n　行きます。」";
            sentJpTmp.raycastTarget = false;

            GameObject sentEngGo = CreateUIObject("SentenceEnglish", sentBoxGo.transform);
            var sentEngLe = sentEngGo.AddComponent<LayoutElement>();
            sentEngLe.flexibleHeight = 0f;
            var sentEngTmp = sentEngGo.AddComponent<TextMeshProUGUI>();
            sentEngTmp.font = nunitoFont;
            sentEngTmp.fontStyle = FontStyles.Italic;
            sentEngTmp.fontSize = 14.5f;
            sentEngTmp.enableAutoSizing = true;
            sentEngTmp.fontSizeMin = 10f;
            sentEngTmp.fontSizeMax = 14.5f;
            sentEngTmp.lineSpacing = 4f;
            sentEngTmp.overflowMode = TextOverflowModes.Ellipsis;
            sentEngTmp.alignment = TextAlignmentOptions.TopLeft;
            sentEngTmp.color = new Color(0.36f, 0.34f, 0.33f, 1f);
            sentEngTmp.textWrappingMode = TextWrappingModes.Normal;
            sentEngTmp.text = "(Kono michi o massugu ikimasu) — Go straight along this street.";
            sentEngTmp.raycastTarget = false;

            // Hint balik disembunyikan agar sisi belakang tetap bersih
            GameObject hintGo = CreateUIObject("BackFlipHint", backGo.transform);
            RectTransform hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.sizeDelta = new Vector2(244f, 22f);
            hintRt.anchoredPosition = new Vector2(0f, 30f);
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.font = nunitoFont;
            hintTmp.fontSize = 12f;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(0.45f, 0.50f, 0.58f);
            hintTmp.text = "tap to flip card";
            hintTmp.raycastTarget = false;
            hintGo.SetActive(false);

            backGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            backGo.SetActive(false); // Sisi belakang awalnya tersembunyi

            // Wire serialized fields on DictionaryPolaroidCardUI
            var cardSo = new SerializedObject(cardUI);
            cardSo.FindProperty("cardContainer").objectReferenceValue = containerRt;
            cardSo.FindProperty("cardCanvasGroup").objectReferenceValue = containerCg;
            cardSo.FindProperty("frontFace").objectReferenceValue = frontGo;
            cardSo.FindProperty("backFace").objectReferenceValue = backGo;
            cardSo.FindProperty("frontClickButton").objectReferenceValue = cardClickBtn;
            cardSo.FindProperty("backClickButton").objectReferenceValue = backClickBtn;
            cardSo.FindProperty("cardClickButton").objectReferenceValue = cardClickBtn;
            cardSo.FindProperty("tapeImage").objectReferenceValue = tapeImg;
            cardSo.FindProperty("photoImage").objectReferenceValue = photoRaw;
            cardSo.FindProperty("kanjiLabel").objectReferenceValue = kanjiTmp;
            cardSo.FindProperty("meaningLabel").objectReferenceValue = meanTmp;
            cardSo.FindProperty("likeButton").objectReferenceValue = likeBtn;
            cardSo.FindProperty("likeIcon").objectReferenceValue = likeImg;
            cardSo.FindProperty("favoriteButton").objectReferenceValue = favBtn;
            cardSo.FindProperty("favoriteIcon").objectReferenceValue = favImg;
            cardSo.FindProperty("likeOffSprite").objectReferenceValue = likeOff;
            cardSo.FindProperty("likeOnSprite").objectReferenceValue = likeOn;
            cardSo.FindProperty("favoriteOffSprite").objectReferenceValue = favOff;
            cardSo.FindProperty("favoriteOnSprite").objectReferenceValue = favOn;
            cardSo.FindProperty("backKanjiLabel").objectReferenceValue = backTitleTmp;
            cardSo.FindProperty("backSentenceJapanese").objectReferenceValue = sentJpTmp;
            cardSo.FindProperty("backSentenceEnglish").objectReferenceValue = sentEngTmp;
            cardSo.ApplyModifiedPropertiesWithoutUndo();

            // Sembunyikan slot saat belum ada foto
            slotGo.SetActive(false);

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
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
