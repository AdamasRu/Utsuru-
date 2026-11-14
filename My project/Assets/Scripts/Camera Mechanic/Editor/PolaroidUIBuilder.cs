    #if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.Capture.Editor
{
    public static class PolaroidUIBuilder
    {
        [MenuItem("Utsuru/Build Final Polaroid UI")]
        public static void BuildUI()
        {
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[PolaroidUIBuilder] Canvas tidak ditemukan!");
                return;
            }

            // Load Sprites
            var cardBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Card Background.png");
            var bannerNewSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Banner New Kotoba.png");
            var audioBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Audio Button.png");
            var sentenceBoxSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Sentence Box.png");
            var catPillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Category Pill.png");
            var saveWithPointsSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Save Button.png");
            var saveNoPointsSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Save Button Without Point Badge.png");
            var pointBadgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Point Badge.png");
            var retakeBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Retake Button.png");
            var keyHintSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Key Hint Badges.png");

            // Load Fonts
            var nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");
            var zenFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ZenMaruGothic-Bold SDF.asset");

            // Find or clean old PolaroidPanel
            Transform oldPanel = canvasGo.transform.Find("PolaroidPanel");
            if (oldPanel != null)
            {
                Undo.DestroyObjectImmediate(oldPanel.gameObject);
            }

            // 1. Root PolaroidPanel
            GameObject panelGo = CreateUIObject("PolaroidPanel", canvasGo.transform);
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            SetStretch(panelRt);

            // 1.1 Dimmer Backdrop
            GameObject dimGo = CreateUIObject("Dim", panelGo.transform);
            SetStretch(dimGo.GetComponent<RectTransform>());
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.65f);
            var dimBtn = dimGo.AddComponent<Button>();

            // 1.2 Top Floating Banner
            GameObject bannerGo = CreateUIObject("TopBanner", panelGo.transform);
            RectTransform bannerRt = bannerGo.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRt.pivot = new Vector2(0.5f, 0.5f);
            bannerRt.sizeDelta = new Vector2(550f, 80f);
            bannerRt.anchoredPosition = new Vector2(0f, 350f);
            var bannerImg = bannerGo.AddComponent<Image>();
            bannerImg.sprite = bannerNewSprite;
            bannerImg.preserveAspect = true;

            // 1.3 Main Card (500 x 600 px)
            GameObject cardGo = CreateUIObject("Card", panelGo.transform);
            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(510f, 610f);
            cardRt.anchoredPosition = new Vector2(0f, 0f);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.sprite = cardBgSprite;

            // 1.3.1 Photo Area (16:9, width 430, height 242)
            GameObject photoGo = CreateUIObject("PhotoImage", cardGo.transform);
            RectTransform photoRt = photoGo.GetComponent<RectTransform>();
            photoRt.anchorMin = new Vector2(0.5f, 1f);
            photoRt.anchorMax = new Vector2(0.5f, 1f);
            photoRt.pivot = new Vector2(0.5f, 1f);
            photoRt.sizeDelta = new Vector2(430f, 242f);
            photoRt.anchoredPosition = new Vector2(0f, -40f);
            var photoRaw = photoGo.AddComponent<RawImage>();
            photoRaw.color = Color.white;

            // Date Stamp overlay inside Photo (bottom-right)
            GameObject dateStampGo = CreateUIObject("DateStamp", photoGo.transform);
            RectTransform dateStampRt = dateStampGo.GetComponent<RectTransform>();
            dateStampRt.anchorMin = new Vector2(1f, 0f);
            dateStampRt.anchorMax = new Vector2(1f, 0f);
            dateStampRt.pivot = new Vector2(1f, 0f);
            dateStampRt.sizeDelta = new Vector2(120f, 24f);
            dateStampRt.anchoredPosition = new Vector2(-10f, 8f);
            var dateStampTmp = dateStampGo.AddComponent<TextMeshProUGUI>();
            dateStampTmp.font = nunitoFont;
            dateStampTmp.text = "24 01 10";
            dateStampTmp.fontSize = 15;
            dateStampTmp.fontStyle = FontStyles.Bold;
            dateStampTmp.alignment = TextAlignmentOptions.Right;
            dateStampTmp.color = new Color(1f, 0.48f, 0f, 0.95f); // Retro orange

            // 1.3.2 Category Pill
            GameObject catPillGo = CreateUIObject("CategoryPill", cardGo.transform);
            RectTransform catPillRt = catPillGo.GetComponent<RectTransform>();
            catPillRt.anchorMin = new Vector2(0.5f, 1f);
            catPillRt.anchorMax = new Vector2(0.5f, 1f);
            catPillRt.pivot = new Vector2(0f, 1f);
            catPillRt.sizeDelta = new Vector2(125f, 26f);
            catPillRt.anchoredPosition = new Vector2(-215f, -290f);
            var catPillImg = catPillGo.AddComponent<Image>();
            catPillImg.sprite = catPillSprite;

            GameObject catTextGo = CreateUIObject("CategoryText", catPillGo.transform);
            RectTransform catTextRt = catTextGo.GetComponent<RectTransform>();
            SetStretch(catTextRt);
            var catTmp = catTextGo.AddComponent<TextMeshProUGUI>();
            catTmp.font = nunitoFont;
            catTmp.text = "PLACES (場所)";
            catTmp.fontSize = 10;
            catTmp.fontStyle = FontStyles.Bold;
            catTmp.alignment = TextAlignmentOptions.Center;
            catTmp.color = new Color(0.01f, 0.52f, 0.78f); // Cyan blue

            // 1.3.3 Word Section (Kanji + Kana)
            GameObject kanjiGo = CreateUIObject("KanjiLabel", cardGo.transform);
            RectTransform kanjiRt = kanjiGo.GetComponent<RectTransform>();
            kanjiRt.anchorMin = new Vector2(0.5f, 1f);
            kanjiRt.anchorMax = new Vector2(0.5f, 1f);
            kanjiRt.pivot = new Vector2(0f, 1f);
            kanjiRt.sizeDelta = new Vector2(430f, 32f);
            kanjiRt.anchoredPosition = new Vector2(-215f, -320f);
            var kanjiTmp = kanjiGo.AddComponent<TextMeshProUGUI>();
            kanjiTmp.font = zenFont;
            kanjiTmp.text = "道 <size=70%><color=#8C7063>(みち)</color></size>";
            kanjiTmp.fontSize = 24;
            kanjiTmp.fontStyle = FontStyles.Bold;
            kanjiTmp.color = new Color(0.12f, 0.1f, 0.08f);

            // Romaji & Meaning
            GameObject romajiGo = CreateUIObject("RomajiLabel", cardGo.transform);
            RectTransform romajiRt = romajiGo.GetComponent<RectTransform>();
            romajiRt.anchorMin = new Vector2(0.5f, 1f);
            romajiRt.anchorMax = new Vector2(0.5f, 1f);
            romajiRt.pivot = new Vector2(0f, 1f);
            romajiRt.sizeDelta = new Vector2(430f, 22f);
            romajiRt.anchoredPosition = new Vector2(-215f, -354f);
            var romajiTmp = romajiGo.AddComponent<TextMeshProUGUI>();
            romajiTmp.font = nunitoFont;
            romajiTmp.text = "<color=#2563EB><b>michi</b></color> <color=#94A3B8>•</color> <color=#475569>Street / Road</color>";
            romajiTmp.fontSize = 14;

            // 1.3.4 Sentence Box
            GameObject sentenceBoxGo = CreateUIObject("SentenceBox", cardGo.transform);
            RectTransform sentenceBoxRt = sentenceBoxGo.GetComponent<RectTransform>();
            sentenceBoxRt.anchorMin = new Vector2(0.5f, 1f);
            sentenceBoxRt.anchorMax = new Vector2(0.5f, 1f);
            sentenceBoxRt.pivot = new Vector2(0.5f, 1f);
            sentenceBoxRt.sizeDelta = new Vector2(430f, 74f);
            sentenceBoxRt.anchoredPosition = new Vector2(0f, -382f);
            var sentenceBoxImg = sentenceBoxGo.AddComponent<Image>();
            sentenceBoxImg.sprite = sentenceBoxSprite;

            // Sentence Japanese text
            GameObject sentJpGo = CreateUIObject("JapaneseSentence", sentenceBoxGo.transform);
            RectTransform sentJpRt = sentJpGo.GetComponent<RectTransform>();
            sentJpRt.anchorMin = new Vector2(0f, 1f);
            sentJpRt.anchorMax = new Vector2(1f, 1f);
            sentJpRt.pivot = new Vector2(0f, 1f);
            sentJpRt.sizeDelta = new Vector2(-24f, 24f);
            sentJpRt.anchoredPosition = new Vector2(12f, -10f);
            var sentJpTmp = sentJpGo.AddComponent<TextMeshProUGUI>();
            sentJpTmp.font = zenFont;
            sentJpTmp.text = "「この道をまっすぐ行きます。」";
            sentJpTmp.fontSize = 12.5f;
            sentJpTmp.fontStyle = FontStyles.Bold;
            sentJpTmp.color = new Color(0.12f, 0.1f, 0.08f);

            // Sentence Translation text
            GameObject sentTransGo = CreateUIObject("TranslationSentence", sentenceBoxGo.transform);
            RectTransform sentTransRt = sentTransGo.GetComponent<RectTransform>();
            sentTransRt.anchorMin = new Vector2(0f, 0f);
            sentTransRt.anchorMax = new Vector2(1f, 1f);
            sentTransRt.pivot = new Vector2(0f, 0f);
            sentTransRt.sizeDelta = new Vector2(-24f, -36f);
            sentTransRt.anchoredPosition = new Vector2(12f, 8f);
            var sentTransTmp = sentTransGo.AddComponent<TextMeshProUGUI>();
            sentTransTmp.font = nunitoFont;
            sentTransTmp.text = "(Kono michi o massugu ikimasu) — Go straight along this street.";
            sentTransTmp.fontSize = 10.5f;
            sentTransTmp.fontStyle = FontStyles.Italic;
            sentTransTmp.color = new Color(0.35f, 0.42f, 0.5f);

            // 1.3.5 Audio Button (Full-width pill)
            GameObject audioBtnGo = CreateUIObject("BtnAudio", cardGo.transform);
            RectTransform audioBtnRt = audioBtnGo.GetComponent<RectTransform>();
            audioBtnRt.anchorMin = new Vector2(0.5f, 1f);
            audioBtnRt.anchorMax = new Vector2(0.5f, 1f);
            audioBtnRt.pivot = new Vector2(0.5f, 1f);
            audioBtnRt.sizeDelta = new Vector2(430f, 58f);
            audioBtnRt.anchoredPosition = new Vector2(0f, -464f);
            var audioBtnImg = audioBtnGo.AddComponent<Image>();
            audioBtnImg.sprite = audioBtnSprite;
            var audioBtn = audioBtnGo.AddComponent<Button>();

            // 1.3.6 Bottom Action Buttons Row
            GameObject actionsRowGo = CreateUIObject("ActionButtonsRow", cardGo.transform);
            RectTransform actionsRowRt = actionsRowGo.GetComponent<RectTransform>();
            actionsRowRt.anchorMin = new Vector2(0.5f, 1f);
            actionsRowRt.anchorMax = new Vector2(0.5f, 1f);
            actionsRowRt.pivot = new Vector2(0.5f, 1f);
            actionsRowRt.sizeDelta = new Vector2(430f, 50f);
            actionsRowRt.anchoredPosition = new Vector2(0f, -532f);

            // Save Button (left, width 310)
            GameObject saveBtnGo = CreateUIObject("BtnSave", actionsRowGo.transform);
            RectTransform saveBtnRt = saveBtnGo.GetComponent<RectTransform>();
            saveBtnRt.anchorMin = new Vector2(0f, 0.5f);
            saveBtnRt.anchorMax = new Vector2(0f, 0.5f);
            saveBtnRt.pivot = new Vector2(0f, 0.5f);
            saveBtnRt.sizeDelta = new Vector2(310f, 50f);
            saveBtnRt.anchoredPosition = new Vector2(0f, 0f);
            var saveBtnImg = saveBtnGo.AddComponent<Image>();
            saveBtnImg.sprite = saveWithPointsSprite;
            var saveBtn = saveBtnGo.AddComponent<Button>();

            // Point Badge inside Save Button (right-aligned)
            GameObject pointBadgeGo = CreateUIObject("PointBadge", saveBtnGo.transform);
            RectTransform pointBadgeRt = pointBadgeGo.GetComponent<RectTransform>();
            pointBadgeRt.anchorMin = new Vector2(1f, 0.5f);
            pointBadgeRt.anchorMax = new Vector2(1f, 0.5f);
            pointBadgeRt.pivot = new Vector2(1f, 0.5f);
            pointBadgeRt.sizeDelta = new Vector2(74f, 24f);
            pointBadgeRt.anchoredPosition = new Vector2(-12f, 0f);
            var pointBadgeImg = pointBadgeGo.AddComponent<Image>();
            pointBadgeImg.sprite = pointBadgeSprite;

            GameObject pointBadgeTextGo = CreateUIObject("PointText", pointBadgeGo.transform);
            RectTransform pointBadgeTextRt = pointBadgeTextGo.GetComponent<RectTransform>();
            SetStretch(pointBadgeTextRt);
            var pointBadgeTmp = pointBadgeTextGo.AddComponent<TextMeshProUGUI>();
            pointBadgeTmp.font = nunitoFont;
            pointBadgeTmp.text = "+15 Points";
            pointBadgeTmp.fontSize = 9.5f;
            pointBadgeTmp.fontStyle = FontStyles.Bold;
            pointBadgeTmp.alignment = TextAlignmentOptions.Center;
            pointBadgeTmp.color = new Color(0.04f, 0.45f, 0.32f);

            // Retake Button (right, width 110)
            GameObject retakeBtnGo = CreateUIObject("BtnRetake", actionsRowGo.transform);
            RectTransform retakeBtnRt = retakeBtnGo.GetComponent<RectTransform>();
            retakeBtnRt.anchorMin = new Vector2(1f, 0.5f);
            retakeBtnRt.anchorMax = new Vector2(1f, 0.5f);
            retakeBtnRt.pivot = new Vector2(1f, 0.5f);
            retakeBtnRt.sizeDelta = new Vector2(110f, 50f);
            retakeBtnRt.anchoredPosition = new Vector2(0f, 0f);
            var retakeBtnImg = retakeBtnGo.AddComponent<Image>();
            retakeBtnImg.sprite = retakeBtnSprite;
            var retakeBtn = retakeBtnGo.AddComponent<Button>();

            // 1.4 Bottom Hotkey Hints Bar
            GameObject hotkeyGo = CreateUIObject("BottomHotkeyBar", panelGo.transform);
            RectTransform hotkeyRt = hotkeyGo.GetComponent<RectTransform>();
            hotkeyRt.anchorMin = new Vector2(0.5f, 0f);
            hotkeyRt.anchorMax = new Vector2(0.5f, 0f);
            hotkeyRt.pivot = new Vector2(0.5f, 0f);
            hotkeyRt.sizeDelta = new Vector2(209f, 27f);
            hotkeyRt.anchoredPosition = new Vector2(0f, 25f);
            var hotkeyImg = hotkeyGo.AddComponent<Image>();
            hotkeyImg.sprite = keyHintSprite;
            hotkeyImg.preserveAspect = true;

            // Wire everything to PolaroidController (PolaroidCardUI)
            var controllerGo = GameObject.Find("PolaroidController");
            if (controllerGo == null)
            {
                controllerGo = new GameObject("PolaroidController");
                controllerGo.transform.SetParent(canvasGo.transform, false);
            }
            var polaroidUI = controllerGo.GetComponent<PolaroidCardUI>() ?? controllerGo.AddComponent<PolaroidCardUI>();
            var audioSource = controllerGo.GetComponent<AudioSource>() ?? controllerGo.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var so = new SerializedObject(polaroidUI);
            so.FindProperty("panelRoot").objectReferenceValue = panelGo;
            so.FindProperty("photoImage").objectReferenceValue = photoRaw;
            so.FindProperty("kanjiLabel").objectReferenceValue = kanjiTmp;
            so.FindProperty("romajiLabel").objectReferenceValue = romajiTmp;
            so.FindProperty("audioButton").objectReferenceValue = audioBtn;
            so.FindProperty("saveButton").objectReferenceValue = saveBtn;
            so.FindProperty("closeButton").objectReferenceValue = retakeBtn;
            so.FindProperty("retakeButton").objectReferenceValue = retakeBtn;
            so.FindProperty("backdropCloseButton").objectReferenceValue = dimBtn;
            so.FindProperty("audioSource").objectReferenceValue = audioSource;

            // New fields
            so.FindProperty("bannerImage").objectReferenceValue = bannerImg;
            so.FindProperty("bannerNewKotobaSprite").objectReferenceValue = bannerNewSprite;
            so.FindProperty("dateStampLabel").objectReferenceValue = dateStampTmp;
            so.FindProperty("categoryLabel").objectReferenceValue = catTmp;
            so.FindProperty("pointBadgeLabel").objectReferenceValue = pointBadgeTmp;
            so.FindProperty("pointBadgeObject").objectReferenceValue = pointBadgeGo;
            so.FindProperty("saveButtonImage").objectReferenceValue = saveBtnImg;
            so.FindProperty("saveButtonWithPointsSprite").objectReferenceValue = saveWithPointsSprite;
            so.FindProperty("saveButtonWithoutPointsSprite").objectReferenceValue = saveNoPointsSprite;
            so.FindProperty("exampleJapaneseLabel").objectReferenceValue = sentJpTmp;
            so.FindProperty("exampleTranslationLabel").objectReferenceValue = sentTransTmp;

            so.ApplyModifiedProperties();

            // Default hidden
            panelGo.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[PolaroidUIBuilder] Final Polaroid UI successfully constructed and wired!");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return go;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif

