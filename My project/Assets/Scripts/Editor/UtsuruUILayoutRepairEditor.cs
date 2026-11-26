#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Utsuru.DictionaryUI.Editor
{
    /// <summary>
    /// Automatically repairs serialized layout/font/sprite sizing inside the currently open
    /// Day_Showcase scene once per version, and provides a manual MenuItem.
    /// </summary>
    public static class UtsuruUILayoutRepairEditor
    {
        private const string PrefKey = "Utsuru_UILayoutRepair_Version";
        private const string TargetVersion = "v5_20261126";

        private static void AutoRepairOnceOnLoad()
        {
            // Disabled automatic scene overwrite so custom CameraHUD & Polaroid layouts are preserved.
        }

        [MenuItem("Tools/Utsuru/Repair All Dictionary & Polaroid UI")]
        public static void RepairActiveSceneMenu()
        {
            RepairOpenSceneInternal(saveScene: true);
            EditorPrefs.SetString(PrefKey, TargetVersion);
        }

        private static bool RepairOpenSceneInternal(bool saveScene)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return false;

            var zenFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ZenMaruGothic-Bold SDF.asset");
            var nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");
            var yugothFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/YUGOTHB SDF.asset");
            var pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Polaroid/Category Pill.png");

            EnsureFontFallback(zenFont, yugothFont);

            bool touched = false;
            touched |= RepairCameraMechanicFonts(canvas.transform, zenFont);
            touched |= RepairPolaroidPopup(canvas.transform, zenFont, nunitoFont, pillSprite);
            touched |= RepairDictionaryBook(canvas.transform, zenFont, nunitoFont, pillSprite);

            if (touched)
            {
                Canvas.ForceUpdateCanvases();
                Scene scene = canvas.gameObject.scene;
                EditorUtility.SetDirty(canvas.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                if (saveScene)
                {
                    EditorSceneManager.SaveScene(scene);
                }
                Debug.Log("[UtsuruUILayoutRepairEditor] Seluruh layout Polaroid & Dictionary (Album, Kana, Module, Quiz, Flashcard) berhasil diperbarui dan disimpan.");
            }

            return true;
        }

        private static void EnsureFontFallback(TMP_FontAsset primary, TMP_FontAsset fallback)
        {
            if (primary == null || fallback == null) return;
            if (primary.fallbackFontAssetTable == null)
                primary.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

            if (!primary.fallbackFontAssetTable.Contains(fallback))
            {
                primary.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(primary);
                AssetDatabase.SaveAssets();
            }
        }

        private static bool RepairCameraMechanicFonts(Transform canvasRoot, TMP_FontAsset zenFont)
        {
            if (zenFont == null) return false;
            Transform camMech = canvasRoot.Find("Camera Mechanic");
            if (camMech == null) return false;

            bool changed = false;
            foreach (var tmp in camMech.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.font == null || tmp.font.name.Contains("LiberationSans"))
                {
                    tmp.font = zenFont;
                    EditorUtility.SetDirty(tmp);
                    changed = true;
                }
            }
            return changed;
        }

        private static bool RepairPolaroidPopup(Transform canvasRoot, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont, Sprite pillSprite)
        {
            // Find the new PolaroidPanel (child directly under Canvas that has TopBanner & ActionButtonsRow)
            Transform panel = null;
            for (int i = 0; i < canvasRoot.childCount; i++)
            {
                Transform c = canvasRoot.GetChild(i);
                if (c.name == "PolaroidPanel" && c.Find("TopBanner") != null)
                {
                    panel = c;
                    break;
                }
            }
            if (panel == null) return false;

            SetRect(panel.Find("TopBanner"), new Vector2(680f, 100f), new Vector2(0f, 428f));
            SetPreserveAspect(panel.Find("TopBanner"), true);

            Transform card = panel.Find("Card");
            if (card != null)
            {
                SetRect(card, new Vector2(640f, 766f), new Vector2(0f, -8f));
                SetPreserveAspect(card, true);

                Transform photo = card.Find("PhotoImage");
                SetRect(photo, new Vector2(510f, 287f), new Vector2(0f, -38f));
                if (photo != null)
                {
                    Transform dateStamp = photo.Find("DateStamp");
                    SetRect(dateStamp, new Vector2(140f, 26f), new Vector2(-12f, 10f));
                    SetTmp(dateStamp, nunitoFont, 16f, false, 16f, 16f);
                }

                Transform catPill = card.Find("CategoryPill");
                SetRect(catPill, new Vector2(165f, 28f), new Vector2(-255f, -338f));
                SetSlicedPpu(catPill, 1.2f);
                if (catPill != null)
                    SetTmp(catPill.Find("CategoryText"), nunitoFont, 12f, true, 9.5f, 12.5f);

                Transform kanji = card.Find("KanjiLabel");
                SetRect(kanji, new Vector2(510f, 42f), new Vector2(-255f, -370f));
                SetTmp(kanji, zenFont, 32f, true, 20f, 32f);

                Transform romaji = card.Find("RomajiLabel");
                SetRect(romaji, new Vector2(510f, 24f), new Vector2(-255f, -414f));
                SetTmp(romaji, nunitoFont, 18f, true, 13f, 18f);

                Transform sBox = card.Find("SentenceBox");
                SetRect(sBox, new Vector2(510f, 74f), new Vector2(0f, -446f));
                SetSlicedPpu(sBox, 1.0f);
                if (sBox != null)
                {
                    Transform jp = sBox.Find("JapaneseSentence");
                    SetRect(jp, new Vector2(-28f, 26f), new Vector2(14f, -9f));
                    SetTmp(jp, zenFont, 16.5f, true, 12f, 16.5f);

                    Transform tr = sBox.Find("TranslationSentence");
                    SetRect(tr, new Vector2(-28f, -38f), new Vector2(14f, 8f));
                    SetTmp(tr, nunitoFont, 13.5f, true, 10f, 13.5f);
                }

                Transform btnAudio = card.Find("BtnAudio");
                SetRect(btnAudio, new Vector2(510f, 69f), new Vector2(0f, -530f));
                SetPreserveAspect(btnAudio, true);

                Transform row = card.Find("ActionButtonsRow");
                SetRect(row, new Vector2(510f, 70f), new Vector2(0f, -612f));
                if (row != null)
                {
                    Transform btnSave = row.Find("BtnSave");
                    SetRect(btnSave, new Vector2(348f, 86f), new Vector2(-12f, 0f));
                    SetPreserveAspect(btnSave, true);

                    Transform pBadge = card.Find("PointBadge") ?? btnSave?.Find("PointBadge");
                    if (pBadge != null)
                    {
                        if (pBadge.parent != card)
                            pBadge.SetParent(card, false);

                        var pbRt = pBadge.GetComponent<RectTransform>();
                        if (pbRt != null)
                        {
                            pbRt.anchorMin = new Vector2(0.5f, 1f);
                            pbRt.anchorMax = new Vector2(0.5f, 1f);
                            pbRt.pivot = new Vector2(1f, 1f);
                            pbRt.sizeDelta = new Vector2(114f, 28f);
                            pbRt.anchoredPosition = new Vector2(255f, -338f);
                        }
                        SetSlicedPpu(pBadge, 1.2f, pillSprite);
                        var pbImg = pBadge.GetComponent<Image>();
                        if (pbImg != null)
                        {
                            pbImg.preserveAspect = false;
                            pbImg.color = new Color(0.84f, 0.97f, 0.89f, 1f);
                        }
                        var pbTmp = SetTmp(pBadge.Find("PointText"), nunitoFont, 12.5f, false, 12.5f, 12.5f);
                        if (pbTmp != null)
                        {
                            pbTmp.textWrappingMode = TextWrappingModes.NoWrap;
                            pbTmp.color = new Color(0.04f, 0.50f, 0.32f);
                        }
                    }

                    Transform btnRetake = row.Find("BtnRetake");
                    SetRect(btnRetake, new Vector2(140f, 63f), new Vector2(0f, 1.5f));
                    SetPreserveAspect(btnRetake, true);
                }
            }

            SetRect(panel.Find("BottomHotkeyBar"), new Vector2(260f, 34f), new Vector2(0f, 22f));
            SetPreserveAspect(panel.Find("BottomHotkeyBar"), true);
            EditorUtility.SetDirty(panel.gameObject);
            return true;
        }

        private static bool RepairDictionaryBook(Transform canvasRoot, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont, Sprite pillSprite)
        {
            Transform spread = canvasRoot.Find("Dictionary/DictionaryView/BookContainer/BookSpread");
            if (spread == null) return false;

            RepairPhotoSpread(spread.Find("PhotoSpreadRoot"), zenFont, nunitoFont);
            RepairModuleSpread(spread.Find("ModuleSpreadRoot"), zenFont, nunitoFont, pillSprite);
            RepairKanaSpread(spread.Find("KanaSpreadRoot"), zenFont, nunitoFont, pillSprite);
            RepairQuizSpread(spread.Find("QuizSpreadRoot"), zenFont, nunitoFont, pillSprite);
            RepairFlashcardSpread(canvasRoot.Find("Dictionary/DictionaryView"), spread.Find("FlashcardSpreadRoot"), zenFont, nunitoFont, pillSprite);
            FixAllPillAndButtonImages(spread, pillSprite);

            return true;
        }

        private static void RepairPhotoSpread(Transform photoRoot, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont)
        {
            if (photoRoot == null) return;
            foreach (var card in photoRoot.GetComponentsInChildren<DictionaryPolaroidCardUI>(true))
            {
                Transform container = card.transform.Find("CardContainer") ?? card.transform.Find("PolaroidCard");
                if (container == null) continue;

                Transform front = container.Find("CardFace_Front");
                if (front != null)
                {
                    SetPreserveAspect(front, true);
                    SetRect(front.Find("PhotoFrame"), new Vector2(244f, 166f), new Vector2(0f, -20f));

                    Transform kanji = front.Find("KanjiLabel");
                    SetRect(kanji, new Vector2(244f, 50f), new Vector2(-111f, -194f));
                    var kTmp = SetTmp(kanji, zenFont, 20f, true, 12f, 20f);
                    if (kTmp != null)
                    {
                        kTmp.lineSpacing = -6f;
                        kTmp.textWrappingMode = TextWrappingModes.NoWrap;
                        kTmp.overflowMode = TextOverflowModes.Ellipsis;
                        kTmp.alignment = TextAlignmentOptions.TopLeft;
                    }

                    Transform mean = front.Find("MeaningLabel");
                    SetRect(mean, new Vector2(244f, 52f), new Vector2(-111f, -248f));
                    var mTmp = SetTmp(mean, nunitoFont, 16f, true, 10.5f, 16f);
                    if (mTmp != null)
                    {
                        mTmp.lineSpacing = -4f;
                        mTmp.textWrappingMode = TextWrappingModes.Normal;
                        mTmp.overflowMode = TextOverflowModes.Ellipsis;
                        mTmp.alignment = TextAlignmentOptions.TopLeft;
                    }

                    SetRect(front.Find("Btn_Like"), new Vector2(26f, 26f), new Vector2(-77f, 56.6f));
                    SetRect(front.Find("Btn_Favorite"), new Vector2(28f, 28f), new Vector2(-45.45f, 57.7f));
                }

                Transform back = container.Find("CardFace_Back");
                if (back != null)
                {
                    SetPreserveAspect(back, true);

                    Transform backKanji = back.Find("BackKanjiLabel");
                    SetRect(backKanji, new Vector2(244f, 42f), new Vector2(0f, -22f));
                    var bkTmp = SetTmp(backKanji, zenFont, 23f, true, 15f, 23f);
                    if (bkTmp != null)
                    {
                        bkTmp.textWrappingMode = TextWrappingModes.NoWrap;
                        bkTmp.overflowMode = TextOverflowModes.Ellipsis;
                        bkTmp.alignment = TextAlignmentOptions.Center;
                    }

                    Transform sentBox = back.Find("SentenceContainer");
                    if (sentBox != null)
                    {
                        var sentRt = sentBox.GetComponent<RectTransform>();
                        sentRt.anchorMin = new Vector2(0.5f, 1f);
                        sentRt.anchorMax = new Vector2(0.5f, 1f);
                        sentRt.pivot = new Vector2(0.5f, 1f);
                        sentRt.sizeDelta = new Vector2(244f, 212f);
                        sentRt.anchoredPosition = new Vector2(0f, -66f);

                        var vlg = sentBox.GetComponent<VerticalLayoutGroup>() ?? sentBox.gameObject.AddComponent<VerticalLayoutGroup>();
                        vlg.padding = new RectOffset(10, 10, 8, 8);
                        vlg.spacing = 8f;
                        vlg.childAlignment = TextAnchor.MiddleCenter;
                        vlg.childControlWidth = true;
                        vlg.childControlHeight = true;
                        vlg.childForceExpandWidth = true;
                        vlg.childForceExpandHeight = true;

                        Transform jp = sentBox.Find("SentenceJapanese");
                        if (jp != null)
                        {
                            var le = jp.GetComponent<LayoutElement>() ?? jp.gameObject.AddComponent<LayoutElement>();
                            le.flexibleHeight = 1.25f;
                            var tmp = SetTmp(jp, zenFont, 15.5f, true, 10.5f, 15.5f);
                            if (tmp != null)
                            {
                                tmp.lineSpacing = -2f;
                                tmp.textWrappingMode = TextWrappingModes.Normal;
                                tmp.overflowMode = TextOverflowModes.Ellipsis;
                                tmp.alignment = TextAlignmentOptions.Center;
                            }
                        }

                        Transform en = sentBox.Find("SentenceEnglish");
                        if (en != null)
                        {
                            var le = en.GetComponent<LayoutElement>() ?? en.gameObject.AddComponent<LayoutElement>();
                            le.flexibleHeight = 1f;
                            var tmp = SetTmp(en, nunitoFont, 13.5f, true, 9.5f, 13.5f);
                            if (tmp != null)
                            {
                                tmp.lineSpacing = -2f;
                                tmp.textWrappingMode = TextWrappingModes.Normal;
                                tmp.overflowMode = TextOverflowModes.Ellipsis;
                                tmp.alignment = TextAlignmentOptions.Center;
                            }
                        }

                        LayoutRebuilder.ForceRebuildLayoutImmediate(sentRt);
                    }

                    Transform hint = back.Find("BackFlipHint");
                    SetRect(hint, new Vector2(244f, 22f), new Vector2(0f, 30f));
                    var hTmp = SetTmp(hint, nunitoFont, 12f, false, 12f, 12f);
                    if (hTmp != null) hTmp.color = new Color(0.45f, 0.50f, 0.58f);
                }

                EditorUtility.SetDirty(card.gameObject);
            }
        }

        private static void RepairModuleSpread(Transform modRoot, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont, Sprite pillSprite)
        {
            if (modRoot == null) return;

            Transform leftPage = modRoot.Find("LeftModulePage_TOC");
            if (leftPage != null)
            {
                Transform tocHeader = leftPage.Find("TOC_Header");
                if (tocHeader != null)
                {
                    var rt = tocHeader.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(-40f, 84f);
                    var vlg = tocHeader.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null) vlg.spacing = 4f;
                    SetTmp(tocHeader.Find("Title"), zenFont, 24f, true, 18f, 24f);
                    var subTmp = SetTmp(tocHeader.Find("Subtitle"), nunitoFont, 16f, true, 12f, 16f);
                    if (subTmp != null) subTmp.color = new Color(0.38f, 0.34f, 0.48f);
                }

                Transform tocScroll = leftPage.Find("TOC_ScrollRect");
                if (tocScroll != null)
                {
                    var rt = tocScroll.GetComponent<RectTransform>();
                    rt.offsetMax = new Vector2(-25f, -106f);

                    Transform content = tocScroll.Find("Viewport/Content");
                    if (content != null)
                    {
                        for (int i = 0; i < content.childCount; i++)
                        {
                            Transform chBtn = content.GetChild(i);
                            var vlg = chBtn.GetComponent<VerticalLayoutGroup>();
                            if (vlg != null) { vlg.padding = new RectOffset(18, 18, 12, 12); vlg.spacing = 6f; }
                            var le = chBtn.GetComponent<LayoutElement>();
                            if (le != null) le.minHeight = 68f;

                            Transform topRow = chBtn.Find("TopRow");
                            if (topRow != null)
                            {
                                var hlg = topRow.GetComponent<HorizontalLayoutGroup>();
                                if (hlg != null) { hlg.spacing = 10f; hlg.childAlignment = TextAnchor.MiddleLeft; }
                                SetTmp(topRow.Find("Title"), zenFont, 20f, true, 15f, 20f);

                                Transform badge = topRow.Find("Badge");
                                if (badge != null)
                                {
                                    var bLe = badge.GetComponent<LayoutElement>();
                                    if (bLe != null) { bLe.preferredWidth = 108f; bLe.preferredHeight = 28f; }
                                    SetSlicedPpu(badge, 1.2f, pillSprite);
                                    var bTmp = SetTmp(badge.Find("Text"), nunitoFont, 13f, false, 13f, 13f);
                                    if (bTmp != null) bTmp.textWrappingMode = TextWrappingModes.NoWrap;
                                }
                            }

                            var chSub = SetTmp(chBtn.Find("Subtitle"), nunitoFont, 15f, true, 12f, 15f);
                            if (chSub != null) chSub.color = new Color(0.38f, 0.42f, 0.54f);
                        }
                    }
                }
            }

            Transform rightContent = modRoot.Find("RightModulePage_Reader/ModuleScrollRect/Viewport/Content");
            if (rightContent != null)
            {
                for (int c = 0; c < rightContent.childCount; c++)
                {
                    Transform sec = rightContent.GetChild(c);
                    Transform banner = sec.Find("ChapterBanner");
                    if (banner != null)
                    {
                        var vlg = banner.GetComponent<VerticalLayoutGroup>();
                        if (vlg != null) vlg.padding = new RectOffset(16, 16, 10, 10);
                        var le = banner.GetComponent<LayoutElement>();
                        if (le != null) le.minHeight = 52f;
                        SetSlicedPpu(banner, 1.6f);
                        SetTmp(banner.Find("Title"), zenFont, 22f, true, 15f, 22f);
                    }

                    for (int m = 0; m < sec.childCount; m++)
                    {
                        Transform card = sec.GetChild(m);
                        if (!card.name.StartsWith("Card_Mod_")) continue;

                        var mVlg = card.GetComponent<VerticalLayoutGroup>();
                        if (mVlg != null) { mVlg.spacing = 12f; mVlg.padding = new RectOffset(18, 18, 16, 16); }

                        Transform header = card.Find("Header");
                        if (header != null)
                            SetTmp(header.Find("Title"), zenFont, 23f, true, 17f, 23f);

                        Transform imgCont = card.Find("ImageContainer");
                        if (imgCont != null)
                        {
                            var imgLe = imgCont.GetComponent<LayoutElement>();
                            if (imgLe != null) imgLe.preferredHeight = 235f;
                        }

                        for (int a = 0; a < card.childCount; a++)
                        {
                            Transform box = card.GetChild(a);
                            if (!box.name.StartsWith("PhraseBox_")) continue;

                            var bVlg = box.GetComponent<VerticalLayoutGroup>();
                            if (bVlg != null) { bVlg.spacing = 6f; bVlg.padding = new RectOffset(16, 16, 12, 12); }

                            SetTmp(box.Find("Goal"), nunitoFont, 17f, false, 17f, 17f);
                            var jpTmp = SetTmp(box.Find("JapanesePhrase"), zenFont, 25f, true, 18f, 25f);
                            if (jpTmp != null) jpTmp.lineSpacing = 4f;

                            var expTmp = SetTmp(box.Find("Explanation"), nunitoFont, 15.5f, false, 15.5f, 15.5f);
                            if (expTmp != null) { expTmp.lineSpacing = 4f; expTmp.color = new Color(0.24f, 0.28f, 0.36f); }

                            var kwTmp = SetTmp(box.Find("Keywords"), zenFont, 15f, false, 15f, 15f);
                            if (kwTmp != null) kwTmp.lineSpacing = 3f;
                        }
                    }
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(rightContent.GetComponent<RectTransform>());
            }

            EditorUtility.SetDirty(modRoot.gameObject);
        }

        private static void RepairKanaSpread(Transform kanaRoot, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont, Sprite pillSprite)
        {
            if (kanaRoot == null) return;
            for (int p = 0; p < kanaRoot.childCount; p++)
            {
                Transform page = kanaRoot.GetChild(p);
                RepairPageHeader(page.Find("PageHeader"), zenFont, nunitoFont);

                Transform subBar = page.Find("SubTabBar");
                if (subBar != null)
                {
                    for (int i = 0; i < subBar.childCount; i++)
                    {
                        Transform tab = subBar.GetChild(i);
                        SetSlicedPpu(tab, 0.85f, pillSprite);
                        var tmp = SetTmp(tab.Find("Text"), zenFont, 16.5f, true, 12f, 16.5f);
                        if (tmp != null) tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    }
                }

                for (int s = 0; s < page.childCount; s++)
                {
                    Transform spreadChild = page.GetChild(s);
                    Transform headers = spreadChild.Find("Headers");
                    if (headers != null)
                    {
                        var hRt = headers.GetComponent<RectTransform>();
                        hRt.sizeDelta = new Vector2(680f, 36f);
                        for (int c = 0; c < headers.childCount; c++)
                        {
                            Transform col = headers.GetChild(c);
                            SetSlicedPpu(col, 1f, pillSprite);
                            SetTmp(col.Find("Text"), zenFont, 16f, true, 12f, 16f);
                        }
                    }

                    Transform table = spreadChild.Find("TableGrid");
                    if (table != null)
                    {
                        for (int k = 0; k < table.childCount; k++)
                        {
                            Transform cell = table.GetChild(k);
                            SetTmp(cell.Find("Kana"), zenFont, 26f, false, 26f, 26f);
                            SetTmp(cell.Find("Romaji"), nunitoFont, 15f, false, 15f, 15f);
                        }
                    }

                    Transform guide = spreadChild.Find("GuideBox");
                    if (guide != null)
                    {
                        Transform badge = guide.Find("Badge");
                        SetRect(badge, new Vector2(380f, 32f), new Vector2(0f, -10f));
                        SetSlicedPpu(badge, 1.05f, pillSprite);
                        if (badge != null) SetTmp(badge.Find("Text"), zenFont, 15.5f, false, 15.5f, 15.5f);

                        Transform content = guide.Find("Content");
                        if (content != null)
                        {
                            var cRt = content.GetComponent<RectTransform>();
                            cRt.offsetMax = new Vector2(-16f, -46f);
                            var cTmp = SetTmp(content, zenFont, 14.5f, true, 11f, 14.5f);
                            if (cTmp != null) cTmp.lineSpacing = 3f;
                        }
                    }
                }
            }
            EditorUtility.SetDirty(kanaRoot.gameObject);
        }

        private static void RepairQuizSpread(Transform quizRoot, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont, Sprite pillSprite)
        {
            if (quizRoot == null) return;
            Transform active = quizRoot.Find("ActiveQuizRoot");
            if (active != null)
            {
                Transform left = active.Find("LeftQuizPage");
                if (left != null)
                {
                    RepairPageHeader(left.Find("PageHeader"), zenFont, nunitoFont);
                    Transform statusRow = left.Find("StatusRow");
                    if (statusRow != null)
                    {
                        foreach (string bName in new[] { "ProgressBadge", "ScoreBadge" })
                        {
                            Transform b = statusRow.Find(bName);
                            SetSlicedPpu(b, 0.85f, pillSprite);
                            if (b != null)
                            {
                                var t = SetTmp(b.Find("Text"), zenFont, 18f, false, 18f, 18f);
                                if (t != null) t.textWrappingMode = TextWrappingModes.NoWrap;
                            }
                        }
                    }

                    Transform catBadge = left.Find("QuestionCard/CategoryBadge");
                    if (catBadge != null)
                    {
                        var le = catBadge.GetComponent<LayoutElement>();
                        if (le != null) le.preferredHeight = 36f;
                        SetSlicedPpu(catBadge, 0.95f, pillSprite);
                        SetTmp(catBadge.Find("Text"), nunitoFont, 16f, true, 12f, 16f);
                    }
                }

                Transform right = active.Find("RightQuizPage");
                if (right != null)
                {
                    RepairPageHeader(right.Find("PageHeader"), zenFont, nunitoFont);
                    Transform choices = right.Find("ChoicesContainer");
                    if (choices != null)
                    {
                        for (int i = 0; i < choices.childCount; i++)
                        {
                            Transform letter = choices.GetChild(i).Find("LetterBadge");
                            SetSlicedPpu(letter, 0.75f, pillSprite);
                            if (letter != null) SetTmp(letter.Find("Text"), zenFont, 21f, false, 21f, 21f);
                        }
                    }

                    Transform fb = right.Find("FeedbackContainer");
                    if (fb != null)
                    {
                        Transform banner = fb.Find("Banner");
                        if (banner != null)
                        {
                            var le = banner.GetComponent<LayoutElement>();
                            if (le != null) le.preferredHeight = 36f;
                            SetSlicedPpu(banner, 0.95f, pillSprite);
                            SetTmp(banner.Find("Text"), zenFont, 18f, false, 18f, 18f);
                        }
                        SetTmp(fb.Find("SentenceText"), zenFont, 17f, true, 13f, 17f);
                    }

                    Transform nextBtn = right.Find("NextQuestionButton");
                    if (nextBtn != null)
                    {
                        SetRect(nextBtn, new Vector2(680f, 50f), new Vector2(0f, -700f));
                        SetSlicedPpu(nextBtn, 0.72f, pillSprite);
                        SetTmp(nextBtn.Find("Text"), zenFont, 20f, false, 20f, 20f);
                    }
                }
            }

            Transform emptyCard = quizRoot.Find("EmptyStateRoot/EmptyCard");
            if (emptyCard != null)
            {
                Transform lb = emptyCard.Find("LockBadge");
                SetSlicedPpu(lb, 0.85f, pillSprite);
                if (lb != null) SetTmp(lb.Find("Text"), zenFont, 19f, false, 19f, 19f);

                Transform demo = emptyCard.Find("DemoPracticeButton");
                SetSlicedPpu(demo, 0.72f, pillSprite);
                if (demo != null) SetTmp(demo.Find("Text"), zenFont, 19f, false, 19f, 19f);
            }

            Transform resCard = quizRoot.Find("ResultSummaryRoot/ResultCard");
            if (resCard != null)
            {
                Transform rb = resCard.Find("Badge");
                SetSlicedPpu(rb, 0.85f, pillSprite);
                if (rb != null) SetTmp(rb.Find("Text"), zenFont, 19f, false, 19f, 19f);

                Transform pa = resCard.Find("PlayAgainButton");
                if (pa != null)
                {
                    var le = pa.GetComponent<LayoutElement>();
                    if (le != null) le.preferredHeight = 52f;
                    SetSlicedPpu(pa, 0.72f, pillSprite);
                    SetTmp(pa.Find("Text"), zenFont, 20f, false, 20f, 20f);
                }
            }
            EditorUtility.SetDirty(quizRoot.gameObject);
        }

        private static void RepairFlashcardSpread(Transform dictView, Transform flashRoot, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont, Sprite pillSprite)
        {
            if (flashRoot == null) return;
            Transform left = flashRoot.Find("LeftFlashcardPage");
            if (left != null)
            {
                SetTmp(left.Find("PageTitle"), zenFont, 28f, true, 20f, 28f);
                SetTmp(left.Find("PageSubtitle"), nunitoFont, 16.5f, false, 16.5f, 16.5f);

                Transform summary = left.Find("DeckSummary");
                SetSlicedPpu(summary, 0.85f, pillSprite);
                if (summary != null) SetTmp(summary.Find("Text"), zenFont, 17f, false, 17f, 17f);

                Transform filterRow = left.Find("FilterRow");
                if (filterRow != null)
                {
                    for (int i = 0; i < filterRow.childCount; i++)
                    {
                        Transform b = filterRow.GetChild(i);
                        SetSlicedPpu(b, 2.0f);
                        var t = SetTmp(b.Find("Text"), zenFont, 16.5f, false, 16.5f, 16.5f);
                        if (t != null) t.textWrappingMode = TextWrappingModes.NoWrap;
                    }
                }

                Transform actionRow = left.Find("ActionRow");
                if (actionRow != null)
                {
                    for (int i = 0; i < actionRow.childCount; i++)
                    {
                        Transform b = actionRow.GetChild(i);
                        SetSlicedPpu(b, 1.85f);
                        var t = SetTmp(b.Find("Text"), zenFont, 17.5f, false, 17.5f, 17.5f);
                        if (t != null) t.textWrappingMode = TextWrappingModes.NoWrap;
                    }
                }

                Transform itemTmpl = left.Find("CardScrollView/Viewport/Content/FlashcardItemTemplate");
                if (itemTmpl != null)
                {
                    var t1 = SetTmp(itemTmpl.Find("TitleText"), zenFont, 19f, true, 13f, 19f);
                    if (t1 != null) t1.overflowMode = TextOverflowModes.Ellipsis;
                    var t2 = SetTmp(itemTmpl.Find("TransText"), nunitoFont, 15f, true, 11f, 15f);
                    if (t2 != null) t2.overflowMode = TextOverflowModes.Ellipsis;
                    Transform sb = itemTmpl.Find("StatusBadge");
                    SetSlicedPpu(sb, 1.25f, pillSprite);
                    if (sb != null) SetTmp(sb.Find("Text"), zenFont, 14f, false, 14f, 14f);
                }
            }

            Transform right = flashRoot.Find("RightFlashcardPage");
            if (right != null)
            {
                SetTmp(right.Find("PageTitle"), zenFont, 28f, true, 20f, 28f);
                SetTmp(right.Find("PageSubtitle"), nunitoFont, 16.5f, false, 16.5f, 16.5f);

                Transform catBadge = right.Find("StudyCardContainer/FlipCardRoot/FrontSide/CategoryBadge");
                SetSlicedPpu(catBadge, 0.9f, pillSprite);
                if (catBadge != null) SetTmp(catBadge.Find("Text"), nunitoFont, 16.5f, true, 12f, 16.5f);

                foreach (string rowPath in new[]
                {
                    "StudyCardContainer/FlipCardRoot/FrontSide/FrontActionRow",
                    "StudyCardContainer/FlipCardRoot/BackSide/BackActionRow",
                    "StudyCardContainer/ReviewRow",
                    "StudyCardContainer/ManageRow"
                })
                {
                    Transform row = right.Find(rowPath);
                    if (row == null) continue;
                    for (int i = 0; i < row.childCount; i++)
                    {
                        Transform b = row.GetChild(i);
                        SetSlicedPpu(b, 1.85f);
                        var t = SetTmp(b.Find("Text"), zenFont, 17.5f, false, 17.5f, 17.5f);
                        if (t != null) t.textWrappingMode = TextWrappingModes.NoWrap;
                    }
                }
            }

            if (dictView != null)
            {
                Transform btnRow = dictView.Find("FlashcardFormModal/ModalBox/ButtonRow");
                if (btnRow != null)
                {
                    for (int i = 0; i < btnRow.childCount; i++)
                    {
                        Transform b = btnRow.GetChild(i);
                        SetSlicedPpu(b, 1.85f);
                        SetTmp(b.Find("Text"), zenFont, 17.5f, false, 17.5f, 17.5f);
                    }
                }
            }
            EditorUtility.SetDirty(flashRoot.gameObject);
        }

        private static void FixAllPillAndButtonImages(Transform spread, Sprite pillSprite)
        {
            if (spread == null) return;
            foreach (var img in spread.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null) continue;
                string sName = img.sprite.name;
                if (sName.Contains("Category Pill"))
                {
                    img.type = Image.Type.Sliced;
                    float h = img.rectTransform.sizeDelta.y;
                    if (h <= 0f) h = img.rectTransform.rect.height;
                    if (h > 10f)
                        img.pixelsPerUnitMultiplier = Mathf.Clamp(34f / h, 0.72f, 1.5f);
                }
                else if (sName.Contains("Button_Selected") || sName.Contains("Button_Unselected"))
                {
                    img.type = Image.Type.Sliced;
                    float h = img.rectTransform.sizeDelta.y;
                    if (h <= 0f) h = img.rectTransform.rect.height;
                    if (h > 10f && h < 58f)
                        img.pixelsPerUnitMultiplier = Mathf.Clamp(84f / h, 1.4f, 2.2f);
                }
            }
        }

        private static void RepairPageHeader(Transform header, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont)
        {
            if (header == null) return;
            SetTmp(header.Find("Title"), zenFont, 26f, true, 20f, 26f);
            Transform sub = header.Find("Subtitle");
            SetRect(sub, new Vector2(0f, 28f), Vector2.zero);
            var sTmp = SetTmp(sub, nunitoFont, 16f, true, 12f, 16f);
            if (sTmp != null) sTmp.color = new Color(0.42f, 0.38f, 0.54f);
        }

        private static void SetRect(Transform t, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            if (t == null) return;
            var rt = t.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
        }

        private static void SetPreserveAspect(Transform t, bool preserve)
        {
            if (t == null) return;
            var img = t.GetComponent<Image>();
            if (img != null) img.preserveAspect = preserve;
        }

        private static void SetSlicedPpu(Transform t, float ppu, Sprite overrideSprite = null)
        {
            if (t == null) return;
            var img = t.GetComponent<Image>();
            if (img == null) return;
            if (overrideSprite != null) img.sprite = overrideSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppu;
        }

        private static TextMeshProUGUI SetTmp(Transform t, TMP_FontAsset font, float size, bool autoSize, float min, float max)
        {
            if (t == null) return null;
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return null;
            if (font != null) tmp.font = font;
            tmp.fontSize = size;
            tmp.enableAutoSizing = autoSize;
            if (autoSize)
            {
                tmp.fontSizeMin = min;
                tmp.fontSizeMax = max;
            }
            return tmp;
        }
    }
}
#endif
