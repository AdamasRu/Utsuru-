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
            var bookPurpleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Book/Book_Purple.png");

            var bmAlbumSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/BM_Album.png");
            var bmLikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/BM_Like.png");
            var bmFavoriteSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/BM_Favorite.png");
            var bmQuizSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/BM_Quiz.png");
            var bmHiraganaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/BM_Hiragana.png");
            var bmModuleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/BM_Module.png");
            var bmExitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Button/BM_Exit.png");

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
            bookImg.sprite = bookPurpleSprite;
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

            // Tab Kanan: Top-Right (Scale 0.9)
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

            // Tombol Klik Halaman Kiri (Previous Spread) - hanya pada strip tepi luar buku (di luar polaroid)
            GameObject leftClickGo = CreateUIObject("LeftPageClickArea", leftPageGo.transform);
            RectTransform leftClickRt = leftClickGo.GetComponent<RectTransform>();
            leftClickRt.anchorMin = new Vector2(0f, 0f);
            leftClickRt.anchorMax = new Vector2(0f, 1f);
            leftClickRt.pivot = new Vector2(0f, 0.5f);
            leftClickRt.sizeDelta = new Vector2(75f, 0f);
            leftClickRt.anchoredPosition = Vector2.zero;
            var leftClickImg = leftClickGo.AddComponent<Image>();
            leftClickImg.color = new Color(0f, 0f, 0f, 0.001f); // Invisible raycast target
            var leftClickBtn = leftClickGo.AddComponent<Button>();
            leftClickBtn.transition = Selectable.Transition.None;

            // Indikator visual panah halaman sebelumnya (<) di area tepi buku
            GameObject leftArrowGo = CreateUIObject("ArrowIcon", leftClickGo.transform);
            RectTransform leftArrowRt = leftArrowGo.GetComponent<RectTransform>();
            leftArrowRt.anchorMin = new Vector2(0.5f, 0.5f);
            leftArrowRt.anchorMax = new Vector2(0.5f, 0.5f);
            leftArrowRt.pivot = new Vector2(0.5f, 0.5f);
            leftArrowRt.sizeDelta = new Vector2(40f, 60f);
            leftArrowRt.anchoredPosition = Vector2.zero;
            var leftArrowTmp = leftArrowGo.AddComponent<TextMeshProUGUI>();
            leftArrowTmp.font = TMP_Settings.defaultFontAsset;
            leftArrowTmp.fontSize = 32;
            leftArrowTmp.fontStyle = FontStyles.Bold;
            leftArrowTmp.alignment = TextAlignmentOptions.Center;
            leftArrowTmp.color = new Color(0.48f, 0.42f, 0.55f, 0.65f);
            leftArrowTmp.text = "<";
            leftArrowTmp.raycastTarget = false;

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

            // Tombol Klik Halaman Kanan (Next Spread) - stretch left dengan posisi x = 25
            GameObject rightClickGo = CreateUIObject("RightPageClickArea", rightPageGo.transform);
            RectTransform rightClickRt = rightClickGo.GetComponent<RectTransform>();
            rightClickRt.anchorMin = new Vector2(0f, 0f);
            rightClickRt.anchorMax = new Vector2(0f, 1f);
            rightClickRt.pivot = new Vector2(0f, 0.5f);
            rightClickRt.sizeDelta = new Vector2(75f, 0f);
            rightClickRt.anchoredPosition = new Vector2(25f, 0f);
            var rightClickImg = rightClickGo.AddComponent<Image>();
            rightClickImg.color = new Color(0f, 0f, 0f, 0.001f); // Invisible raycast target
            var rightClickBtn = rightClickGo.AddComponent<Button>();
            rightClickBtn.transition = Selectable.Transition.None;

            // Indikator visual panah halaman selanjutnya (>) di area tepi buku
            GameObject rightArrowGo = CreateUIObject("ArrowIcon", rightClickGo.transform);
            RectTransform rightArrowRt = rightArrowGo.GetComponent<RectTransform>();
            rightArrowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rightArrowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rightArrowRt.pivot = new Vector2(0.5f, 0.5f);
            rightArrowRt.sizeDelta = new Vector2(40f, 60f);
            rightArrowRt.anchoredPosition = Vector2.zero;
            var rightArrowTmp = rightArrowGo.AddComponent<TextMeshProUGUI>();
            rightArrowTmp.font = TMP_Settings.defaultFontAsset;
            rightArrowTmp.fontSize = 32;
            rightArrowTmp.fontStyle = FontStyles.Bold;
            rightArrowTmp.alignment = TextAlignmentOptions.Center;
            rightArrowTmp.color = new Color(0.48f, 0.42f, 0.55f, 0.65f);
            rightArrowTmp.text = ">";
            rightArrowTmp.raycastTarget = false;

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
            leftNumRt.sizeDelta = new Vector2(80f, 30f);
            leftNumRt.anchoredPosition = new Vector2(30f, 15f);
            var leftNumTmp = leftNumGo.AddComponent<TextMeshProUGUI>();
            leftNumTmp.font = nunitoFont;
            leftNumTmp.fontSize = 18;
            leftNumTmp.fontStyle = FontStyles.Bold;
            leftNumTmp.color = new Color(0.48f, 0.42f, 0.52f, 0.8f);
            leftNumTmp.text = "1";
            leftNumTmp.raycastTarget = false;

            GameObject rightNumGo = CreateUIObject("RightPageNum", rightPageGo.transform);
            RectTransform rightNumRt = rightNumGo.GetComponent<RectTransform>();
            rightNumRt.anchorMin = new Vector2(1f, 0f);
            rightNumRt.anchorMax = new Vector2(1f, 0f);
            rightNumRt.pivot = new Vector2(1f, 0f);
            rightNumRt.sizeDelta = new Vector2(80f, 30f);
            rightNumRt.anchoredPosition = new Vector2(-30f, 15f);
            var rightNumTmp = rightNumGo.AddComponent<TextMeshProUGUI>();
            rightNumTmp.font = nunitoFont;
            rightNumTmp.fontSize = 18;
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
            emptyRt.sizeDelta = new Vector2(700f, 200f);
            emptyRt.anchoredPosition = new Vector2(0f, 0f);
            var emptyTmp = emptyGo.AddComponent<TextMeshProUGUI>();
            emptyTmp.font = nunitoFont;
            emptyTmp.fontSize = 20;
            emptyTmp.alignment = TextAlignmentOptions.Center;
            emptyTmp.color = new Color(0.45f, 0.4f, 0.5f, 0.9f);
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
            placeTitleTmp.fontSize = 34;
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
            placeDescTmp.fontSize = 19;
            placeDescTmp.alignment = TextAlignmentOptions.Center;
            placeDescTmp.color = new Color(0.4f, 0.35f, 0.45f);
            placeDescTmp.text = "Feature under construction.\nComing soon in next update!";

            placeGo.SetActive(false);

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

            // PhotoFrame (Rasio 4:3! Ukuran 240 x 180 px)
            GameObject photoFrameGo = CreateUIObject("PhotoFrame", frontGo.transform);
            RectTransform photoFrameRt = photoFrameGo.GetComponent<RectTransform>();
            photoFrameRt.anchorMin = new Vector2(0.5f, 1f);
            photoFrameRt.anchorMax = new Vector2(0.5f, 1f);
            photoFrameRt.pivot = new Vector2(0.5f, 1f);
            photoFrameRt.sizeDelta = new Vector2(240f, 180f);
            photoFrameRt.anchoredPosition = new Vector2(0f, -24f);

            // RawImage untuk foto snapshot (raycastTarget = false agar klik tembus ke CardClickTrigger)
            GameObject photoGo = CreateUIObject("PhotoImage", photoFrameGo.transform);
            RectTransform photoRt = photoGo.GetComponent<RectTransform>();
            SetStretch(photoRt);
            var photoRaw = photoGo.AddComponent<RawImage>();
            photoRaw.color = Color.white;
            photoRaw.raycastTarget = false;

            // Kanji & Furigana Label (Zen Maru Gothic) (raycastTarget = false)
            GameObject kanjiGo = CreateUIObject("KanjiLabel", frontGo.transform);
            RectTransform kanjiRt = kanjiGo.GetComponent<RectTransform>();
            kanjiRt.anchorMin = new Vector2(0.5f, 1f);
            kanjiRt.anchorMax = new Vector2(0.5f, 1f);
            kanjiRt.pivot = new Vector2(0f, 1f);
            kanjiRt.sizeDelta = new Vector2(240f, 28f);
            kanjiRt.anchoredPosition = new Vector2(-120f, -214f);
            var kanjiTmp = kanjiGo.AddComponent<TextMeshProUGUI>();
            kanjiTmp.font = zenFont;
            kanjiTmp.fontSize = 20;
            kanjiTmp.fontStyle = FontStyles.Bold;
            kanjiTmp.color = new Color(0.15f, 0.12f, 0.12f);
            kanjiTmp.text = "<b>道</b> <size=75%>(みち)</size>";
            kanjiTmp.raycastTarget = false;

            // Meaning & Romaji Label (Nunito) (raycastTarget = false)
            GameObject meanGo = CreateUIObject("MeaningLabel", frontGo.transform);
            RectTransform meanRt = meanGo.GetComponent<RectTransform>();
            meanRt.anchorMin = new Vector2(0.5f, 1f);
            meanRt.anchorMax = new Vector2(0.5f, 1f);
            meanRt.pivot = new Vector2(0f, 1f);
            meanRt.sizeDelta = new Vector2(240f, 22f);
            meanRt.anchoredPosition = new Vector2(-120f, -244f);
            var meanTmp = meanGo.AddComponent<TextMeshProUGUI>();
            meanTmp.font = nunitoFont;
            meanTmp.fontSize = 13;
            meanTmp.fontStyle = FontStyles.Bold;
            meanTmp.color = new Color(0.15f, 0.42f, 0.92f); // Vibrant blue
            meanTmp.text = "michi • Street / Road";
            meanTmp.raycastTarget = false;

            // Tombol Like (Hati) di pojok kanan bawah
            GameObject likeGo = CreateUIObject("Btn_Like", frontGo.transform);
            RectTransform likeRt = likeGo.GetComponent<RectTransform>();
            likeRt.anchorMin = new Vector2(1f, 0f);
            likeRt.anchorMax = new Vector2(1f, 0f);
            likeRt.pivot = new Vector2(0.5f, 0.5f);
            likeRt.sizeDelta = new Vector2(30f, 30f);
            likeRt.anchoredPosition = new Vector2(-84f, 60f);
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
            favRt.sizeDelta = new Vector2(32f, 32f);
            favRt.anchoredPosition = new Vector2(-47f, 60f);
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

            // Judul Kata Belakang
            GameObject backTitleGo = CreateUIObject("BackKanjiLabel", backGo.transform);
            RectTransform backTitleRt = backTitleGo.GetComponent<RectTransform>();
            backTitleRt.anchorMin = new Vector2(0.5f, 1f);
            backTitleRt.anchorMax = new Vector2(0.5f, 1f);
            backTitleRt.pivot = new Vector2(0.5f, 1f);
            backTitleRt.sizeDelta = new Vector2(240f, 35f);
            backTitleRt.anchoredPosition = new Vector2(0f, -30f);
            var backTitleTmp = backTitleGo.AddComponent<TextMeshProUGUI>();
            backTitleTmp.font = zenFont;
            backTitleTmp.fontSize = 22;
            backTitleTmp.fontStyle = FontStyles.Bold;
            backTitleTmp.alignment = TextAlignmentOptions.Center;
            backTitleTmp.color = new Color(0.2f, 0.16f, 0.18f);
            backTitleTmp.text = "道";
            backTitleTmp.raycastTarget = false;

            // Kotak Contoh Kalimat
            GameObject sentBoxGo = CreateUIObject("SentenceContainer", backGo.transform);
            RectTransform sentBoxRt = sentBoxGo.GetComponent<RectTransform>();
            sentBoxRt.anchorMin = new Vector2(0.5f, 0.5f);
            sentBoxRt.anchorMax = new Vector2(0.5f, 0.5f);
            sentBoxRt.pivot = new Vector2(0.5f, 0.5f);
            sentBoxRt.sizeDelta = new Vector2(250f, 150f);
            sentBoxRt.anchoredPosition = new Vector2(0f, 10f);

            GameObject sentJpGo = CreateUIObject("SentenceJapanese", sentBoxGo.transform);
            RectTransform sentJpRt = sentJpGo.GetComponent<RectTransform>();
            sentJpRt.anchorMin = new Vector2(0.5f, 1f);
            sentJpRt.anchorMax = new Vector2(0.5f, 1f);
            sentJpRt.pivot = new Vector2(0.5f, 1f);
            sentJpRt.sizeDelta = new Vector2(240f, 60f);
            sentJpRt.anchoredPosition = new Vector2(0f, 0f);
            var sentJpTmp = sentJpGo.AddComponent<TextMeshProUGUI>();
            sentJpTmp.font = zenFont;
            sentJpTmp.fontSize = 15;
            sentJpTmp.alignment = TextAlignmentOptions.Center;
            sentJpTmp.color = new Color(0.12f, 0.16f, 0.22f);
            sentJpTmp.text = "「この道をまっすぐ行きます。」";
            sentJpTmp.raycastTarget = false;

            GameObject sentEngGo = CreateUIObject("SentenceEnglish", sentBoxGo.transform);
            RectTransform sentEngRt = sentEngGo.GetComponent<RectTransform>();
            sentEngRt.anchorMin = new Vector2(0.5f, 0f);
            sentEngRt.anchorMax = new Vector2(0.5f, 0f);
            sentEngRt.pivot = new Vector2(0.5f, 0f);
            sentEngRt.sizeDelta = new Vector2(240f, 75f);
            sentEngRt.anchoredPosition = new Vector2(0f, 5f);
            var sentEngTmp = sentEngGo.AddComponent<TextMeshProUGUI>();
            sentEngTmp.font = nunitoFont;
            sentEngTmp.fontSize = 12;
            sentEngTmp.alignment = TextAlignmentOptions.Center;
            sentEngTmp.color = new Color(0.35f, 0.4f, 0.48f);
            sentEngTmp.text = "<i>(Kono michi o massugu ikimasu)</i>\nGo straight along this street.";
            sentEngTmp.raycastTarget = false;

            // Teks petunjuk balik
            GameObject hintGo = CreateUIObject("BackFlipHint", backGo.transform);
            RectTransform hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.sizeDelta = new Vector2(200f, 20f);
            hintRt.anchoredPosition = new Vector2(0f, 15f);
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.font = nunitoFont;
            hintTmp.fontSize = 11;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(0.6f, 0.65f, 0.72f);
            hintTmp.text = "tap to flip card";
            hintTmp.raycastTarget = false;

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
