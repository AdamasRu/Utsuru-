#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utsuru.Capture;
using Utsuru.Store;

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
            FixAllPillAndButtonImages(spread, pillSprite);

            return true;
        }

        internal static bool RepairPhotoSpread(Transform photoRoot, TMP_FontAsset zenFont, TMP_FontAsset nunitoFont)
        {
            if (photoRoot == null) return false;
            bool anyChanged = false;
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
                    SetRect(kanji, new Vector2(244f, 38f), new Vector2(-122f, -195f));
                    var kTmp = SetTmp(kanji, zenFont, 24f, true, 10.5f, 24f);
                    if (kTmp != null)
                    {
                        kTmp.fontStyle = FontStyles.Normal;
                        kTmp.lineSpacing = 0f;
                        kTmp.textWrappingMode = TextWrappingModes.NoWrap;
                        kTmp.overflowMode = TextOverflowModes.Ellipsis;
                        kTmp.alignment = TextAlignmentOptions.MidlineLeft;
                        kTmp.color = new Color(0.12f, 0.10f, 0.10f, 1f);
                        EditorUtility.SetDirty(kTmp);
                        anyChanged = true;
                    }

                    Transform mean = front.Find("MeaningLabel");
                    SetRect(mean, new Vector2(244f, 44f), new Vector2(-122f, -234f));
                    var mTmp = SetTmp(mean, nunitoFont, 14.5f, true, 9.5f, 14.5f);
                    if (mTmp != null)
                    {
                        mTmp.fontStyle = FontStyles.Bold;
                        mTmp.lineSpacing = 2f;
                        mTmp.textWrappingMode = TextWrappingModes.Normal;
                        mTmp.overflowMode = TextOverflowModes.Ellipsis;
                        mTmp.alignment = TextAlignmentOptions.TopLeft;
                        mTmp.color = new Color(0.118f, 0.388f, 0.914f, 1f);
                        EditorUtility.SetDirty(mTmp);
                        anyChanged = true;
                    }

                    SetRect(front.Find("Btn_Like"), new Vector2(26f, 26f), new Vector2(-77f, 56.6f));
                    SetRect(front.Find("Btn_Favorite"), new Vector2(28f, 28f), new Vector2(-45.45f, 57.7f));
                }

                Transform back = container.Find("CardFace_Back");
                if (back != null)
                {
                    SetPreserveAspect(back, true);

                    Transform backKanji = back.Find("BackKanjiLabel");
                    if (backKanji != null && backKanji.gameObject.activeSelf)
                    {
                        backKanji.gameObject.SetActive(false);
                        EditorUtility.SetDirty(backKanji.gameObject);
                        anyChanged = true;
                    }

                    Transform sentBox = back.Find("SentenceContainer");
                    if (sentBox != null)
                    {
                        var sentRt = sentBox.GetComponent<RectTransform>();
                        sentRt.anchorMin = new Vector2(0.5f, 1f);
                        sentRt.anchorMax = new Vector2(0.5f, 1f);
                        sentRt.pivot = new Vector2(0.5f, 1f);
                        sentRt.sizeDelta = new Vector2(236f, 265f);
                        sentRt.anchoredPosition = new Vector2(0f, -34f);

                        var vlg = sentBox.GetComponent<VerticalLayoutGroup>() ?? sentBox.gameObject.AddComponent<VerticalLayoutGroup>();
                        vlg.padding = new RectOffset(6, 6, 4, 4);
                        vlg.spacing = 12f;
                        vlg.childAlignment = TextAnchor.UpperLeft;
                        vlg.childControlWidth = true;
                        vlg.childControlHeight = true;
                        vlg.childForceExpandWidth = true;
                        vlg.childForceExpandHeight = false;

                        Transform jp = sentBox.Find("SentenceJapanese");
                        if (jp != null)
                        {
                            var le = jp.GetComponent<LayoutElement>() ?? jp.gameObject.AddComponent<LayoutElement>();
                            le.flexibleHeight = 0f;
                            var tmp = SetTmp(jp, zenFont, 18.5f, true, 12f, 18.5f);
                            if (tmp != null)
                            {
                                tmp.fontStyle = FontStyles.Normal;
                                tmp.lineSpacing = 6f;
                                tmp.textWrappingMode = TextWrappingModes.Normal;
                                tmp.overflowMode = TextOverflowModes.Ellipsis;
                                tmp.alignment = TextAlignmentOptions.TopLeft;
                                tmp.color = new Color(0.14f, 0.12f, 0.12f, 1f);
                                EditorUtility.SetDirty(tmp);
                                anyChanged = true;
                            }
                        }

                        Transform en = sentBox.Find("SentenceEnglish");
                        if (en != null)
                        {
                            var le = en.GetComponent<LayoutElement>() ?? en.gameObject.AddComponent<LayoutElement>();
                            le.flexibleHeight = 0f;
                            var tmp = SetTmp(en, nunitoFont, 14.5f, true, 10f, 14.5f);
                            if (tmp != null)
                            {
                                tmp.fontStyle = FontStyles.Italic;
                                tmp.lineSpacing = 4f;
                                tmp.textWrappingMode = TextWrappingModes.Normal;
                                tmp.overflowMode = TextOverflowModes.Ellipsis;
                                tmp.alignment = TextAlignmentOptions.TopLeft;
                                tmp.color = new Color(0.36f, 0.34f, 0.33f, 1f);
                                EditorUtility.SetDirty(tmp);
                                anyChanged = true;
                            }
                        }

                        LayoutRebuilder.ForceRebuildLayoutImmediate(sentRt);
                    }

                    Transform hint = back.Find("BackFlipHint");
                    if (hint != null && hint.gameObject.activeSelf)
                    {
                        hint.gameObject.SetActive(false);
                        EditorUtility.SetDirty(hint.gameObject);
                        anyChanged = true;
                    }
                }

                EditorUtility.SetDirty(card.gameObject);
            }
            return anyChanged;
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

    /// <summary>
    /// Automatically configures HUD hint pills in Day_Showcase.unity:
    /// - CameraHint (C) at bottom-left
    /// - ScoreBadge (Assets/UI/Score.png) + ScoreText at top-left
    /// - StoreHint (T), AchievementHint (Q), DictionaryHint (Tab) aligned top-right (left-to-right: T, Q, Tab)
    /// - Dictionary toggle key -> Tab, Store toggle key -> T
    /// - Wired into CameraModeController so all HUD hints hide cleanly in Camera Mode.
    /// </summary>
    [InitializeOnLoad]
    public static class UtsuruHudButtonsSetupEditor
    {
        private const string DayShowcaseScenePath = "Assets/Japanese_Street/Scenes/Day_Showcase.unity";
        private const string PrefKey = "Utsuru_HudButtonsSetup_Version";
        private const string TargetVersion = "v10_20261127";

        private const string AchievementBtnAssetPath = "Assets/UI/achievment btn.png";
        private const string StoreBtnAssetPath = "Assets/UI/Store/store button.png";
        private const string DictionaryBtnAssetPath = "Assets/UI/Camera/dictionary_hint.png";
        private const string CameraBtnAssetPath = "Assets/UI/Camera/camera_hint.png";
        private const string ScoreBgAssetPath = "Assets/UI/Score.png";
        private const string CoverCounterPillAssetPath = "Assets/UI/Dictionary/Book/Book_CoverCounterPill.png";

        static UtsuruHudButtonsSetupEditor()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureSetupOnEditorLoad;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (Application.isPlaying) return;
            if (scene.path == DayShowcaseScenePath)
            {
                EnsureSetupIfNeeded(force: false, saveScene: !scene.isDirty);
            }
        }

        private static void EnsureSetupOnEditorLoad()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene active = EditorSceneManager.GetActiveScene();
            if (!active.IsValid()) return;

            if (active.path == DayShowcaseScenePath)
            {
                EnsureSetupIfNeeded(force: false, saveScene: !active.isDirty);
                return;
            }

            if (EditorPrefs.GetString(PrefKey, string.Empty) != TargetVersion && !active.isDirty)
            {
                string prevPath = active.path;
                Scene dayScene = EditorSceneManager.OpenScene(DayShowcaseScenePath, OpenSceneMode.Single);
                if (EnsureSetupInternal(dayScene, saveScene: true))
                {
                    EditorPrefs.SetString(PrefKey, TargetVersion);
                }
                if (!string.IsNullOrEmpty(prevPath))
                {
                    EditorSceneManager.OpenScene(prevPath, OpenSceneMode.Single);
                }
            }
        }

        [MenuItem("Tools/Utsuru/Setup HUD Hint Buttons (Tab, Q, T)")]
        public static void SetupMenu()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (!active.IsValid() || active.path != DayShowcaseScenePath)
            {
                if (active.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                active = EditorSceneManager.OpenScene(DayShowcaseScenePath, OpenSceneMode.Single);
            }

            if (EnsureSetupInternal(active, saveScene: true))
            {
                EditorPrefs.SetString(PrefKey, TargetVersion);
            }
        }

        private static void EnsureSetupIfNeeded(bool force, bool saveScene)
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (!active.IsValid() || active.path != DayShowcaseScenePath) return;

            bool needsSetup = force || EditorPrefs.GetString(PrefKey, string.Empty) != TargetVersion;
            if (!needsSetup)
            {
                Canvas canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform camMech = canvas.transform.Find("Camera Mechanic");
                    if (camMech != null && (camMech.Find("AchievementHint") == null || camMech.Find("StoreHint") == null))
                    {
                        needsSetup = true;
                    }
                    if (canvas.transform.Find("ScoreBadge") == null)
                    {
                        needsSetup = true;
                    }
                    if (canvas.transform.Find("Dictionary/DictionaryView/BookContainer/DiscoveryProgressBadge") == null)
                    {
                        needsSetup = true;
                    }
                    if (canvas.transform.Find("Dictionary/DictionaryView/BookContainer/TopTabs/Tab_Flashcard") != null ||
                        canvas.transform.Find("Dictionary/DictionaryView/BookContainer/BookSpread/FlashcardSpreadRoot") != null ||
                        canvas.transform.Find("Dictionary/DictionaryView/FlashcardFormModal") != null)
                    {
                        needsSetup = true;
                    }
                    Transform dictObj = canvas.transform.Find("Dictionary");
                    if (dictObj != null && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(dictObj.gameObject) > 0)
                    {
                        needsSetup = true;
                    }
                }
            }

            if (!needsSetup) return;

            if (EnsureSetupInternal(active, saveScene))
            {
                EditorPrefs.SetString(PrefKey, TargetVersion);
            }
        }

        private static bool EnsureSetupInternal(Scene scene, bool saveScene)
        {
            bool changed = false;

            // 1. DictionaryUIController.toggleKey -> KeyCode.Tab
            DictionaryUIController dictCtrl = Object.FindFirstObjectByType<DictionaryUIController>(FindObjectsInactive.Include);
            if (dictCtrl != null)
            {
                SerializedObject so = new SerializedObject(dictCtrl);
                SerializedProperty prop = so.FindProperty("toggleKey");
                if (prop != null && prop.intValue != (int)KeyCode.Tab)
                {
                    prop.intValue = (int)KeyCode.Tab;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(dictCtrl);
                    changed = true;
                }
            }

            // 2. StoreUIController.toggleKey -> KeyCode.T
            StoreUIController storeCtrl = Object.FindFirstObjectByType<StoreUIController>(FindObjectsInactive.Include);
            if (storeCtrl != null)
            {
                SerializedObject so = new SerializedObject(storeCtrl);
                SerializedProperty prop = so.FindProperty("toggleKey");
                if (prop != null && prop.intValue != (int)KeyCode.T)
                {
                    prop.intValue = (int)KeyCode.T;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(storeCtrl);
                    changed = true;
                }
            }

            // 3. Ensure AchievementHint & StoreHint under Canvas/Camera Mechanic
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform camMech = canvas.transform.Find("Camera Mechanic");
                if (camMech != null)
                {
                    Sprite achSprite = LoadSpriteAsset(AchievementBtnAssetPath);
                    Sprite storeSprite = LoadSpriteAsset(StoreBtnAssetPath);
                    Sprite dictSprite = LoadSpriteAsset(DictionaryBtnAssetPath);
                    Sprite camSprite = LoadSpriteAsset(CameraBtnAssetPath);

                    // Bottom-left: Camera [C]
                    GameObject camHint = EnsureHintObject(
                        camMech,
                        "CameraHint",
                        camSprite,
                        Vector2.zero,
                        Vector2.zero,
                        Vector2.zero,
                        new Vector2(37.5f, 30f),
                        new Vector2(394f, 124f),
                        ref changed
                    );

                    // Top-right row (left-to-right: Store [T], Achievement [Q], Dictionary [Tab])
                    GameObject storeHint = EnsureHintObject(
                        camMech,
                        "StoreHint",
                        storeSprite,
                        Vector2.one,
                        Vector2.one,
                        Vector2.one,
                        new Vector2(-312f, -5f),
                        new Vector2(196f, 141f),
                        ref changed
                    );

                    GameObject achHint = EnsureHintObject(
                        camMech,
                        "AchievementHint",
                        achSprite,
                        Vector2.one,
                        Vector2.one,
                        Vector2.one,
                        new Vector2(-161f, -4f),
                        new Vector2(198f, 142f),
                        ref changed
                    );

                    GameObject dictHint = EnsureHintObject(
                        camMech,
                        "DictionaryHint",
                        dictSprite,
                        Vector2.one,
                        Vector2.one,
                        Vector2.one,
                        new Vector2(3f, -6f),
                        new Vector2(209f, 132f),
                        ref changed
                    );

                    if (camHint != null)
                    {
                        int baseIdx = camHint.transform.GetSiblingIndex();
                        if (storeHint != null && storeHint.transform.GetSiblingIndex() != baseIdx + 1)
                        {
                            storeHint.transform.SetSiblingIndex(baseIdx + 1);
                            changed = true;
                        }
                        if (achHint != null && achHint.transform.GetSiblingIndex() != baseIdx + 2)
                        {
                            achHint.transform.SetSiblingIndex(baseIdx + 2);
                            changed = true;
                        }
                        if (dictHint != null && dictHint.transform.GetSiblingIndex() != baseIdx + 3)
                        {
                            dictHint.transform.SetSiblingIndex(baseIdx + 3);
                            changed = true;
                        }
                    }

                    CameraModeController camMode = Object.FindFirstObjectByType<CameraModeController>(FindObjectsInactive.Include);
                    if (camMode != null)
                    {
                        SerializedObject camSo = new SerializedObject(camMode);
                        changed |= AssignObjectPropIfDifferent(camSo.FindProperty("cameraHintObject"), camHint);
                        changed |= AssignObjectPropIfDifferent(camSo.FindProperty("dictionaryHintObject"), dictHint);
                        changed |= AssignObjectPropIfDifferent(camSo.FindProperty("achievementHintObject"), achHint);
                        changed |= AssignObjectPropIfDifferent(camSo.FindProperty("storeHintObject"), storeHint);
                        if (camSo.ApplyModifiedProperties())
                        {
                            EditorUtility.SetDirty(camMode);
                            changed = true;
                        }
                    }
                }

                // Top-left ScoreBadge using Assets/UI/Score.png + ScoreText centered in right area of pill
                EnsureTopLeftScoreBadge(canvas.transform, ref changed);

                // Dictionary bottom-center cover clasp pill (X / 72) + Polaroid card typography
                EnsureDictionaryCoverProgressBadge(canvas.transform, ref changed);
                Transform photoSpread = canvas.transform.Find("Dictionary/DictionaryView/BookContainer/BookSpread/PhotoSpreadRoot");
                if (photoSpread != null)
                {
                    TMP_FontAsset zenFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ZenMaruGothic-Bold SDF.asset");
                    TMP_FontAsset nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");
                    changed |= UtsuruUILayoutRepairEditor.RepairPhotoSpread(photoSpread, zenFont, nunitoFont);
                }

                // Hapus fitur Flashcard dari scene + perbaiki layout hasil Quiz (kiri hasil, kanan logo Utsuru)
                RemoveFlashcardFeatureFromScene(canvas.transform, ref changed);
                EnsureQuizTranslationHiddenAndResultLayout(canvas.transform, ref changed);
                EnsureCameraAudioClips(ref changed);
                EnsureGlobalButtonSoundPrefab();
                EnsureBgmManagerPrefab();

                Transform storeHintTxt = canvas.transform.Find("Dictionary/DictionaryView/BookContainer/BookSpread/ThemeSpreadRoot/RightThemePage/BtnOpenStoreHint/Text");
                if (storeHintTxt != null)
                {
                    var tmp = storeHintTxt.GetComponent<TMP_Text>();
                    if (tmp != null && tmp.text.Contains("Open Store (F)"))
                    {
                        tmp.text = tmp.text.Replace("Open Store (F)", "Open Store (T)");
                        EditorUtility.SetDirty(tmp);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                Canvas.ForceUpdateCanvases();
                EditorSceneManager.MarkSceneDirty(scene);
                if (saveScene)
                {
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[UtsuruHudButtonsSetupEditor] HUD buttons (Tab Dictionary, Q Achievement, T Store) configured and Day_Showcase saved.");
                }
                else
                {
                    Debug.Log("[UtsuruHudButtonsSetupEditor] HUD buttons (Tab Dictionary, Q Achievement, T Store) configured in Day_Showcase.");
                }
            }

            return true;
        }

        private static void EnsureBgmManagerPrefab()
        {
            const string prefabPath = "Assets/Resources/Audio/BGMManager.prefab";
            const string menuClipPath = "Assets/Sounds/BGM_MainMenu.mp3";
            const string gameClipPath = "Assets/Sounds/BGM_InGame.mp3";
            const string nightClipPath = "Assets/Sounds/snoozybeats-warm-breeze-lofi-music-chill-lofi-344259.mp3";

            AudioClip menuClip = AssetDatabase.LoadAssetAtPath<AudioClip>(menuClipPath);
            AudioClip gameClip = AssetDatabase.LoadAssetAtPath<AudioClip>(gameClipPath);
            AudioClip nightClip = AssetDatabase.LoadAssetAtPath<AudioClip>(nightClipPath);
            if (menuClip == null && gameClip == null && nightClip == null) return;

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                var existingMgr = existingPrefab.GetComponent<Utsuru.Sound.BGMManager>();
                if (existingMgr != null)
                {
                    SerializedObject exSo = new SerializedObject(existingMgr);
                    if (exSo.FindProperty("mainMenuBgm")?.objectReferenceValue == menuClip &&
                        exSo.FindProperty("inGameBgm")?.objectReferenceValue == gameClip &&
                        exSo.FindProperty("inGameNightBgm")?.objectReferenceValue == nightClip)
                    {
                        return;
                    }
                }
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Audio"))
                AssetDatabase.CreateFolder("Assets/Resources", "Audio");

            GameObject tempGo = new GameObject("BGMManager");
            AudioSource srcA = tempGo.AddComponent<AudioSource>();
            AudioSource srcB = tempGo.AddComponent<AudioSource>();
            srcA.playOnAwake = srcB.playOnAwake = false;
            srcA.loop = srcB.loop = true;
            srcA.ignoreListenerPause = srcB.ignoreListenerPause = true;

            var mgr = tempGo.AddComponent<Utsuru.Sound.BGMManager>();
            mgr.SetBgmClipsEditor(menuClip, gameClip, nightClip, srcA, srcB);

            PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
            Object.DestroyImmediate(tempGo);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureGlobalButtonSoundPrefab()
        {
            const string prefabPath = "Assets/Resources/Audio/GlobalButtonSoundManager.prefab";
            const string clipPath = "Assets/Sounds/chisper-ui-click-wooden-634791.mp3";

            AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clickClip == null) return;

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                var existingMgr = existingPrefab.GetComponent<Utsuru.Sound.GlobalButtonSoundManager>();
                if (existingMgr != null)
                {
                    SerializedObject exSo = new SerializedObject(existingMgr);
                    if (exSo.FindProperty("buttonClickClip")?.objectReferenceValue == clickClip)
                    {
                        return;
                    }
                }
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Audio"))
                AssetDatabase.CreateFolder("Assets/Resources", "Audio");

            GameObject tempGo = new GameObject("GlobalButtonSoundManager");
            AudioSource src = tempGo.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.ignoreListenerPause = true;

            var mgr = tempGo.AddComponent<Utsuru.Sound.GlobalButtonSoundManager>();
            mgr.SetClickClipEditor(clickClip, src);

            PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
            Object.DestroyImmediate(tempGo);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureCameraAudioClips(ref bool changed)
        {
            AudioClip openClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Camera/open.wav");
            AudioClip closeClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Camera/close.wav");
            AudioClip shutterClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Camera/shutter.mp3");

            CameraModeController camMode = Object.FindFirstObjectByType<CameraModeController>(FindObjectsInactive.Include);
            if (camMode != null)
            {
                AudioSource modeSource = camMode.GetComponent<AudioSource>();
                if (modeSource == null)
                {
                    modeSource = Undo.AddComponent<AudioSource>(camMode.gameObject);
                    modeSource.playOnAwake = false;
                    modeSource.loop = false;
                    modeSource.ignoreListenerPause = true;
                    changed = true;
                }

                SerializedObject cmSo = new SerializedObject(camMode);
                bool cmChanged = false;
                cmChanged |= AssignObjectPropIfDifferent(cmSo.FindProperty("sfxSource"), modeSource);
                cmChanged |= AssignObjectPropIfDifferent(cmSo.FindProperty("openCameraClip"), openClip);
                cmChanged |= AssignObjectPropIfDifferent(cmSo.FindProperty("closeCameraClip"), closeClip);

                if (cmChanged && cmSo.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(camMode);
                    changed = true;
                }
            }

            CameraCaptureSystem captureSys = Object.FindFirstObjectByType<CameraCaptureSystem>(FindObjectsInactive.Include);
            if (captureSys != null)
            {
                AudioSource capSource = captureSys.GetComponent<AudioSource>();
                if (capSource == null)
                {
                    capSource = Undo.AddComponent<AudioSource>(captureSys.gameObject);
                    capSource.playOnAwake = false;
                    capSource.loop = false;
                    capSource.ignoreListenerPause = true;
                    changed = true;
                }

                SerializedObject capSo = new SerializedObject(captureSys);
                bool capChanged = false;
                capChanged |= AssignObjectPropIfDifferent(capSo.FindProperty("sfxSource"), capSource);
                capChanged |= AssignObjectPropIfDifferent(capSo.FindProperty("shutterClip"), shutterClip);
                if (capChanged && capSo.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(captureSys);
                    changed = true;
                }
            }
        }

        private static void RemoveFlashcardFeatureFromScene(Transform canvasTransform, ref bool changed)
        {
            if (canvasTransform == null) return;
            Transform dictTr = canvasTransform.Find("Dictionary");
            if (dictTr == null) return;

            int missingRemoved = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(dictTr.gameObject);
            if (missingRemoved > 0)
            {
                EditorUtility.SetDirty(dictTr.gameObject);
                changed = true;
            }

            string[] flashcardPaths =
            {
                "Dictionary/DictionaryView/BookContainer/TopTabs/Tab_Flashcard",
                "Dictionary/DictionaryView/BookContainer/BookSpread/FlashcardSpreadRoot",
                "Dictionary/DictionaryView/FlashcardFormModal"
            };

            foreach (string path in flashcardPaths)
            {
                Transform target = canvasTransform.Find(path);
                if (target != null)
                {
                    Undo.DestroyObjectImmediate(target.gameObject);
                    changed = true;
                }
            }

            Transform tabTheme = canvasTransform.Find("Dictionary/DictionaryView/BookContainer/TopTabs/Tab_Theme");
            if (tabTheme != null && tabTheme.TryGetComponent<RectTransform>(out var themeRt))
            {
                Vector2 targetPos = new Vector2(-380f, -29.36f);
                if (Vector2.Distance(themeRt.anchoredPosition, targetPos) > 1f)
                {
                    themeRt.anchoredPosition = targetPos;
                    EditorUtility.SetDirty(themeRt);
                    changed = true;
                }
            }
        }

        private static void EnsureQuizTranslationHiddenAndResultLayout(Transform canvasTransform, ref bool changed)
        {
            if (canvasTransform == null) return;
            Transform quizRoot = canvasTransform.Find("Dictionary/DictionaryView/BookContainer/BookSpread/QuizSpreadRoot");
            if (quizRoot == null) return;

            foreach (string footerPath in new[] { "ActiveQuizRoot/LeftQuizPage/TranslationFooter", "ActiveQuizRoot/RightQuizPage/TranslationFooter" })
            {
                Transform footer = quizRoot.Find(footerPath);
                if (footer != null)
                {
                    var tmp = footer.GetComponent<TextMeshProUGUI>();
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                    {
                        tmp.text = "";
                        EditorUtility.SetDirty(tmp);
                        changed = true;
                    }
                    if (footer.gameObject.activeSelf)
                    {
                        footer.gameObject.SetActive(false);
                        EditorUtility.SetDirty(footer.gameObject);
                        changed = true;
                    }
                }
            }

            // Jangan menimpa ResultSummaryRoot yang sudah ada / sudah disesuaikan manual di scene
            if (quizRoot.Find("ResultSummaryRoot") == null)
            {
                DictionaryQuizSpreadUI quizUI = quizRoot.GetComponent<DictionaryQuizSpreadUI>();
                if (quizUI != null && DictionaryQuizUIBuilder.RebuildResultSummaryOnExistingQuiz(quizUI))
                {
                    changed = true;
                }
            }
        }

        private static void EnsureDictionaryCoverProgressBadge(Transform canvasTransform, ref bool changed)
        {
            if (canvasTransform == null) return;
            Transform bookContainer = canvasTransform.Find("Dictionary/DictionaryView/BookContainer");
            if (bookContainer == null) return;

            var importer = AssetImporter.GetAtPath(CoverCounterPillAssetPath) as TextureImporter;
            if (importer != null)
            {
                bool impChanged = false;
                Vector4 targetBorder = new Vector4(44f, 20f, 44f, 20f);
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; impChanged = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; impChanged = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; impChanged = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; impChanged = true; }
                if (importer.spriteBorder != targetBorder) { importer.spriteBorder = targetBorder; impChanged = true; }
                if (impChanged) importer.SaveAndReimport();
            }

            Transform badgeTr = bookContainer.Find("DiscoveryProgressBadge");
            if (badgeTr != null)
            {
                // Pertahankan layout & posisi manual yang sudah disesuaikan di scene
                return;
            }

            GameObject badgeGo = new GameObject("DiscoveryProgressBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeGo.layer = bookContainer.gameObject.layer != 0 ? bookContainer.gameObject.layer : 5;
            badgeGo.transform.SetParent(bookContainer, false);
            Undo.RegisterCreatedObjectUndo(badgeGo, "Create DiscoveryProgressBadge");
            changed = true;

            if (badgeGo.transform.GetSiblingIndex() != bookContainer.childCount - 1)
            {
                badgeGo.transform.SetAsLastSibling();
                changed = true;
            }

            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            if (badgeRt != null)
            {
                Vector2 bottomCenter = new Vector2(0.5f, 0f);
                Vector2 center = new Vector2(0.5f, 0.5f);
                Vector2 targetPos = new Vector2(-17f, 64f);
                Vector2 targetSize = new Vector2(224f, 62f);

                if (badgeRt.anchorMin != bottomCenter) { badgeRt.anchorMin = bottomCenter; changed = true; }
                if (badgeRt.anchorMax != bottomCenter) { badgeRt.anchorMax = bottomCenter; changed = true; }
                if (badgeRt.pivot != center) { badgeRt.pivot = center; changed = true; }
                if (badgeRt.anchoredPosition != targetPos) { badgeRt.anchoredPosition = targetPos; changed = true; }
                if (badgeRt.sizeDelta != targetSize) { badgeRt.sizeDelta = targetSize; changed = true; }
                if (badgeRt.localScale != Vector3.one) { badgeRt.localScale = Vector3.one; changed = true; }
            }

            Sprite pillSprite = LoadSpriteAsset(CoverCounterPillAssetPath);
            Image badgeImg = badgeGo.GetComponent<Image>() ?? badgeGo.AddComponent<Image>();
            if (pillSprite != null && badgeImg.sprite != pillSprite) { badgeImg.sprite = pillSprite; changed = true; }
            if (badgeImg.type != Image.Type.Sliced) { badgeImg.type = Image.Type.Sliced; changed = true; }
            if (badgeImg.raycastTarget) { badgeImg.raycastTarget = false; changed = true; }
            if (badgeImg.color != Color.white) { badgeImg.color = Color.white; changed = true; }

            TMP_FontAsset nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");
            TMP_FontAsset balooFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Baloo_2/static/Baloo2-ExtraBold SDF.asset") ?? nunitoFont;

            // Top eyebrow label ("DISCOVERED")
            Transform hdrTr = badgeGo.transform.Find("ProgressHeaderText");
            GameObject hdrGo;
            if (hdrTr == null)
            {
                hdrGo = new GameObject("ProgressHeaderText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                hdrGo.layer = badgeGo.layer;
                hdrGo.transform.SetParent(badgeGo.transform, false);
                Undo.RegisterCreatedObjectUndo(hdrGo, "Create ProgressHeaderText");
                changed = true;
            }
            else
            {
                hdrGo = hdrTr.gameObject;
            }

            RectTransform hdrRt = hdrGo.GetComponent<RectTransform>();
            if (hdrRt != null)
            {
                Vector2 minA = new Vector2(0f, 0.5f);
                Vector2 maxA = new Vector2(1f, 1f);
                Vector2 offMin = new Vector2(16f, -3f);
                Vector2 offMax = new Vector2(-16f, -7f);
                if (hdrRt.anchorMin != minA) { hdrRt.anchorMin = minA; changed = true; }
                if (hdrRt.anchorMax != maxA) { hdrRt.anchorMax = maxA; changed = true; }
                if (hdrRt.offsetMin != offMin) { hdrRt.offsetMin = offMin; changed = true; }
                if (hdrRt.offsetMax != offMax) { hdrRt.offsetMax = offMax; changed = true; }
            }

            TextMeshProUGUI hdrTmp = hdrGo.GetComponent<TextMeshProUGUI>();
            if (hdrTmp != null)
            {
                if (nunitoFont != null && hdrTmp.font != nunitoFont) { hdrTmp.font = nunitoFont; changed = true; }
                if (hdrTmp.fontStyle != FontStyles.Bold) { hdrTmp.fontStyle = FontStyles.Bold; changed = true; }
                if (!Mathf.Approximately(hdrTmp.fontSize, 10.5f)) { hdrTmp.fontSize = 10.5f; changed = true; }
                if (!Mathf.Approximately(hdrTmp.characterSpacing, 14f)) { hdrTmp.characterSpacing = 14f; changed = true; }
                if (hdrTmp.alignment != TextAlignmentOptions.Center) { hdrTmp.alignment = TextAlignmentOptions.Center; changed = true; }
                Color hdrColor = new Color(0.92f, 0.83f, 0.75f, 0.92f);
                if (hdrTmp.color != hdrColor) { hdrTmp.color = hdrColor; changed = true; }
                if (hdrTmp.text != "DISCOVERED") { hdrTmp.text = "DISCOVERED"; changed = true; }
                if (hdrTmp.raycastTarget) { hdrTmp.raycastTarget = false; changed = true; }
            }

            // Main counter ("0 / 72")
            Transform cntTr = badgeGo.transform.Find("ProgressCountText");
            GameObject cntGo;
            if (cntTr == null)
            {
                cntGo = new GameObject("ProgressCountText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                cntGo.layer = badgeGo.layer;
                cntGo.transform.SetParent(badgeGo.transform, false);
                Undo.RegisterCreatedObjectUndo(cntGo, "Create ProgressCountText");
                changed = true;
            }
            else
            {
                cntGo = cntTr.gameObject;
            }

            RectTransform cntRt = cntGo.GetComponent<RectTransform>();
            if (cntRt != null)
            {
                Vector2 minA = new Vector2(0f, 0f);
                Vector2 maxA = new Vector2(1f, 0.65f);
                Vector2 offMin = new Vector2(16f, 6f);
                Vector2 offMax = new Vector2(-16f, -2f);
                if (cntRt.anchorMin != minA) { cntRt.anchorMin = minA; changed = true; }
                if (cntRt.anchorMax != maxA) { cntRt.anchorMax = maxA; changed = true; }
                if (cntRt.offsetMin != offMin) { cntRt.offsetMin = offMin; changed = true; }
                if (cntRt.offsetMax != offMax) { cntRt.offsetMax = offMax; changed = true; }
            }

            TextMeshProUGUI cntTmp = cntGo.GetComponent<TextMeshProUGUI>();
            if (cntTmp != null)
            {
                if (balooFont != null && cntTmp.font != balooFont) { cntTmp.font = balooFont; changed = true; }
                if (cntTmp.fontStyle != FontStyles.Bold) { cntTmp.fontStyle = FontStyles.Bold; changed = true; }
                if (!Mathf.Approximately(cntTmp.fontSize, 24f)) { cntTmp.fontSize = 24f; changed = true; }
                if (!cntTmp.enableAutoSizing) { cntTmp.enableAutoSizing = true; changed = true; }
                if (!Mathf.Approximately(cntTmp.fontSizeMin, 16f)) { cntTmp.fontSizeMin = 16f; changed = true; }
                if (!Mathf.Approximately(cntTmp.fontSizeMax, 24f)) { cntTmp.fontSizeMax = 24f; changed = true; }
                if (cntTmp.textWrappingMode != TextWrappingModes.NoWrap) { cntTmp.textWrappingMode = TextWrappingModes.NoWrap; changed = true; }
                if (cntTmp.alignment != TextAlignmentOptions.Center) { cntTmp.alignment = TextAlignmentOptions.Center; changed = true; }
                if (cntTmp.color != Color.white) { cntTmp.color = Color.white; changed = true; }
                const string defaultText = "<color=#FFF8EE><b>0</b></color> <color=#DFC6AE><size=80%>/ 72</size></color>";
                if (string.IsNullOrEmpty(cntTmp.text)) { cntTmp.text = defaultText; changed = true; }
                if (cntTmp.raycastTarget) { cntTmp.raycastTarget = false; changed = true; }
            }

            DictionaryBookSpreadUI bookSpread = bookContainer.GetComponentInChildren<DictionaryBookSpreadUI>(true);
            if (bookSpread != null && cntTmp != null)
            {
                SerializedObject bsSo = new SerializedObject(bookSpread);
                if (AssignObjectPropIfDifferent(bsSo.FindProperty("discoveryProgressText"), cntTmp) && bsSo.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(bookSpread);
                    changed = true;
                }
            }

            if (!badgeGo.activeSelf) { badgeGo.SetActive(true); changed = true; }
            EditorUtility.SetDirty(badgeGo);
        }

        private static void EnsureTopLeftScoreBadge(Transform canvasTransform, ref bool changed)
        {
            if (canvasTransform == null) return;

            Transform existingScoreText = canvasTransform.Find("ScoreText");
            Transform badgeTr = canvasTransform.Find("ScoreBadge");
            GameObject badgeGo;
            if (badgeTr == null)
            {
                badgeGo = new GameObject("ScoreBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeGo.layer = canvasTransform.gameObject.layer != 0 ? canvasTransform.gameObject.layer : 5;
                badgeGo.transform.SetParent(canvasTransform, false);
                badgeGo.transform.SetSiblingIndex(0);
                Undo.RegisterCreatedObjectUndo(badgeGo, "Create ScoreBadge");
                changed = true;
            }
            else
            {
                badgeGo = badgeTr.gameObject;
                if (badgeGo.transform.GetSiblingIndex() != 0)
                {
                    badgeGo.transform.SetSiblingIndex(0);
                    changed = true;
                }
            }

            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            if (badgeRt != null)
            {
                Vector2 topLeft = new Vector2(0f, 1f);
                Vector2 targetPos = new Vector2(34f, -22f);
                Vector2 targetSize = new Vector2(226f, 85f);

                if (badgeRt.anchorMin != topLeft) { badgeRt.anchorMin = topLeft; changed = true; }
                if (badgeRt.anchorMax != topLeft) { badgeRt.anchorMax = topLeft; changed = true; }
                if (badgeRt.pivot != topLeft) { badgeRt.pivot = topLeft; changed = true; }
                if (badgeRt.anchoredPosition != targetPos) { badgeRt.anchoredPosition = targetPos; changed = true; }
                if (badgeRt.sizeDelta != targetSize) { badgeRt.sizeDelta = targetSize; changed = true; }
                if (badgeRt.localScale != Vector3.one) { badgeRt.localScale = Vector3.one; changed = true; }
            }

            Sprite scoreSprite = LoadSpriteAsset(ScoreBgAssetPath);
            Image badgeImg = badgeGo.GetComponent<Image>() ?? badgeGo.AddComponent<Image>();
            if (scoreSprite != null && badgeImg.sprite != scoreSprite)
            {
                badgeImg.sprite = scoreSprite;
                changed = true;
            }
            if (badgeImg.type != Image.Type.Simple) { badgeImg.type = Image.Type.Simple; changed = true; }
            if (!badgeImg.preserveAspect) { badgeImg.preserveAspect = true; changed = true; }
            if (badgeImg.raycastTarget) { badgeImg.raycastTarget = false; changed = true; }
            if (badgeImg.color != Color.white) { badgeImg.color = Color.white; changed = true; }

            Transform scoreTr = badgeGo.transform.Find("ScoreText") ?? existingScoreText;
            GameObject scoreGo;
            if (scoreTr == null)
            {
                scoreGo = new GameObject("ScoreText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                scoreGo.layer = badgeGo.layer;
                scoreGo.transform.SetParent(badgeGo.transform, false);
                Undo.RegisterCreatedObjectUndo(scoreGo, "Create ScoreText");
                changed = true;
            }
            else
            {
                scoreGo = scoreTr.gameObject;
                if (scoreTr.parent != badgeGo.transform)
                {
                    scoreTr.SetParent(badgeGo.transform, false);
                    changed = true;
                }
            }

            RectTransform scoreRt = scoreGo.GetComponent<RectTransform>();
            if (scoreRt != null)
            {
                Vector2 minAnchor = new Vector2(0.38f, 0.14f);
                Vector2 maxAnchor = new Vector2(0.92f, 0.94f);
                Vector2 center = new Vector2(0.5f, 0.5f);

                if (scoreRt.anchorMin != minAnchor) { scoreRt.anchorMin = minAnchor; changed = true; }
                if (scoreRt.anchorMax != maxAnchor) { scoreRt.anchorMax = maxAnchor; changed = true; }
                if (scoreRt.pivot != center) { scoreRt.pivot = center; changed = true; }
                if (scoreRt.anchoredPosition != Vector2.zero) { scoreRt.anchoredPosition = Vector2.zero; changed = true; }
                if (scoreRt.sizeDelta != Vector2.zero) { scoreRt.sizeDelta = Vector2.zero; changed = true; }
                if (scoreRt.localScale != Vector3.one) { scoreRt.localScale = Vector3.one; changed = true; }
            }

            TextMeshProUGUI scoreTmp = scoreGo.GetComponent<TextMeshProUGUI>();
            if (scoreTmp != null)
            {
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Baloo_2/static/Baloo2-ExtraBold SDF.asset")
                    ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold SDF.asset");
                if (font != null && scoreTmp.font != font)
                {
                    scoreTmp.font = font;
                    changed = true;
                }
                if (scoreTmp.alignment != TextAlignmentOptions.Center) { scoreTmp.alignment = TextAlignmentOptions.Center; changed = true; }
                if (!Mathf.Approximately(scoreTmp.fontSize, 34f)) { scoreTmp.fontSize = 34f; changed = true; }
                if (!scoreTmp.enableAutoSizing) { scoreTmp.enableAutoSizing = true; changed = true; }
                if (!Mathf.Approximately(scoreTmp.fontSizeMin, 18f)) { scoreTmp.fontSizeMin = 18f; changed = true; }
                if (!Mathf.Approximately(scoreTmp.fontSizeMax, 34f)) { scoreTmp.fontSizeMax = 34f; changed = true; }
                Color targetColor = new Color(1f, 0.97f, 0.90f, 1f);
                if (scoreTmp.color != targetColor) { scoreTmp.color = targetColor; changed = true; }
                if (scoreTmp.raycastTarget) { scoreTmp.raycastTarget = false; changed = true; }
                if (scoreTmp.text.StartsWith("Skor:")) { scoreTmp.text = "0"; changed = true; }
            }

            ScoreManager scoreMgr = Object.FindFirstObjectByType<ScoreManager>(FindObjectsInactive.Include);
            if (scoreMgr != null && scoreTmp != null)
            {
                SerializedObject smSo = new SerializedObject(scoreMgr);
                bool smChanged = AssignObjectPropIfDifferent(smSo.FindProperty("scoreText"), scoreTmp);
                smChanged |= AssignObjectPropIfDifferent(smSo.FindProperty("scoreBadgeRoot"), badgeGo);
                SerializedProperty fmtProp = smSo.FindProperty("scoreFormat");
                if (fmtProp != null && fmtProp.stringValue != "{0}")
                {
                    fmtProp.stringValue = "{0}";
                    smChanged = true;
                }
                if (smChanged && smSo.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(scoreMgr);
                    changed = true;
                }
            }

            if (!badgeGo.activeSelf) { badgeGo.SetActive(true); changed = true; }
            EditorUtility.SetDirty(badgeGo);
            EditorUtility.SetDirty(scoreGo);
        }

        private static GameObject EnsureHintObject(
            Transform parent,
            string objectName,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            ref bool changed)
        {
            Transform existing = parent.Find(objectName);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = parent.gameObject.layer != 0 ? parent.gameObject.layer : 5;
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, $"Create {objectName}");
                changed = true;
            }
            else
            {
                go = existing.gameObject;
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                if (rt.anchorMin != anchorMin) { rt.anchorMin = anchorMin; changed = true; }
                if (rt.anchorMax != anchorMax) { rt.anchorMax = anchorMax; changed = true; }
                if (rt.pivot != pivot) { rt.pivot = pivot; changed = true; }
                if (rt.anchoredPosition != anchoredPosition) { rt.anchoredPosition = anchoredPosition; changed = true; }
                if (rt.sizeDelta != sizeDelta) { rt.sizeDelta = sizeDelta; changed = true; }
                if (rt.localScale != Vector3.one) { rt.localScale = Vector3.one; changed = true; }
            }

            Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            if (sprite != null && img.sprite != sprite)
            {
                img.sprite = sprite;
                changed = true;
            }
            if (img.type != Image.Type.Simple) { img.type = Image.Type.Simple; changed = true; }
            if (!img.preserveAspect) { img.preserveAspect = true; changed = true; }
            if (img.raycastTarget) { img.raycastTarget = false; changed = true; }
            if (img.color != Color.white) { img.color = Color.white; changed = true; }

            if (!go.activeSelf) { go.SetActive(true); changed = true; }
            EditorUtility.SetDirty(go);
            return go;
        }

        private static bool AssignObjectPropIfDifferent(SerializedProperty prop, Object value)
        {
            if (prop == null || value == null) return false;
            if (prop.objectReferenceValue == value) return false;
            prop.objectReferenceValue = value;
            return true;
        }

        private static Sprite LoadSpriteAsset(string assetPath)
        {
            Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (direct != null) return direct;
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
        }
    }
}
#endif
