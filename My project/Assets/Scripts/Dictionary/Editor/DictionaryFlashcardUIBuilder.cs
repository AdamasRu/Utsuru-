using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;
using Utsuru.FlashcardSystem;

namespace Utsuru.DictionaryUI.Editor
{
    public static class DictionaryFlashcardUIBuilder
    {
        [MenuItem("Tools/Utsuru/Build Dictionary Flashcard UI")]
        public static void BuildFlashcardUI()
        {
            // 1. Muat Aset Font
            var zenFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ZenMaruGothic-Bold SDF.asset");
            var nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");

            // 2. Muat Aset Sprite
            var cardChoice1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Card_Choice1_Bg.png");
            var cardChoice2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Card_Choice2_Bg.png");
            var btnSelectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Button_Selected.png");
            var btnUnselectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Button_Unselected.png");
            var pillBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Category Pill.png");
            var cardBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Card Background.png");
            var tapeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Polaroid/Tape_Washi.png");
            var speakerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/IconSpeaker.png");

            // 3. Cari BookSpread di scene
            var bookSpread = Object.FindFirstObjectByType<DictionaryBookSpreadUI>(FindObjectsInactive.Include);
            if (bookSpread == null)
            {
                Debug.LogError("[DictionaryFlashcardUIBuilder] DictionaryBookSpreadUI tidak ditemukan di scene!");
                return;
            }

            Undo.RegisterCompleteObjectUndo(bookSpread.gameObject, "Build Dictionary Flashcard UI");
            Transform spreadTrans = bookSpread.transform;

            // Pastikan FlashcardManager ada di GameObject Canvas/Dictionary atau BookSpread
            var dictController = Object.FindFirstObjectByType<DictionaryUIController>(FindObjectsInactive.Include);
            if (dictController != null && dictController.GetComponent<FlashcardManager>() == null)
            {
                dictController.gameObject.AddComponent<FlashcardManager>();
            }

            // Bersihkan FlashcardSpreadRoot lama jika ada
            Transform oldRoot = spreadTrans.Find("FlashcardSpreadRoot");
            if (oldRoot != null) Object.DestroyImmediate(oldRoot.gameObject);

            GameObject flashRoot = CreateUIObject("FlashcardSpreadRoot", spreadTrans);
            RectTransform flashRt = flashRoot.GetComponent<RectTransform>();
            SetStretch(flashRt);

            var spreadUI = flashRoot.AddComponent<DictionaryFlashcardSpreadUI>();

            // ========================================================
            // A. HALAMAN KIRI (MANAJEMEN DECK, FILTER, DAFTAR KARTU)
            // ========================================================
            GameObject leftPageGo = CreateUIObject("LeftFlashcardPage", flashRoot.transform);
            RectTransform leftPageRt = leftPageGo.GetComponent<RectTransform>();
            SetPageTransform(leftPageRt, isLeft: true);
            leftPageRt.anchoredPosition = new Vector2(56f, 20f);
            leftPageRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            CreatePageHeader(leftPageGo.transform, "FLASHCARD DECK", "Active Recall & PBO Vocabulary Management", zenFont, nunitoFont);

            // Ringkasan Deck Status
            GameObject summaryGo = CreateUIObject("DeckSummary", leftPageGo.transform);
            RectTransform summaryRt = summaryGo.GetComponent<RectTransform>();
            summaryRt.anchorMin = new Vector2(0.5f, 1f);
            summaryRt.anchorMax = new Vector2(0.5f, 1f);
            summaryRt.pivot = new Vector2(0.5f, 1f);
            summaryRt.sizeDelta = new Vector2(680f, 40f);
            summaryRt.anchoredPosition = new Vector2(0f, -135f);
            var summaryImg = summaryGo.AddComponent<Image>();
            summaryImg.sprite = pillBgSprite;
            summaryImg.type = Image.Type.Sliced;
            summaryImg.color = new Color(0.92f, 0.90f, 0.97f, 0.95f);

            GameObject summaryTextGo = CreateUIObject("Text", summaryGo.transform);
            SetStretch(summaryTextGo.GetComponent<RectTransform>());
            var summaryTmp = summaryTextGo.AddComponent<TextMeshProUGUI>();
            summaryTmp.font = zenFont;
            summaryTmp.fontSize = 20f;
            summaryTmp.fontStyle = FontStyles.Bold;
            summaryTmp.alignment = TextAlignmentOptions.Center;
            summaryTmp.color = new Color(0.28f, 0.22f, 0.55f, 1f);
            summaryTmp.text = "Total: 0 Kartu  |  Hafal: 0  |  Belajar: 0";

            // Baris Filter (Semua / Belajar / Hafal)
            GameObject filterRowGo = CreateUIObject("FilterRow", leftPageGo.transform);
            RectTransform filterRowRt = filterRowGo.GetComponent<RectTransform>();
            filterRowRt.anchorMin = new Vector2(0.5f, 1f);
            filterRowRt.anchorMax = new Vector2(0.5f, 1f);
            filterRowRt.pivot = new Vector2(0.5f, 1f);
            filterRowRt.sizeDelta = new Vector2(680f, 38f);
            filterRowRt.anchoredPosition = new Vector2(0f, -182f);

            var filterHlg = filterRowGo.AddComponent<HorizontalLayoutGroup>();
            filterHlg.spacing = 10f;
            filterHlg.childControlWidth = true;
            filterHlg.childControlHeight = true;
            filterHlg.childForceExpandWidth = true;
            filterHlg.childForceExpandHeight = true;

            var (btnFilterAll, txtFilterAll) = CreateTabFilterButton("BtnFilterAll", filterRowGo.transform, "Semua (0)", btnUnselectSprite, zenFont);
            var (btnFilterLearning, txtFilterLearning) = CreateTabFilterButton("BtnFilterLearning", filterRowGo.transform, "Belajar (0)", btnUnselectSprite, zenFont);
            var (btnFilterMastered, txtFilterMastered) = CreateTabFilterButton("BtnFilterMastered", filterRowGo.transform, "Hafal (0)", btnUnselectSprite, zenFont);

            // Baris Tombol Aksi Create & Auto
            GameObject actionRowGo = CreateUIObject("ActionRow", leftPageGo.transform);
            RectTransform actionRowRt = actionRowGo.GetComponent<RectTransform>();
            actionRowRt.anchorMin = new Vector2(0.5f, 1f);
            actionRowRt.anchorMax = new Vector2(0.5f, 1f);
            actionRowRt.pivot = new Vector2(0.5f, 1f);
            actionRowRt.sizeDelta = new Vector2(680f, 44f);
            actionRowRt.anchoredPosition = new Vector2(0f, -228f);

            var actionHlg = actionRowGo.AddComponent<HorizontalLayoutGroup>();
            actionHlg.spacing = 14f;
            actionHlg.childControlWidth = true;
            actionHlg.childControlHeight = true;
            actionHlg.childForceExpandWidth = true;
            actionHlg.childForceExpandHeight = true;

            var btnCreateNew = CreateActionButton("BtnCreateNew", actionRowGo.transform, "+ Buat Flashcard Baru", btnSelectSprite, new Color(0.18f, 0.68f, 0.38f, 1f), zenFont);
            var btnAutoGenerate = CreateActionButton("BtnAutoGenerate", actionRowGo.transform, "Otomatis dari Album", btnUnselectSprite, new Color(0.38f, 0.32f, 0.65f, 1f), zenFont);

            // Wadah Scroll Rect untuk Daftar Kartu
            GameObject scrollGo = CreateUIObject("CardScrollView", leftPageGo.transform);
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 1f);
            scrollRt.anchorMax = new Vector2(0.5f, 1f);
            scrollRt.pivot = new Vector2(0.5f, 1f);
            scrollRt.sizeDelta = new Vector2(680f, 510f);
            scrollRt.anchoredPosition = new Vector2(0f, -282f);

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 25f;

            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.02f);

            // Viewport
            GameObject viewportGo = CreateUIObject("Viewport", scrollGo.transform);
            RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
            SetStretch(viewportRt);
            viewportGo.AddComponent<RectMask2D>();

            // Content
            GameObject contentGo = CreateUIObject("Content", viewportGo.transform);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 8f;
            contentVlg.padding = new RectOffset(4, 4, 6, 6);
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = false;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;

            var contentCsf = contentGo.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            // Template Item Flashcard Mini
            GameObject itemTemplate = CreateFlashcardItemTemplate(contentGo.transform, cardChoice1Sprite, zenFont, nunitoFont);
            itemTemplate.SetActive(false);

            // Notice saat deck kosong
            GameObject emptyDeckNotice = CreateUIObject("EmptyDeckNotice", leftPageGo.transform);
            RectTransform emptyDeckRt = emptyDeckNotice.GetComponent<RectTransform>();
            emptyDeckRt.anchorMin = new Vector2(0.5f, 0.5f);
            emptyDeckRt.anchorMax = new Vector2(0.5f, 0.5f);
            emptyDeckRt.pivot = new Vector2(0.5f, 0.5f);
            emptyDeckRt.sizeDelta = new Vector2(500f, 180f);
            emptyDeckRt.anchoredPosition = new Vector2(0f, -80f);

            var emptyDeckTmp = emptyDeckNotice.AddComponent<TextMeshProUGUI>();
            emptyDeckTmp.font = nunitoFont;
            emptyDeckTmp.fontSize = 24f;
            emptyDeckTmp.alignment = TextAlignmentOptions.Center;
            emptyDeckTmp.color = new Color(0.48f, 0.44f, 0.58f, 1f);
            emptyDeckTmp.text = "Deck Anda masih kosong.\nTekan [+ Buat Flashcard] untuk membuat kartu dari foto!";
            emptyDeckNotice.SetActive(false);


            // ========================================================
            // B. HALAMAN KANAN (INTERACTIVE STUDY CARD & REVIEW ACTIONS)
            // ========================================================
            GameObject rightPageGo = CreateUIObject("RightFlashcardPage", flashRoot.transform);
            RectTransform rightPageRt = rightPageGo.GetComponent<RectTransform>();
            SetPageTransform(rightPageRt, isLeft: false);
            rightPageRt.anchoredPosition = new Vector2(-56f, 20f);
            rightPageRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            CreatePageHeader(rightPageGo.transform, "INTERACTIVE STUDY", "Active Recall: Front Picture & Back Vocabulary", zenFont, nunitoFont);

            // Wadah Empty Selection State
            GameObject emptyStudyGo = CreateUIObject("EmptySelectionContainer", rightPageGo.transform);
            RectTransform emptyStudyRt = emptyStudyGo.GetComponent<RectTransform>();
            SetStretch(emptyStudyRt);
            emptyStudyRt.offsetMin = new Vector2(40f, 40f);
            emptyStudyRt.offsetMax = new Vector2(-40f, -140f);

            var emptyStudyTmp = emptyStudyGo.AddComponent<TextMeshProUGUI>();
            emptyStudyTmp.font = nunitoFont;
            emptyStudyTmp.fontSize = 26f;
            emptyStudyTmp.alignment = TextAlignmentOptions.Center;
            emptyStudyTmp.color = new Color(0.48f, 0.44f, 0.58f, 1f);
            emptyStudyTmp.text = "Pilih kartu di sebelah kiri atau tekan\n<b>[+ Buat Flashcard Baru]</b>\nuntuk mulai belajar!";

            // Wadah Kartu Belajar Aktif
            GameObject studyContainerGo = CreateUIObject("StudyCardContainer", rightPageGo.transform);
            RectTransform studyContainerRt = studyContainerGo.GetComponent<RectTransform>();
            SetStretch(studyContainerRt);

            // Container Flip Card (Yang mengalami animasi skala sumbu X)
            GameObject flipCardGo = CreateUIObject("FlipCardRoot", studyContainerGo.transform);
            RectTransform flipCardRt = flipCardGo.GetComponent<RectTransform>();
            flipCardRt.anchorMin = new Vector2(0.5f, 1f);
            flipCardRt.anchorMax = new Vector2(0.5f, 1f);
            flipCardRt.pivot = new Vector2(0.5f, 1f);
            flipCardRt.sizeDelta = new Vector2(680f, 550f);
            flipCardRt.anchoredPosition = new Vector2(0f, -140f);

            // 1. SISI DEPAN (FRONT SIDE) - Persis Gaya Quiz Gambar 2
            GameObject frontSideGo = CreateUIObject("FrontSide", flipCardGo.transform);
            SetStretch(frontSideGo.GetComponent<RectTransform>());
            var frontBgImg = frontSideGo.AddComponent<Image>();
            frontBgImg.sprite = cardChoice2Sprite != null ? cardChoice2Sprite : cardChoice1Sprite;
            frontBgImg.type = Image.Type.Sliced;
            frontBgImg.color = Color.white;

            var frontVlg = frontSideGo.AddComponent<VerticalLayoutGroup>();
            frontVlg.padding = new RectOffset(24, 24, 20, 20);
            frontVlg.spacing = 14f;
            frontVlg.childControlWidth = true;
            frontVlg.childControlHeight = false; // CRITICAL: agar tidak collapse atau override LayoutElement
            frontVlg.childForceExpandWidth = true;
            frontVlg.childForceExpandHeight = false; // CRITICAL: agar LayoutElement dihormati
            frontVlg.childAlignment = TextAnchor.MiddleCenter;

            // Foto Frame Polaroid untuk foto pemain (Persis Quiz QuestionCard)
            GameObject frontPhotoBox = CreateUIObject("PhotoFrameContainer", frontSideGo.transform);
            RectTransform frontPhotoBoxRt = frontPhotoBox.GetComponent<RectTransform>();
            frontPhotoBoxRt.sizeDelta = new Vector2(380f, 255f);
            var frontPhotoBoxLe = frontPhotoBox.AddComponent<LayoutElement>();
            frontPhotoBoxLe.preferredWidth = 380f;
            frontPhotoBoxLe.preferredHeight = 255f;
            frontPhotoBoxLe.minWidth = 380f;
            frontPhotoBoxLe.minHeight = 255f;

            var frontPhotoBoxImg = frontPhotoBox.AddComponent<Image>();
            frontPhotoBoxImg.sprite = cardBgSprite != null ? cardBgSprite : cardChoice1Sprite;
            frontPhotoBoxImg.type = Image.Type.Sliced;
            frontPhotoBoxImg.color = new Color(0.96f, 0.95f, 0.98f, 1f);

            // Tape di atas foto (Peach / Washi Tape)
            if (tapeSprite != null)
            {
                GameObject tapeGo = CreateUIObject("Tape", frontPhotoBox.transform);
                RectTransform tapeRt = tapeGo.GetComponent<RectTransform>();
                tapeRt.anchorMin = new Vector2(0.5f, 1f);
                tapeRt.anchorMax = new Vector2(0.5f, 1f);
                tapeRt.pivot = new Vector2(0.5f, 0.5f);
                tapeRt.sizeDelta = new Vector2(90f, 24f);
                tapeRt.anchoredPosition = new Vector2(0f, -4f);
                var tapeImg = tapeGo.AddComponent<Image>();
                tapeImg.sprite = tapeSprite;
                tapeImg.color = new Color(1f, 0.85f, 0.7f, 0.9f);
                tapeImg.raycastTarget = false;
            }

            // RawImage Foto
            GameObject frontRawImgGo = CreateUIObject("PhotoRawImage", frontPhotoBox.transform);
            RectTransform frontRawImgRt = frontRawImgGo.GetComponent<RectTransform>();
            frontRawImgRt.anchorMin = new Vector2(0.5f, 0.5f);
            frontRawImgRt.anchorMax = new Vector2(0.5f, 0.5f);
            frontRawImgRt.pivot = new Vector2(0.5f, 0.5f);
            frontRawImgRt.sizeDelta = new Vector2(345f, 215f);
            frontRawImgRt.anchoredPosition = new Vector2(0f, -6f);

            var frontRawImg = frontRawImgGo.AddComponent<RawImage>();
            frontRawImg.color = Color.white;
            frontRawImg.raycastTarget = false;

            // Prompt Label ("What is the Japanese word for:")
            GameObject frontPromptGo = CreateUIObject("PromptLabel", frontSideGo.transform);
            RectTransform frontPromptRt = frontPromptGo.GetComponent<RectTransform>();
            frontPromptRt.sizeDelta = new Vector2(632f, 30f);
            var frontPromptLe = frontPromptGo.AddComponent<LayoutElement>();
            frontPromptLe.preferredHeight = 30f;
            frontPromptLe.minHeight = 30f;

            var frontPromptTmp = frontPromptGo.AddComponent<TextMeshProUGUI>();
            frontPromptTmp.font = nunitoFont;
            frontPromptTmp.fontSize = 24f;
            frontPromptTmp.alignment = TextAlignmentOptions.Center;
            frontPromptTmp.color = new Color(0.48f, 0.44f, 0.58f, 1f);
            frontPromptTmp.text = "What is the Japanese word for:";

            // Target Word ("House" / "Flowerpot") - HIGHLIGHT: 38f
            GameObject frontObjNameGo = CreateUIObject("TargetWord", frontSideGo.transform);
            RectTransform frontObjNameRt = frontObjNameGo.GetComponent<RectTransform>();
            frontObjNameRt.sizeDelta = new Vector2(632f, 48f);
            var frontObjNameLe = frontObjNameGo.AddComponent<LayoutElement>();
            frontObjNameLe.preferredHeight = 48f;
            frontObjNameLe.minHeight = 48f;

            var frontObjNameTmp = frontObjNameGo.AddComponent<TextMeshProUGUI>();
            frontObjNameTmp.font = zenFont;
            frontObjNameTmp.fontSize = 38f;
            frontObjNameTmp.fontStyle = FontStyles.Bold;
            frontObjNameTmp.alignment = TextAlignmentOptions.Center;
            frontObjNameTmp.color = new Color(0.18f, 0.14f, 0.28f, 1f);
            frontObjNameTmp.text = "House";

            // Category Badge (Pill Persis Gambar 2)
            GameObject catBadgeGo = CreateUIObject("CategoryBadge", frontSideGo.transform);
            RectTransform catBadgeRt = catBadgeGo.GetComponent<RectTransform>();
            catBadgeRt.sizeDelta = new Vector2(280f, 38f);
            var catBadgeLe = catBadgeGo.AddComponent<LayoutElement>();
            catBadgeLe.preferredWidth = 280f;
            catBadgeLe.preferredHeight = 38f;
            catBadgeLe.minHeight = 38f;

            var catBadgeImg = catBadgeGo.AddComponent<Image>();
            catBadgeImg.sprite = pillBgSprite;
            catBadgeImg.type = Image.Type.Sliced;
            catBadgeImg.color = new Color(0.92f, 0.90f, 0.96f, 0.9f);

            GameObject catTextGo = CreateUIObject("Text", catBadgeGo.transform);
            SetStretch(catTextGo.GetComponent<RectTransform>());
            var catBadgeTmp = catTextGo.AddComponent<TextMeshProUGUI>();
            catBadgeTmp.font = nunitoFont;
            catBadgeTmp.fontSize = 22f;
            catBadgeTmp.fontStyle = FontStyles.Bold;
            catBadgeTmp.alignment = TextAlignmentOptions.Center;
            catBadgeTmp.color = new Color(0.42f, 0.36f, 0.60f, 1f);
            catBadgeTmp.text = "Category: General";

            // Baris Tombol Aksi Sisi Depan (Putar Audio + Balik Kartu)
            GameObject frontActionRowGo = CreateUIObject("FrontActionRow", frontSideGo.transform);
            RectTransform frontActionRowRt = frontActionRowGo.GetComponent<RectTransform>();
            frontActionRowRt.sizeDelta = new Vector2(632f, 44f);
            var frontActionRowLe = frontActionRowGo.AddComponent<LayoutElement>();
            frontActionRowLe.preferredHeight = 44f;
            frontActionRowLe.minHeight = 44f;

            var frontActionHlg = frontActionRowGo.AddComponent<HorizontalLayoutGroup>();
            frontActionHlg.spacing = 16f;
            frontActionHlg.childControlWidth = true;
            frontActionHlg.childControlHeight = true;
            frontActionHlg.childForceExpandWidth = true;

            var frontAudioBtn = CreateActionButton("BtnAudio", frontActionRowGo.transform, "Putar Audio", btnUnselectSprite, new Color(0.40f, 0.36f, 0.62f, 1f), zenFont);
            var btnFlipToBack = CreateActionButton("BtnFlipToBack", frontActionRowGo.transform, "Balik Kartu (Lihat Jawaban)", btnSelectSprite, new Color(0.28f, 0.52f, 0.88f, 1f), zenFont);

            // 2. SISI BELAKANG (BACK SIDE)
            GameObject backSideGo = CreateUIObject("BackSide", flipCardGo.transform);
            SetStretch(backSideGo.GetComponent<RectTransform>());
            var backBgImg = backSideGo.AddComponent<Image>();
            backBgImg.sprite = cardChoice2Sprite != null ? cardChoice2Sprite : cardChoice1Sprite;
            backBgImg.type = Image.Type.Sliced;
            backBgImg.color = Color.white;

            var backVlg = backSideGo.AddComponent<VerticalLayoutGroup>();
            backVlg.padding = new RectOffset(24, 24, 22, 20);
            backVlg.spacing = 12f;
            backVlg.childControlWidth = true;
            backVlg.childControlHeight = false; // CRITICAL
            backVlg.childForceExpandWidth = true;
            backVlg.childForceExpandHeight = false; // CRITICAL
            backVlg.childAlignment = TextAnchor.MiddleCenter;

            // Badge Header Jawaban
            GameObject backBadgeGo = CreateUIObject("AnswerBadge", backSideGo.transform);
            RectTransform backBadgeRt = backBadgeGo.GetComponent<RectTransform>();
            backBadgeRt.sizeDelta = new Vector2(260f, 36f);
            var backBadgeLe = backBadgeGo.AddComponent<LayoutElement>();
            backBadgeLe.preferredWidth = 260f;
            backBadgeLe.preferredHeight = 36f;
            backBadgeLe.minHeight = 36f;

            var backBadgeImg = backBadgeGo.AddComponent<Image>();
            backBadgeImg.sprite = pillBgSprite;
            backBadgeImg.type = Image.Type.Sliced;
            backBadgeImg.color = new Color(0.88f, 0.94f, 0.90f, 1f);

            GameObject backBadgeTextGo = CreateUIObject("Text", backBadgeGo.transform);
            SetStretch(backBadgeTextGo.GetComponent<RectTransform>());
            var backBadgeTmp = backBadgeTextGo.AddComponent<TextMeshProUGUI>();
            backBadgeTmp.font = zenFont;
            backBadgeTmp.fontSize = 19f;
            backBadgeTmp.fontStyle = FontStyles.Bold;
            backBadgeTmp.alignment = TextAlignmentOptions.Center;
            backBadgeTmp.color = new Color(0.18f, 0.52f, 0.32f, 1f);
            backBadgeTmp.text = "JAWABAN KOSAKATA";

            // Kanji / Hiragana Besar
            GameObject backJpGo = CreateUIObject("JapaneseWordText", backSideGo.transform);
            RectTransform backJpRt = backJpGo.GetComponent<RectTransform>();
            backJpRt.sizeDelta = new Vector2(632f, 68f);
            var backJpLe = backJpGo.AddComponent<LayoutElement>();
            backJpLe.preferredHeight = 68f;
            backJpLe.minHeight = 68f;

            var backJpTmp = backJpGo.AddComponent<TextMeshProUGUI>();
            backJpTmp.font = zenFont;
            backJpTmp.fontSize = 52f;
            backJpTmp.fontStyle = FontStyles.Bold;
            backJpTmp.alignment = TextAlignmentOptions.Center;
            backJpTmp.color = new Color(0.18f, 0.14f, 0.28f, 1f);
            backJpTmp.text = "自転車 / じてんしゃ";

            // Romaji
            GameObject backRomajiGo = CreateUIObject("RomajiText", backSideGo.transform);
            RectTransform backRomajiRt = backRomajiGo.GetComponent<RectTransform>();
            backRomajiRt.sizeDelta = new Vector2(632f, 36f);
            var backRomajiLe = backRomajiGo.AddComponent<LayoutElement>();
            backRomajiLe.preferredHeight = 36f;
            backRomajiLe.minHeight = 36f;

            var backRomajiTmp = backRomajiGo.AddComponent<TextMeshProUGUI>();
            backRomajiTmp.font = nunitoFont;
            backRomajiTmp.fontSize = 28f;
            backRomajiTmp.alignment = TextAlignmentOptions.Center;
            backRomajiTmp.color = new Color(0.48f, 0.44f, 0.58f, 1f);
            backRomajiTmp.text = "[jitensha]";

            // Arti Indonesia
            GameObject backTransGo = CreateUIObject("TranslationText", backSideGo.transform);
            RectTransform backTransRt = backTransGo.GetComponent<RectTransform>();
            backTransRt.sizeDelta = new Vector2(632f, 44f);
            var backTransLe = backTransGo.AddComponent<LayoutElement>();
            backTransLe.preferredHeight = 44f;
            backTransLe.minHeight = 44f;

            var backTransTmp = backTransGo.AddComponent<TextMeshProUGUI>();
            backTransTmp.font = zenFont;
            backTransTmp.fontSize = 36f;
            backTransTmp.fontStyle = FontStyles.Bold;
            backTransTmp.alignment = TextAlignmentOptions.Center;
            backTransTmp.color = new Color(0.18f, 0.65f, 0.38f, 1f); // Hijau emerald
            backTransTmp.text = "Sepeda";

            // Kotak Catatan Mnemonik Pribadi
            GameObject backNoteBox = CreateUIObject("NoteBox", backSideGo.transform);
            RectTransform backNoteBoxRt = backNoteBox.GetComponent<RectTransform>();
            backNoteBoxRt.sizeDelta = new Vector2(632f, 115f);
            var backNoteBoxLe = backNoteBox.AddComponent<LayoutElement>();
            backNoteBoxLe.preferredHeight = 115f;
            backNoteBoxLe.minHeight = 115f;

            var backNoteBoxImg = backNoteBox.AddComponent<Image>();
            backNoteBoxImg.sprite = cardChoice1Sprite;
            backNoteBoxImg.type = Image.Type.Sliced;
            backNoteBoxImg.color = new Color(0.98f, 0.97f, 0.91f, 0.98f); // Soft cream note

            GameObject backNoteTextGo = CreateUIObject("NoteText", backNoteBox.transform);
            SetStretch(backNoteTextGo.GetComponent<RectTransform>());
            backNoteTextGo.GetComponent<RectTransform>().offsetMin = new Vector2(16f, 8f);
            backNoteTextGo.GetComponent<RectTransform>().offsetMax = new Vector2(-16f, -8f);
            var backNoteTmp = backNoteTextGo.AddComponent<TextMeshProUGUI>();
            backNoteTmp.font = nunitoFont;
            backNoteTmp.fontSize = 21f;
            backNoteTmp.alignment = TextAlignmentOptions.Center;
            backNoteTmp.color = new Color(0.38f, 0.30f, 0.22f, 1f);
            backNoteTmp.text = "Catatan pribadi pemain.";

            // Baris Tombol Aksi Sisi Belakang (Putar Audio + Tutup Kartu)
            GameObject backActionRowGo = CreateUIObject("BackActionRow", backSideGo.transform);
            RectTransform backActionRowRt = backActionRowGo.GetComponent<RectTransform>();
            backActionRowRt.sizeDelta = new Vector2(632f, 44f);
            var backActionRowLe = backActionRowGo.AddComponent<LayoutElement>();
            backActionRowLe.preferredHeight = 44f;
            backActionRowLe.minHeight = 44f;

            var backActionHlg = backActionRowGo.AddComponent<HorizontalLayoutGroup>();
            backActionHlg.spacing = 16f;
            backActionHlg.childControlWidth = true;
            backActionHlg.childControlHeight = true;
            backActionHlg.childForceExpandWidth = true;

            var backAudioBtn = CreateActionButton("BtnBackAudio", backActionRowGo.transform, "Putar Audio", btnUnselectSprite, new Color(0.40f, 0.36f, 0.62f, 1f), zenFont);
            var btnFlipToFront = CreateActionButton("BtnFlipToFront", backActionRowGo.transform, "Tutup Kartu (Sisi Depan)", btnSelectSprite, new Color(0.40f, 0.36f, 0.62f, 1f), zenFont);

            backSideGo.SetActive(false); // Default: mulai dari sisi depan

            // BARIS EVALUASI DIRI (Belum Hafal / Sudah Hafal)
            GameObject reviewRowGo = CreateUIObject("ReviewRow", studyContainerGo.transform);
            RectTransform reviewRowRt = reviewRowGo.GetComponent<RectTransform>();
            reviewRowRt.anchorMin = new Vector2(0.5f, 0f);
            reviewRowRt.anchorMax = new Vector2(0.5f, 0f);
            reviewRowRt.pivot = new Vector2(0.5f, 0f);
            reviewRowRt.sizeDelta = new Vector2(680f, 48f);
            reviewRowRt.anchoredPosition = new Vector2(0f, 72f);

            var reviewHlg = reviewRowGo.AddComponent<HorizontalLayoutGroup>();
            reviewHlg.spacing = 16f;
            reviewHlg.childControlWidth = true;
            reviewHlg.childControlHeight = true;
            reviewHlg.childForceExpandWidth = true;

            var btnMarkLearning = CreateActionButton("BtnMarkLearning", reviewRowGo.transform, "Belum Hafal", btnSelectSprite, new Color(0.92f, 0.42f, 0.38f, 1f), zenFont);
            var btnMarkMastered = CreateActionButton("BtnMarkMastered", reviewRowGo.transform, "Sudah Hafal", btnSelectSprite, new Color(0.20f, 0.72f, 0.44f, 1f), zenFont);

            // BARIS MANAJEMEN KARTU (Edit Catatan / Hapus Kartu)
            GameObject manageRowGo = CreateUIObject("ManageRow", studyContainerGo.transform);
            RectTransform manageRowRt = manageRowGo.GetComponent<RectTransform>();
            manageRowRt.anchorMin = new Vector2(0.5f, 0f);
            manageRowRt.anchorMax = new Vector2(0.5f, 0f);
            manageRowRt.pivot = new Vector2(0.5f, 0f);
            manageRowRt.sizeDelta = new Vector2(680f, 40f);
            manageRowRt.anchoredPosition = new Vector2(0f, 18f);

            var manageHlg = manageRowGo.AddComponent<HorizontalLayoutGroup>();
            manageHlg.spacing = 16f;
            manageHlg.childControlWidth = true;
            manageHlg.childControlHeight = true;
            manageHlg.childForceExpandWidth = true;

            var btnEditNote = CreateActionButton("BtnEditNote", manageRowGo.transform, "Edit Catatan", btnUnselectSprite, new Color(0.40f, 0.36f, 0.58f, 1f), zenFont);
            var btnDeleteCard = CreateActionButton("BtnDeleteCard", manageRowGo.transform, "Hapus Kartu", btnUnselectSprite, new Color(0.72f, 0.32f, 0.36f, 1f), zenFont);

            // ========================================================
            // C. MODAL FORM CREATE / UPDATE FLASHCARD
            // ========================================================
            Transform modalParent = dictController != null ? dictController.transform.Find("DictionaryView") : spreadTrans;
            if (modalParent == null) modalParent = spreadTrans;

            // Bersihkan modal lama jika ada
            Transform oldModal = modalParent.Find("FlashcardFormModal");
            if (oldModal != null) Object.DestroyImmediate(oldModal.gameObject);

            var formModalUI = CreateFlashcardModalDialog(modalParent, cardChoice2Sprite, cardChoice1Sprite, btnSelectSprite, btnUnselectSprite, zenFont, nunitoFont);

            // ========================================================
            // D. WIRE SERIALIZED PROPERTIES DI SPREAD UI
            // ========================================================
            var spreadSo = new SerializedObject(spreadUI);
            spreadSo.FindProperty("dictionaryManager").objectReferenceValue = Object.FindFirstObjectByType<DictionaryManager>();
            spreadSo.FindProperty("confirmDialog").objectReferenceValue = Object.FindFirstObjectByType<DictionarySimpleDialog>(FindObjectsInactive.Include);
            spreadSo.FindProperty("formModal").objectReferenceValue = formModalUI;

            spreadSo.FindProperty("deckSummaryText").objectReferenceValue = summaryTmp;
            spreadSo.FindProperty("filterAllButton").objectReferenceValue = btnFilterAll;
            spreadSo.FindProperty("filterLearningButton").objectReferenceValue = btnFilterLearning;
            spreadSo.FindProperty("filterMasteredButton").objectReferenceValue = btnFilterMastered;
            spreadSo.FindProperty("filterAllText").objectReferenceValue = txtFilterAll;
            spreadSo.FindProperty("filterLearningText").objectReferenceValue = txtFilterLearning;
            spreadSo.FindProperty("filterMasteredText").objectReferenceValue = txtFilterMastered;

            spreadSo.FindProperty("createNewCardButton").objectReferenceValue = btnCreateNew;
            spreadSo.FindProperty("autoGenerateButton").objectReferenceValue = btnAutoGenerate;
            spreadSo.FindProperty("cardListContent").objectReferenceValue = contentRt;
            spreadSo.FindProperty("cardItemPrefab").objectReferenceValue = itemTemplate;
            spreadSo.FindProperty("emptyDeckNotice").objectReferenceValue = emptyDeckNotice;
            spreadSo.FindProperty("emptyDeckNoticeText").objectReferenceValue = emptyDeckTmp;

            spreadSo.FindProperty("studyCardContainer").objectReferenceValue = studyContainerGo;
            spreadSo.FindProperty("emptySelectionContainer").objectReferenceValue = emptyStudyGo;
            spreadSo.FindProperty("flipCardTransform").objectReferenceValue = flipCardRt;
            spreadSo.FindProperty("frontSideRoot").objectReferenceValue = frontSideGo;
            spreadSo.FindProperty("backSideRoot").objectReferenceValue = backSideGo;

            spreadSo.FindProperty("frontPhotoImage").objectReferenceValue = frontRawImg;
            spreadSo.FindProperty("frontPromptLabel").objectReferenceValue = frontPromptTmp;
            spreadSo.FindProperty("frontObjectNameText").objectReferenceValue = frontObjNameTmp;
            spreadSo.FindProperty("categoryBadgeText").objectReferenceValue = catBadgeTmp;
            spreadSo.FindProperty("frontAudioButton").objectReferenceValue = frontAudioBtn;
            spreadSo.FindProperty("flipToBackButton").objectReferenceValue = btnFlipToBack;

            spreadSo.FindProperty("backJapaneseText").objectReferenceValue = backJpTmp;
            spreadSo.FindProperty("backRomajiText").objectReferenceValue = backRomajiTmp;
            spreadSo.FindProperty("backTranslationText").objectReferenceValue = backTransTmp;
            spreadSo.FindProperty("backNoteText").objectReferenceValue = backNoteTmp;
            spreadSo.FindProperty("backAudioButton").objectReferenceValue = backAudioBtn;
            spreadSo.FindProperty("flipToFrontButton").objectReferenceValue = btnFlipToFront;

            spreadSo.FindProperty("markLearningButton").objectReferenceValue = btnMarkLearning;
            spreadSo.FindProperty("markMasteredButton").objectReferenceValue = btnMarkMastered;
            spreadSo.FindProperty("editCardButton").objectReferenceValue = btnEditNote;
            spreadSo.FindProperty("deleteCardButton").objectReferenceValue = btnDeleteCard;
            spreadSo.ApplyModifiedPropertiesWithoutUndo();

            // ========================================================
            // E. WIRE DICTIONARY BOOK SPREAD UI
            // ========================================================
            var bookSpreadSo = new SerializedObject(bookSpread);
            bookSpreadSo.FindProperty("flashcardSpreadRoot").objectReferenceValue = flashRoot;
            bookSpreadSo.FindProperty("flashcardSpreadUI").objectReferenceValue = spreadUI;
            bookSpreadSo.ApplyModifiedPropertiesWithoutUndo();

            // Default: sembunyikan Flashcard saat pertama dibuka (karena defaultnya Album)
            flashRoot.SetActive(false);

            EditorUtility.SetDirty(bookSpread.gameObject);
            Debug.Log("[DictionaryFlashcardUIBuilder] SUKSES! UI Flashcard berhasil dibuat dan dihubungkan secara lengkap.");
        }

        private static GameObject CreateFlashcardItemTemplate(Transform parent, Sprite bgSprite, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont)
        {
            GameObject itemGo = CreateUIObject("FlashcardItemTemplate", parent);
            RectTransform itemRt = itemGo.GetComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(660f, 76f);

            var le = itemGo.AddComponent<LayoutElement>();
            le.preferredHeight = 76f;
            le.minHeight = 76f;

            var bgImg = itemGo.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            var btn = itemGo.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            var itemUi = itemGo.AddComponent<DictionaryFlashcardItemUI>();

            // Thumbnail Foto (Kiri)
            GameObject thumbGo = CreateUIObject("Thumbnail", itemGo.transform);
            RectTransform thumbRt = thumbGo.GetComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0f, 0.5f);
            thumbRt.anchorMax = new Vector2(0f, 0.5f);
            thumbRt.pivot = new Vector2(0f, 0.5f);
            thumbRt.sizeDelta = new Vector2(85f, 62f);
            thumbRt.anchoredPosition = new Vector2(8f, 0f);

            var thumbRaw = thumbGo.AddComponent<RawImage>();
            thumbRaw.color = Color.white;

            // Teks Info Kosakata (Tengah)
            GameObject titleGo = CreateUIObject("TitleText", itemGo.transform);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.offsetMin = new Vector2(105f, 4f);
            titleRt.offsetMax = new Vector2(-120f, 28f);

            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.font = zenFont;
            titleTmp.fontSize = 24f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0.18f, 0.14f, 0.28f, 1f);
            titleTmp.text = "自転車 / じてんしゃ";

            GameObject transGo = CreateUIObject("TransText", itemGo.transform);
            RectTransform transRt = transGo.GetComponent<RectTransform>();
            transRt.anchorMin = new Vector2(0f, 0.5f);
            transRt.anchorMax = new Vector2(1f, 0.5f);
            transRt.pivot = new Vector2(0f, 0.5f);
            transRt.offsetMin = new Vector2(105f, -28f);
            transRt.offsetMax = new Vector2(-120f, 2f);

            var transTmp = transGo.AddComponent<TextMeshProUGUI>();
            transTmp.font = nunitoFont;
            transTmp.fontSize = 20f;
            transTmp.color = new Color(0.48f, 0.44f, 0.58f, 1f);
            transTmp.text = "Sepeda [jitensha]";

            // Status Badge (Kanan)
            GameObject badgeGo = CreateUIObject("StatusBadge", itemGo.transform);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1f, 0.5f);
            badgeRt.anchorMax = new Vector2(1f, 0.5f);
            badgeRt.pivot = new Vector2(1f, 0.5f);
            badgeRt.sizeDelta = new Vector2(100f, 32f);
            badgeRt.anchoredPosition = new Vector2(-12f, 0f);

            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.color = new Color(0.18f, 0.72f, 0.42f, 1f);

            GameObject badgeTextGo = CreateUIObject("Text", badgeGo.transform);
            SetStretch(badgeTextGo.GetComponent<RectTransform>());
            var badgeTmp = badgeTextGo.AddComponent<TextMeshProUGUI>();
            badgeTmp.font = zenFont;
            badgeTmp.fontSize = 17f;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = Color.white;
            badgeTmp.text = "HAFAL";

            // Highlight Border
            GameObject borderGo = CreateUIObject("SelectedBorder", itemGo.transform);
            SetStretch(borderGo.GetComponent<RectTransform>());
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.24f, 0.52f, 0.88f, 0.4f);
            borderGo.SetActive(false);

            // Wire SerializedObject on DictionaryFlashcardItemUI
            var itemSo = new SerializedObject(itemUi);
            itemSo.FindProperty("selectButton").objectReferenceValue = btn;
            itemSo.FindProperty("thumbnailImage").objectReferenceValue = thumbRaw;
            itemSo.FindProperty("titleText").objectReferenceValue = titleTmp;
            itemSo.FindProperty("translationText").objectReferenceValue = transTmp;
            itemSo.FindProperty("statusBadgeImage").objectReferenceValue = badgeImg;
            itemSo.FindProperty("statusBadgeText").objectReferenceValue = badgeTmp;
            itemSo.FindProperty("selectedHighlightBorder").objectReferenceValue = borderGo;
            itemSo.ApplyModifiedPropertiesWithoutUndo();

            return itemGo;
        }

        private static FlashcardFormModalUI CreateFlashcardModalDialog(
            Transform parent, 
            Sprite windowBg, 
            Sprite boxBg, 
            Sprite btnGreen, 
            Sprite btnGray, 
            TMP_FontAsset zenFont, 
            TMP_FontAsset nunitoFont)
        {
            GameObject modalGo = CreateUIObject("FlashcardFormModal", parent);
            RectTransform modalRt = modalGo.GetComponent<RectTransform>();
            SetStretch(modalRt);

            // Backdrop Dimmer
            var backdropImg = modalGo.AddComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.65f);
            var backdropBtn = modalGo.AddComponent<Button>();

            // Modal Box Container
            GameObject boxGo = CreateUIObject("ModalBox", modalGo.transform);
            RectTransform boxRt = boxGo.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(620f, 540f);
            boxRt.anchoredPosition = Vector2.zero;

            var boxImg = boxGo.AddComponent<Image>();
            boxImg.sprite = windowBg;
            boxImg.type = Image.Type.Sliced;
            boxImg.color = Color.white;

            var formUi = modalGo.AddComponent<FlashcardFormModalUI>();

            // Header Title
            GameObject titleGo = CreateUIObject("Title", boxGo.transform);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(560f, 40f);
            titleRt.anchoredPosition = new Vector2(0f, -20f);

            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.font = zenFont;
            titleTmp.fontSize = 28f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.18f, 0.14f, 0.28f, 1f);
            titleTmp.text = "Buat Flashcard Baru";

            // Dropdown Foto
            GameObject dropGo = CreateUIObject("PhotoDropdown", boxGo.transform);
            RectTransform dropRt = dropGo.GetComponent<RectTransform>();
            dropRt.anchorMin = new Vector2(0.5f, 1f);
            dropRt.anchorMax = new Vector2(0.5f, 1f);
            dropRt.pivot = new Vector2(0.5f, 1f);
            dropRt.sizeDelta = new Vector2(520f, 44f);
            dropRt.anchoredPosition = new Vector2(0f, -70f);

            var dropImg = dropGo.AddComponent<Image>();
            dropImg.sprite = boxBg;
            dropImg.type = Image.Type.Sliced;
            dropImg.color = Color.white;

            var dropdown = dropGo.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = dropImg;

            GameObject dropLabelGo = CreateUIObject("Label", dropGo.transform);
            SetStretch(dropLabelGo.GetComponent<RectTransform>());
            dropLabelGo.GetComponent<RectTransform>().offsetMin = new Vector2(16f, 0f);
            dropLabelGo.GetComponent<RectTransform>().offsetMax = new Vector2(-40f, 0f);
            var dropLabelTmp = dropLabelGo.AddComponent<TextMeshProUGUI>();
            dropLabelTmp.font = nunitoFont;
            dropLabelTmp.fontSize = 22f;
            dropLabelTmp.alignment = TextAlignmentOptions.Left;
            dropLabelTmp.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            dropdown.captionText = dropLabelTmp;

            // Preview Box (Foto + Info Kosakata)
            GameObject previewBoxGo = CreateUIObject("PreviewBox", boxGo.transform);
            RectTransform previewBoxRt = previewBoxGo.GetComponent<RectTransform>();
            previewBoxRt.anchorMin = new Vector2(0.5f, 1f);
            previewBoxRt.anchorMax = new Vector2(0.5f, 1f);
            previewBoxRt.pivot = new Vector2(0.5f, 1f);
            previewBoxRt.sizeDelta = new Vector2(520f, 160f);
            previewBoxRt.anchoredPosition = new Vector2(0f, -125f);

            var previewBoxImg = previewBoxGo.AddComponent<Image>();
            previewBoxImg.color = new Color(0f, 0f, 0f, 0.04f);

            // Preview RawImage
            GameObject rawGo = CreateUIObject("PreviewImage", previewBoxGo.transform);
            RectTransform rawRt = rawGo.GetComponent<RectTransform>();
            rawRt.anchorMin = new Vector2(0f, 0.5f);
            rawRt.anchorMax = new Vector2(0f, 0.5f);
            rawRt.pivot = new Vector2(0f, 0.5f);
            rawRt.sizeDelta = new Vector2(210f, 140f);
            rawRt.anchoredPosition = new Vector2(10f, 0f);
            var rawImg = rawGo.AddComponent<RawImage>();

            // Vocab Info Teks
            GameObject infoGo = CreateUIObject("VocabInfoText", previewBoxGo.transform);
            RectTransform infoRt = infoGo.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0f, 0f);
            infoRt.anchorMax = new Vector2(1f, 1f);
            infoRt.offsetMin = new Vector2(230f, 10f);
            infoRt.offsetMax = new Vector2(-10f, -10f);
            var infoTmp = infoGo.AddComponent<TextMeshProUGUI>();
            infoTmp.font = zenFont;
            infoTmp.fontSize = 22f;
            infoTmp.color = new Color(0.18f, 0.14f, 0.28f, 1f);
            infoTmp.text = "自転車 / じてんしゃ\n(jitensha)\nArtinya: Sepeda";

            // Input Field Catatan
            GameObject inputGo = CreateUIObject("NoteInputField", boxGo.transform);
            RectTransform inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0.5f, 1f);
            inputRt.anchorMax = new Vector2(0.5f, 1f);
            inputRt.pivot = new Vector2(0.5f, 1f);
            inputRt.sizeDelta = new Vector2(520f, 110f);
            inputRt.anchoredPosition = new Vector2(0f, -300f);

            var inputImg = inputGo.AddComponent<Image>();
            inputImg.sprite = boxBg;
            inputImg.type = Image.Type.Sliced;
            inputImg.color = Color.white;

            var inputField = inputGo.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;

            GameObject textAreaGo = CreateUIObject("TextArea", inputGo.transform);
            SetStretch(textAreaGo.GetComponent<RectTransform>());
            textAreaGo.GetComponent<RectTransform>().offsetMin = new Vector2(14f, 10f);
            textAreaGo.GetComponent<RectTransform>().offsetMax = new Vector2(-14f, -10f);

            GameObject inputTextGo = CreateUIObject("Text", textAreaGo.transform);
            SetStretch(inputTextGo.GetComponent<RectTransform>());
            var inputTextTmp = inputTextGo.AddComponent<TextMeshProUGUI>();
            inputTextTmp.font = nunitoFont;
            inputTextTmp.fontSize = 20f;
            inputTextTmp.color = new Color(0.18f, 0.14f, 0.28f, 1f);

            GameObject placeholderGo = CreateUIObject("Placeholder", textAreaGo.transform);
            SetStretch(placeholderGo.GetComponent<RectTransform>());
            var placeTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeTmp.font = nunitoFont;
            placeTmp.fontSize = 20f;
            placeTmp.fontStyle = FontStyles.Italic;
            placeTmp.color = new Color(0.6f, 0.58f, 0.65f, 1f);
            placeTmp.text = "Tulis catatan pribadi atau petunjuk mnemonik di sini...";

            inputField.textViewport = textAreaGo.GetComponent<RectTransform>();
            inputField.textComponent = inputTextTmp;
            inputField.placeholder = placeTmp;

            // Baris Tombol Aksi (Batal & Simpan)
            GameObject btnRowGo = CreateUIObject("ButtonRow", boxGo.transform);
            RectTransform btnRowRt = btnRowGo.GetComponent<RectTransform>();
            btnRowRt.anchorMin = new Vector2(0.5f, 0f);
            btnRowRt.anchorMax = new Vector2(0.5f, 0f);
            btnRowRt.pivot = new Vector2(0.5f, 0f);
            btnRowRt.sizeDelta = new Vector2(520f, 48f);
            btnRowRt.anchoredPosition = new Vector2(0f, 24f);

            var btnRowHlg = btnRowGo.AddComponent<HorizontalLayoutGroup>();
            btnRowHlg.spacing = 16f;
            btnRowHlg.childControlWidth = true;
            btnRowHlg.childControlHeight = true;
            btnRowHlg.childForceExpandWidth = true;

            var cancelBtn = CreateActionButton("BtnCancel", btnRowGo.transform, "Batal", btnGray, new Color(0.48f, 0.44f, 0.58f, 1f), zenFont);
            var saveBtn = CreateActionButton("BtnSave", btnRowGo.transform, "Simpan Kartu", btnGreen, new Color(0.18f, 0.68f, 0.38f, 1f), zenFont);

            // Wire Serialized Properties on FlashcardFormModalUI
            var modalSo = new SerializedObject(formUi);
            modalSo.FindProperty("modalRoot").objectReferenceValue = modalGo;
            modalSo.FindProperty("modalTitleText").objectReferenceValue = titleTmp;
            modalSo.FindProperty("photoDropdown").objectReferenceValue = dropdown;
            modalSo.FindProperty("photoPreviewImage").objectReferenceValue = rawImg;
            modalSo.FindProperty("vocabInfoText").objectReferenceValue = infoTmp;
            modalSo.FindProperty("noteInputField").objectReferenceValue = inputField;
            modalSo.FindProperty("saveButton").objectReferenceValue = saveBtn;
            modalSo.FindProperty("cancelButton").objectReferenceValue = cancelBtn;
            modalSo.FindProperty("backdropCloseButton").objectReferenceValue = backdropBtn;
            modalSo.FindProperty("saveButtonText").objectReferenceValue = saveBtn.GetComponentInChildren<TextMeshProUGUI>();
            modalSo.ApplyModifiedPropertiesWithoutUndo();

            modalGo.SetActive(false); // Mulai dalam keadaan tersembunyi
            return formUi;
        }

        private static (Button, TextMeshProUGUI) CreateTabFilterButton(string name, Transform parent, string label, Sprite bgSprite, TMP_FontAsset font)
        {
            GameObject go = CreateUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject textGo = CreateUIObject("Text", go.transform);
            SetStretch(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 19f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.32f, 0.28f, 0.45f, 1f);
            tmp.text = label;

            return (btn, tmp);
        }

        private static Button CreateActionButton(string name, Transform parent, string label, Sprite bgSprite, Color color, TMP_FontAsset font)
        {
            GameObject go = CreateUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            img.color = color;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject textGo = CreateUIObject("Text", go.transform);
            SetStretch(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.text = label;

            return btn;
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
            titleTmp.fontSize = 36f;
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
            subRt.anchoredPosition = new Vector2(0f, -88f);

            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.font = nunitoFont;
            subTmp.fontSize = 20f;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.48f, 0.44f, 0.58f, 1f);
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
