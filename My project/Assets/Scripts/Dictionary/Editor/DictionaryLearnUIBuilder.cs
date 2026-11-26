using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.DictionaryUI.Editor
{
    public static class DictionaryLearnUIBuilder
    {
        private struct KanaCell
        {
            public string kana;
            public string romaji;
            public KanaCell(string k, string r) { kana = k; romaji = r; }
            public bool IsEmpty => string.IsNullOrEmpty(kana);
        }

        [MenuItem("Tools/Utsuru/Build Dictionary Learn UI (Hiragana & Module)")]
        public static void BuildLearnUI()
        {
            // 1. Muat Database Modul
            var db = IrodoriDatabase.LoadFromProject();
            if (db == null)
            {
                Debug.LogError("[DictionaryLearnUIBuilder] Gagal memuat IrodoriDatabase dari JSON!");
                return;
            }

            // 2. Muat Font TMP SDF
            var zenFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ZenMaruGothic-Bold SDF.asset");
            var nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");

            // 3. Muat Sprite UI
            var cardBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Card Background.png");
            var pillBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Category Pill.png");
            var btnSelectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Button_Unselected.png");
            var cardChoice1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Card_Choice1_Bg.png");
            var cardChoice2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Card_Choice2_Bg.png");
            Sprite cellBgSprite = btnSelectSprite != null ? btnSelectSprite : cardChoice2Sprite;

            // 4. Cari BookSpread di scene
            var bookSpread = Object.FindFirstObjectByType<DictionaryBookSpreadUI>(FindObjectsInactive.Include);
            if (bookSpread == null)
            {
                Debug.LogError("[DictionaryLearnUIBuilder] DictionaryBookSpreadUI tidak ditemukan di scene!");
                return;
            }

            Undo.RegisterCompleteObjectUndo(bookSpread.gameObject, "Build Dictionary Learn UI");
            Transform spreadTrans = bookSpread.transform;

            // ==========================================
            // BAGIAN A: BANGUN KANA SPREAD ROOT (TABEL NATIVE LENGKAP HIRAGANA & KATAKANA)
            // ==========================================
            Transform oldKana = spreadTrans.Find("KanaSpreadRoot");
            if (oldKana != null) Object.DestroyImmediate(oldKana.gameObject);

            GameObject kanaRoot = CreateUIObject("KanaSpreadRoot", spreadTrans);
            RectTransform kanaRt = kanaRoot.GetComponent<RectTransform>();
            SetStretch(kanaRt);

            var kanaSpreadUI = kanaRoot.AddComponent<DictionaryKanaSpreadUI>();

            // --- DATA SILABUS KANA LENGKAP ---
            #region Kana Data
            // Spread 0: Gojuon (46 Karakter Dasar - 10 Baris x 5 Kolom)
            KanaCell[] gojuonHiragana = new KanaCell[50]
            {
                new KanaCell("あ", "a"),   new KanaCell("い", "i"),   new KanaCell("う", "u"),   new KanaCell("え", "e"),   new KanaCell("お", "o"),
                new KanaCell("か", "ka"),  new KanaCell("き", "ki"),  new KanaCell("く", "ku"),  new KanaCell("け", "ke"),  new KanaCell("こ", "ko"),
                new KanaCell("さ", "sa"),  new KanaCell("し", "shi"), new KanaCell("す", "su"),  new KanaCell("せ", "se"),  new KanaCell("そ", "so"),
                new KanaCell("た", "ta"),  new KanaCell("ち", "chi"), new KanaCell("つ", "tsu"), new KanaCell("て", "te"),  new KanaCell("と", "to"),
                new KanaCell("な", "na"),  new KanaCell("に", "ni"),  new KanaCell("ぬ", "nu"),  new KanaCell("ね", "ne"),  new KanaCell("の", "no"),
                new KanaCell("は", "ha"),  new KanaCell("ひ", "hi"),  new KanaCell("ふ", "fu"),  new KanaCell("へ", "he"),  new KanaCell("ほ", "ho"),
                new KanaCell("ま", "ma"),  new KanaCell("み", "mi"),  new KanaCell("む", "mu"),  new KanaCell("め", "me"),  new KanaCell("も", "mo"),
                new KanaCell("や", "ya"),  new KanaCell("", ""),     new KanaCell("ゆ", "yu"),  new KanaCell("", ""),     new KanaCell("よ", "yo"),
                new KanaCell("ら", "ra"),  new KanaCell("り", "ri"),  new KanaCell("る", "ru"),  new KanaCell("れ", "re"),  new KanaCell("ろ", "ro"),
                new KanaCell("わ", "wa"),  new KanaCell("", ""),     new KanaCell("を", "wo"),  new KanaCell("", ""),     new KanaCell("ん", "n")
            };

            KanaCell[] gojuonKatakana = new KanaCell[50]
            {
                new KanaCell("ア", "a"),   new KanaCell("イ", "i"),   new KanaCell("ウ", "u"),   new KanaCell("エ", "e"),   new KanaCell("オ", "o"),
                new KanaCell("カ", "ka"),  new KanaCell("キ", "ki"),  new KanaCell("ク", "ku"),  new KanaCell("ケ", "ke"),  new KanaCell("コ", "ko"),
                new KanaCell("サ", "sa"),  new KanaCell("シ", "shi"), new KanaCell("ス", "su"),  new KanaCell("セ", "se"),  new KanaCell("ソ", "so"),
                new KanaCell("タ", "ta"),  new KanaCell("チ", "chi"), new KanaCell("ツ", "tsu"), new KanaCell("テ", "te"),  new KanaCell("ト", "to"),
                new KanaCell("ナ", "na"),  new KanaCell("ニ", "ni"),  new KanaCell("ヌ", "nu"),  new KanaCell("ネ", "ne"),  new KanaCell("ノ", "no"),
                new KanaCell("ハ", "ha"),  new KanaCell("ヒ", "hi"),  new KanaCell("フ", "fu"),  new KanaCell("ヘ", "he"),  new KanaCell("ホ", "ho"),
                new KanaCell("マ", "ma"),  new KanaCell("ミ", "mi"),  new KanaCell("む", "mu"),  new KanaCell("ме", "me"),  new KanaCell("モ", "mo"),
                new KanaCell("ヤ", "ya"),  new KanaCell("", ""),     new KanaCell("ユ", "yu"),  new KanaCell("", ""),     new KanaCell("ヨ", "yo"),
                new KanaCell("ラ", "ra"),  new KanaCell("リ", "ri"),  new KanaCell("ル", "ru"),  new KanaCell("レ", "re"),  new KanaCell("ロ", "ro"),
                new KanaCell("ワ", "wa"),  new KanaCell("", ""),     new KanaCell("ヲ", "wo"),  new KanaCell("", ""),     new KanaCell("ン", "n")
            };

            // Spread 1: Dakuon & Handakuon (25 Karakter Bunyi Sengau - 5 Baris x 5 Kolom)
            KanaCell[] dakuonHiragana = new KanaCell[25]
            {
                new KanaCell("が", "ga"),  new KanaCell("ぎ", "gi"),  new KanaCell("ぐ", "gu"),  new KanaCell("げ", "ge"),  new KanaCell("ご", "go"),
                new KanaCell("ざ", "za"),  new KanaCell("じ", "ji"),  new KanaCell("ず", "zu"),  new KanaCell("ぜ", "ze"),  new KanaCell("ぞ", "zo"),
                new KanaCell("だ", "da"),  new KanaCell("ぢ", "ji"),  new KanaCell("づ", "zu"),  new KanaCell("で", "de"),  new KanaCell("ど", "do"),
                new KanaCell("ば", "ba"),  new KanaCell("び", "bi"),  new KanaCell("ぶ", "bu"),  new KanaCell("べ", "be"),  new KanaCell("ぼ", "bo"),
                new KanaCell("ぱ", "pa"),  new KanaCell("ぴ", "pi"),  new KanaCell("ぷ", "pu"),  new KanaCell("ぺ", "pe"),  new KanaCell("ぽ", "po")
            };

            KanaCell[] dakuonKatakana = new KanaCell[25]
            {
                new KanaCell("ガ", "ga"),  new KanaCell("ギ", "gi"),  new KanaCell("グ", "gu"),  new KanaCell("ゲ", "ge"),  new KanaCell("ゴ", "go"),
                new KanaCell("ザ", "za"),  new KanaCell("ジ", "ji"),  new KanaCell("ズ", "zu"),  new KanaCell("ゼ", "ze"),  new KanaCell("ゾ", "zo"),
                new KanaCell("ダ", "da"),  new KanaCell("ヂ", "ji"),  new KanaCell("ヅ", "zu"),  new KanaCell("デ", "de"),  new KanaCell("ド", "do"),
                new KanaCell("バ", "ba"),  new KanaCell("ビ", "bi"),  new KanaCell("ブ", "bu"),  new KanaCell("ベ", "be"),  new KanaCell("ボ", "bo"),
                new KanaCell("パ", "pa"),  new KanaCell("ピ", "pi"),  new KanaCell("プ", "pu"),  new KanaCell("ペ", "pe"),  new KanaCell("ポ", "po")
            };

            // Spread 2: Yoon (33 Karakter Kombinasi - 11 Baris x 3 Kolom)
            KanaCell[] yoonHiragana = new KanaCell[33]
            {
                new KanaCell("きゃ", "kya"), new KanaCell("きゅ", "kyu"), new KanaCell("きょ", "kyo"),
                new KanaCell("しゃ", "sha"), new KanaCell("しゅ", "shu"), new KanaCell("しょ", "sho"),
                new KanaCell("ちゃ", "cha"), new KanaCell("ちゅ", "chu"), new KanaCell("ちょ", "cho"),
                new KanaCell("にゃ", "nya"), new KanaCell("にゅ", "nyu"), new KanaCell("にょ", "nyo"),
                new KanaCell("ひゃ", "hya"), new KanaCell("ひゅ", "hyu"), new KanaCell("ひょ", "hyo"),
                new KanaCell("みゃ", "mya"), new KanaCell("みゅ", "myu"), new KanaCell("みょ", "myo"),
                new KanaCell("りゃ", "rya"), new KanaCell("りゅ", "ryu"), new KanaCell("りょ", "ryo"),
                new KanaCell("ぎゃ", "gya"), new KanaCell("ぎゅ", "gyu"), new KanaCell("ぎょ", "gyo"),
                new KanaCell("じゃ", "ja"),  new KanaCell("じゅ", "ju"),  new KanaCell("じょ", "jo"),
                new KanaCell("びゃ", "bya"), new KanaCell("びゅ", "byu"), new KanaCell("びょ", "byo"),
                new KanaCell("ぴゃ", "pya"), new KanaCell("ぴゅ", "pyu"), new KanaCell("ぴょ", "pyo")
            };

            KanaCell[] yoonKatakana = new KanaCell[33]
            {
                new KanaCell("キャ", "kya"), new KanaCell("キュ", "kyu"), new KanaCell("キョ", "kyo"),
                new KanaCell("シャ", "sha"), new KanaCell("シュ", "shu"), new KanaCell("ショ", "sho"),
                new KanaCell("チャ", "cha"), new KanaCell("チュ", "chu"), new KanaCell("チョ", "cho"),
                new KanaCell("ニャ", "nya"), new KanaCell("ニュ", "nyu"), new KanaCell("ニョ", "nyo"),
                new KanaCell("ヒャ", "hya"), new KanaCell("ヒュ", "hyu"), new KanaCell("ヒョ", "hyo"),
                new KanaCell("ミャ", "mya"), new KanaCell("ミュ", "myu"), new KanaCell("ミョ", "myo"),
                new KanaCell("リャ", "rya"), new KanaCell("リュ", "ryu"), new KanaCell("リョ", "ryo"),
                new KanaCell("ギャ", "gya"), new KanaCell("ギュ", "gyu"), new KanaCell("ギョ", "gyo"),
                new KanaCell("ジャ", "ja"),  new KanaCell("ジュ", "ju"),  new KanaCell("ジョ", "jo"),
                new KanaCell("ビャ", "bya"), new KanaCell("ビュ", "byu"), new KanaCell("ビョ", "byo"),
                new KanaCell("ピャ", "pya"), new KanaCell("ピュ", "pyu"), new KanaCell("ピョ", "pyo")
            };

            string[] header5ColsHira = new string[] { "A (あ)", "I (い)", "U (う)", "E (え)", "O (お)" };
            string[] header5ColsKata = new string[] { "A (ア)", "I (イ)", "U (ウ)", "E (エ)", "O (オ)" };
            string[] header3ColsHira = new string[] { "— YA ( ゃ )", "— YU ( ゅ )", "— YO ( ょ )" };
            string[] header3ColsKata = new string[] { "— YA ( ャ )", "— YU ( ュ )", "— YO ( ョ )" };
            #endregion

            // --- A1. HALAMAN KIRI: HIRAGANA ---
            GameObject leftKanaPage = CreateUIObject("LeftKanaPage", kanaRoot.transform);
            RectTransform leftKanaPageRt = leftKanaPage.GetComponent<RectTransform>();
            SetPageTransform(leftKanaPageRt, isLeft: true);
            leftKanaPageRt.anchoredPosition = new Vector2(56f, 20f);
            leftKanaPageRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            CreatePageHeader(leftKanaPage.transform, "HIRAGANA (ひらがな)", "Basic Phonetic Script • 46 Syllabic Characters", zenFont, nunitoFont);
            Button[] leftTabs;
            Image[] leftTabBgs;
            CreateSubTabBar(leftKanaPage.transform, zenFont, pillBgSprite, out leftTabs, out leftTabBgs);

            // Left Spread 0: Gojuon
            GameObject gojuonLeftGo = CreateUIObject("GojuonContent_Left", leftKanaPage.transform);
            SetStretch(gojuonLeftGo.GetComponent<RectTransform>());
            CreateColumnHeaders("Headers", gojuonLeftGo.transform, header5ColsHira, zenFont, pillBgSprite, spacing: 10f);
            CreateKanaTable("TableGrid", gojuonLeftGo.transform, gojuonHiragana, 5, new Vector2(128f, 58f), new Vector2(10f, 4f), new Vector2(0f, -182f), zenFont, nunitoFont, cellBgSprite);

            // Left Spread 1: Dakuon & Handakuon
            GameObject dakuonLeftGo = CreateUIObject("DakuonContent_Left", leftKanaPage.transform);
            SetStretch(dakuonLeftGo.GetComponent<RectTransform>());
            CreateColumnHeaders("Headers", dakuonLeftGo.transform, header5ColsHira, zenFont, pillBgSprite, spacing: 10f);
            CreateKanaTable("TableGrid", dakuonLeftGo.transform, dakuonHiragana, 5, new Vector2(128f, 62f), new Vector2(10f, 6f), new Vector2(0f, -182f), zenFont, nunitoFont, cellBgSprite);
            string hiraGuide = "• <b>Dakuten ( ゛ / Tenten ):</b> Adds two small strokes to K, S, T, H rows to voice consonants into G, Z, D, B.\n  <i>Example:</i> か (ka) → が (ga), さ (sa) → ざ (za), は (ha) → ば (ba)\n\n• <b>Handakuten ( ゜ / Maru ):</b> Adds a small circle to the H row to change it into P sounds.\n  <i>Example:</i> は (ha) → ぱ (pa), ひ (hi) → ぴ (pi), ほ (ho) → ぽ (po)\n\n• <b>Sokuon ( っ / Small Tsu ):</b> Creates a slight pause (glottal stop) doubling the following consonant.\n  <i>Example:</i> きって (kitte = stamp), がっこう (gakkou = school)";
            CreateGuideBox("GuideBox", dakuonLeftGo.transform, "💡 PHONETIC RULES & DIACRITICS GUIDE", hiraGuide, zenFont, cardChoice2Sprite, pillBgSprite);

            // Left Spread 2: Yoon
            GameObject yoonLeftGo = CreateUIObject("YoonContent_Left", leftKanaPage.transform);
            SetStretch(yoonLeftGo.GetComponent<RectTransform>());
            CreateColumnHeaders("Headers", yoonLeftGo.transform, header3ColsHira, zenFont, pillBgSprite, spacing: 16f);
            CreateKanaTable("TableGrid", yoonLeftGo.transform, yoonHiragana, 3, new Vector2(216f, 52f), new Vector2(16f, 4f), new Vector2(0f, -182f), zenFont, nunitoFont, cellBgSprite);

            // --- A2. HALAMAN KANAN: KATAKANA ---
            GameObject rightKanaPage = CreateUIObject("RightKanaPage", kanaRoot.transform);
            RectTransform rightKanaPageRt = rightKanaPage.GetComponent<RectTransform>();
            SetPageTransform(rightKanaPageRt, isLeft: false);
            rightKanaPageRt.anchoredPosition = new Vector2(-770f, 20f);
            rightKanaPageRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            CreatePageHeader(rightKanaPage.transform, "KATAKANA (カタカナ)", "Foreign Loanword Script • 46 Syllabic Characters", zenFont, nunitoFont);
            Button[] rightTabs;
            Image[] rightTabBgs;
            CreateSubTabBar(rightKanaPage.transform, zenFont, pillBgSprite, out rightTabs, out rightTabBgs);

            // Right Spread 0: Gojuon
            GameObject gojuonRightGo = CreateUIObject("GojuonContent_Right", rightKanaPage.transform);
            SetStretch(gojuonRightGo.GetComponent<RectTransform>());
            CreateColumnHeaders("Headers", gojuonRightGo.transform, header5ColsKata, zenFont, pillBgSprite, spacing: 10f);
            CreateKanaTable("TableGrid", gojuonRightGo.transform, gojuonKatakana, 5, new Vector2(128f, 58f), new Vector2(10f, 4f), new Vector2(0f, -182f), zenFont, nunitoFont, cellBgSprite);

            // Right Spread 1: Dakuon & Handakuon
            GameObject dakuonRightGo = CreateUIObject("DakuonContent_Right", rightKanaPage.transform);
            SetStretch(dakuonRightGo.GetComponent<RectTransform>());
            CreateColumnHeaders("Headers", dakuonRightGo.transform, header5ColsKata, zenFont, pillBgSprite, spacing: 10f);
            CreateKanaTable("TableGrid", dakuonRightGo.transform, dakuonKatakana, 5, new Vector2(128f, 62f), new Vector2(10f, 6f), new Vector2(0f, -182f), zenFont, nunitoFont, cellBgSprite);
            string kataGuide = "• <b>Chōonpu ( ー / Long Vowel ):</b> A horizontal bar extending the vowel sound in foreign loanwords.\n  <i>Example:</i> コーヒー (kōhī = coffee), ケーキ (kēki = cake), ノート (nōto = notebook)\n\n• <b>Sokuon Katakana ( ッ / Small Tsu ):</b> Pauses to double consonants in loanwords.\n  <i>Example:</i> コップ (koppu = cup), ベッド (beddo = bed)\n\n• <b>Modern Loanword Combinations:</b> Used for non-Japanese foreign names and terms:\n  • ヴ (vu)  • ファ (fa)  • フィ (fi)  • フェ (fe)  • フォ (fo)\n  • ティ (ti)  • ディ (di)  • シェ (she)  • ジェ (je)  • チェ (che)";
            CreateGuideBox("GuideBox", dakuonRightGo.transform, "💡 SPECIAL KATAKANA RULES", kataGuide, zenFont, cardChoice2Sprite, pillBgSprite);

            // Right Spread 2: Yoon
            GameObject yoonRightGo = CreateUIObject("YoonContent_Right", rightKanaPage.transform);
            SetStretch(yoonRightGo.GetComponent<RectTransform>());
            CreateColumnHeaders("Headers", yoonRightGo.transform, header3ColsKata, zenFont, pillBgSprite, spacing: 16f);
            CreateKanaTable("TableGrid", yoonRightGo.transform, yoonKatakana, 3, new Vector2(216f, 52f), new Vector2(16f, 4f), new Vector2(0f, -182f), zenFont, nunitoFont, cellBgSprite);

            // Hubungkan properti serialized pada DictionaryKanaSpreadUI
            var kanaSo = new SerializedObject(kanaSpreadUI);
            kanaSo.FindProperty("gojuonLeft").objectReferenceValue = gojuonLeftGo;
            kanaSo.FindProperty("gojuonRight").objectReferenceValue = gojuonRightGo;
            kanaSo.FindProperty("dakuonLeft").objectReferenceValue = dakuonLeftGo;
            kanaSo.FindProperty("dakuonRight").objectReferenceValue = dakuonRightGo;
            kanaSo.FindProperty("yoonLeft").objectReferenceValue = yoonLeftGo;
            kanaSo.FindProperty("yoonRight").objectReferenceValue = yoonRightGo;

            var tabGojuonProp = kanaSo.FindProperty("tabGojuonButtons");
            tabGojuonProp.arraySize = 2;
            tabGojuonProp.GetArrayElementAtIndex(0).objectReferenceValue = leftTabs[0];
            tabGojuonProp.GetArrayElementAtIndex(1).objectReferenceValue = rightTabs[0];

            var tabDakuonProp = kanaSo.FindProperty("tabDakuonButtons");
            tabDakuonProp.arraySize = 2;
            tabDakuonProp.GetArrayElementAtIndex(0).objectReferenceValue = leftTabs[1];
            tabDakuonProp.GetArrayElementAtIndex(1).objectReferenceValue = rightTabs[1];

            var tabYoonProp = kanaSo.FindProperty("tabYoonButtons");
            tabYoonProp.arraySize = 2;
            tabYoonProp.GetArrayElementAtIndex(0).objectReferenceValue = leftTabs[2];
            tabYoonProp.GetArrayElementAtIndex(1).objectReferenceValue = rightTabs[2];

            var bgGojuonProp = kanaSo.FindProperty("tabGojuonBackgrounds");
            bgGojuonProp.arraySize = 2;
            bgGojuonProp.GetArrayElementAtIndex(0).objectReferenceValue = leftTabBgs[0];
            bgGojuonProp.GetArrayElementAtIndex(1).objectReferenceValue = rightTabBgs[0];

            var bgDakuonProp = kanaSo.FindProperty("tabDakuonBackgrounds");
            bgDakuonProp.arraySize = 2;
            bgDakuonProp.GetArrayElementAtIndex(0).objectReferenceValue = leftTabBgs[1];
            bgDakuonProp.GetArrayElementAtIndex(1).objectReferenceValue = rightTabBgs[1];

            var bgYoonProp = kanaSo.FindProperty("tabYoonBackgrounds");
            bgYoonProp.arraySize = 2;
            bgYoonProp.GetArrayElementAtIndex(0).objectReferenceValue = leftTabBgs[2];
            bgYoonProp.GetArrayElementAtIndex(1).objectReferenceValue = rightTabBgs[2];

            kanaSo.FindProperty("bookSpread").objectReferenceValue = bookSpread;
            kanaSo.ApplyModifiedPropertiesWithoutUndo();

            kanaSpreadUI.SetSpread(0);
            kanaRoot.SetActive(false);

            // ==========================================
            // BAGIAN B: BANGUN MODULE SPREAD ROOT (TAB MODULE / COURSE)
            // ==========================================
            Transform oldModule = spreadTrans.Find("ModuleSpreadRoot");
            if (oldModule != null) Object.DestroyImmediate(oldModule.gameObject);

            GameObject moduleRoot = CreateUIObject("ModuleSpreadRoot", spreadTrans);
            RectTransform moduleRt = moduleRoot.GetComponent<RectTransform>();
            SetStretch(moduleRt);

            var moduleSpreadUI = moduleRoot.AddComponent<DictionaryModuleSpreadUI>();

            // --- B1. HALAMAN KIRI: DAFTAR ISI (TOC) ---
            GameObject leftModPage = CreateUIObject("LeftModulePage_TOC", moduleRoot.transform);
            RectTransform leftModPageRt = leftModPage.GetComponent<RectTransform>();
            SetPageTransform(leftModPageRt, isLeft: true);
            leftModPageRt.anchoredPosition = new Vector2(56f, 20f);
            leftModPageRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            // Header TOC (Auto-wrap dan rapi tanpa bertumpuk di dalam halaman buku)
            GameObject tocHeaderGo = CreateUIObject("TOC_Header", leftModPage.transform);
            RectTransform tocHeaderRt = tocHeaderGo.GetComponent<RectTransform>();
            tocHeaderRt.anchorMin = new Vector2(0f, 1f);
            tocHeaderRt.anchorMax = new Vector2(1f, 1f);
            tocHeaderRt.pivot = new Vector2(0.5f, 1f);
            tocHeaderRt.sizeDelta = new Vector2(-40f, 84f);
            tocHeaderRt.anchoredPosition = new Vector2(0f, -14f);

            var tocHeaderVlg = tocHeaderGo.AddComponent<VerticalLayoutGroup>();
            tocHeaderVlg.spacing = 4f;
            tocHeaderVlg.childControlWidth = true;
            tocHeaderVlg.childControlHeight = true;
            tocHeaderVlg.childForceExpandWidth = true;
            tocHeaderVlg.childForceExpandHeight = false;

            GameObject tocTitleGo = CreateUIObject("Title", tocHeaderGo.transform);
            var tocTitleTmp = tocTitleGo.AddComponent<TextMeshProUGUI>();
            tocTitleTmp.font = zenFont;
            tocTitleTmp.fontSize = 24f;
            tocTitleTmp.enableAutoSizing = true;
            tocTitleTmp.fontSizeMin = 18f;
            tocTitleTmp.fontSizeMax = 24f;
            tocTitleTmp.fontStyle = FontStyles.Bold;
            tocTitleTmp.alignment = TextAlignmentOptions.Center;
            tocTitleTmp.color = new Color(0.20f, 0.16f, 0.28f);
            tocTitleTmp.textWrappingMode = TextWrappingModes.Normal;
            tocTitleTmp.text = "MODULE TABLE OF CONTENTS (IRODORI A1)";

            // Subtitle TOC
            GameObject tocSubGo = CreateUIObject("Subtitle", tocHeaderGo.transform);
            var tocSubTmp = tocSubGo.AddComponent<TextMeshProUGUI>();
            tocSubTmp.font = nunitoFont;
            tocSubTmp.fontSize = 16f;
            tocSubTmp.enableAutoSizing = true;
            tocSubTmp.fontSizeMin = 12f;
            tocSubTmp.fontSizeMax = 16f;
            tocSubTmp.alignment = TextAlignmentOptions.Center;
            tocSubTmp.color = new Color(0.38f, 0.34f, 0.48f);
            tocSubTmp.textWrappingMode = TextWrappingModes.Normal;
            tocSubTmp.text = "Select a chapter to jump directly to its content:";

            // Container Scrollable TOC
            GameObject tocScrollGo = CreateUIObject("TOC_ScrollRect", leftModPage.transform);
            RectTransform tocScrollRt = tocScrollGo.GetComponent<RectTransform>();
            tocScrollRt.anchorMin = new Vector2(0f, 0f);
            tocScrollRt.anchorMax = new Vector2(1f, 1f);
            tocScrollRt.offsetMin = new Vector2(25f, 25f);
            tocScrollRt.offsetMax = new Vector2(-25f, -106f);

            var tocScroll = tocScrollGo.AddComponent<ScrollRect>();
            tocScroll.horizontal = false;
            tocScroll.vertical = true;
            tocScroll.movementType = ScrollRect.MovementType.Clamped;
            tocScroll.scrollSensitivity = 45f;

            GameObject tocVpGo = CreateUIObject("Viewport", tocScrollGo.transform);
            RectTransform tocVpRt = tocVpGo.GetComponent<RectTransform>();
            SetStretch(tocVpRt);
            tocVpGo.AddComponent<RectMask2D>();
            tocScroll.viewport = tocVpRt;

            GameObject tocListGo = CreateUIObject("Content", tocVpGo.transform);
            RectTransform tocListRt = tocListGo.GetComponent<RectTransform>();
            tocListRt.anchorMin = new Vector2(0f, 1f);
            tocListRt.anchorMax = new Vector2(1f, 1f);
            tocListRt.pivot = new Vector2(0.5f, 1f);
            tocListRt.sizeDelta = new Vector2(0f, 0f);
            tocScroll.content = tocListRt;

            var tocVlg = tocListGo.AddComponent<VerticalLayoutGroup>();
            tocVlg.spacing = 16f;
            tocVlg.padding = new RectOffset(8, 8, 10, 30);
            tocVlg.childControlWidth = true;
            tocVlg.childControlHeight = true;
            tocVlg.childForceExpandWidth = true;
            tocVlg.childForceExpandHeight = false;

            var tocCsf = tocListGo.AddComponent<ContentSizeFitter>();
            tocCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button[] tocButtons = new Button[db.chapters.Length];
            Image[] tocButtonImages = new Image[db.chapters.Length];

            for (int c = 0; c < db.chapters.Length; c++)
            {
                var ch = db.chapters[c];
                int chIdx = c;

                GameObject btnGo = CreateUIObject($"Btn_Chapter_{ch.chapterId}", tocListGo.transform);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.sprite = cardChoice2Sprite != null ? cardChoice2Sprite : btnSelectSprite;
                btnImg.type = Image.Type.Sliced;
                btnImg.color = Color.white;

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                tocButtons[c] = btn;
                tocButtonImages[c] = btnImg;

                var btnVlg = btnGo.AddComponent<VerticalLayoutGroup>();
                btnVlg.padding = new RectOffset(18, 18, 12, 12);
                btnVlg.spacing = 6f;
                btnVlg.childControlWidth = true;
                btnVlg.childControlHeight = true;
                btnVlg.childForceExpandWidth = true;
                btnVlg.childForceExpandHeight = false;

                var btnCsf = btnGo.AddComponent<ContentSizeFitter>();
                btnCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var btnLe = btnGo.AddComponent<LayoutElement>();
                btnLe.minHeight = 68f;

                // Baris Atas: Judul Bab + Badge Jumlah Modul
                GameObject btnTopRowGo = CreateUIObject("TopRow", btnGo.transform);
                var btnTopHlg = btnTopRowGo.AddComponent<HorizontalLayoutGroup>();
                btnTopHlg.spacing = 10f;
                btnTopHlg.childAlignment = TextAnchor.MiddleLeft;
                btnTopHlg.childControlWidth = true;
                btnTopHlg.childControlHeight = true;
                btnTopHlg.childForceExpandWidth = false;
                btnTopHlg.childForceExpandHeight = false;

                // Teks Judul Bab
                GameObject btnTitleGo = CreateUIObject("Title", btnTopRowGo.transform);
                var titleTmp = btnTitleGo.AddComponent<TextMeshProUGUI>();
                titleTmp.font = zenFont;
                titleTmp.fontSize = 20f;
                titleTmp.enableAutoSizing = true;
                titleTmp.fontSizeMin = 15f;
                titleTmp.fontSizeMax = 20f;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.color = new Color(0.18f, 0.14f, 0.28f);
                titleTmp.textWrappingMode = TextWrappingModes.Normal;
                titleTmp.text = ch.chapterTitle;

                var titleLe = btnTitleGo.AddComponent<LayoutElement>();
                titleLe.flexibleWidth = 1f;

                // Badge Jumlah Modul di Kanan
                CreateBadge(btnTopRowGo.transform, $"{ch.moduleCount} Modules", new Color(0.85f, 0.88f, 0.98f, 0.95f), new Color(0.25f, 0.35f, 0.75f), nunitoFont, 142f, pillBgSprite, height: 31f, fontSize: 16f);

                // Baris Bawah: Teks Subtitle Bab
                GameObject btnSubGo = CreateUIObject("Subtitle", btnGo.transform);
                var subTmp = btnSubGo.AddComponent<TextMeshProUGUI>();
                subTmp.font = nunitoFont;
                subTmp.fontSize = 15f;
                subTmp.enableAutoSizing = true;
                subTmp.fontSizeMin = 12f;
                subTmp.fontSizeMax = 15f;
                subTmp.color = new Color(0.38f, 0.42f, 0.54f);
                subTmp.textWrappingMode = TextWrappingModes.Normal;
                subTmp.text = ch.chapterSubtitle;
            }

            // --- B2. HALAMAN KANAN: SCROLLABLE READER ---
            GameObject rightModPage = CreateUIObject("RightModulePage_Reader", moduleRoot.transform);
            RectTransform rightModPageRt = rightModPage.GetComponent<RectTransform>();
            SetPageTransform(rightModPageRt, isLeft: false);
            rightModPageRt.anchoredPosition = new Vector2(-770f, 20f);
            rightModPageRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            // ScrollRect Component
            GameObject scrollRectGo = CreateUIObject("ModuleScrollRect", rightModPage.transform);
            RectTransform scrollRectRt = scrollRectGo.GetComponent<RectTransform>();
            SetStretch(scrollRectRt, padding: 15f);

            var scrollRect = scrollRectGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 45f;

            // Viewport dengan RectMask2D
            GameObject viewportGo = CreateUIObject("Viewport", scrollRectGo.transform);
            RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
            SetStretch(viewportRt);
            viewportGo.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportRt;

            // Content Container
            GameObject contentGo = CreateUIObject("Content", viewportGo.transform);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            scrollRect.content = contentRt;

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 24f;
            vlg.padding = new RectOffset(12, 12, 16, 50);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Buat Setiap Bagian Bab (Chapter Sections) di Reader Kanan
            RectTransform[] chapterSections = new RectTransform[db.chapters.Length];

            for (int c = 0; c < db.chapters.Length; c++)
            {
                var ch = db.chapters[c];

                GameObject chSecGo = CreateUIObject($"Section_Chapter_{ch.chapterId}", contentGo.transform);
                RectTransform chSecRt = chSecGo.GetComponent<RectTransform>();
                chapterSections[c] = chSecRt;

                var chVlg = chSecGo.AddComponent<VerticalLayoutGroup>();
                chVlg.spacing = 16f;
                chVlg.childControlWidth = true;
                chVlg.childControlHeight = true;
                chVlg.childForceExpandWidth = true;
                chVlg.childForceExpandHeight = false;

                var chCsf = chSecGo.AddComponent<ContentSizeFitter>();
                chCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Banner Header Bab (Straight 9-sliced, Deep Royal Purple)
                GameObject bannerGo = CreateUIObject("ChapterBanner", chSecGo.transform);
                var bannerImg = bannerGo.AddComponent<Image>();
                bannerImg.sprite = btnSelectSprite != null ? btnSelectSprite : cardChoice2Sprite;
                bannerImg.type = Image.Type.Sliced;
                bannerImg.pixelsPerUnitMultiplier = 1.6f;
                bannerImg.color = new Color(0.36f, 0.30f, 0.62f, 0.98f); // Deep Royal Purple

                var bannerVlg = bannerGo.AddComponent<VerticalLayoutGroup>();
                bannerVlg.padding = new RectOffset(16, 16, 10, 10);
                bannerVlg.childControlWidth = true;
                bannerVlg.childControlHeight = true;
                bannerVlg.childForceExpandWidth = true;
                bannerVlg.childForceExpandHeight = false;

                var bannerCsf = bannerGo.AddComponent<ContentSizeFitter>();
                bannerCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var bannerLe = bannerGo.AddComponent<LayoutElement>();
                bannerLe.minHeight = 52f;

                GameObject bannerTextGo = CreateUIObject("Title", bannerGo.transform);
                var bannerTmp = bannerTextGo.AddComponent<TextMeshProUGUI>();
                bannerTmp.font = zenFont;
                bannerTmp.fontSize = 22f;
                bannerTmp.enableAutoSizing = true;
                bannerTmp.fontSizeMin = 15f;
                bannerTmp.fontSizeMax = 22f;
                bannerTmp.fontStyle = FontStyles.Bold;
                bannerTmp.alignment = TextAlignmentOptions.Center;
                bannerTmp.color = Color.white;
                bannerTmp.textWrappingMode = TextWrappingModes.Normal;
                bannerTmp.text = $"{ch.chapterTitle}  •  {ch.chapterSubtitle}";

                // Render Setiap Modul di Bab Ini
                if (ch.modules != null)
                {
                    for (int m = 0; m < ch.modules.Length; m++)
                    {
                        var mod = ch.modules[m];

                        GameObject matCardGo = CreateUIObject($"Card_Mod_{mod.id}", chSecGo.transform);
                        var matCardImg = matCardGo.AddComponent<Image>();
                        matCardImg.sprite = cardChoice2Sprite != null ? cardChoice2Sprite : btnSelectSprite;
                        matCardImg.type = Image.Type.Sliced;
                        matCardImg.color = Color.white;

                        var matVlg = matCardGo.AddComponent<VerticalLayoutGroup>();
                        matVlg.spacing = 12f;
                        matVlg.padding = new RectOffset(18, 18, 16, 16);
                        matVlg.childControlWidth = true;
                        matVlg.childControlHeight = true;
                        matVlg.childForceExpandWidth = true;
                        matVlg.childForceExpandHeight = false;

                        var matCsf = matCardGo.AddComponent<ContentSizeFitter>();
                        matCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                        // Header Kartu Modul
                        GameObject matHeaderGo = CreateUIObject("Header", matCardGo.transform);
                        var matHlg = matHeaderGo.AddComponent<HorizontalLayoutGroup>();
                        matHlg.spacing = 10f;
                        matHlg.padding = new RectOffset(2, 2, 2, 2);
                        matHlg.childControlWidth = true;
                        matHlg.childControlHeight = true;
                        matHlg.childForceExpandWidth = true;
                        matHlg.childForceExpandHeight = false;

                        // Module Title
                        GameObject matTitleGo = CreateUIObject("Title", matHeaderGo.transform);
                        var matTitleTmp = matTitleGo.AddComponent<TextMeshProUGUI>();
                        matTitleTmp.font = zenFont;
                        matTitleTmp.fontSize = 23f;
                        matTitleTmp.enableAutoSizing = true;
                        matTitleTmp.fontSizeMin = 17f;
                        matTitleTmp.fontSizeMax = 23f;
                        matTitleTmp.fontStyle = FontStyles.Bold;
                        matTitleTmp.color = new Color(0.18f, 0.15f, 0.25f);
                        matTitleTmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                        matTitleTmp.textWrappingMode = TextWrappingModes.Normal;
                        matTitleTmp.text = mod.title;

                        var matTitleLe = matTitleGo.AddComponent<LayoutElement>();
                        matTitleLe.flexibleWidth = 1f;

                        // Gambar Infografis / Ilustrasi Modul
                        if (mod.panelImages != null && mod.panelImages.Length > 0 && !string.IsNullOrEmpty(mod.panelImages[0].assetPath))
                        {
                            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(mod.panelImages[0].assetPath);
                            if (sprite != null)
                            {
                                GameObject imgContainerGo = CreateUIObject("ImageContainer", matCardGo.transform);
                                var imgLe = imgContainerGo.AddComponent<LayoutElement>();
                                imgLe.preferredHeight = 235f;

                                GameObject spriteImgGo = CreateUIObject("SpriteImage", imgContainerGo.transform);
                                SetStretch(spriteImgGo.GetComponent<RectTransform>());
                                var spriteImg = spriteImgGo.AddComponent<Image>();
                                spriteImg.sprite = sprite;
                                spriteImg.preserveAspect = true;
                            }
                        }

                        // Daftar Aktivitas Can-Do / Percakapan
                        if (mod.activities != null)
                        {
                            for (int a = 0; a < mod.activities.Length; a++)
                            {
                                var act = mod.activities[a];

                                GameObject phraseBoxGo = CreateUIObject($"PhraseBox_{a + 1}", matCardGo.transform);
                                var boxImg = phraseBoxGo.AddComponent<Image>();
                                boxImg.sprite = cardChoice1Sprite != null ? cardChoice1Sprite : cardChoice2Sprite;
                                boxImg.type = Image.Type.Sliced;
                                boxImg.color = Color.white; // Warm ivory paper tint

                                var boxVlg = phraseBoxGo.AddComponent<VerticalLayoutGroup>();
                                boxVlg.spacing = 6f;
                                boxVlg.padding = new RectOffset(16, 16, 12, 12);
                                boxVlg.childControlWidth = true;
                                boxVlg.childControlHeight = true;
                                boxVlg.childForceExpandWidth = true;
                                boxVlg.childForceExpandHeight = false;

                                var boxCsf = phraseBoxGo.AddComponent<ContentSizeFitter>();
                                boxCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                                // Target Can-do Goal
                                if (!string.IsNullOrEmpty(act.indonesianTitle))
                                {
                                    GameObject goalGo = CreateUIObject("Goal", phraseBoxGo.transform);
                                    var goalTmp = goalGo.AddComponent<TextMeshProUGUI>();
                                    goalTmp.font = nunitoFont;
                                    goalTmp.fontSize = 17f;
                                    goalTmp.fontStyle = FontStyles.Bold;
                                    goalTmp.color = new Color(0.20f, 0.18f, 0.35f);
                                    goalTmp.textWrappingMode = TextWrappingModes.Normal;
                                    goalTmp.text = $"Goal: {act.indonesianTitle}";
                                }

                                // Frasa Bahasa Jepang
                                if (!string.IsNullOrEmpty(act.japanesePhrase))
                                {
                                    GameObject jpGo = CreateUIObject("JapanesePhrase", phraseBoxGo.transform);
                                    var jpTmp = jpGo.AddComponent<TextMeshProUGUI>();
                                    jpTmp.font = zenFont;
                                    jpTmp.fontSize = 25f;
                                    jpTmp.enableAutoSizing = true;
                                    jpTmp.fontSizeMin = 18f;
                                    jpTmp.fontSizeMax = 25f;
                                    jpTmp.lineSpacing = 4f;
                                    jpTmp.fontStyle = FontStyles.Bold;
                                    jpTmp.color = new Color(0.12f, 0.10f, 0.22f);
                                    jpTmp.textWrappingMode = TextWrappingModes.Normal;
                                    jpTmp.text = act.japanesePhrase;
                                }

                                // Penjelasan Situasi
                                if (!string.IsNullOrEmpty(act.explanation))
                                {
                                    GameObject expGo = CreateUIObject("Explanation", phraseBoxGo.transform);
                                    var expTmp = expGo.AddComponent<TextMeshProUGUI>();
                                    expTmp.font = nunitoFont;
                                    expTmp.fontSize = 15.5f;
                                    expTmp.lineSpacing = 4f;
                                    expTmp.color = new Color(0.24f, 0.28f, 0.36f);
                                    expTmp.textWrappingMode = TextWrappingModes.Normal;
                                    expTmp.text = act.explanation;
                                }

                                // Kosa Kata / Kata Kunci
                                if (!string.IsNullOrEmpty(act.keywords))
                                {
                                    GameObject kwGo = CreateUIObject("Keywords", phraseBoxGo.transform);
                                    var kwTmp = kwGo.AddComponent<TextMeshProUGUI>();
                                    kwTmp.font = zenFont;
                                    kwTmp.fontSize = 15f;
                                    kwTmp.lineSpacing = 3f;
                                    kwTmp.fontStyle = FontStyles.Normal;
                                    kwTmp.color = new Color(0.38f, 0.32f, 0.54f);
                                    kwTmp.textWrappingMode = TextWrappingModes.Normal;
                                    kwTmp.text = $"• Expressions: {act.keywords}";
                                }
                            }
                        }
                    }
                }
            }

            // Wire serialized fields on DictionaryModuleSpreadUI
            var modSo = new SerializedObject(moduleSpreadUI);
            modSo.FindProperty("rightScrollRect").objectReferenceValue = scrollRect;
            modSo.FindProperty("scrollContent").objectReferenceValue = contentRt;

            var secProp = modSo.FindProperty("chapterSections");
            secProp.arraySize = chapterSections.Length;
            for (int i = 0; i < chapterSections.Length; i++)
            {
                secProp.GetArrayElementAtIndex(i).objectReferenceValue = chapterSections[i];
            }

            var btnProp = modSo.FindProperty("tocButtons");
            btnProp.arraySize = tocButtons.Length;
            for (int i = 0; i < tocButtons.Length; i++)
            {
                btnProp.GetArrayElementAtIndex(i).objectReferenceValue = tocButtons[i];
            }

            var imgProp = modSo.FindProperty("tocButtonBackgrounds");
            imgProp.arraySize = tocButtonImages.Length;
            for (int i = 0; i < tocButtonImages.Length; i++)
            {
                imgProp.GetArrayElementAtIndex(i).objectReferenceValue = tocButtonImages[i];
            }

            modSo.ApplyModifiedPropertiesWithoutUndo();
            moduleRoot.SetActive(false);

            // ==========================================
            // BAGIAN C: HUBUNGKAN KE DICTIONARY BOOK SPREAD UI
            // ==========================================
            var spreadSo = new SerializedObject(bookSpread);
            spreadSo.FindProperty("kanaSpreadRoot").objectReferenceValue = kanaRoot;
            spreadSo.FindProperty("kanaSpreadUI").objectReferenceValue = kanaSpreadUI;
            spreadSo.FindProperty("moduleSpreadRoot").objectReferenceValue = moduleRoot;
            spreadSo.FindProperty("moduleSpreadUI").objectReferenceValue = moduleSpreadUI;
            spreadSo.ApplyModifiedPropertiesWithoutUndo();

            // Mark Scene Dirty and Save
            var scene = bookSpread.gameObject.scene;
            EditorUtility.SetDirty(bookSpread.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[DictionaryLearnUIBuilder] Berhasil membangun UI Tab Hiragana & Tab Module secara komplit!");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return go;
        }

        private static void SetStretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
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

            // Title (Highlight: 34f)
            GameObject titleGo = CreateUIObject("Title", headerGo.transform);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, 0f);
            titleRt.sizeDelta = new Vector2(0f, 40f);

            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.font = zenFont;
            titleTmp.fontSize = 26f;
            titleTmp.enableAutoSizing = true;
            titleTmp.fontSizeMin = 20f;
            titleTmp.fontSizeMax = 26f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.18f, 0.14f, 0.28f);
            titleTmp.text = title;

            // Subtitle (rapi tanpa bertumpuk)
            GameObject subGo = CreateUIObject("Subtitle", headerGo.transform);
            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0f);
            subRt.pivot = new Vector2(0.5f, 0f);
            subRt.anchoredPosition = new Vector2(0f, 0f);
            subRt.sizeDelta = new Vector2(0f, 28f);

            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.font = nunitoFont;
            subTmp.fontSize = 16f;
            subTmp.enableAutoSizing = true;
            subTmp.fontSizeMin = 12f;
            subTmp.fontSizeMax = 16f;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.42f, 0.38f, 0.54f);
            subTmp.text = subtitle;

            // Thin Divider Line
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

        private static void CreateSubTabBar(Transform pageParent, TMP_FontAsset font, Sprite pillSprite, out Button[] buttons, out Image[] images)
        {
            buttons = new Button[3];
            images = new Image[3];

            GameObject barGo = CreateUIObject("SubTabBar", pageParent);
            RectTransform barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 1f);
            barRt.anchorMax = new Vector2(0.5f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(680f, 44f);
            barRt.anchoredPosition = new Vector2(0f, -10f);

            var hlg = barGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            string[] tabTitles = new string[] { "1. 五十音 (Gojūon)", "2. 濁音 (Dakuon)", "3. 拗音 (Yōon)" };

            for (int i = 0; i < 3; i++)
            {
                GameObject btnGo = CreateUIObject($"Tab_{i}", barGo.transform);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.sprite = pillSprite;
                btnImg.type = Image.Type.Sliced;
                btnImg.pixelsPerUnitMultiplier = 0.85f;
                btnImg.color = new Color(0.92f, 0.90f, 0.96f, 0.85f);

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;

                GameObject textGo = CreateUIObject("Text", btnGo.transform);
                SetStretch(textGo.GetComponent<RectTransform>());
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.font = font;
                tmp.fontSize = 16.5f;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 12f;
                tmp.fontSizeMax = 16.5f;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.35f, 0.30f, 0.48f, 1f);
                tmp.text = tabTitles[i];

                buttons[i] = btn;
                images[i] = btnImg;
            }
        }

        private static void CreateColumnHeaders(string name, Transform parent, string[] headers, TMP_FontAsset font, Sprite pillSprite, float spacing)
        {
            GameObject barGo = CreateUIObject(name, parent);
            RectTransform barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 1f);
            barRt.anchorMax = new Vector2(0.5f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(680f, 36f);
            barRt.anchoredPosition = new Vector2(0f, -138f);

            var hlg = barGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            for (int i = 0; i < headers.Length; i++)
            {
                GameObject colGo = CreateUIObject($"Col_{i}", barGo.transform);
                var colImg = colGo.AddComponent<Image>();
                colImg.sprite = pillSprite;
                colImg.type = Image.Type.Sliced;
                colImg.pixelsPerUnitMultiplier = 1f;
                colImg.color = new Color(0.92f, 0.89f, 0.96f, 0.80f);
                colImg.raycastTarget = false;

                GameObject textGo = CreateUIObject("Text", colGo.transform);
                SetStretch(textGo.GetComponent<RectTransform>());
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.font = font;
                tmp.fontSize = 16f;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 12f;
                tmp.fontSizeMax = 16f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.35f, 0.28f, 0.48f, 1f);
                tmp.text = headers[i];
            }
        }

        private static void CreateKanaTable(string name, Transform parent, KanaCell[] cells, int columnCount, Vector2 cellSize, Vector2 spacing, Vector2 anchoredPos, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont, Sprite cellBgSprite)
        {
            GameObject gridGo = CreateUIObject(name, parent);
            RectTransform gridRt = gridGo.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.5f, 1f);
            gridRt.anchorMax = new Vector2(0.5f, 1f);
            gridRt.pivot = new Vector2(0.5f, 1f);
            gridRt.sizeDelta = new Vector2(680f, 620f);
            gridRt.anchoredPosition = anchoredPos;

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columnCount;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;

            for (int i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                GameObject cellGo = CreateUIObject($"Cell_{i}_{cell.romaji}", gridGo.transform);

                var img = cellGo.AddComponent<Image>();
                if (cell.IsEmpty)
                {
                    img.color = Color.clear;
                    img.raycastTarget = false;

                    // Subtle dot placeholder
                    GameObject dotGo = CreateUIObject("Dot", cellGo.transform);
                    SetStretch(dotGo.GetComponent<RectTransform>());
                    var dotTmp = dotGo.AddComponent<TextMeshProUGUI>();
                    dotTmp.font = nunitoFont;
                    dotTmp.fontSize = 24f;
                    dotTmp.alignment = TextAlignmentOptions.Center;
                    dotTmp.color = new Color(0.75f, 0.72f, 0.82f, 0.35f);
                    dotTmp.text = "·";
                    continue;
                }

                img.sprite = cellBgSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.98f, 0.97f, 1.0f, 0.94f);
                img.raycastTarget = false;

                // Teks Kana (ZenMaruGothic-Bold) - HIGHLIGHT: 32f
                GameObject kanaGo = CreateUIObject("Kana", cellGo.transform);
                RectTransform kanaRt = kanaGo.GetComponent<RectTransform>();
                kanaRt.anchorMin = new Vector2(0f, 0f);
                kanaRt.anchorMax = new Vector2(1f, 1f);
                kanaRt.pivot = new Vector2(0.5f, 0.5f);
                kanaRt.offsetMin = Vector2.zero;
                kanaRt.offsetMax = Vector2.zero;
                kanaRt.anchoredPosition = new Vector2(0f, 9f);

                var kanaTmp = kanaGo.AddComponent<TextMeshProUGUI>();
                kanaTmp.font = zenFont;
                kanaTmp.fontSize = 26f;
                kanaTmp.fontStyle = FontStyles.Bold;
                kanaTmp.alignment = TextAlignmentOptions.Center;
                kanaTmp.color = new Color(0.18f, 0.14f, 0.26f, 1f);
                kanaTmp.text = cell.kana;

                // Teks Romaji (Nunito-Bold)
                GameObject romajiGo = CreateUIObject("Romaji", cellGo.transform);
                RectTransform romajiRt = romajiGo.GetComponent<RectTransform>();
                romajiRt.anchorMin = new Vector2(0f, 0f);
                romajiRt.anchorMax = new Vector2(1f, 1f);
                romajiRt.pivot = new Vector2(0.5f, 0.5f);
                romajiRt.offsetMin = Vector2.zero;
                romajiRt.offsetMax = Vector2.zero;
                romajiRt.anchoredPosition = new Vector2(0f, -14f);

                var romajiTmp = romajiGo.AddComponent<TextMeshProUGUI>();
                romajiTmp.font = nunitoFont;
                romajiTmp.fontSize = 15f;
                romajiTmp.fontStyle = FontStyles.Bold;
                romajiTmp.alignment = TextAlignmentOptions.Center;
                romajiTmp.color = new Color(0.48f, 0.42f, 0.58f, 1f);
                romajiTmp.text = cell.romaji;
            }
        }

        private static void CreateGuideBox(string name, Transform parent, string title, string contentText, TMP_FontAsset zenFont, Sprite boxSprite, Sprite pillSprite)
        {
            GameObject boxGo = CreateUIObject(name, parent);
            RectTransform boxRt = boxGo.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0f);
            boxRt.anchorMax = new Vector2(0.5f, 0f);
            boxRt.pivot = new Vector2(0.5f, 0f);
            boxRt.sizeDelta = new Vector2(680f, 270f);
            boxRt.anchoredPosition = new Vector2(0f, 25f);

            var boxImg = boxGo.AddComponent<Image>();
            boxImg.sprite = boxSprite;
            boxImg.type = Image.Type.Sliced;
            boxImg.color = new Color(0.97f, 0.96f, 0.99f, 0.96f);

            // Title Badge
            GameObject badgeGo = CreateUIObject("Badge", boxGo.transform);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 1f);
            badgeRt.anchorMax = new Vector2(0.5f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 1f);
            badgeRt.anchoredPosition = new Vector2(0f, -10f);
            badgeRt.sizeDelta = new Vector2(380f, 32f);

            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.sprite = pillSprite;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.pixelsPerUnitMultiplier = 1.05f;
            badgeImg.color = new Color(0.38f, 0.32f, 0.65f, 1f);

            GameObject badgeTextGo = CreateUIObject("Text", badgeGo.transform);
            SetStretch(badgeTextGo.GetComponent<RectTransform>());
            var badgeTmp = badgeTextGo.AddComponent<TextMeshProUGUI>();
            badgeTmp.font = zenFont;
            badgeTmp.fontSize = 15.5f;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = Color.white;
            badgeTmp.text = title;

            // Content Text
            GameObject contentGo = CreateUIObject("Content", boxGo.transform);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(16f, 10f);
            contentRt.offsetMax = new Vector2(-16f, -46f);

            var contentTmp = contentGo.AddComponent<TextMeshProUGUI>();
            contentTmp.font = zenFont;
            contentTmp.fontSize = 14.5f;
            contentTmp.enableAutoSizing = true;
            contentTmp.fontSizeMin = 11f;
            contentTmp.fontSizeMax = 14.5f;
            contentTmp.lineSpacing = 3f;
            contentTmp.color = new Color(0.24f, 0.20f, 0.32f, 1f);
            contentTmp.textWrappingMode = TextWrappingModes.Normal;
            contentTmp.text = contentText;
        }

        private static void CreateBadge(Transform parent, string text, Color bgColor, Color textColor, TMP_FontAsset font, float width, Sprite pillSprite, float height = 30f, float fontSize = 14f)
        {
            GameObject badgeGo = CreateUIObject("Badge", parent);
            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.sprite = pillSprite;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.pixelsPerUnitMultiplier = Mathf.Clamp(34f / Mathf.Max(20f, height), 0.75f, 1.5f);
            badgeImg.color = bgColor;
            var le = badgeGo.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            GameObject textGo = CreateUIObject("Text", badgeGo.transform);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            SetStretch(textRt);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.text = text;
        }
    }
}
