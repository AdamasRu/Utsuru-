#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utsuru.Capture;
using Utsuru.DictionaryUI;

namespace Utsuru.DictionaryUI.Editor
{
    public static class DictionaryUIBuilder
    {
        private static Sprite defaultSprite;
        private static TMP_FontAsset defaultFont;

        public static GameObject BuildDictionaryUI()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[DictionaryUIBuilder] Canvas tidak ditemukan!");
                return null;
            }

            defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            defaultFont = TMP_Settings.defaultFontAsset;

            // Hapus jika sudah ada Dictionary lama di bawah Canvas agar selalu segar dan bersih
            Transform oldDict = canvasGo.transform.Find("Dictionary");
            if (oldDict != null)
            {
                Undo.DestroyObjectImmediate(oldDict.gameObject);
            }

            // 1. Root Dictionary (SELALU AKTIF di scene agar script controller mendengarkan tombol 'B')
            GameObject dictRoot = CreateUIObject("Dictionary", canvasGo.transform);
            RectTransform dictRt = dictRoot.GetComponent<RectTransform>();
            SetStretch(dictRt);
            dictRoot.SetActive(true);

            var controller = dictRoot.AddComponent<DictionaryUIController>();

            // 1.1 View Root (Kontainer tampilan visual yang di-toggle hide/show oleh controller)
            GameObject viewRoot = CreateUIObject("DictionaryView", dictRoot.transform);
            SetStretch(viewRoot.GetComponent<RectTransform>());

            // Dimmer Backdrop
            GameObject dimmer = CreateUIObject("BackdropDimmer", viewRoot.transform);
            SetStretch(dimmer.GetComponent<RectTransform>());
            var dimmerImg = dimmer.AddComponent<Image>();
            dimmerImg.color = new Color(0f, 0f, 0f, 0.75f);
            var dimmerBtn = dimmer.AddComponent<Button>();

            // 1.2 Book Container (Buku Terbuka)
            GameObject book = CreateUIObject("BookContainer", viewRoot.transform);
            RectTransform bookRt = book.GetComponent<RectTransform>();
            bookRt.anchorMin = new Vector2(0.5f, 0.5f);
            bookRt.anchorMax = new Vector2(0.5f, 0.5f);
            bookRt.pivot = new Vector2(0.5f, 0.5f);
            bookRt.sizeDelta = new Vector2(1480f, 840f);
            bookRt.anchoredPosition = new Vector2(30f, 0f);

            // Background Cover Buku (Kayu / Kulit Cokelat)
            var bookCoverImg = book.AddComponent<Image>();
            bookCoverImg.sprite = defaultSprite;
            bookCoverImg.type = Image.Type.Sliced;
            bookCoverImg.color = new Color(0.24f, 0.16f, 0.12f, 1f);

            // Spine / Shadow Tengah Buku
            GameObject spine = CreateUIObject("BookSpine", book.transform);
            RectTransform spineRt = spine.GetComponent<RectTransform>();
            spineRt.anchorMin = new Vector2(0.5f, 0f);
            spineRt.anchorMax = new Vector2(0.5f, 1f);
            spineRt.pivot = new Vector2(0.5f, 0.5f);
            spineRt.sizeDelta = new Vector2(16f, 0f);
            var spineImg = spine.AddComponent<Image>();
            spineImg.color = new Color(0.12f, 0.08f, 0.06f, 0.7f);

            // Left Page Background (Krem Kertas)
            GameObject leftPageBg = CreateUIObject("LeftPageBg", book.transform);
            RectTransform leftPageRt = leftPageBg.GetComponent<RectTransform>();
            leftPageRt.anchorMin = new Vector2(0f, 0f);
            leftPageRt.anchorMax = new Vector2(0.5f, 1f);
            leftPageRt.offsetMin = new Vector2(20f, 20f);
            leftPageRt.offsetMax = new Vector2(-10f, -20f);
            var leftPageImg = leftPageBg.AddComponent<Image>();
            leftPageImg.sprite = defaultSprite;
            leftPageImg.type = Image.Type.Sliced;
            leftPageImg.color = new Color(0.97f, 0.95f, 0.91f, 1f);

            // Right Page Background (Krem Kertas)
            GameObject rightPageBg = CreateUIObject("RightPageBg", book.transform);
            RectTransform rightPageRt = rightPageBg.GetComponent<RectTransform>();
            rightPageRt.anchorMin = new Vector2(0.5f, 0f);
            rightPageRt.anchorMax = new Vector2(1f, 1f);
            rightPageRt.offsetMin = new Vector2(10f, 20f);
            rightPageRt.offsetMax = new Vector2(-20f, -20f);
            var rightPageImg = rightPageBg.AddComponent<Image>();
            rightPageImg.sprite = defaultSprite;
            rightPageImg.type = Image.Type.Sliced;
            rightPageImg.color = new Color(0.97f, 0.95f, 0.91f, 1f);

            // Tombol Tutup (X) di pojok kanan atas luar buku
            GameObject closeBtnGo = CreateButton("Btn_Close", book.transform, "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(36f, 36f), new Vector2(-10f, 30f));
            closeBtnGo.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.2f, 1f);

            // 1.3 Marker Tabs di sebelah kiri luar buku
            GameObject tabsRoot = CreateUIObject("MarkerTabs", book.transform);
            RectTransform tabsRt = tabsRoot.GetComponent<RectTransform>();
            tabsRt.anchorMin = new Vector2(0f, 0.5f);
            tabsRt.anchorMax = new Vector2(0f, 0.5f);
            tabsRt.pivot = new Vector2(1f, 0.5f);
            tabsRt.sizeDelta = new Vector2(110f, 480f);
            tabsRt.anchoredPosition = new Vector2(0f, 50f);

            var tabsLayout = tabsRoot.AddComponent<VerticalLayoutGroup>();
            tabsLayout.spacing = 16f;
            tabsLayout.childAlignment = TextAnchor.UpperRight;
            tabsLayout.childControlHeight = false;
            tabsLayout.childControlWidth = true;
            tabsLayout.childForceExpandHeight = false;
            tabsLayout.childForceExpandWidth = true;

            GameObject tabVocabGo = CreateTabMarker("Tab_Marker_Vocab", tabsRoot.transform, "Kosakata");
            GameObject tabCourseGo = CreateTabMarker("Tab_Marker_Course", tabsRoot.transform, "Course");
            GameObject tabAlbumGo = CreateTabMarker("Tab_Marker_Album", tabsRoot.transform, "Album");
            GameObject tabDecorGo = CreateTabMarker("Tab_Marker_Decor", tabsRoot.transform, "Dekorasi");

            // 1.4 Tab Album Panel
            GameObject albumPanel = CreateUIObject("Tab_Album_Panel", book.transform);
            SetStretchMargins(albumPanel.GetComponent<RectTransform>(), 20f, 20f, 20f, 20f);
            var albumTabUI = albumPanel.AddComponent<DictionaryAlbumTabUI>();

            BuildAlbumContent(albumPanel, albumTabUI);

            // 1.5 Tab Kosakata Panel
            GameObject vocabPanel = CreateUIObject("Tab_Vocab_Panel", book.transform);
            SetStretchMargins(vocabPanel.GetComponent<RectTransform>(), 20f, 20f, 20f, 20f);
            var vocabTabUI = vocabPanel.AddComponent<DictionaryVocabularyTabUI>();

            BuildVocabContent(vocabPanel, vocabTabUI);

            // 1.6 Tab Course Placeholder
            GameObject coursePanel = CreatePlaceholderPanel("Tab_Course_Panel", book.transform, "MODUL PEMBELAJARAN (KANA & N5)\n\nFitur ini sedang dalam tahap perancangan.\nSegera hadir untuk latihan huruf Hiragana, Katakana, dan tata bahasa!");

            // 1.7 Tab Dekorasi Placeholder
            GameObject decorPanel = CreatePlaceholderPanel("Tab_Decor_Panel", book.transform, "KANVAS DEKORASI DICTIONARY\n\nFitur personalisasi buku dengan stiker, bingkai, dan tema.\nSegera hadir!");

            // 1.8 Modals & Dialog (di bawah viewRoot agar ikut muncul saat dibuka)
            GameObject photoModalGo = BuildPhotoDetailModal(viewRoot.transform);
            var photoModal = photoModalGo.GetComponent<DictionaryPhotoDetailModal>();

            GameObject vocabModalGo = BuildVocabDetailModal(viewRoot.transform);
            var vocabModal = vocabModalGo.GetComponent<DictionaryVocabDetailModal>();

            GameObject dialogGo = BuildSimpleDialog(viewRoot.transform);
            var simpleDialog = dialogGo.GetComponent<DictionarySimpleDialog>();

            // Hubungkan referensi di Controller
            SerializedObject soCtrl = new SerializedObject(controller);
            soCtrl.FindProperty("viewRoot").objectReferenceValue = viewRoot;
            soCtrl.FindProperty("closeButton").objectReferenceValue = closeBtnGo.GetComponent<Button>();
            soCtrl.FindProperty("vocabTabButton").objectReferenceValue = tabVocabGo.GetComponent<Button>();
            soCtrl.FindProperty("courseTabButton").objectReferenceValue = tabCourseGo.GetComponent<Button>();
            soCtrl.FindProperty("albumTabButton").objectReferenceValue = tabAlbumGo.GetComponent<Button>();
            soCtrl.FindProperty("decorTabButton").objectReferenceValue = tabDecorGo.GetComponent<Button>();
            soCtrl.FindProperty("vocabPanel").objectReferenceValue = vocabPanel;
            soCtrl.FindProperty("coursePanel").objectReferenceValue = coursePanel;
            soCtrl.FindProperty("albumPanel").objectReferenceValue = albumPanel;
            soCtrl.FindProperty("decorPanel").objectReferenceValue = decorPanel;
            soCtrl.FindProperty("vocabTabController").objectReferenceValue = vocabTabUI;
            soCtrl.FindProperty("albumTabController").objectReferenceValue = albumTabUI;
            soCtrl.ApplyModifiedProperties();

            // Hubungkan dialog ke Album Tab & Photo Detail Modal
            SerializedObject soAlbum = new SerializedObject(albumTabUI);
            soAlbum.FindProperty("detailModal").objectReferenceValue = photoModal;
            soAlbum.FindProperty("dialog").objectReferenceValue = simpleDialog;
            soAlbum.ApplyModifiedProperties();

            SerializedObject soPhotoModal = new SerializedObject(photoModal);
            soPhotoModal.FindProperty("confirmDialog").objectReferenceValue = simpleDialog;
            soPhotoModal.ApplyModifiedProperties();

            SerializedObject soVocab = new SerializedObject(vocabTabUI);
            soVocab.FindProperty("detailModal").objectReferenceValue = vocabModal;
            soVocab.ApplyModifiedProperties();

            // Hubungkan dimmer tombol tutup
            dimmerBtn.onClick.AddListener(controller.Close);

            // Default: viewRoot tertutup
            viewRoot.SetActive(false);

            // Simpan Scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[DictionaryUIBuilder] UI Dictionary Buku Terbuka berhasil dibangun!");
            return dictRoot;
        }

        private static void BuildAlbumContent(GameObject albumPanel, DictionaryAlbumTabUI albumTabUI)
        {
            SerializedObject so = new SerializedObject(albumTabUI);

            // Toolbar Atas (Kiri: Album Selector & Manage | Kanan: Filter Favorit & Sorting)
            GameObject toolbar = CreateUIObject("Toolbar", albumPanel.transform);
            RectTransform toolRt = toolbar.GetComponent<RectTransform>();
            toolRt.anchorMin = new Vector2(0f, 1f);
            toolRt.anchorMax = new Vector2(1f, 1f);
            toolRt.pivot = new Vector2(0.5f, 1f);
            toolRt.sizeDelta = new Vector2(0f, 56f);
            toolRt.anchoredPosition = new Vector2(0f, -8f);

            // Sisi Kiri Toolbar: Album Dropdown + Tombol Kelola
            GameObject albumGroup = CreateUIObject("AlbumGroup", toolbar.transform);
            RectTransform agRt = albumGroup.GetComponent<RectTransform>();
            agRt.anchorMin = new Vector2(0f, 0.5f);
            agRt.anchorMax = new Vector2(0.5f, 0.5f);
            agRt.pivot = new Vector2(0f, 0.5f);
            agRt.anchoredPosition = new Vector2(16f, 0f);
            agRt.sizeDelta = new Vector2(400f, 44f);
            var agLayout = albumGroup.AddComponent<HorizontalLayoutGroup>();
            agLayout.spacing = 8f;
            agLayout.childControlWidth = false;
            agLayout.childControlHeight = false;

            GameObject albumDropdownGo = CreateDropdown("Dropdown_Album", albumGroup.transform, new Vector2(180f, 38f));
            GameObject btnAddAlbum = CreateButton("Btn_AddAlbum", albumGroup.transform, "+", new Vector2(38f, 38f), new Color(0.2f, 0.65f, 0.3f));
            GameObject btnRenameAlbum = CreateButton("Btn_RenameAlbum", albumGroup.transform, "Ubah", new Vector2(75f, 38f), new Color(0.35f, 0.45f, 0.6f));
            GameObject btnDeleteAlbum = CreateButton("Btn_DeleteAlbum", albumGroup.transform, "Hapus", new Vector2(65f, 38f), new Color(0.75f, 0.25f, 0.2f));

            // Sisi Kanan Toolbar: Filter Favorit + Sorting
            GameObject filterGroup = CreateUIObject("FilterGroup", toolbar.transform);
            RectTransform fgRt = filterGroup.GetComponent<RectTransform>();
            fgRt.anchorMin = new Vector2(0.5f, 0.5f);
            fgRt.anchorMax = new Vector2(1f, 0.5f);
            fgRt.pivot = new Vector2(1f, 0.5f);
            fgRt.anchoredPosition = new Vector2(-16f, 0f);
            fgRt.sizeDelta = new Vector2(400f, 44f);
            var fgLayout = filterGroup.AddComponent<HorizontalLayoutGroup>();
            fgLayout.spacing = 10f;
            fgLayout.childAlignment = TextAnchor.MiddleRight;
            fgLayout.childControlWidth = false;
            fgLayout.childControlHeight = false;

            GameObject btnFavFilter = CreateButton("Btn_FavFilter", filterGroup.transform, "Semua Foto", new Vector2(140f, 38f), new Color(0.3f, 0.3f, 0.35f));
            GameObject sortDropdownGo = CreateDropdown("Dropdown_Sort", filterGroup.transform, new Vector2(180f, 38f));

            // Kontainer Halaman Kiri (4 Slot: Grid 2x2)
            GameObject leftSlotsRoot = CreateUIObject("LeftPage_Slots", albumPanel.transform);
            RectTransform leftRt = leftSlotsRoot.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0f, 0f);
            leftRt.anchorMax = new Vector2(0.5f, 1f);
            leftRt.offsetMin = new Vector2(30f, 70f);
            leftRt.offsetMax = new Vector2(-30f, -70f);
            var leftGrid = leftSlotsRoot.AddComponent<GridLayoutGroup>();
            leftGrid.cellSize = new Vector2(285f, 320f);
            leftGrid.spacing = new Vector2(25f, 25f);
            leftGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            leftGrid.constraintCount = 2;
            leftGrid.childAlignment = TextAnchor.MiddleCenter;

            // Kontainer Halaman Kanan (4 Slot: Grid 2x2)
            GameObject rightSlotsRoot = CreateUIObject("RightPage_Slots", albumPanel.transform);
            RectTransform rightRt = rightSlotsRoot.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.5f, 0f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.offsetMin = new Vector2(30f, 70f);
            rightRt.offsetMax = new Vector2(-30f, -70f);
            var rightGrid = rightSlotsRoot.AddComponent<GridLayoutGroup>();
            rightGrid.cellSize = new Vector2(285f, 320f);
            rightGrid.spacing = new Vector2(25f, 25f);
            rightGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            rightGrid.constraintCount = 2;
            rightGrid.childAlignment = TextAnchor.MiddleCenter;

            // Buat 8 Slot Foto (4 kiri, 4 kanan)
            DictionaryPhotoSlotUI[] slots = new DictionaryPhotoSlotUI[8];
            for (int i = 0; i < 4; i++)
            {
                slots[i] = CreatePhotoSlot($"PhotoSlot_L{i}", leftSlotsRoot.transform);
            }
            for (int i = 0; i < 4; i++)
            {
                slots[4 + i] = CreatePhotoSlot($"PhotoSlot_R{i}", rightSlotsRoot.transform);
            }

            // Paginasi Bawah Buku
            GameObject pagination = CreateUIObject("PaginationBar", albumPanel.transform);
            RectTransform pagRt = pagination.GetComponent<RectTransform>();
            pagRt.anchorMin = new Vector2(0f, 0f);
            pagRt.anchorMax = new Vector2(1f, 0f);
            pagRt.pivot = new Vector2(0.5f, 0f);
            pagRt.sizeDelta = new Vector2(0f, 50f);
            pagRt.anchoredPosition = new Vector2(0f, 10f);

            GameObject btnPrev = CreateButton("Btn_PrevPage", pagination.transform, "< Sebelumnya", new Vector2(160f, 36f), new Color(0.35f, 0.35f, 0.35f));
            RectTransform bpRt = btnPrev.GetComponent<RectTransform>();
            bpRt.anchorMin = new Vector2(0f, 0.5f);
            bpRt.anchorMax = new Vector2(0f, 0.5f);
            bpRt.pivot = new Vector2(0f, 0.5f);
            bpRt.anchoredPosition = new Vector2(40f, 0f);

            GameObject labelPageGo = CreateText("Label_PageIndicator", pagination.transform, "Halaman 1 / 1", 18, TextAlignmentOptions.Center, new Color(0.2f, 0.2f, 0.2f));
            RectTransform lpRt = labelPageGo.GetComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0.5f, 0.5f);
            lpRt.anchorMax = new Vector2(0.5f, 0.5f);
            lpRt.pivot = new Vector2(0.5f, 0.5f);
            lpRt.sizeDelta = new Vector2(200f, 40f);
            lpRt.anchoredPosition = Vector2.zero;

            GameObject btnNext = CreateButton("Btn_NextPage", pagination.transform, "Selanjutnya >", new Vector2(160f, 36f), new Color(0.35f, 0.35f, 0.35f));
            RectTransform bnRt = btnNext.GetComponent<RectTransform>();
            bnRt.anchorMin = new Vector2(1f, 0.5f);
            bnRt.anchorMax = new Vector2(1f, 0.5f);
            bnRt.pivot = new Vector2(1f, 0.5f);
            bnRt.anchoredPosition = new Vector2(-40f, 0f);

            // Empty State
            GameObject emptyState = CreateUIObject("EmptyState", albumPanel.transform);
            RectTransform esRt = emptyState.GetComponent<RectTransform>();
            SetStretchMargins(esRt, 100f, 100f, 100f, 100f);
            GameObject esTextGo = CreateText("EmptyText", emptyState.transform, "Album ini masih kosong.\nBuka Mode Kamera (C) untuk memotret objek jalanan!", 24, TextAlignmentOptions.Center, new Color(0.4f, 0.35f, 0.3f));
            SetStretch(esTextGo.GetComponent<RectTransform>());

            // Pasang ke SerializedObject
            so.FindProperty("albumDropdown").objectReferenceValue = albumDropdownGo.GetComponent<TMP_Dropdown>();
            so.FindProperty("createAlbumButton").objectReferenceValue = btnAddAlbum.GetComponent<Button>();
            so.FindProperty("renameAlbumButton").objectReferenceValue = btnRenameAlbum.GetComponent<Button>();
            so.FindProperty("deleteAlbumButton").objectReferenceValue = btnDeleteAlbum.GetComponent<Button>();
            so.FindProperty("favoriteFilterButton").objectReferenceValue = btnFavFilter.GetComponent<Button>();
            so.FindProperty("favoriteFilterText").objectReferenceValue = btnFavFilter.GetComponentInChildren<TMP_Text>();
            so.FindProperty("sortDropdown").objectReferenceValue = sortDropdownGo.GetComponent<TMP_Dropdown>();
            so.FindProperty("prevPageButton").objectReferenceValue = btnPrev.GetComponent<Button>();
            so.FindProperty("nextPageButton").objectReferenceValue = btnNext.GetComponent<Button>();
            so.FindProperty("pageIndicatorLabel").objectReferenceValue = labelPageGo.GetComponent<TMP_Text>();
            so.FindProperty("emptyStateObject").objectReferenceValue = emptyState;
            so.FindProperty("emptyStateLabel").objectReferenceValue = esTextGo.GetComponent<TMP_Text>();

            SerializedProperty slotsProp = so.FindProperty("slots");
            slotsProp.arraySize = 8;
            for (int i = 0; i < 8; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            so.ApplyModifiedProperties();
        }

        private static void BuildVocabContent(GameObject vocabPanel, DictionaryVocabularyTabUI vocabTabUI)
        {
            SerializedObject so = new SerializedObject(vocabTabUI);

            // Toolbar Atas (Search Bar & Progress Label)
            GameObject toolbar = CreateUIObject("Toolbar", vocabPanel.transform);
            RectTransform toolRt = toolbar.GetComponent<RectTransform>();
            toolRt.anchorMin = new Vector2(0f, 1f);
            toolRt.anchorMax = new Vector2(1f, 1f);
            toolRt.pivot = new Vector2(0.5f, 1f);
            toolRt.sizeDelta = new Vector2(0f, 56f);
            toolRt.anchoredPosition = new Vector2(0f, -8f);

            GameObject searchInputGo = CreateInputField("SearchBar", toolbar.transform, "Cari kanji, romaji, atau arti...", new Vector2(360f, 40f));
            RectTransform siRt = searchInputGo.GetComponent<RectTransform>();
            siRt.anchorMin = new Vector2(0f, 0.5f);
            siRt.anchorMax = new Vector2(0.5f, 0.5f);
            siRt.pivot = new Vector2(0f, 0.5f);
            siRt.anchoredPosition = new Vector2(24f, 0f);

            GameObject progressTextGo = CreateText("ProgressText", toolbar.transform, "Kosakata Ditemukan: 0 / 0 (0%)", 20, TextAlignmentOptions.Right, new Color(0.2f, 0.35f, 0.5f));
            RectTransform ptRt = progressTextGo.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(1f, 0.5f);
            ptRt.anchorMax = new Vector2(1f, 0.5f);
            ptRt.pivot = new Vector2(1f, 0.5f);
            ptRt.sizeDelta = new Vector2(400f, 40f);
            ptRt.anchoredPosition = new Vector2(-24f, 0f);

            // Kontainer Halaman Kiri (4 Kartu)
            GameObject leftSlotsRoot = CreateUIObject("LeftPage_Cards", vocabPanel.transform);
            RectTransform leftRt = leftSlotsRoot.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0f, 0f);
            leftRt.anchorMax = new Vector2(0.5f, 1f);
            leftRt.offsetMin = new Vector2(30f, 70f);
            leftRt.offsetMax = new Vector2(-30f, -70f);
            var leftGrid = leftSlotsRoot.AddComponent<GridLayoutGroup>();
            leftGrid.cellSize = new Vector2(285f, 320f);
            leftGrid.spacing = new Vector2(25f, 25f);
            leftGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            leftGrid.constraintCount = 2;
            leftGrid.childAlignment = TextAnchor.MiddleCenter;

            // Kontainer Halaman Kanan (4 Kartu)
            GameObject rightSlotsRoot = CreateUIObject("RightPage_Cards", vocabPanel.transform);
            RectTransform rightRt = rightSlotsRoot.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.5f, 0f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.offsetMin = new Vector2(30f, 70f);
            rightRt.offsetMax = new Vector2(-30f, -70f);
            var rightGrid = rightSlotsRoot.AddComponent<GridLayoutGroup>();
            rightGrid.cellSize = new Vector2(285f, 320f);
            rightGrid.spacing = new Vector2(25f, 25f);
            rightGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            rightGrid.constraintCount = 2;
            rightGrid.childAlignment = TextAnchor.MiddleCenter;

            // 8 Kartu Kosakata
            DictionaryVocabCardUI[] cards = new DictionaryVocabCardUI[8];
            for (int i = 0; i < 4; i++)
            {
                cards[i] = CreateVocabCard($"VocabCard_L{i}", leftSlotsRoot.transform);
            }
            for (int i = 0; i < 4; i++)
            {
                cards[4 + i] = CreateVocabCard($"VocabCard_R{i}", rightSlotsRoot.transform);
            }

            // Paginasi Bawah
            GameObject pagination = CreateUIObject("PaginationBar", vocabPanel.transform);
            RectTransform pagRt = pagination.GetComponent<RectTransform>();
            pagRt.anchorMin = new Vector2(0f, 0f);
            pagRt.anchorMax = new Vector2(1f, 0f);
            pagRt.pivot = new Vector2(0.5f, 0f);
            pagRt.sizeDelta = new Vector2(0f, 50f);
            pagRt.anchoredPosition = new Vector2(0f, 10f);

            GameObject btnPrev = CreateButton("Btn_PrevPage", pagination.transform, "< Sebelumnya", new Vector2(160f, 36f), new Color(0.35f, 0.35f, 0.35f));
            RectTransform bpRt = btnPrev.GetComponent<RectTransform>();
            bpRt.anchorMin = new Vector2(0f, 0.5f);
            bpRt.anchorMax = new Vector2(0f, 0.5f);
            bpRt.pivot = new Vector2(0f, 0.5f);
            bpRt.anchoredPosition = new Vector2(40f, 0f);

            GameObject labelPageGo = CreateText("Label_PageIndicator", pagination.transform, "Halaman 1 / 1", 18, TextAlignmentOptions.Center, new Color(0.2f, 0.2f, 0.2f));
            RectTransform lpRt = labelPageGo.GetComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0.5f, 0.5f);
            lpRt.anchorMax = new Vector2(0.5f, 0.5f);
            lpRt.pivot = new Vector2(0.5f, 0.5f);
            lpRt.sizeDelta = new Vector2(200f, 40f);
            lpRt.anchoredPosition = Vector2.zero;

            GameObject btnNext = CreateButton("Btn_NextPage", pagination.transform, "Selanjutnya >", new Vector2(160f, 36f), new Color(0.35f, 0.35f, 0.35f));
            RectTransform bnRt = btnNext.GetComponent<RectTransform>();
            bnRt.anchorMin = new Vector2(1f, 0.5f);
            bnRt.anchorMax = new Vector2(1f, 0.5f);
            bnRt.pivot = new Vector2(1f, 0.5f);
            bnRt.anchoredPosition = new Vector2(-40f, 0f);

            // Empty State
            GameObject emptyState = CreateUIObject("EmptyState", vocabPanel.transform);
            RectTransform esRt = emptyState.GetComponent<RectTransform>();
            SetStretchMargins(esRt, 100f, 100f, 100f, 100f);
            GameObject esTextGo = CreateText("EmptyText", emptyState.transform, "Tidak ada kosakata yang ditemukan.", 24, TextAlignmentOptions.Center, new Color(0.4f, 0.35f, 0.3f));
            SetStretch(esTextGo.GetComponent<RectTransform>());

            // Pasang ke SerializedObject
            so.FindProperty("searchInput").objectReferenceValue = searchInputGo.GetComponent<TMP_InputField>();
            so.FindProperty("progressLabel").objectReferenceValue = progressTextGo.GetComponent<TMP_Text>();
            so.FindProperty("prevPageButton").objectReferenceValue = btnPrev.GetComponent<Button>();
            so.FindProperty("nextPageButton").objectReferenceValue = btnNext.GetComponent<Button>();
            so.FindProperty("pageIndicatorLabel").objectReferenceValue = labelPageGo.GetComponent<TMP_Text>();
            so.FindProperty("emptyStateObject").objectReferenceValue = emptyState;
            so.FindProperty("emptyStateLabel").objectReferenceValue = esTextGo.GetComponent<TMP_Text>();

            SerializedProperty cardsProp = so.FindProperty("cards");
            cardsProp.arraySize = 8;
            for (int i = 0; i < 8; i++)
            {
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }

            so.ApplyModifiedProperties();
        }

        private static DictionaryPhotoSlotUI CreatePhotoSlot(string name, Transform parent)
        {
            GameObject slotGo = CreateUIObject(name, parent);
            RectTransform slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(285f, 320f);

            // Frame Polaroid Putih
            var bgImg = slotGo.AddComponent<Image>();
            bgImg.sprite = defaultSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;
            var clickBtn = slotGo.AddComponent<Button>();

            // Foto Snapshot (Kotak di bagian atas)
            GameObject photoGo = CreateUIObject("Photo", slotGo.transform);
            RectTransform photoRt = photoGo.GetComponent<RectTransform>();
            photoRt.anchorMin = new Vector2(0.5f, 1f);
            photoRt.anchorMax = new Vector2(0.5f, 1f);
            photoRt.pivot = new Vector2(0.5f, 1f);
            photoRt.sizeDelta = new Vector2(255f, 240f);
            photoRt.anchoredPosition = new Vector2(0f, -14f);
            var rawImg = photoGo.AddComponent<RawImage>();
            rawImg.color = new Color(0.85f, 0.85f, 0.85f);

            // Bintang Favorit di pojok kanan atas foto
            GameObject favBtnGo = CreateUIObject("Btn_Favorite", photoGo.transform);
            RectTransform favRt = favBtnGo.GetComponent<RectTransform>();
            favRt.anchorMin = new Vector2(1f, 1f);
            favRt.anchorMax = new Vector2(1f, 1f);
            favRt.pivot = new Vector2(1f, 1f);
            favRt.sizeDelta = new Vector2(36f, 36f);
            favRt.anchoredPosition = new Vector2(-4f, -4f);
            var favBtn = favBtnGo.AddComponent<Button>();
            var favImg = favBtnGo.AddComponent<Image>();
            favImg.color = new Color(0f, 0f, 0f, 0.35f);

            GameObject favTextGo = CreateText("StarText", favBtnGo.transform, "FAV", 11, TextAlignmentOptions.Center, Color.white);
            SetStretch(favTextGo.GetComponent<RectTransform>());

            // Label Nama Objek di bawah foto polaroid
            GameObject titleGo = CreateText("Title", slotGo.transform, "Nama Objek", 16, TextAlignmentOptions.Center, new Color(0.2f, 0.2f, 0.2f));
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0f);
            titleRt.anchorMax = new Vector2(1f, 0f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(0f, 55f);
            titleRt.anchoredPosition = Vector2.zero;

            var slotUI = slotGo.AddComponent<DictionaryPhotoSlotUI>();
            SerializedObject so = new SerializedObject(slotUI);
            so.FindProperty("rootObject").objectReferenceValue = slotGo;
            so.FindProperty("photoImage").objectReferenceValue = rawImg;
            so.FindProperty("titleLabel").objectReferenceValue = titleGo.GetComponent<TMP_Text>();
            so.FindProperty("clickButton").objectReferenceValue = clickBtn;
            so.FindProperty("favoriteButton").objectReferenceValue = favBtn;
            so.FindProperty("favoriteIconText").objectReferenceValue = favTextGo.GetComponent<TMP_Text>();
            so.ApplyModifiedProperties();

            return slotUI;
        }

        private static DictionaryVocabCardUI CreateVocabCard(string name, Transform parent)
        {
            GameObject cardGo = CreateUIObject(name, parent);
            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(285f, 320f);

            var bgImg = cardGo.AddComponent<Image>();
            bgImg.sprite = defaultSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.92f, 0.90f, 0.85f, 1f);
            var clickBtn = cardGo.AddComponent<Button>();

            // Discovered Group
            GameObject discGroup = CreateUIObject("DiscoveredGroup", cardGo.transform);
            SetStretch(discGroup.GetComponent<RectTransform>());

            GameObject kanjiGo = CreateText("Kanji", discGroup.transform, "Sepeda", 28, TextAlignmentOptions.Center, new Color(0.1f, 0.15f, 0.25f));
            RectTransform kanjiRt = kanjiGo.GetComponent<RectTransform>();
            kanjiRt.anchorMin = new Vector2(0f, 0.5f);
            kanjiRt.anchorMax = new Vector2(1f, 0.5f);
            kanjiRt.pivot = new Vector2(0.5f, 0.5f);
            kanjiRt.anchoredPosition = new Vector2(0f, 50f);
            kanjiRt.sizeDelta = new Vector2(0f, 60f);

            GameObject romajiGo = CreateText("Romaji", discGroup.transform, "Jitensha", 18, TextAlignmentOptions.Center, new Color(0.35f, 0.4f, 0.45f));
            RectTransform romajiRt = romajiGo.GetComponent<RectTransform>();
            romajiRt.anchorMin = new Vector2(0f, 0.5f);
            romajiRt.anchorMax = new Vector2(1f, 0.5f);
            romajiRt.pivot = new Vector2(0.5f, 0.5f);
            romajiRt.anchoredPosition = new Vector2(0f, 5f);
            romajiRt.sizeDelta = new Vector2(0f, 30f);

            GameObject meaningGo = CreateText("Meaning", discGroup.transform, "Sepeda", 20, TextAlignmentOptions.Center, new Color(0.15f, 0.45f, 0.25f));
            RectTransform meanRt = meaningGo.GetComponent<RectTransform>();
            meanRt.anchorMin = new Vector2(0.5f, 0.5f);
            meanRt.anchorMax = new Vector2(0.5f, 0.5f);
            meanRt.pivot = new Vector2(0.5f, 0.5f);
            meanRt.anchoredPosition = new Vector2(0f, -40f);
            meanRt.sizeDelta = new Vector2(260f, 35f);

            GameObject badgeGo = CreateText("Badge", discGroup.transform, "Transport", 14, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 0f);
            badgeRt.anchorMax = new Vector2(0.5f, 0f);
            badgeRt.pivot = new Vector2(0.5f, 0f);
            badgeRt.anchoredPosition = new Vector2(0f, 15f);
            badgeRt.sizeDelta = new Vector2(150f, 25f);

            // Locked Group
            GameObject lockedGroup = CreateUIObject("LockedGroup", cardGo.transform);
            SetStretch(lockedGroup.GetComponent<RectTransform>());

            var lockedBg = lockedGroup.AddComponent<Image>();
            lockedBg.sprite = defaultSprite;
            lockedBg.type = Image.Type.Sliced;
            lockedBg.color = new Color(0.3f, 0.28f, 0.26f, 0.85f);

            GameObject lockHintGo = CreateText("LockHint", lockedGroup.transform, "[TERKUNCI]\n???\n\nBelum Ditemukan\n(Bidik & foto objek di kota)", 16, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f));
            SetStretch(lockHintGo.GetComponent<RectTransform>());

            var cardUI = cardGo.AddComponent<DictionaryVocabCardUI>();
            SerializedObject so = new SerializedObject(cardUI);
            so.FindProperty("rootObject").objectReferenceValue = cardGo;
            so.FindProperty("cardButton").objectReferenceValue = clickBtn;
            so.FindProperty("discoveredGroup").objectReferenceValue = discGroup;
            so.FindProperty("kanjiLabel").objectReferenceValue = kanjiGo.GetComponent<TMP_Text>();
            so.FindProperty("romajiLabel").objectReferenceValue = romajiGo.GetComponent<TMP_Text>();
            so.FindProperty("meaningLabel").objectReferenceValue = meaningGo.GetComponent<TMP_Text>();
            so.FindProperty("categoryBadge").objectReferenceValue = badgeGo.GetComponent<TMP_Text>();
            so.FindProperty("lockedGroup").objectReferenceValue = lockedGroup;
            so.FindProperty("lockedHintLabel").objectReferenceValue = lockHintGo.GetComponent<TMP_Text>();
            so.ApplyModifiedProperties();

            return cardUI;
        }

        private static GameObject BuildPhotoDetailModal(Transform parent)
        {
            GameObject modal = CreateUIObject("Modal_PhotoDetail", parent);
            SetStretch(modal.GetComponent<RectTransform>());

            var dimImg = modal.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.7f);
            var backdropBtn = modal.AddComponent<Button>();

            GameObject dialog = CreateUIObject("DialogWindow", modal.transform);
            RectTransform dlgRt = dialog.GetComponent<RectTransform>();
            dlgRt.anchorMin = new Vector2(0.5f, 0.5f);
            dlgRt.anchorMax = new Vector2(0.5f, 0.5f);
            dlgRt.pivot = new Vector2(0.5f, 0.5f);
            dlgRt.sizeDelta = new Vector2(650f, 750f);
            var dlgImg = dialog.AddComponent<Image>();
            dlgImg.sprite = defaultSprite;
            dlgImg.type = Image.Type.Sliced;
            dlgImg.color = new Color(0.96f, 0.94f, 0.9f);

            GameObject photoGo = CreateUIObject("PreviewImage", dialog.transform);
            RectTransform photoRt = photoGo.GetComponent<RectTransform>();
            photoRt.anchorMin = new Vector2(0.5f, 1f);
            photoRt.anchorMax = new Vector2(0.5f, 1f);
            photoRt.pivot = new Vector2(0.5f, 1f);
            photoRt.sizeDelta = new Vector2(580f, 440f);
            photoRt.anchoredPosition = new Vector2(0f, -40f);
            var photoRaw = photoGo.AddComponent<RawImage>();

            GameObject titleGo = CreateText("Title", dialog.transform, "Nama Objek", 24, TextAlignmentOptions.Center, new Color(0.15f, 0.15f, 0.2f));
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(580f, 35f);
            titleRt.anchoredPosition = new Vector2(0f, -495f);

            GameObject vocabGo = CreateText("VocabInfo", dialog.transform, "Info Kosakata", 18, TextAlignmentOptions.Center, new Color(0.3f, 0.3f, 0.35f));
            RectTransform vocabRt = vocabGo.GetComponent<RectTransform>();
            vocabRt.anchorMin = new Vector2(0.5f, 1f);
            vocabRt.anchorMax = new Vector2(0.5f, 1f);
            vocabRt.pivot = new Vector2(0.5f, 1f);
            vocabRt.sizeDelta = new Vector2(580f, 50f);
            vocabRt.anchoredPosition = new Vector2(0f, -535f);

            GameObject dateGo = CreateText("DateTaken", dialog.transform, "Diambil: 2026-10-31", 14, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            RectTransform dateRt = dateGo.GetComponent<RectTransform>();
            dateRt.anchorMin = new Vector2(0.5f, 1f);
            dateRt.anchorMax = new Vector2(0.5f, 1f);
            dateRt.pivot = new Vector2(0.5f, 1f);
            dateRt.sizeDelta = new Vector2(580f, 25f);
            dateRt.anchoredPosition = new Vector2(0f, -590f);

            // Action Buttons Bar
            GameObject actions = CreateUIObject("ActionButtons", dialog.transform);
            RectTransform actRt = actions.GetComponent<RectTransform>();
            actRt.anchorMin = new Vector2(0.5f, 0f);
            actRt.anchorMax = new Vector2(0.5f, 0f);
            actRt.pivot = new Vector2(0.5f, 0f);
            actRt.sizeDelta = new Vector2(600f, 50f);
            actRt.anchoredPosition = new Vector2(0f, 75f);
            var actLayout = actions.AddComponent<HorizontalLayoutGroup>();
            actLayout.spacing = 10f;
            actLayout.childAlignment = TextAnchor.MiddleCenter;
            actLayout.childControlWidth = false;
            actLayout.childControlHeight = false;

            GameObject btnFav = CreateButton("Btn_Fav", actions.transform, "Tambah Favorit", new Vector2(150f, 42f), new Color(0.4f, 0.35f, 0.2f));
            GameObject moveDropdownGo = CreateDropdown("Dropdown_MoveAlbum", actions.transform, new Vector2(180f, 42f));
            GameObject btnMove = CreateButton("Btn_Move", actions.transform, "Pindah", new Vector2(90f, 42f), new Color(0.2f, 0.5f, 0.7f));
            GameObject btnDel = CreateButton("Btn_Delete", actions.transform, "Hapus", new Vector2(100f, 42f), new Color(0.8f, 0.25f, 0.2f));

            GameObject btnClose = CreateButton("Btn_Close", dialog.transform, "Tutup", new Vector2(160f, 40f), new Color(0.4f, 0.4f, 0.4f));
            RectTransform bcRt = btnClose.GetComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0f);
            bcRt.anchorMax = new Vector2(0.5f, 0f);
            bcRt.pivot = new Vector2(0.5f, 0f);
            bcRt.anchoredPosition = new Vector2(0f, 20f);

            var modalUI = modal.AddComponent<DictionaryPhotoDetailModal>();
            SerializedObject so = new SerializedObject(modalUI);
            so.FindProperty("modalRoot").objectReferenceValue = modal;
            so.FindProperty("previewImage").objectReferenceValue = photoRaw;
            so.FindProperty("objectNameLabel").objectReferenceValue = titleGo.GetComponent<TMP_Text>();
            so.FindProperty("dateTakenLabel").objectReferenceValue = dateGo.GetComponent<TMP_Text>();
            so.FindProperty("vocabLabel").objectReferenceValue = vocabGo.GetComponent<TMP_Text>();
            so.FindProperty("favoriteButton").objectReferenceValue = btnFav.GetComponent<Button>();
            so.FindProperty("favoriteButtonText").objectReferenceValue = btnFav.GetComponentInChildren<TMP_Text>();
            so.FindProperty("moveAlbumDropdown").objectReferenceValue = moveDropdownGo.GetComponent<TMP_Dropdown>();
            so.FindProperty("moveAlbumButton").objectReferenceValue = btnMove.GetComponent<Button>();
            so.FindProperty("deleteButton").objectReferenceValue = btnDel.GetComponent<Button>();
            so.FindProperty("closeButton").objectReferenceValue = btnClose.GetComponent<Button>();
            so.FindProperty("backdropCloseButton").objectReferenceValue = backdropBtn;
            so.ApplyModifiedProperties();

            return modal;
        }

        private static GameObject BuildVocabDetailModal(Transform parent)
        {
            GameObject modal = CreateUIObject("Modal_VocabDetail", parent);
            SetStretch(modal.GetComponent<RectTransform>());

            var dimImg = modal.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.7f);
            var backdropBtn = modal.AddComponent<Button>();

            GameObject dialog = CreateUIObject("DialogWindow", modal.transform);
            RectTransform dlgRt = dialog.GetComponent<RectTransform>();
            dlgRt.anchorMin = new Vector2(0.5f, 0.5f);
            dlgRt.anchorMax = new Vector2(0.5f, 0.5f);
            dlgRt.pivot = new Vector2(0.5f, 0.5f);
            dlgRt.sizeDelta = new Vector2(560f, 600f);
            var dlgImg = dialog.AddComponent<Image>();
            dlgImg.sprite = defaultSprite;
            dlgImg.type = Image.Type.Sliced;
            dlgImg.color = new Color(0.96f, 0.94f, 0.9f);

            GameObject kanjiGo = CreateText("Kanji", dialog.transform, "Kata Kosakata", 36, TextAlignmentOptions.Center, new Color(0.1f, 0.15f, 0.25f));
            RectTransform kanjiRt = kanjiGo.GetComponent<RectTransform>();
            kanjiRt.anchorMin = new Vector2(0.5f, 1f);
            kanjiRt.anchorMax = new Vector2(0.5f, 1f);
            kanjiRt.pivot = new Vector2(0.5f, 1f);
            kanjiRt.sizeDelta = new Vector2(500f, 60f);
            kanjiRt.anchoredPosition = new Vector2(0f, -40f);

            GameObject romajiGo = CreateText("Romaji", dialog.transform, "Romaji", 22, TextAlignmentOptions.Center, new Color(0.35f, 0.4f, 0.45f));
            RectTransform romajiRt = romajiGo.GetComponent<RectTransform>();
            romajiRt.anchorMin = new Vector2(0.5f, 1f);
            romajiRt.anchorMax = new Vector2(0.5f, 1f);
            romajiRt.pivot = new Vector2(0.5f, 1f);
            romajiRt.sizeDelta = new Vector2(500f, 35f);
            romajiRt.anchoredPosition = new Vector2(0f, -105f);

            GameObject meanGo = CreateText("Meaning", dialog.transform, "Terjemahan", 24, TextAlignmentOptions.Center, new Color(0.15f, 0.45f, 0.25f));
            RectTransform meanRt = meanGo.GetComponent<RectTransform>();
            meanRt.anchorMin = new Vector2(0.5f, 1f);
            meanRt.anchorMax = new Vector2(0.5f, 1f);
            meanRt.pivot = new Vector2(0.5f, 1f);
            meanRt.sizeDelta = new Vector2(500f, 40f);
            meanRt.anchoredPosition = new Vector2(0f, -145f);

            GameObject catGo = CreateText("Category", dialog.transform, "Kategori: Umum", 16, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            RectTransform catRt = catGo.GetComponent<RectTransform>();
            catRt.anchorMin = new Vector2(0.5f, 1f);
            catRt.anchorMax = new Vector2(0.5f, 1f);
            catRt.pivot = new Vector2(0.5f, 1f);
            catRt.sizeDelta = new Vector2(500f, 25f);
            catRt.anchoredPosition = new Vector2(0f, -190f);

            // Tombol Audio
            GameObject btnAudio = CreateButton("Btn_Audio", dialog.transform, "Audio Pelafalan", new Vector2(200f, 44f), new Color(0.2f, 0.55f, 0.8f));
            RectTransform baRt = btnAudio.GetComponent<RectTransform>();
            baRt.anchorMin = new Vector2(0.5f, 1f);
            baRt.anchorMax = new Vector2(0.5f, 1f);
            baRt.pivot = new Vector2(0.5f, 1f);
            baRt.anchoredPosition = new Vector2(0f, -230f);

            var audioSrc = modal.AddComponent<AudioSource>();

            GameObject toggleGo = CreateToggle("Toggle_Example", dialog.transform, "Lihat Contoh Kalimat");
            RectTransform tgRt = toggleGo.GetComponent<RectTransform>();
            tgRt.anchorMin = new Vector2(0.5f, 1f);
            tgRt.anchorMax = new Vector2(0.5f, 1f);
            tgRt.pivot = new Vector2(0.5f, 1f);
            tgRt.anchoredPosition = new Vector2(0f, -295f);
            tgRt.sizeDelta = new Vector2(240f, 32f);

            GameObject exGo = CreateText("ExampleSentence", dialog.transform, "Contoh kalimat akan muncul di sini.", 18, TextAlignmentOptions.Center, new Color(0.2f, 0.2f, 0.25f));
            RectTransform exRt = exGo.GetComponent<RectTransform>();
            exRt.anchorMin = new Vector2(0.5f, 1f);
            exRt.anchorMax = new Vector2(0.5f, 1f);
            exRt.pivot = new Vector2(0.5f, 1f);
            exRt.sizeDelta = new Vector2(480f, 120f);
            exRt.anchoredPosition = new Vector2(0f, -345f);

            GameObject btnClose = CreateButton("Btn_Close", dialog.transform, "Tutup", new Vector2(160f, 40f), new Color(0.4f, 0.4f, 0.4f));
            RectTransform bcRt = btnClose.GetComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0f);
            bcRt.anchorMax = new Vector2(0.5f, 0f);
            bcRt.pivot = new Vector2(0.5f, 0f);
            bcRt.anchoredPosition = new Vector2(0f, 25f);

            var modalUI = modal.AddComponent<DictionaryVocabDetailModal>();
            SerializedObject so = new SerializedObject(modalUI);
            so.FindProperty("modalRoot").objectReferenceValue = modal;
            so.FindProperty("kanjiLabel").objectReferenceValue = kanjiGo.GetComponent<TMP_Text>();
            so.FindProperty("romajiLabel").objectReferenceValue = romajiGo.GetComponent<TMP_Text>();
            so.FindProperty("meaningLabel").objectReferenceValue = meanGo.GetComponent<TMP_Text>();
            so.FindProperty("categoryLabel").objectReferenceValue = catGo.GetComponent<TMP_Text>();
            so.FindProperty("exampleLabel").objectReferenceValue = exGo.GetComponent<TMP_Text>();
            so.FindProperty("exampleToggle").objectReferenceValue = toggleGo.GetComponent<Toggle>();
            so.FindProperty("audioButton").objectReferenceValue = btnAudio.GetComponent<Button>();
            so.FindProperty("closeButton").objectReferenceValue = btnClose.GetComponent<Button>();
            so.FindProperty("backdropCloseButton").objectReferenceValue = backdropBtn;
            so.FindProperty("audioSource").objectReferenceValue = audioSrc;
            so.ApplyModifiedProperties();

            return modal;
        }

        private static GameObject BuildSimpleDialog(Transform parent)
        {
            GameObject modal = CreateUIObject("Dialog_Simple", parent);
            SetStretch(modal.GetComponent<RectTransform>());

            var dimImg = modal.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.7f);

            GameObject dialog = CreateUIObject("DialogWindow", modal.transform);
            RectTransform dlgRt = dialog.GetComponent<RectTransform>();
            dlgRt.anchorMin = new Vector2(0.5f, 0.5f);
            dlgRt.anchorMax = new Vector2(0.5f, 0.5f);
            dlgRt.pivot = new Vector2(0.5f, 0.5f);
            dlgRt.sizeDelta = new Vector2(460f, 260f);
            var dlgImg = dialog.AddComponent<Image>();
            dlgImg.sprite = defaultSprite;
            dlgImg.type = Image.Type.Sliced;
            dlgImg.color = new Color(0.96f, 0.94f, 0.9f);

            GameObject titleGo = CreateText("Title", dialog.transform, "Judul Dialog", 22, TextAlignmentOptions.Center, new Color(0.15f, 0.15f, 0.2f));
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(420f, 35f);
            titleRt.anchoredPosition = new Vector2(0f, -25f);

            GameObject messageGo = CreateText("Message", dialog.transform, "Pesan konfirmasi?", 18, TextAlignmentOptions.Center, new Color(0.3f, 0.3f, 0.3f));
            RectTransform msgRt = messageGo.GetComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0.5f, 1f);
            msgRt.anchorMax = new Vector2(0.5f, 1f);
            msgRt.pivot = new Vector2(0.5f, 1f);
            msgRt.sizeDelta = new Vector2(420f, 60f);
            msgRt.anchoredPosition = new Vector2(0f, -70f);

            GameObject inputGo = CreateInputField("InputField", dialog.transform, "Masukkan teks...", new Vector2(380f, 44f));
            RectTransform inRt = inputGo.GetComponent<RectTransform>();
            inRt.anchorMin = new Vector2(0.5f, 1f);
            inRt.anchorMax = new Vector2(0.5f, 1f);
            inRt.pivot = new Vector2(0.5f, 1f);
            inRt.anchoredPosition = new Vector2(0f, -95f);

            GameObject btnConfirm = CreateButton("Btn_Confirm", dialog.transform, "Ya", new Vector2(130f, 40f), new Color(0.2f, 0.65f, 0.35f));
            RectTransform bcRt = btnConfirm.GetComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0f);
            bcRt.anchorMax = new Vector2(0.5f, 0f);
            bcRt.pivot = new Vector2(0.5f, 0f);
            bcRt.anchoredPosition = new Vector2(80f, 25f);

            GameObject btnCancel = CreateButton("Btn_Cancel", dialog.transform, "Batal", new Vector2(130f, 40f), new Color(0.55f, 0.55f, 0.55f));
            RectTransform bcanRt = btnCancel.GetComponent<RectTransform>();
            bcanRt.anchorMin = new Vector2(0.5f, 0f);
            bcanRt.anchorMax = new Vector2(0.5f, 0f);
            bcanRt.pivot = new Vector2(0.5f, 0f);
            bcanRt.anchoredPosition = new Vector2(-80f, 25f);

            var dialogUI = modal.AddComponent<DictionarySimpleDialog>();
            SerializedObject so = new SerializedObject(dialogUI);
            so.FindProperty("dialogRoot").objectReferenceValue = modal;
            so.FindProperty("titleLabel").objectReferenceValue = titleGo.GetComponent<TMP_Text>();
            so.FindProperty("messageLabel").objectReferenceValue = messageGo.GetComponent<TMP_Text>();
            so.FindProperty("inputField").objectReferenceValue = inputGo.GetComponent<TMP_InputField>();
            so.FindProperty("confirmButton").objectReferenceValue = btnConfirm.GetComponent<Button>();
            so.FindProperty("cancelButton").objectReferenceValue = btnCancel.GetComponent<Button>();
            so.FindProperty("confirmButtonText").objectReferenceValue = btnConfirm.GetComponentInChildren<TMP_Text>();
            so.ApplyModifiedProperties();

            return modal;
        }

        private static GameObject CreatePlaceholderPanel(string name, Transform parent, string message)
        {
            GameObject panel = CreateUIObject(name, parent);
            SetStretchMargins(panel.GetComponent<RectTransform>(), 40f, 40f, 40f, 40f);
            var text = CreateText("Message", panel.transform, message, 26, TextAlignmentOptions.Center, new Color(0.45f, 0.4f, 0.35f));
            SetStretch(text.GetComponent<RectTransform>());
            return panel;
        }

        private static GameObject CreateTabMarker(string name, Transform parent, string label)
        {
            GameObject tabGo = CreateUIObject(name, parent);
            RectTransform rt = tabGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(110f, 58f);

            var img = tabGo.AddComponent<Image>();
            img.sprite = defaultSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.55f, 0.5f, 0.45f, 1f);

            tabGo.AddComponent<Button>();

            GameObject textGo = CreateText("Label", tabGo.transform, label, 16, TextAlignmentOptions.Center, new Color(0.15f, 0.12f, 0.1f));
            SetStretch(textGo.GetComponent<RectTransform>());

            return tabGo;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Vector2 size, Color color)
        {
            return CreateButton(name, parent, text, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero, color);
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos, Color? color = null)
        {
            GameObject btnGo = CreateUIObject(name, parent);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var img = btnGo.AddComponent<Image>();
            img.sprite = defaultSprite;
            img.type = Image.Type.Sliced;
            img.color = color ?? new Color(0.85f, 0.85f, 0.85f);

            btnGo.AddComponent<Button>();

            GameObject textGo = CreateText("Text", btnGo.transform, text, 16, TextAlignmentOptions.Center, Color.white);
            SetStretch(textGo.GetComponent<RectTransform>());

            return btnGo;
        }

        private static GameObject CreateText(string name, Transform parent, string content, float fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject textGo = CreateUIObject(name, parent);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return textGo;
        }

        private static GameObject CreateInputField(string name, Transform parent, string placeholder, Vector2 size)
        {
            GameObject inputGo = CreateUIObject(name, parent);
            RectTransform rt = inputGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var bgImg = inputGo.AddComponent<Image>();
            bgImg.sprite = defaultSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            GameObject textArea = CreateUIObject("TextArea", inputGo.transform);
            RectTransform taRt = textArea.GetComponent<RectTransform>();
            SetStretchMargins(taRt, 10f, 10f, 5f, 5f);
            textArea.AddComponent<RectMask2D>();

            GameObject placeholderGo = CreateText("Placeholder", textArea.transform, placeholder, 16, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            SetStretch(placeholderGo.GetComponent<RectTransform>());

            GameObject textGo = CreateText("Text", textArea.transform, "", 16, TextAlignmentOptions.Left, new Color(0.2f, 0.2f, 0.2f));
            SetStretch(textGo.GetComponent<RectTransform>());

            var input = inputGo.AddComponent<TMP_InputField>();
            input.textViewport = taRt;
            input.textComponent = textGo.GetComponent<TextMeshProUGUI>();
            input.placeholder = placeholderGo.GetComponent<TextMeshProUGUI>();
            input.fontAsset = defaultFont;

            return inputGo;
        }

        private static GameObject CreateDropdown(string name, Transform parent, Vector2 size)
        {
            GameObject ddGo = CreateUIObject(name, parent);
            RectTransform rt = ddGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var img = ddGo.AddComponent<Image>();
            img.sprite = defaultSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.9f, 0.9f, 0.9f);

            GameObject labelGo = CreateText("Label", ddGo.transform, "Pilih...", 16, TextAlignmentOptions.Left, new Color(0.2f, 0.2f, 0.2f));
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            SetStretchMargins(labelRt, 12f, 32f, 0f, 0f);

            GameObject arrowGo = CreateText("Arrow", ddGo.transform, "v", 12, TextAlignmentOptions.Center, new Color(0.4f, 0.4f, 0.4f));
            RectTransform arrowRt = arrowGo.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1f, 0.5f);
            arrowRt.anchorMax = new Vector2(1f, 0.5f);
            arrowRt.sizeDelta = new Vector2(25f, 25f);
            arrowRt.anchoredPosition = new Vector2(-15f, 0f);

            GameObject template = CreateUIObject("Template", ddGo.transform);
            RectTransform tRt = template.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 0f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.sizeDelta = new Vector2(0f, 200f);
            var tImg = template.AddComponent<Image>();
            tImg.sprite = defaultSprite;
            tImg.type = Image.Type.Sliced;
            tImg.color = Color.white;
            var scroll = template.AddComponent<ScrollRect>();

            GameObject viewport = CreateUIObject("Viewport", template.transform);
            SetStretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<Mask>();
            var vpImg = viewport.AddComponent<Image>();
            vpImg.sprite = defaultSprite;

            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 35f);

            GameObject item = CreateUIObject("Item", content.transform);
            RectTransform itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0f, 0.5f);
            itemRt.anchorMax = new Vector2(1f, 0.5f);
            itemRt.sizeDelta = new Vector2(0f, 35f);
            item.AddComponent<Toggle>();

            GameObject itemLabel = CreateText("Item Label", item.transform, "Item", 16, TextAlignmentOptions.Left, new Color(0.2f, 0.2f, 0.2f));
            RectTransform ilRt = itemLabel.GetComponent<RectTransform>();
            SetStretchMargins(ilRt, 12f, 12f, 0f, 0f);

            scroll.content = cRt;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            template.SetActive(false);

            var dropdown = ddGo.AddComponent<TMP_Dropdown>();
            dropdown.captionText = labelGo.GetComponent<TextMeshProUGUI>();
            dropdown.itemText = itemLabel.GetComponent<TextMeshProUGUI>();
            dropdown.template = tRt;

            return ddGo;
        }

        private static GameObject CreateToggle(string name, Transform parent, string label)
        {
            GameObject toggleGo = CreateUIObject(name, parent);
            var toggle = toggleGo.AddComponent<Toggle>();

            GameObject bg = CreateUIObject("Background", toggleGo.transform);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(0f, 0.5f);
            bgRt.sizeDelta = new Vector2(24f, 24f);
            bgRt.anchoredPosition = new Vector2(15f, 0f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = defaultSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            GameObject check = CreateUIObject("Checkmark", bg.transform);
            SetStretchMargins(check.GetComponent<RectTransform>(), 4f, 4f, 4f, 4f);
            var checkImg = check.AddComponent<Image>();
            checkImg.sprite = defaultSprite;
            checkImg.color = new Color(0.2f, 0.65f, 0.35f);

            GameObject textGo = CreateText("Label", toggleGo.transform, label, 16, TextAlignmentOptions.Left, new Color(0.2f, 0.2f, 0.2f));
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            SetStretchMargins(textRt, 35f, 0f, 0f, 0f);

            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;

            return toggleGo;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
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

        private static void SetStretchMargins(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
#endif
