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

            // 2. Muat Aset Sprite Quiz Baru
            var cardNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Quiz/Card_Choice_Normal.png");
            var cardCorrectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Quiz/Card_Choice_Correct.png");
            var cardWrongSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Quiz/Card_Choice_Wrong.png");
            var pillHeaderSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Quiz/Pill_Header.png");
            var badgeNumberSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Quiz/Badge_Number.png");
            var iconCheckmarkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Quiz/Icon_Checkmark.png");
            var framePolaroidSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Quiz/Frame_Polaroid_Landscape.png");

            // Sprite pendukung untuk popup summary
            var cardChoice2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Card_Choice2_Bg.png");
            var pillBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Category Pill.png");

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
            // SUB-ROOT 1: ACTIVE QUIZ ROOT (2 Halaman)
            // ==========================================
            GameObject activeQuizRoot = CreateUIObject("ActiveQuizRoot", quizRoot.transform);
            SetStretch(activeQuizRoot.GetComponent<RectTransform>());

            // A. HALAMAN KIRI
            GameObject leftPageGo = CreateUIObject("LeftQuizPage", activeQuizRoot.transform);
            RectTransform leftRt = leftPageGo.GetComponent<RectTransform>();
            SetPageTransform(leftRt, isLeft: true);
            var leftCg = leftPageGo.AddComponent<CanvasGroup>();

            var leftSlot = BuildQuestionSlot(leftPageGo.transform, isLeft: true, pillHeaderSprite, framePolaroidSprite, cardNormalSprite, badgeNumberSprite, iconCheckmarkSprite, zenFont, nunitoFont);
            leftSlot.pageRoot = leftPageGo;
            leftSlot.canvasGroup = leftCg;
            leftSlot.pageRect = leftRt;

            // B. HALAMAN KANAN
            GameObject rightPageGo = CreateUIObject("RightQuizPage", activeQuizRoot.transform);
            RectTransform rightRt = rightPageGo.GetComponent<RectTransform>();
            SetPageTransform(rightRt, isLeft: false);
            var rightCg = rightPageGo.AddComponent<CanvasGroup>();

            var rightSlot = BuildQuestionSlot(rightPageGo.transform, isLeft: false, pillHeaderSprite, framePolaroidSprite, cardNormalSprite, badgeNumberSprite, iconCheckmarkSprite, zenFont, nunitoFont);
            rightSlot.pageRoot = rightPageGo;
            rightSlot.canvasGroup = rightCg;
            rightSlot.pageRect = rightRt;

            // ==========================================
            // SUB-ROOT 2: EMPTY STATE ROOT
            // ==========================================
            GameObject emptyRoot = CreateUIObject("EmptyStateRoot", quizRoot.transform);
            SetStretch(emptyRoot.GetComponent<RectTransform>());
            emptyRoot.SetActive(false);

            GameObject emptyCardGo = CreateUIObject("EmptyCard", emptyRoot.transform);
            RectTransform emptyCardRt = emptyCardGo.GetComponent<RectTransform>();
            emptyCardRt.anchorMin = new Vector2(0.5f, 0.5f);
            emptyCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            emptyCardRt.pivot = new Vector2(0.5f, 0.5f);
            emptyCardRt.sizeDelta = new Vector2(620f, 340f);

            var emptyCardImg = emptyCardGo.AddComponent<Image>();
            emptyCardImg.sprite = cardChoice2Sprite;
            emptyCardImg.type = Image.Type.Sliced;
            emptyCardImg.color = Color.white;

            var emptyVlg = emptyCardGo.AddComponent<VerticalLayoutGroup>();
            emptyVlg.padding = new RectOffset(36, 36, 32, 32);
            emptyVlg.spacing = 16f;
            emptyVlg.childAlignment = TextAnchor.MiddleCenter;
            emptyVlg.childControlWidth = true;
            emptyVlg.childControlHeight = false;
            emptyVlg.childForceExpandWidth = true;
            emptyVlg.childForceExpandHeight = false;

            GameObject emptyTitleGo = CreateUIObject("Title", emptyCardGo.transform);
            var emptyTitleTmp = emptyTitleGo.AddComponent<TextMeshProUGUI>();
            emptyTitleTmp.font = zenFont;
            emptyTitleTmp.fontSize = 28f;
            emptyTitleTmp.fontStyle = FontStyles.Bold;
            emptyTitleTmp.alignment = TextAlignmentOptions.Center;
            emptyTitleTmp.color = new Color(0.24f, 0.16f, 0.13f);
            emptyTitleTmp.text = "Mini Quiz Challenge";

            GameObject emptyDescGo = CreateUIObject("Description", emptyCardGo.transform);
            var emptyDescTmp = emptyDescGo.AddComponent<TextMeshProUGUI>();
            emptyDescTmp.font = nunitoFont;
            emptyDescTmp.fontSize = 20f;
            emptyDescTmp.alignment = TextAlignmentOptions.Center;
            emptyDescTmp.color = new Color(0.45f, 0.38f, 0.35f);
            emptyDescTmp.text = "Temukan dan foto minimal 4 objek kosakata di kota untuk membuka kuis interaktif ini!";

            GameObject emptyProgGo = CreateUIObject("ProgressText", emptyCardGo.transform);
            var emptyProgTmp = emptyProgGo.AddComponent<TextMeshProUGUI>();
            emptyProgTmp.font = nunitoFont;
            emptyProgTmp.fontSize = 18f;
            emptyProgTmp.fontStyle = FontStyles.Bold;
            emptyProgTmp.alignment = TextAlignmentOptions.Center;
            emptyProgTmp.color = new Color(0.25f, 0.45f, 0.85f);
            emptyProgTmp.text = "Discovered: 0 / 4 Required Vocabulary";

            GameObject demoBtnGo = CreateUIObject("DemoPracticeButton", emptyCardGo.transform);
            var demoBtnRt = demoBtnGo.GetComponent<RectTransform>();
            demoBtnRt.sizeDelta = new Vector2(300f, 52f);
            var demoBtnImg = demoBtnGo.AddComponent<Image>();
            demoBtnImg.sprite = pillHeaderSprite;
            demoBtnImg.type = Image.Type.Sliced;
            demoBtnImg.color = new Color(0.24f, 0.16f, 0.13f);

            var demoBtn = demoBtnGo.AddComponent<Button>();
            demoBtn.targetGraphic = demoBtnImg;

            GameObject demoBtnTextGo = CreateUIObject("Text", demoBtnGo.transform);
            SetStretch(demoBtnTextGo.GetComponent<RectTransform>());
            var demoBtnTmp = demoBtnTextGo.AddComponent<TextMeshProUGUI>();
            demoBtnTmp.font = zenFont;
            demoBtnTmp.fontSize = 19f;
            demoBtnTmp.fontStyle = FontStyles.Bold;
            demoBtnTmp.alignment = TextAlignmentOptions.Center;
            demoBtnTmp.color = Color.white;
            demoBtnTmp.text = "Coba Mode Latihan (Demo) >";

            // ==========================================
            // SUB-ROOT 3: RESULT SUMMARY ROOT
            // ==========================================
            GameObject resultRoot = CreateUIObject("ResultSummaryRoot", quizRoot.transform);
            SetStretch(resultRoot.GetComponent<RectTransform>());
            resultRoot.SetActive(false);

            GameObject summaryCardGo = CreateUIObject("SummaryCard", resultRoot.transform);
            RectTransform summaryCardRt = summaryCardGo.GetComponent<RectTransform>();
            summaryCardRt.anchorMin = new Vector2(0.5f, 0.5f);
            summaryCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            summaryCardRt.pivot = new Vector2(0.5f, 0.5f);
            summaryCardRt.sizeDelta = new Vector2(620f, 420f);

            var sumCardImg = summaryCardGo.AddComponent<Image>();
            sumCardImg.sprite = cardChoice2Sprite;
            sumCardImg.type = Image.Type.Sliced;
            sumCardImg.color = Color.white;

            var sumVlg = summaryCardGo.AddComponent<VerticalLayoutGroup>();
            sumVlg.padding = new RectOffset(40, 40, 32, 32);
            sumVlg.spacing = 14f;
            sumVlg.childAlignment = TextAnchor.MiddleCenter;
            sumVlg.childControlWidth = true;
            sumVlg.childControlHeight = false;
            sumVlg.childForceExpandWidth = true;
            sumVlg.childForceExpandHeight = false;

            GameObject resTitleGo = CreateUIObject("Title", summaryCardGo.transform);
            var resTitleTmp = resTitleGo.AddComponent<TextMeshProUGUI>();
            resTitleTmp.font = zenFont;
            resTitleTmp.fontSize = 30f;
            resTitleTmp.fontStyle = FontStyles.Bold;
            resTitleTmp.alignment = TextAlignmentOptions.Center;
            resTitleTmp.color = new Color(0.24f, 0.16f, 0.13f);
            resTitleTmp.text = "Outstanding! / 完璧です！";

            GameObject resScoreGo = CreateUIObject("ScoreText", summaryCardGo.transform);
            var resScoreTmp = resScoreGo.AddComponent<TextMeshProUGUI>();
            resScoreTmp.font = zenFont;
            resScoreTmp.fontSize = 24f;
            resScoreTmp.alignment = TextAlignmentOptions.Center;
            resScoreTmp.color = new Color(0.35f, 0.30f, 0.28f);
            resScoreTmp.text = "Score: 4 / 4";

            GameObject resPctGo = CreateUIObject("PercentageText", summaryCardGo.transform);
            var resPctTmp = resPctGo.AddComponent<TextMeshProUGUI>();
            resPctTmp.font = zenFont;
            resPctTmp.fontSize = 44f;
            resPctTmp.fontStyle = FontStyles.Bold;
            resPctTmp.alignment = TextAlignmentOptions.Center;
            resPctTmp.color = new Color(0.18f, 0.68f, 0.38f);
            resPctTmp.text = "100%";

            GameObject resPtsGo = CreateUIObject("PointsText", summaryCardGo.transform);
            var resPtsTmp = resPtsGo.AddComponent<TextMeshProUGUI>();
            resPtsTmp.font = nunitoFont;
            resPtsTmp.fontSize = 22f;
            resPtsTmp.fontStyle = FontStyles.Bold;
            resPtsTmp.alignment = TextAlignmentOptions.Center;
            resPtsTmp.color = new Color(0.85f, 0.55f, 0.15f);
            resPtsTmp.text = "+40 Points Awarded!";

            GameObject playAgainBtnGo = CreateUIObject("PlayAgainButton", summaryCardGo.transform);
            var playAgainRt = playAgainBtnGo.GetComponent<RectTransform>();
            playAgainRt.sizeDelta = new Vector2(280f, 52f);
            var playAgainImg = playAgainBtnGo.AddComponent<Image>();
            playAgainImg.sprite = pillHeaderSprite;
            playAgainImg.type = Image.Type.Sliced;
            playAgainImg.color = new Color(0.24f, 0.16f, 0.13f);

            var playAgainBtn = playAgainBtnGo.AddComponent<Button>();
            playAgainBtn.targetGraphic = playAgainImg;

            GameObject playAgainTextGo = CreateUIObject("Text", playAgainBtnGo.transform);
            SetStretch(playAgainTextGo.GetComponent<RectTransform>());
            var playAgainTmp = playAgainTextGo.AddComponent<TextMeshProUGUI>();
            playAgainTmp.font = zenFont;
            playAgainTmp.fontSize = 20f;
            playAgainTmp.fontStyle = FontStyles.Bold;
            playAgainTmp.alignment = TextAlignmentOptions.Center;
            playAgainTmp.color = Color.white;
            playAgainTmp.text = "Play Again / Main Lagi ↺";

            // ==========================================
            // SERIALIZE KE SCRIPT DICTIONARYQUIZSPREADUI
            // ==========================================
            var so = new SerializedObject(quizUI);
            so.FindProperty("dictionaryManager").objectReferenceValue = Object.FindFirstObjectByType<DictionaryManager>();
            so.FindProperty("pointRewardSystem").objectReferenceValue = Object.FindFirstObjectByType<PointRewardSystem>();
            so.FindProperty("bookSpread").objectReferenceValue = bookSpread;

            so.FindProperty("activeQuizRoot").objectReferenceValue = activeQuizRoot;
            so.FindProperty("emptyStateRoot").objectReferenceValue = emptyRoot;
            so.FindProperty("resultSummaryRoot").objectReferenceValue = resultRoot;

            so.FindProperty("cardNormalSprite").objectReferenceValue = cardNormalSprite;
            so.FindProperty("cardCorrectSprite").objectReferenceValue = cardCorrectSprite;
            so.FindProperty("cardWrongSprite").objectReferenceValue = cardWrongSprite;

            // Wire Left Slot
            SerializeSlot(so.FindProperty("leftPageSlot"), leftSlot);
            // Wire Right Slot
            SerializeSlot(so.FindProperty("rightPageSlot"), rightSlot);

            // Wire Summary UI
            so.FindProperty("resultTitleText").objectReferenceValue = resTitleTmp;
            so.FindProperty("resultScoreText").objectReferenceValue = resScoreTmp;
            so.FindProperty("resultPercentageText").objectReferenceValue = resPctTmp;
            so.FindProperty("resultPointsRewardText").objectReferenceValue = resPtsTmp;
            so.FindProperty("playAgainButton").objectReferenceValue = playAgainBtn;

            // Wire Empty UI
            so.FindProperty("emptyProgressText").objectReferenceValue = emptyProgTmp;
            so.FindProperty("demoPracticeButton").objectReferenceValue = demoBtn;

            so.ApplyModifiedProperties();

            // Hubungkan quizSpreadRoot dan quizSpreadUI ke BookSpread
            var bookSo = new SerializedObject(bookSpread);
            bookSo.FindProperty("quizSpreadRoot").objectReferenceValue = quizRoot;
            bookSo.FindProperty("quizSpreadUI").objectReferenceValue = quizUI;
            bookSo.ApplyModifiedProperties();

            EditorUtility.SetDirty(quizRoot);
            EditorUtility.SetDirty(bookSpread);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(bookSpread.gameObject.scene);

            Debug.Log("[DictionaryQuizUIBuilder] Quiz UI dengan layout 2 halaman simetris berhasil di-build!");
        }

        private static QuizPageSlotUI BuildQuestionSlot(Transform pageParent, bool isLeft, Sprite pillHeaderSprite, Sprite framePolaroidSprite, Sprite cardNormalSprite, Sprite badgeNumberSprite, Sprite iconCheckmarkSprite, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont)
        {
            var slot = new QuizPageSlotUI();

            // 1. Header Pill Badge ("Mini Quiz - Question 1/4")
            GameObject headerBadgeGo = CreateUIObject("HeaderBadge", pageParent);
            RectTransform headerBadgeRt = headerBadgeGo.GetComponent<RectTransform>();
            headerBadgeRt.anchorMin = new Vector2(0.5f, 1f);
            headerBadgeRt.anchorMax = new Vector2(0.5f, 1f);
            headerBadgeRt.pivot = new Vector2(0.5f, 1f);
            headerBadgeRt.sizeDelta = new Vector2(496f, 63f);
            headerBadgeRt.anchoredPosition = new Vector2(0f, -49.56f);

            var headerImg = headerBadgeGo.AddComponent<Image>();
            headerImg.sprite = pillHeaderSprite;
            headerImg.type = Image.Type.Sliced;
            headerImg.color = Color.white;

            GameObject headerTextGo = CreateUIObject("Text", headerBadgeGo.transform);
            SetStretch(headerTextGo.GetComponent<RectTransform>());
            var headerTmp = headerTextGo.AddComponent<TextMeshProUGUI>();
            headerTmp.font = zenFont;
            headerTmp.fontSize = 28f;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color = Color.white;
            headerTmp.text = isLeft ? "Mini Quiz - Question 1/4" : "Mini Quiz - Question 2/4";
            slot.questionBadgeText = headerTmp;

            // 2. Prompt Text ("Which kotoba matches this photo?")
            GameObject promptGo = CreateUIObject("PromptText", pageParent);
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
            promptTmp.color = new Color(0.24f, 0.16f, 0.13f);
            promptTmp.text = "Which kotoba matches this photo?";
            slot.promptText = promptTmp;

            // 3. Frame Polaroid Foto
            GameObject frameGo = CreateUIObject("PhotoFrame", pageParent);
            RectTransform frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0.5f, 1f);
            frameRt.anchorMax = new Vector2(0.5f, 1f);
            frameRt.pivot = new Vector2(0.5f, 1f);
            frameRt.sizeDelta = new Vector2(507f, 357f);
            frameRt.anchoredPosition = new Vector2(0f, -169.1f);

            var frameImg = frameGo.AddComponent<Image>();
            frameImg.sprite = framePolaroidSprite;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.white;
            slot.photoFrame = frameGo;

            // RawImage Foto di dalam Polaroid (proporsi landscape 16:10 / 4:3 dengan border bawah lebih tebal)
            GameObject rawPhotoGo = CreateUIObject("PhotoRawImage", frameGo.transform);
            RectTransform rawRt = rawPhotoGo.GetComponent<RectTransform>();
            rawRt.anchorMin = new Vector2(0.5f, 0.5f);
            rawRt.anchorMax = new Vector2(0.5f, 0.5f);
            rawRt.pivot = new Vector2(0.5f, 0.5f);
            rawRt.sizeDelta = new Vector2(372f, 222f);
            rawRt.anchoredPosition = new Vector2(0f, 10f);

            var rawImg = rawPhotoGo.AddComponent<RawImage>();
            rawImg.color = Color.white;
            slot.photoImage = rawImg;

            // 4. Choices Container (Memuat 3 Kartu Pilihan Jawaban)
            GameObject choicesContainerGo = CreateUIObject("ChoicesContainer", pageParent);
            RectTransform choicesRt = choicesContainerGo.GetComponent<RectTransform>();
            choicesRt.anchorMin = new Vector2(0.5f, 0.5f);
            choicesRt.anchorMax = new Vector2(0.5f, 0.5f);
            choicesRt.pivot = new Vector2(0.5f, 1f);
            choicesRt.sizeDelta = new Vector2(480f, 230f);
            choicesRt.anchoredPosition = new Vector2(0f, -87.3f);

            var choicesVlg = choicesContainerGo.AddComponent<VerticalLayoutGroup>();
            choicesVlg.spacing = 10f;
            choicesVlg.childControlWidth = true;
            choicesVlg.childControlHeight = false;
            choicesVlg.childForceExpandWidth = true;
            choicesVlg.childForceExpandHeight = false;

            slot.choiceButtons = new Button[3];
            slot.choiceBackgrounds = new Image[3];
            slot.choiceNumberBadges = new Image[3];
            slot.choiceNumberTexts = new TextMeshProUGUI[3];
            slot.choiceJapaneseTexts = new TextMeshProUGUI[3];
            slot.choiceRomajiTexts = new TextMeshProUGUI[3];
            slot.choiceCheckmarkObjects = new GameObject[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject btnGo = CreateUIObject($"Btn_Choice_{i}", choicesContainerGo.transform);
                var btnLe = btnGo.AddComponent<LayoutElement>();
                btnLe.preferredHeight = 64f;
                btnLe.minHeight = 64f;

                var btnImg = btnGo.AddComponent<Image>();
                btnImg.sprite = cardNormalSprite;
                btnImg.type = Image.Type.Sliced;
                btnImg.color = Color.white;

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                slot.choiceButtons[i] = btn;
                slot.choiceBackgrounds[i] = btnImg;

                var btnHlg = btnGo.AddComponent<HorizontalLayoutGroup>();
                btnHlg.padding = new RectOffset(16, 20, 10, 10);
                btnHlg.spacing = 16f;
                btnHlg.childAlignment = TextAnchor.MiddleLeft;
                btnHlg.childControlWidth = false;
                btnHlg.childControlHeight = false;
                btnHlg.childForceExpandWidth = false;
                btnHlg.childForceExpandHeight = false;

                // A. Number Badge (Circle Dark Brown)
                GameObject badgeGo = CreateUIObject("BadgeNumber", btnGo.transform);
                var badgeRt = badgeGo.GetComponent<RectTransform>();
                badgeRt.sizeDelta = new Vector2(36f, 36f);

                var badgeImg = badgeGo.AddComponent<Image>();
                badgeImg.sprite = badgeNumberSprite;
                badgeImg.color = Color.white;
                slot.choiceNumberBadges[i] = badgeImg;

                GameObject numTextGo = CreateUIObject("NumText", badgeGo.transform);
                SetStretch(numTextGo.GetComponent<RectTransform>());
                var numTmp = numTextGo.AddComponent<TextMeshProUGUI>();
                numTmp.font = zenFont;
                numTmp.fontSize = 20f;
                numTmp.fontStyle = FontStyles.Bold;
                numTmp.alignment = TextAlignmentOptions.Center;
                numTmp.color = Color.white;
                numTmp.text = (i + 1).ToString();
                slot.choiceNumberTexts[i] = numTmp;

                // B. Japanese Text (Kanji bold hitam)
                GameObject jpTextGo = CreateUIObject("JapaneseText", btnGo.transform);
                var jpTextRt = jpTextGo.GetComponent<RectTransform>();
                jpTextRt.sizeDelta = new Vector2(140f, 40f);

                var jpTmp = jpTextGo.AddComponent<TextMeshProUGUI>();
                jpTmp.font = zenFont;
                jpTmp.fontSize = 22f;
                jpTmp.fontStyle = FontStyles.Bold;
                jpTmp.alignment = TextAlignmentOptions.MidlineLeft;
                jpTmp.color = new Color(0.13f, 0.10f, 0.08f);
                jpTmp.text = "桜";
                slot.choiceJapaneseTexts[i] = jpTmp;

                // C. Romaji Text (Biru lembut)
                GameObject romajiGo = CreateUIObject("RomajiText", btnGo.transform);
                var romajiRt = romajiGo.GetComponent<RectTransform>();
                romajiRt.sizeDelta = new Vector2(170f, 40f);

                var romajiTmp = romajiGo.AddComponent<TextMeshProUGUI>();
                romajiTmp.font = nunitoFont;
                romajiTmp.fontSize = 21f;
                romajiTmp.fontStyle = FontStyles.Bold;
                romajiTmp.alignment = TextAlignmentOptions.MidlineLeft;
                romajiTmp.color = new Color(0.12f, 0.42f, 0.72f);
                romajiTmp.text = "sakura";
                slot.choiceRomajiTexts[i] = romajiTmp;

                // D. Checkmark Icon (Ikon Centang Hijau di sisi kanan)
                GameObject checkGo = CreateUIObject("Checkmark", btnGo.transform);
                var checkRt = checkGo.GetComponent<RectTransform>();
                checkRt.sizeDelta = new Vector2(28f, 28f);

                var checkImg = checkGo.AddComponent<Image>();
                checkImg.sprite = iconCheckmarkSprite;
                checkImg.color = Color.white;
                checkGo.SetActive(false);
                slot.choiceCheckmarkObjects[i] = checkGo;
            }

            // 5. Footer Terjemahan ("桜 = Cherry Blossom")
            GameObject footerGo = CreateUIObject("TranslationFooter", pageParent);
            RectTransform footerRt = footerGo.GetComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0.5f, 1f);
            footerRt.anchorMax = new Vector2(0.5f, 1f);
            footerRt.pivot = new Vector2(0.5f, 1f);
            footerRt.sizeDelta = new Vector2(480f, 34f);
            footerRt.anchoredPosition = new Vector2(0f, -675f);

            var footerTmp = footerGo.AddComponent<TextMeshProUGUI>();
            footerTmp.font = zenFont;
            footerTmp.fontSize = 18f;
            footerTmp.fontStyle = FontStyles.Bold;
            footerTmp.alignment = TextAlignmentOptions.Center;
            footerTmp.color = new Color(0.24f, 0.16f, 0.13f);
            footerTmp.text = "桜 = Cherry Blossom";
            footerGo.SetActive(false);
            slot.translationFooterText = footerTmp;

            return slot;
        }

        private static void SerializeSlot(SerializedProperty slotProp, QuizPageSlotUI slot)
        {
            slotProp.FindPropertyRelative("pageRoot").objectReferenceValue = slot.pageRoot;
            slotProp.FindPropertyRelative("canvasGroup").objectReferenceValue = slot.canvasGroup;
            slotProp.FindPropertyRelative("pageRect").objectReferenceValue = slot.pageRect;
            slotProp.FindPropertyRelative("questionBadgeText").objectReferenceValue = slot.questionBadgeText;
            slotProp.FindPropertyRelative("promptText").objectReferenceValue = slot.promptText;
            slotProp.FindPropertyRelative("photoFrame").objectReferenceValue = slot.photoFrame;
            slotProp.FindPropertyRelative("photoImage").objectReferenceValue = slot.photoImage;
            slotProp.FindPropertyRelative("translationFooterText").objectReferenceValue = slot.translationFooterText;

            var choiceBtnsProp = slotProp.FindPropertyRelative("choiceButtons");
            var choiceBgsProp = slotProp.FindPropertyRelative("choiceBackgrounds");
            var choiceBadgesProp = slotProp.FindPropertyRelative("choiceNumberBadges");
            var choiceNumTextsProp = slotProp.FindPropertyRelative("choiceNumberTexts");
            var choiceJpTextsProp = slotProp.FindPropertyRelative("choiceJapaneseTexts");
            var choiceRomajiTextsProp = slotProp.FindPropertyRelative("choiceRomajiTexts");
            var choiceChecksProp = slotProp.FindPropertyRelative("choiceCheckmarkObjects");

            choiceBtnsProp.arraySize = 3;
            choiceBgsProp.arraySize = 3;
            choiceBadgesProp.arraySize = 3;
            choiceNumTextsProp.arraySize = 3;
            choiceJpTextsProp.arraySize = 3;
            choiceRomajiTextsProp.arraySize = 3;
            choiceChecksProp.arraySize = 3;

            for (int i = 0; i < 3; i++)
            {
                choiceBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = slot.choiceButtons[i];
                choiceBgsProp.GetArrayElementAtIndex(i).objectReferenceValue = slot.choiceBackgrounds[i];
                choiceBadgesProp.GetArrayElementAtIndex(i).objectReferenceValue = slot.choiceNumberBadges[i];
                choiceNumTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = slot.choiceNumberTexts[i];
                choiceJpTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = slot.choiceJapaneseTexts[i];
                choiceRomajiTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = slot.choiceRomajiTexts[i];
                choiceChecksProp.GetArrayElementAtIndex(i).objectReferenceValue = slot.choiceCheckmarkObjects[i];
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
    }
}
