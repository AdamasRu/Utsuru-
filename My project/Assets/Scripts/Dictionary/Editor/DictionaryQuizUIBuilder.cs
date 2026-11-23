using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI.Editor
{
    public static class DictionaryQuizUIBuilder
    {
        [MenuItem("Tools/Utsuru/Build Dictionary Quiz UI")]
        public static void BuildQuizUI()
        {
            // 1. Muat Aset Font
            var zenFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ZenMaruGothic-Bold SDF.asset");
            var nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");

            // 2. Muat Aset Sprite
            var cardChoice1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Card_Choice1_Bg.png");
            var cardChoice2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Card_Choice2_Bg.png");
            var btnSelectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Button_Unselected.png");
            var pillBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Category Pill.png");
            var cardBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Card Background.png");
            var tapeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dictionary/Polaroid/Tape_Washi.png");

            // 3. Cari BookSpread di scene
            var bookSpread = Object.FindFirstObjectByType<DictionaryBookSpreadUI>(FindObjectsInactive.Include);
            if (bookSpread == null)
            {
                Debug.LogError("[DictionaryQuizUIBuilder] DictionaryBookSpreadUI tidak ditemukan di scene!");
                return;
            }

            Undo.RegisterCompleteObjectUndo(bookSpread.gameObject, "Build Dictionary Quiz UI");
            Transform spreadTrans = bookSpread.transform;

            // Bersihkan QuizSpreadRoot lama jika ada
            Transform oldQuiz = spreadTrans.Find("QuizSpreadRoot");
            if (oldQuiz != null) Object.DestroyImmediate(oldQuiz.gameObject);

            GameObject quizRoot = CreateUIObject("QuizSpreadRoot", spreadTrans);
            RectTransform quizRt = quizRoot.GetComponent<RectTransform>();
            SetStretch(quizRt);

            var quizUI = quizRoot.AddComponent<DictionaryQuizSpreadUI>();

            // ==========================================
            // SUB-ROOT 1: ACTIVE QUIZ ROOT
            // ==========================================
            GameObject activeQuizRoot = CreateUIObject("ActiveQuizRoot", quizRoot.transform);
            SetStretch(activeQuizRoot.GetComponent<RectTransform>());

            // --- A. HALAMAN KIRI (PERTANYAAN & FOTO SNAPSHOT) ---
            GameObject leftQuizPage = CreateUIObject("LeftQuizPage", activeQuizRoot.transform);
            RectTransform leftQuizRt = leftQuizPage.GetComponent<RectTransform>();
            SetPageTransform(leftQuizRt, isLeft: true);
            leftQuizRt.anchoredPosition = new Vector2(56f, 20f);
            leftQuizRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            CreatePageHeader(leftQuizPage.transform, "QUIZ CHALLENGE", "Test your knowledge on discovered vocabulary", zenFont, nunitoFont);

            // Baris Status (Progress & Skor)
            GameObject statusRow = CreateUIObject("StatusRow", leftQuizPage.transform);
            RectTransform statusRowRt = statusRow.GetComponent<RectTransform>();
            statusRowRt.anchorMin = new Vector2(0.5f, 1f);
            statusRowRt.anchorMax = new Vector2(0.5f, 1f);
            statusRowRt.pivot = new Vector2(0.5f, 1f);
            statusRowRt.sizeDelta = new Vector2(680f, 44f);
            statusRowRt.anchoredPosition = new Vector2(0f, -138f);

            var statusHlg = statusRow.AddComponent<HorizontalLayoutGroup>();
            statusHlg.spacing = 16f;
            statusHlg.childControlWidth = true;
            statusHlg.childControlHeight = true;
            statusHlg.childForceExpandWidth = true;
            statusHlg.childForceExpandHeight = true;

            var progressBadgeText = CreateStatusBadge("ProgressBadge", statusRow.transform, "Question 1 / 5", pillBgSprite, new Color(0.88f, 0.85f, 0.96f, 1f), new Color(0.28f, 0.22f, 0.55f, 1f), zenFont, 24f);
            var scoreBadgeText = CreateStatusBadge("ScoreBadge", statusRow.transform, "Score: 0", pillBgSprite, new Color(0.85f, 0.94f, 0.88f, 1f), new Color(0.18f, 0.52f, 0.32f, 1f), zenFont, 24f);

            // Kotak Kartu Pertanyaan
            GameObject questionCardGo = CreateUIObject("QuestionCard", leftQuizPage.transform);
            RectTransform qCardRt = questionCardGo.GetComponent<RectTransform>();
            qCardRt.anchorMin = new Vector2(0.5f, 1f);
            qCardRt.anchorMax = new Vector2(0.5f, 1f);
            qCardRt.pivot = new Vector2(0.5f, 1f);
            qCardRt.sizeDelta = new Vector2(680f, 550f);
            qCardRt.anchoredPosition = new Vector2(0f, -195f);

            var qCardImg = questionCardGo.AddComponent<Image>();
            qCardImg.sprite = cardChoice2Sprite != null ? cardChoice2Sprite : btnSelectSprite;
            qCardImg.type = Image.Type.Sliced;
            qCardImg.color = Color.white;

            var qCardVlg = questionCardGo.AddComponent<VerticalLayoutGroup>();
            qCardVlg.padding = new RectOffset(24, 24, 20, 20);
            qCardVlg.spacing = 14f;
            qCardVlg.childControlWidth = true;
            qCardVlg.childControlHeight = false;
            qCardVlg.childForceExpandWidth = true;
            qCardVlg.childForceExpandHeight = false;
            qCardVlg.childAlignment = TextAnchor.MiddleCenter;

            // Foto Frame Polaroid untuk foto pemain
            GameObject photoFrameGo = CreateUIObject("PhotoFrameContainer", questionCardGo.transform);
            RectTransform photoFrameRt = photoFrameGo.GetComponent<RectTransform>();
            photoFrameRt.sizeDelta = new Vector2(380f, 255f);
            var photoFrameLe = photoFrameGo.AddComponent<LayoutElement>();
            photoFrameLe.preferredWidth = 380f;
            photoFrameLe.preferredHeight = 255f;

            var photoFrameImg = photoFrameGo.AddComponent<Image>();
            photoFrameImg.sprite = cardBgSprite != null ? cardBgSprite : cardChoice1Sprite;
            photoFrameImg.type = Image.Type.Sliced;
            photoFrameImg.color = new Color(0.96f, 0.95f, 0.98f, 1f);

            // Tape di atas foto
            if (tapeSprite != null)
            {
                GameObject tapeGo = CreateUIObject("Tape", photoFrameGo.transform);
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
            GameObject rawImgGo = CreateUIObject("PhotoRawImage", photoFrameGo.transform);
            RectTransform rawImgRt = rawImgGo.GetComponent<RectTransform>();
            rawImgRt.anchorMin = new Vector2(0.5f, 0.5f);
            rawImgRt.anchorMax = new Vector2(0.5f, 0.5f);
            rawImgRt.pivot = new Vector2(0.5f, 0.5f);
            rawImgRt.sizeDelta = new Vector2(345f, 215f);
            rawImgRt.anchoredPosition = new Vector2(0f, -6f);

            var photoRaw = rawImgGo.AddComponent<RawImage>();
            photoRaw.color = Color.white;
            photoRaw.raycastTarget = false;

            // Prompt Label ("What is the Japanese word for:")
            GameObject promptGo = CreateUIObject("PromptLabel", questionCardGo.transform);
            var promptTmp = promptGo.AddComponent<TextMeshProUGUI>();
            promptTmp.font = nunitoFont;
            promptTmp.fontSize = 24f;
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.color = new Color(0.48f, 0.44f, 0.58f, 1f);
            promptTmp.text = "What is the Japanese word for:";
            var promptLe = promptGo.AddComponent<LayoutElement>();
            promptLe.preferredHeight = 30f;

            // Target Word ("Fence" / "Bicycle") - HIGHLIGHT: 38f
            GameObject targetWordGo = CreateUIObject("TargetWord", questionCardGo.transform);
            var targetWordTmp = targetWordGo.AddComponent<TextMeshProUGUI>();
            targetWordTmp.font = zenFont;
            targetWordTmp.fontSize = 38f;
            targetWordTmp.fontStyle = FontStyles.Bold;
            targetWordTmp.alignment = TextAlignmentOptions.Center;
            targetWordTmp.color = new Color(0.18f, 0.14f, 0.28f, 1f);
            targetWordTmp.text = "Fence";
            var targetWordLe = targetWordGo.AddComponent<LayoutElement>();
            targetWordLe.preferredHeight = 48f;

            // Category Badge
            GameObject catBadgeGo = CreateUIObject("CategoryBadge", questionCardGo.transform);
            var catBadgeLe = catBadgeGo.AddComponent<LayoutElement>();
            catBadgeLe.preferredWidth = 260f;
            catBadgeLe.preferredHeight = 38f;

            var catBadgeImg = catBadgeGo.AddComponent<Image>();
            catBadgeImg.sprite = pillBgSprite;
            catBadgeImg.type = Image.Type.Sliced;
            catBadgeImg.color = new Color(0.92f, 0.90f, 0.96f, 0.9f);

            GameObject catTextGo = CreateUIObject("Text", catBadgeGo.transform);
            SetStretch(catTextGo.GetComponent<RectTransform>());
            var catBadgeTmp = catTextGo.AddComponent<TextMeshProUGUI>();
            catBadgeTmp.font = nunitoFont;
            catBadgeTmp.fontSize = 24f;
            catBadgeTmp.fontStyle = FontStyles.Bold;
            catBadgeTmp.alignment = TextAlignmentOptions.Center;
            catBadgeTmp.color = new Color(0.42f, 0.36f, 0.60f, 1f);
            catBadgeTmp.text = "Category: Transport";

            // --- B. HALAMAN KANAN (4 PILIHAN JAWABAN & FEEDBACK) ---
            GameObject rightQuizPage = CreateUIObject("RightQuizPage", activeQuizRoot.transform);
            RectTransform rightQuizRt = rightQuizPage.GetComponent<RectTransform>();
            SetPageTransform(rightQuizRt, isLeft: false);
            rightQuizRt.anchoredPosition = new Vector2(-770f, 20f);
            rightQuizRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            CreatePageHeader(rightQuizPage.transform, "SELECT ANSWER", "Choose the matching Japanese expression", zenFont, nunitoFont);

            // Choices Container (Memuat 4 Kartu Pilihan Jawaban Horizontal yang Rapi)
            GameObject choicesContainerGo = CreateUIObject("ChoicesContainer", rightQuizPage.transform);
            RectTransform choicesRt = choicesContainerGo.GetComponent<RectTransform>();
            choicesRt.anchorMin = new Vector2(0.5f, 1f);
            choicesRt.anchorMax = new Vector2(0.5f, 1f);
            choicesRt.pivot = new Vector2(0.5f, 1f);
            choicesRt.sizeDelta = new Vector2(680f, 386f);
            choicesRt.anchoredPosition = new Vector2(0f, -140f);

            var choicesVlg = choicesContainerGo.AddComponent<VerticalLayoutGroup>();
            choicesVlg.spacing = 10f;
            choicesVlg.childControlWidth = true;
            choicesVlg.childControlHeight = true;
            choicesVlg.childForceExpandWidth = true;
            choicesVlg.childForceExpandHeight = false;

            Button[] choiceButtons = new Button[4];
            Image[] choiceBackgrounds = new Image[4];
            Image[] choiceBadgeImages = new Image[4];
            TextMeshProUGUI[] choiceLetters = new TextMeshProUGUI[4];
            TextMeshProUGUI[] choiceJpTexts = new TextMeshProUGUI[4];
            TextMeshProUGUI[] choiceRomajiTexts = new TextMeshProUGUI[4];

            string[] optionLetters = new string[] { "A", "B", "C", "D" };

            for (int i = 0; i < 4; i++)
            {
                GameObject btnGo = CreateUIObject($"Btn_Choice_{i}", choicesContainerGo.transform);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.sprite = cardChoice2Sprite != null ? cardChoice2Sprite : btnSelectSprite;
                btnImg.type = Image.Type.Sliced;
                btnImg.color = new Color(0.99f, 0.99f, 1f, 0.98f);

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                choiceButtons[i] = btn;
                choiceBackgrounds[i] = btnImg;

                var btnLe = btnGo.AddComponent<LayoutElement>();
                btnLe.preferredHeight = 88f;
                btnLe.minHeight = 88f;
                btnLe.flexibleWidth = 1f;

                // HorizontalLayoutGroup di dalam kartu agar badge dan teks berjejer rapi
                var btnHlg = btnGo.AddComponent<HorizontalLayoutGroup>();
                btnHlg.padding = new RectOffset(16, 20, 12, 12);
                btnHlg.spacing = 16f;
                btnHlg.childAlignment = TextAnchor.MiddleLeft;
                btnHlg.childControlWidth = true;
                btnHlg.childControlHeight = true;
                btnHlg.childForceExpandWidth = false;
                btnHlg.childForceExpandHeight = false;

                // Letter Badge ("A", "B", "C", "D")
                GameObject letterGo = CreateUIObject("LetterBadge", btnGo.transform);
                var letterLe = letterGo.AddComponent<LayoutElement>();
                letterLe.preferredWidth = 46f;
                letterLe.preferredHeight = 46f;
                letterLe.minWidth = 46f;
                letterLe.minHeight = 46f;
                letterLe.flexibleWidth = 0f;
                letterLe.flexibleHeight = 0f;

                var letterImg = letterGo.AddComponent<Image>();
                letterImg.sprite = pillBgSprite;
                letterImg.type = Image.Type.Sliced;
                letterImg.color = new Color(0.38f, 0.32f, 0.65f, 1f);
                choiceBadgeImages[i] = letterImg;

                GameObject letterTextGo = CreateUIObject("Text", letterGo.transform);
                SetStretch(letterTextGo.GetComponent<RectTransform>());
                var letterTmp = letterTextGo.AddComponent<TextMeshProUGUI>();
                letterTmp.font = zenFont;
                letterTmp.fontSize = 24f;
                letterTmp.fontStyle = FontStyles.Bold;
                letterTmp.alignment = TextAlignmentOptions.Center;
                letterTmp.color = Color.white;
                letterTmp.text = optionLetters[i];
                choiceLetters[i] = letterTmp;

                // Text Column (Mengisi 100% sisa lebar kartu, Japanese di atas + Romaji di bawah)
                GameObject textColGo = CreateUIObject("TextCol", btnGo.transform);
                var textColLe = textColGo.AddComponent<LayoutElement>();
                textColLe.flexibleWidth = 1f;
                textColLe.minHeight = 58f;

                var textColVlg = textColGo.AddComponent<VerticalLayoutGroup>();
                textColVlg.spacing = 1f;
                textColVlg.childAlignment = TextAnchor.MiddleLeft;
                textColVlg.childControlWidth = true;
                textColVlg.childControlHeight = false;
                textColVlg.childForceExpandWidth = true;
                textColVlg.childForceExpandHeight = false;

                // Japanese Text (ZenMaruGothic 28f - 30f, auto-sizing bila frasa panjang)
                GameObject jpGo = CreateUIObject("JapaneseText", textColGo.transform);
                var jpLe = jpGo.AddComponent<LayoutElement>();
                jpLe.preferredHeight = 34f;

                var jpTmp = jpGo.AddComponent<TextMeshProUGUI>();
                jpTmp.font = zenFont;
                jpTmp.fontSize = 28f;
                jpTmp.enableAutoSizing = true;
                jpTmp.fontSizeMin = 24f;
                jpTmp.fontSizeMax = 28f;
                jpTmp.fontStyle = FontStyles.Bold;
                jpTmp.alignment = TextAlignmentOptions.MidlineLeft;
                jpTmp.color = new Color(0.18f, 0.14f, 0.26f, 1f);
                jpTmp.textWrappingMode = TextWrappingModes.Normal;
                jpTmp.text = "<b>自転車</b> <size=82%>(じてんしゃ)</size>";
                choiceJpTexts[i] = jpTmp;

                // Romaji Text (Nunito 22f - rapi dan proporsional)
                GameObject romajiGo = CreateUIObject("RomajiText", textColGo.transform);
                var romajiLe = romajiGo.AddComponent<LayoutElement>();
                romajiLe.preferredHeight = 22f;

                var romajiTmp = romajiGo.AddComponent<TextMeshProUGUI>();
                romajiTmp.font = nunitoFont;
                romajiTmp.fontSize = 22f;
                romajiTmp.alignment = TextAlignmentOptions.MidlineLeft;
                romajiTmp.color = new Color(0.48f, 0.44f, 0.58f, 1f);
                romajiTmp.textWrappingMode = TextWrappingModes.Normal;
                romajiTmp.text = "jitensha";
                choiceRomajiTexts[i] = romajiTmp;
            }

            // Feedback Area (Muncul setelah memilih jawaban)
            GameObject feedbackGo = CreateUIObject("FeedbackContainer", rightQuizPage.transform);
            RectTransform feedbackRt = feedbackGo.GetComponent<RectTransform>();
            feedbackRt.anchorMin = new Vector2(0.5f, 1f);
            feedbackRt.anchorMax = new Vector2(0.5f, 1f);
            feedbackRt.pivot = new Vector2(0.5f, 1f);
            feedbackRt.sizeDelta = new Vector2(680f, 150f);
            feedbackRt.anchoredPosition = new Vector2(0f, -536f);

            var fbImg = feedbackGo.AddComponent<Image>();
            fbImg.sprite = cardChoice1Sprite != null ? cardChoice1Sprite : cardChoice2Sprite;
            fbImg.type = Image.Type.Sliced;
            fbImg.color = new Color(0.98f, 0.98f, 1f, 0.98f);

            var fbVlg = feedbackGo.AddComponent<VerticalLayoutGroup>();
            fbVlg.padding = new RectOffset(18, 18, 10, 10);
            fbVlg.spacing = 6f;
            fbVlg.childControlWidth = true;
            fbVlg.childControlHeight = false;
            fbVlg.childForceExpandWidth = true;
            fbVlg.childForceExpandHeight = false;

            // Feedback Banner (Pill hijau/merah)
            GameObject fbBannerGo = CreateUIObject("Banner", feedbackGo.transform);
            var fbBannerLe = fbBannerGo.AddComponent<LayoutElement>();
            fbBannerLe.preferredHeight = 38f;

            var fbBannerImg = fbBannerGo.AddComponent<Image>();
            fbBannerImg.sprite = pillBgSprite;
            fbBannerImg.type = Image.Type.Sliced;
            fbBannerImg.color = new Color(0.18f, 0.68f, 0.38f, 1f);

            GameObject fbBannerTextGo = CreateUIObject("Text", fbBannerGo.transform);
            SetStretch(fbBannerTextGo.GetComponent<RectTransform>());
            var fbBannerTmp = fbBannerTextGo.AddComponent<TextMeshProUGUI>();
            fbBannerTmp.font = zenFont;
            fbBannerTmp.fontSize = 24f;
            fbBannerTmp.fontStyle = FontStyles.Bold;
            fbBannerTmp.alignment = TextAlignmentOptions.Center;
            fbBannerTmp.color = Color.white;
            fbBannerTmp.text = "CORRECT! (正解)";

            // Example Sentence Text (ZenMaruGothic 24f)
            GameObject fbSentGo = CreateUIObject("SentenceText", feedbackGo.transform);
            var fbSentTmp = fbSentGo.AddComponent<TextMeshProUGUI>();
            fbSentTmp.font = zenFont;
            fbSentTmp.fontSize = 24f;
            fbSentTmp.color = new Color(0.24f, 0.20f, 0.32f, 1f);
            fbSentTmp.textWrappingMode = TextWrappingModes.Normal;
            fbSentTmp.text = "「毎朝、自転車で学校に行きます。」\nI go to school by bicycle every morning.";
            var fbSentLe = fbSentGo.AddComponent<LayoutElement>();
            fbSentLe.preferredHeight = 86f;

            feedbackGo.SetActive(false);

            // Tombol Next Question (Kanan Bawah)
            GameObject nextBtnGo = CreateUIObject("NextQuestionButton", rightQuizPage.transform);
            RectTransform nextBtnRt = nextBtnGo.GetComponent<RectTransform>();
            nextBtnRt.anchorMin = new Vector2(0.5f, 1f);
            nextBtnRt.anchorMax = new Vector2(0.5f, 1f);
            nextBtnRt.pivot = new Vector2(0.5f, 1f);
            nextBtnRt.sizeDelta = new Vector2(680f, 54f);
            nextBtnRt.anchoredPosition = new Vector2(0f, -700f);

            var nextBtnImg = nextBtnGo.AddComponent<Image>();
            nextBtnImg.sprite = pillBgSprite;
            nextBtnImg.type = Image.Type.Sliced;
            nextBtnImg.color = new Color(0.38f, 0.32f, 0.65f, 1f); // Royal purple

            var nextBtn = nextBtnGo.AddComponent<Button>();
            nextBtn.targetGraphic = nextBtnImg;

            GameObject nextTextGo = CreateUIObject("Text", nextBtnGo.transform);
            SetStretch(nextTextGo.GetComponent<RectTransform>());
            var nextTmp = nextTextGo.AddComponent<TextMeshProUGUI>();
            nextTmp.font = zenFont;
            nextTmp.fontSize = 26f;
            nextTmp.fontStyle = FontStyles.Bold;
            nextTmp.alignment = TextAlignmentOptions.Center;
            nextTmp.color = Color.white;
            nextTmp.text = "Next Question >";

            nextBtnGo.SetActive(false);

            // ==========================================
            // SUB-ROOT 2: EMPTY / LOCKED STATE
            // ==========================================
            GameObject emptyRoot = CreateUIObject("EmptyStateRoot", quizRoot.transform);
            SetStretch(emptyRoot.GetComponent<RectTransform>());

            GameObject emptyCardGo = CreateUIObject("EmptyCard", emptyRoot.transform);
            RectTransform emptyCardRt = emptyCardGo.GetComponent<RectTransform>();
            emptyCardRt.anchorMin = new Vector2(0.5f, 0.5f);
            emptyCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            emptyCardRt.pivot = new Vector2(0.5f, 0.5f);
            emptyCardRt.sizeDelta = new Vector2(800f, 440f);
            emptyCardRt.anchoredPosition = new Vector2(0f, 0f);

            var emptyCardImg = emptyCardGo.AddComponent<Image>();
            emptyCardImg.sprite = cardChoice2Sprite != null ? cardChoice2Sprite : btnSelectSprite;
            emptyCardImg.type = Image.Type.Sliced;
            emptyCardImg.color = Color.white;

            var emptyCardVlg = emptyCardGo.AddComponent<VerticalLayoutGroup>();
            emptyCardVlg.padding = new RectOffset(40, 40, 35, 35);
            emptyCardVlg.spacing = 18f;
            emptyCardVlg.childControlWidth = true;
            emptyCardVlg.childControlHeight = false;
            emptyCardVlg.childForceExpandWidth = true;
            emptyCardVlg.childForceExpandHeight = false;
            emptyCardVlg.childAlignment = TextAnchor.MiddleCenter;

            // Lock Icon / Badge
            GameObject lockBadgeGo = CreateUIObject("LockBadge", emptyCardGo.transform);
            var lockLe = lockBadgeGo.AddComponent<LayoutElement>();
            lockLe.preferredWidth = 240f;
            lockLe.preferredHeight = 44f;

            var lockImg = lockBadgeGo.AddComponent<Image>();
            lockImg.sprite = pillBgSprite;
            lockImg.type = Image.Type.Sliced;
            lockImg.color = new Color(0.85f, 0.40f, 0.45f, 1f);

            GameObject lockTextGo = CreateUIObject("Text", lockBadgeGo.transform);
            SetStretch(lockTextGo.GetComponent<RectTransform>());
            var lockTmp = lockTextGo.AddComponent<TextMeshProUGUI>();
            lockTmp.font = zenFont;
            lockTmp.fontSize = 24f;
            lockTmp.fontStyle = FontStyles.Bold;
            lockTmp.alignment = TextAlignmentOptions.Center;
            lockTmp.color = Color.white;
            lockTmp.text = "QUIZ LOCKED";

            // Title
            GameObject emptyTitleGo = CreateUIObject("Title", emptyCardGo.transform);
            var emptyTitleTmp = emptyTitleGo.AddComponent<TextMeshProUGUI>();
            emptyTitleTmp.font = zenFont;
            emptyTitleTmp.fontSize = 34f;
            emptyTitleTmp.fontStyle = FontStyles.Bold;
            emptyTitleTmp.alignment = TextAlignmentOptions.Center;
            emptyTitleTmp.color = new Color(0.20f, 0.16f, 0.28f, 1f);
            emptyTitleTmp.text = "Discover More Vocabulary to Unlock Quiz";

            // Description
            GameObject emptyDescGo = CreateUIObject("Description", emptyCardGo.transform);
            var emptyDescTmp = emptyDescGo.AddComponent<TextMeshProUGUI>();
            emptyDescTmp.font = nunitoFont;
            emptyDescTmp.fontSize = 26f;
            emptyDescTmp.alignment = TextAlignmentOptions.Center;
            emptyDescTmp.color = new Color(0.45f, 0.42f, 0.55f, 1f);
            emptyDescTmp.textWrappingMode = TextWrappingModes.Normal;
            emptyDescTmp.text = "Explore the town using Camera Mode (C) and snap photos of street objects to build your quiz deck!";

            // Progress Text
            GameObject emptyProgGo = CreateUIObject("Progress", emptyCardGo.transform);
            var emptyProgTmp = emptyProgGo.AddComponent<TextMeshProUGUI>();
            emptyProgTmp.font = nunitoFont;
            emptyProgTmp.fontSize = 24f;
            emptyProgTmp.fontStyle = FontStyles.Bold;
            emptyProgTmp.alignment = TextAlignmentOptions.Center;
            emptyProgTmp.color = new Color(0.38f, 0.32f, 0.65f, 1f);
            emptyProgTmp.text = "Discovered: 0 / 4 Required Vocabulary";

            // Quick Practice Button (memungkinkan mencoba kuis dari seluruh katalog database)
            GameObject demoBtnGo = CreateUIObject("DemoPracticeButton", emptyCardGo.transform);
            var demoBtnLe = demoBtnGo.AddComponent<LayoutElement>();
            demoBtnLe.preferredWidth = 380f;
            demoBtnLe.preferredHeight = 56f;

            var demoBtnImg = demoBtnGo.AddComponent<Image>();
            demoBtnImg.sprite = pillBgSprite;
            demoBtnImg.type = Image.Type.Sliced;
            demoBtnImg.color = new Color(0.38f, 0.32f, 0.65f, 1f);

            var demoBtn = demoBtnGo.AddComponent<Button>();
            demoBtn.targetGraphic = demoBtnImg;

            GameObject demoTextGo = CreateUIObject("Text", demoBtnGo.transform);
            SetStretch(demoTextGo.GetComponent<RectTransform>());
            var demoTmp = demoTextGo.AddComponent<TextMeshProUGUI>();
            demoTmp.font = zenFont;
            demoTmp.fontSize = 24f;
            demoTmp.fontStyle = FontStyles.Bold;
            demoTmp.alignment = TextAlignmentOptions.Center;
            demoTmp.color = Color.white;
            demoTmp.text = "Quick Practice (Use Full Catalog) >";

            emptyRoot.SetActive(false);

            // ==========================================
            // SUB-ROOT 3: RESULT SUMMARY ROOT
            // ==========================================
            GameObject resultRoot = CreateUIObject("ResultSummaryRoot", quizRoot.transform);
            SetStretch(resultRoot.GetComponent<RectTransform>());

            GameObject resultCardGo = CreateUIObject("ResultCard", resultRoot.transform);
            RectTransform resultCardRt = resultCardGo.GetComponent<RectTransform>();
            resultCardRt.anchorMin = new Vector2(0.5f, 0.5f);
            resultCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            resultCardRt.pivot = new Vector2(0.5f, 0.5f);
            resultCardRt.sizeDelta = new Vector2(800f, 480f);
            resultCardRt.anchoredPosition = new Vector2(0f, 0f);

            var resultCardImg = resultCardGo.AddComponent<Image>();
            resultCardImg.sprite = cardChoice2Sprite != null ? cardChoice2Sprite : btnSelectSprite;
            resultCardImg.type = Image.Type.Sliced;
            resultCardImg.color = Color.white;

            var resultCardVlg = resultCardGo.AddComponent<VerticalLayoutGroup>();
            resultCardVlg.padding = new RectOffset(40, 40, 35, 35);
            resultCardVlg.spacing = 16f;
            resultCardVlg.childControlWidth = true;
            resultCardVlg.childControlHeight = false;
            resultCardVlg.childForceExpandWidth = true;
            resultCardVlg.childForceExpandHeight = false;
            resultCardVlg.childAlignment = TextAnchor.MiddleCenter;

            // Result Badge
            GameObject resBadgeGo = CreateUIObject("Badge", resultCardGo.transform);
            var resBadgeLe = resBadgeGo.AddComponent<LayoutElement>();
            resBadgeLe.preferredWidth = 260f;
            resBadgeLe.preferredHeight = 44f;

            var resBadgeImg = resBadgeGo.AddComponent<Image>();
            resBadgeImg.sprite = pillBgSprite;
            resBadgeImg.type = Image.Type.Sliced;
            resBadgeImg.color = new Color(0.18f, 0.72f, 0.42f, 1f);

            GameObject resBadgeTextGo = CreateUIObject("Text", resBadgeGo.transform);
            SetStretch(resBadgeTextGo.GetComponent<RectTransform>());
            var resBadgeTmp = resBadgeTextGo.AddComponent<TextMeshProUGUI>();
            resBadgeTmp.font = zenFont;
            resBadgeTmp.fontSize = 24f;
            resBadgeTmp.fontStyle = FontStyles.Bold;
            resBadgeTmp.alignment = TextAlignmentOptions.Center;
            resBadgeTmp.color = Color.white;
            resBadgeTmp.text = "QUIZ COMPLETED!";

            // Title Rating
            GameObject resTitleGo = CreateUIObject("Title", resultCardGo.transform);
            var resTitleTmp = resTitleGo.AddComponent<TextMeshProUGUI>();
            resTitleTmp.font = zenFont;
            resTitleTmp.fontSize = 36f;
            resTitleTmp.fontStyle = FontStyles.Bold;
            resTitleTmp.alignment = TextAlignmentOptions.Center;
            resTitleTmp.color = new Color(0.18f, 0.14f, 0.28f, 1f);
            resTitleTmp.text = "Great Job! / よくできました！";

            // Percentage
            GameObject resPercentGo = CreateUIObject("Percentage", resultCardGo.transform);
            var resPercentTmp = resPercentGo.AddComponent<TextMeshProUGUI>();
            resPercentTmp.font = zenFont;
            resPercentTmp.fontSize = 54f;
            resPercentTmp.fontStyle = FontStyles.Bold;
            resPercentTmp.alignment = TextAlignmentOptions.Center;
            resPercentTmp.color = new Color(0.38f, 0.32f, 0.65f, 1f);
            resPercentTmp.text = "80%";

            // Score Detail
            GameObject resScoreGo = CreateUIObject("ScoreDetail", resultCardGo.transform);
            var resScoreTmp = resScoreGo.AddComponent<TextMeshProUGUI>();
            resScoreTmp.font = nunitoFont;
            resScoreTmp.fontSize = 26f;
            resScoreTmp.alignment = TextAlignmentOptions.Center;
            resScoreTmp.color = new Color(0.45f, 0.40f, 0.55f, 1f);
            resScoreTmp.text = "Score: 4 / 5 Correct";

            // Points Reward Text
            GameObject resRewardGo = CreateUIObject("PointsReward", resultCardGo.transform);
            var resRewardTmp = resRewardGo.AddComponent<TextMeshProUGUI>();
            resRewardTmp.font = zenFont;
            resRewardTmp.fontSize = 28f;
            resRewardTmp.fontStyle = FontStyles.Bold;
            resRewardTmp.alignment = TextAlignmentOptions.Center;
            resRewardTmp.color = new Color(0.18f, 0.68f, 0.38f, 1f);
            resRewardTmp.text = "+40 Points Awarded!";

            // Play Again Button
            GameObject playAgainBtnGo = CreateUIObject("PlayAgainButton", resultCardGo.transform);
            var playAgainBtnLe = playAgainBtnGo.AddComponent<LayoutElement>();
            playAgainBtnLe.preferredWidth = 320f;
            playAgainBtnLe.preferredHeight = 56f;

            var playAgainBtnImg = playAgainBtnGo.AddComponent<Image>();
            playAgainBtnImg.sprite = pillBgSprite;
            playAgainBtnImg.type = Image.Type.Sliced;
            playAgainBtnImg.color = new Color(0.38f, 0.32f, 0.65f, 1f);

            var playAgainBtn = playAgainBtnGo.AddComponent<Button>();
            playAgainBtn.targetGraphic = playAgainBtnImg;

            GameObject playAgainTextGo = CreateUIObject("Text", playAgainBtnGo.transform);
            SetStretch(playAgainTextGo.GetComponent<RectTransform>());
            var playAgainTmp = playAgainTextGo.AddComponent<TextMeshProUGUI>();
            playAgainTmp.font = zenFont;
            playAgainTmp.fontSize = 26f;
            playAgainTmp.fontStyle = FontStyles.Bold;
            playAgainTmp.alignment = TextAlignmentOptions.Center;
            playAgainTmp.color = Color.white;
            playAgainTmp.text = "Try Another Quiz >";

            resultRoot.SetActive(false);

            // ==========================================
            // WIRE SERIALIZED FIELDS ON DictionaryQuizSpreadUI
            // ==========================================
            var quizSo = new SerializedObject(quizUI);
            quizSo.FindProperty("dictionaryManager").objectReferenceValue = Object.FindFirstObjectByType<DictionaryManager>();
            quizSo.FindProperty("pointRewardSystem").objectReferenceValue = Object.FindFirstObjectByType<PointRewardSystem>();
            quizSo.FindProperty("bookSpread").objectReferenceValue = bookSpread;

            quizSo.FindProperty("activeQuizRoot").objectReferenceValue = activeQuizRoot;
            quizSo.FindProperty("emptyStateRoot").objectReferenceValue = emptyRoot;
            quizSo.FindProperty("resultSummaryRoot").objectReferenceValue = resultRoot;

            quizSo.FindProperty("progressBadgeText").objectReferenceValue = progressBadgeText;
            quizSo.FindProperty("scoreBadgeText").objectReferenceValue = scoreBadgeText;
            quizSo.FindProperty("photoPreviewImage").objectReferenceValue = photoRaw;
            quizSo.FindProperty("photoFrameContainer").objectReferenceValue = photoFrameGo;
            quizSo.FindProperty("promptLabel").objectReferenceValue = promptTmp;
            quizSo.FindProperty("questionTargetWordText").objectReferenceValue = targetWordTmp;
            quizSo.FindProperty("categoryBadgeText").objectReferenceValue = catBadgeTmp;

            var btnProp = quizSo.FindProperty("choiceButtons");
            btnProp.arraySize = 4;
            var bgProp = quizSo.FindProperty("choiceBackgrounds");
            bgProp.arraySize = 4;
            var badgeProp = quizSo.FindProperty("choiceLetterBadgeImages");
            badgeProp.arraySize = 4;
            var letProp = quizSo.FindProperty("choiceOptionLetters");
            letProp.arraySize = 4;
            var jpProp = quizSo.FindProperty("choiceJapaneseTexts");
            jpProp.arraySize = 4;
            var romProp = quizSo.FindProperty("choiceRomajiTexts");
            romProp.arraySize = 4;

            for (int i = 0; i < 4; i++)
            {
                btnProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceButtons[i];
                bgProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceBackgrounds[i];
                badgeProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceBadgeImages[i];
                letProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceLetters[i];
                jpProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceJpTexts[i];
                romProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceRomajiTexts[i];
            }

            quizSo.FindProperty("feedbackContainer").objectReferenceValue = feedbackGo;
            quizSo.FindProperty("feedbackBannerImage").objectReferenceValue = fbBannerImg;
            quizSo.FindProperty("feedbackBannerText").objectReferenceValue = fbBannerTmp;
            quizSo.FindProperty("feedbackSentenceText").objectReferenceValue = fbSentTmp;
            quizSo.FindProperty("nextQuestionButton").objectReferenceValue = nextBtn;
            quizSo.FindProperty("nextButtonText").objectReferenceValue = nextTmp;

            quizSo.FindProperty("resultTitleText").objectReferenceValue = resTitleTmp;
            quizSo.FindProperty("resultScoreText").objectReferenceValue = resScoreTmp;
            quizSo.FindProperty("resultPercentageText").objectReferenceValue = resPercentTmp;
            quizSo.FindProperty("resultPointsRewardText").objectReferenceValue = resRewardTmp;
            quizSo.FindProperty("playAgainButton").objectReferenceValue = playAgainBtn;

            quizSo.FindProperty("emptyProgressText").objectReferenceValue = emptyProgTmp;
            quizSo.FindProperty("demoPracticeButton").objectReferenceValue = demoBtn;

            quizSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire juga ke DictionaryBookSpreadUI
            var spreadSo = new SerializedObject(bookSpread);
            var quizRootProp = spreadSo.FindProperty("quizSpreadRoot");
            if (quizRootProp != null) quizRootProp.objectReferenceValue = quizRoot;

            var quizUIProp = spreadSo.FindProperty("quizSpreadUI");
            if (quizUIProp != null) quizUIProp.objectReferenceValue = quizUI;

            spreadSo.ApplyModifiedPropertiesWithoutUndo();

            quizRoot.SetActive(false);

            // Rebuild scene layout
            Canvas.ForceUpdateCanvases();

            Debug.Log("[DictionaryQuizUIBuilder] Berhasil merevisi layout UI Tab Quiz dengan struktur horizontal penuh!");
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

        private static void SetPageTransform(RectTransform rt, bool isLeft)
        {
            if (isLeft)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(740f, 820f);
                rt.anchoredPosition = new Vector2(56f, 20f);
                rt.localScale = new Vector3(0.9f, 0.9f, 1f);
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(740f, 820f);
                rt.anchoredPosition = new Vector2(-770f, 20f);
                rt.localScale = new Vector3(0.9f, 0.9f, 1f);
            }
        }

        private static void CreatePageHeader(Transform pageParent, string title, string subtitle, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont)
        {
            GameObject headerGo = CreateUIObject("PageHeader", pageParent);
            RectTransform headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.5f, 1f);
            headerRt.anchorMax = new Vector2(0.5f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(680f, 74f);
            headerRt.anchoredPosition = new Vector2(0f, -52f);

            GameObject titleGo = CreateUIObject("Title", headerGo.transform);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, 0f);
            titleRt.sizeDelta = new Vector2(0f, 40f);

            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.font = zenFont;
            titleTmp.fontSize = 34f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.18f, 0.14f, 0.28f);
            titleTmp.text = title;

            GameObject subGo = CreateUIObject("Subtitle", headerGo.transform);
            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0f);
            subRt.pivot = new Vector2(0.5f, 0f);
            subRt.anchoredPosition = new Vector2(0f, 0f);
            subRt.sizeDelta = new Vector2(0f, 30f);

            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.font = nunitoFont;
            subTmp.fontSize = 24f;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.48f, 0.44f, 0.58f);
            subTmp.text = subtitle;

            GameObject lineGo = CreateUIObject("DividerLine", pageParent);
            RectTransform lineRt = lineGo.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.5f, 1f);
            lineRt.anchorMax = new Vector2(0.5f, 1f);
            lineRt.pivot = new Vector2(0.5f, 1f);
            lineRt.sizeDelta = new Vector2(680f, 2f);
            lineRt.anchoredPosition = new Vector2(0f, -130f);

            var lineImg = lineGo.AddComponent<Image>();
            lineImg.color = new Color(0.80f, 0.76f, 0.88f, 0.65f);
            lineImg.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateStatusBadge(string name, Transform parent, string defaultText, Sprite pillSprite, Color bgColor, Color textColor, TMP_FontAsset font, float fontSize)
        {
            GameObject badgeGo = CreateUIObject(name, parent);
            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.sprite = pillSprite;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = bgColor;

            GameObject textGo = CreateUIObject("Text", badgeGo.transform);
            SetStretch(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.text = defaultText;

            return tmp;
        }
    }
}
