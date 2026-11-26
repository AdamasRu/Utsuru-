using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    [System.Serializable]
    public class QuizPageSlotUI
    {
        [Header("Root & Animasi")]
        public GameObject pageRoot;
        public CanvasGroup canvasGroup;
        public RectTransform pageRect;

        [Header("Header")]
        public TextMeshProUGUI questionBadgeText;
        public TextMeshProUGUI promptText;

        [Header("Foto Polaroid")]
        public GameObject photoFrame;
        public RawImage photoImage;

        [Header("3 Pilihan Jawaban")]
        public Button[] choiceButtons = new Button[3];
        public Image[] choiceBackgrounds = new Image[3];
        public Image[] choiceNumberBadges = new Image[3];
        public TextMeshProUGUI[] choiceNumberTexts = new TextMeshProUGUI[3];
        public TextMeshProUGUI[] choiceJapaneseTexts = new TextMeshProUGUI[3];
        public TextMeshProUGUI[] choiceRomajiTexts = new TextMeshProUGUI[3];
        public GameObject[] choiceCheckmarkObjects = new GameObject[3];

        [Header("Footer Terjemahan")]
        public TextMeshProUGUI translationFooterText;

        [HideInInspector] public Vector3 originalScale = Vector3.one;

        public struct AnimSubItem
        {
            public RectTransform rect;
            public CanvasGroup canvasGroup;
            public Vector3 baseScale;
        }

        private readonly List<AnimSubItem> animSubItems = new List<AnimSubItem>();
        private bool isSubItemsCached = false;

        public void CacheAnimSubItems()
        {
            if (isSubItemsCached) return;
            animSubItems.Clear();

            // Urutan animasi dari atas ke bawah: HeaderBadge -> PromptText -> PhotoFrame -> Pilihan 1, 2, 3 -> Footer
            if (questionBadgeText != null && questionBadgeText.transform.parent != null)
                AddAnimSubItem(questionBadgeText.transform.parent.gameObject);

            if (promptText != null)
                AddAnimSubItem(promptText.gameObject);

            if (photoFrame != null)
                AddAnimSubItem(photoFrame);

            if (choiceButtons != null)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    if (choiceButtons[i] != null)
                        AddAnimSubItem(choiceButtons[i].gameObject);
                }
            }

            if (translationFooterText != null)
                AddAnimSubItem(translationFooterText.gameObject);

            isSubItemsCached = true;
        }

        private void AddAnimSubItem(GameObject target)
        {
            if (target == null) return;
            var rt = target.GetComponent<RectTransform>();
            if (rt == null) return;

            var cg = target.GetComponent<CanvasGroup>();
            if (cg == null) cg = target.AddComponent<CanvasGroup>();

            animSubItems.Add(new AnimSubItem
            {
                rect = rt,
                canvasGroup = cg,
                baseScale = rt.localScale
            });
        }

        public List<AnimSubItem> GetAnimSubItems()
        {
            if (!isSubItemsCached) CacheAnimSubItems();
            return animSubItems;
        }

        public void SetSubItemVisualState(int index, float alpha, float scaleMultiplier)
        {
            if (!isSubItemsCached) CacheAnimSubItems();
            if (index < 0 || index >= animSubItems.Count) return;

            var item = animSubItems[index];
            if (item.canvasGroup != null) item.canvasGroup.alpha = alpha;
            if (item.rect != null) item.rect.localScale = item.baseScale * scaleMultiplier;
        }

        public void SetAllSubItemsVisualState(float alpha, float scaleMultiplier)
        {
            if (!isSubItemsCached) CacheAnimSubItems();
            for (int i = 0; i < animSubItems.Count; i++)
            {
                SetSubItemVisualState(i, alpha, scaleMultiplier);
            }
        }

        public void SetVisualState(float alpha, float scale)
        {
            if (canvasGroup != null) canvasGroup.alpha = alpha;
            if (pageRect != null) pageRect.localScale = originalScale * scale;
        }

        public void ResetAllVisuals()
        {
            SetVisualState(1f, 1f);
            SetAllSubItemsVisualState(1f, 1f);
        }
    }

    /// <summary>
    /// Mengelola fitur Kuis Interaktif pada Tab Quiz di dalam buku Dictionary.
    /// Tampilan simetris 2 halaman per spread (Halaman Kiri = Soal Ganjil, Halaman Kanan = Soal Genap).
    /// Mendukung 3 pilihan jawaban per soal, foto polaroid dari foto player, dan animasi domino bertahap antar elemen.
    /// </summary>
    public class DictionaryQuizSpreadUI : MonoBehaviour
    {
        [Header("Referensi Sistem")]
        [SerializeField] private DictionaryManager dictionaryManager;
        [SerializeField] private PointRewardSystem pointRewardSystem;
        [SerializeField] private DictionaryBookSpreadUI bookSpread;

        [Header("Root Sub-Tampilan")]
        [SerializeField] private GameObject activeQuizRoot;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject resultSummaryRoot;

        [Header("Slot Halaman Kuis (2 Halaman Per Spread)")]
        [SerializeField] private QuizPageSlotUI leftPageSlot = new QuizPageSlotUI();
        [SerializeField] private QuizPageSlotUI rightPageSlot = new QuizPageSlotUI();

        [Header("Aset Sprite Kartu Pilihan")]
        [SerializeField] private Sprite cardNormalSprite;
        [SerializeField] private Sprite cardCorrectSprite;
        [SerializeField] private Sprite cardWrongSprite;

        [Header("Hasil Akhir (Result Summary)")]
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultScoreText;
        [SerializeField] private TextMeshProUGUI resultPercentageText;
        [SerializeField] private TextMeshProUGUI resultPointsRewardText;
        [SerializeField] private Button playAgainButton;

        [Header("Empty / Locked State")]
        [SerializeField] private TextMeshProUGUI emptyProgressText;
        [SerializeField] private Button demoPracticeButton;

        [Header("Pengaturan Animasi Domino Sub-Hierarchy")]
        [Tooltip("Jeda waktu antar objek di dalam sub-hierarchy halaman (detik).")]
        [SerializeField] private float dominoStaggerDelay = 0.045f;
        [Tooltip("Jeda tambahan antara halaman pertama dan halaman kedua (detik).")]
        [SerializeField] private float pageStaggerDelay = 0.06f;
        [Tooltip("Durasi fade-out per objek (detik).")]
        [SerializeField] private float dominoFadeOutDuration = 0.09f;
        [Tooltip("Durasi pop-in per objek (detik).")]
        [SerializeField] private float dominoPopInDuration = 0.22f;

        // Data Kuis Sesi Berjalan
        private class QuizQuestion
        {
            public ObjectInfo targetObject;
            public Texture2D snapshotTexture;
            public List<ObjectInfo> choices = new List<ObjectInfo>();
            public int correctChoiceIndex;
            public bool isAnswered;
            public int selectedChoiceIndex = -1;
            public bool isCorrect;
        }

        private const int MinDiscoveredRequired = 4;
        private const int QuestionsPerSession = 4; // 2 spread: Soal 1-2 & Soal 3-4
        private const int PointsPerCorrectAnswer = 10;

        private readonly List<QuizQuestion> currentQuestions = new List<QuizQuestion>();
        private int currentSpreadIndex = 0;
        private int correctAnswersCount = 0;
        private bool isDemoPracticeMode = false;
        private bool isPageTurning = false;
        private int lastRefreshFrame = -1;
        private Coroutine pageTurnCoroutine;

        public int CurrentSpreadIndex => currentSpreadIndex;
        public int TotalSpreads => Mathf.Max(1, Mathf.CeilToInt(currentQuestions.Count / 2f));
        public bool CanGoPrevious => currentSpreadIndex > 0 && !isPageTurning;
        public bool CanGoNext => currentSpreadIndex < TotalSpreads - 1 && !isPageTurning;
        public bool IsPageTurning => isPageTurning;

        private void Awake()
        {
            if (dictionaryManager == null)
                dictionaryManager = FindFirstObjectByType<DictionaryManager>();
            if (pointRewardSystem == null)
                pointRewardSystem = FindFirstObjectByType<PointRewardSystem>();
            if (bookSpread == null)
                bookSpread = GetComponentInParent<DictionaryBookSpreadUI>();

            // Simpan scale asli dan cache sub-elemen animasi
            if (leftPageSlot.pageRect != null) leftPageSlot.originalScale = leftPageSlot.pageRect.localScale;
            if (rightPageSlot.pageRect != null) rightPageSlot.originalScale = rightPageSlot.pageRect.localScale;
            leftPageSlot.CacheAnimSubItems();
            rightPageSlot.CacheAnimSubItems();

            // Hubungkan listener tombol pilihan
            SetupChoiceListeners(leftPageSlot, isLeftPage: true);
            SetupChoiceListeners(rightPageSlot, isLeftPage: false);

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(StartQuizSession);

            if (demoPracticeButton != null)
                demoPracticeButton.onClick.AddListener(() =>
                {
                    isDemoPracticeMode = true;
                    StartQuizSession();
                });
        }

        private void SetupChoiceListeners(QuizPageSlotUI slot, bool isLeftPage)
        {
            if (slot.choiceButtons == null) return;
            for (int i = 0; i < slot.choiceButtons.Length; i++)
            {
                int choiceIndex = i;
                if (slot.choiceButtons[i] != null)
                {
                    slot.choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(isLeftPage, choiceIndex));
                }
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            StartOrRefreshQuiz();
        }

        private void OnDisable()
        {
            if (pageTurnCoroutine != null)
            {
                StopCoroutine(pageTurnCoroutine);
                pageTurnCoroutine = null;
            }
            isPageTurning = false;
            leftPageSlot.ResetAllVisuals();
            rightPageSlot.ResetAllVisuals();
        }

        public void StartOrRefreshQuiz()
        {
            // Hindari inisialisasi ganda di frame yang sama (misal OnEnable + Refresh dari DictionaryBookSpreadUI)
            if (Application.isPlaying && lastRefreshFrame == Time.frameCount)
                return;
            lastRefreshFrame = Time.frameCount;

            if (dictionaryManager == null)
                dictionaryManager = FindFirstObjectByType<DictionaryManager>();

            var discovered = GetDiscoveredObjects();

            if (discovered.Count < MinDiscoveredRequired && !isDemoPracticeMode)
            {
                ShowEmptyState(discovered.Count);
            }
            else
            {
                StartQuizSession();
            }
        }

        private List<ObjectInfo> GetDiscoveredObjects()
        {
            var result = new List<ObjectInfo>();
            if (dictionaryManager == null) return result;

            var catalog = dictionaryManager.GetAllCatalogObjects();
            if (catalog == null) return result;

            foreach (var obj in catalog)
            {
                if (obj == null) continue;
                bool isFound = dictionaryManager.IsObjectDiscovered(obj.objectId) ||
                               (obj.vocabulary != null && dictionaryManager.IsVocabularyDiscovered(obj.vocabulary.vocabularyId));
                if (isFound)
                {
                    result.Add(obj);
                }
            }

            return result;
        }

        private void ShowEmptyState(int currentCount)
        {
            if (activeQuizRoot != null) activeQuizRoot.SetActive(false);
            if (resultSummaryRoot != null) resultSummaryRoot.SetActive(false);
            if (emptyStateRoot != null) emptyStateRoot.SetActive(true);

            if (emptyProgressText != null)
            {
                emptyProgressText.text = $"Discovered: <b>{currentCount}</b> / {MinDiscoveredRequired} Required Vocabulary";
            }
        }

        public void StartQuizSession()
        {
            if (dictionaryManager == null)
                dictionaryManager = FindFirstObjectByType<DictionaryManager>();

            var catalog = dictionaryManager.GetAllCatalogObjects();
            if (catalog == null || catalog.Count < 3)
            {
                Debug.LogWarning("[DictionaryQuizSpreadUI] Katalog kosakata belum cukup untuk membuat kuis.");
                return;
            }

            List<ObjectInfo> pool = isDemoPracticeMode ? catalog.ToList() : GetDiscoveredObjects();
            if (pool.Count < MinDiscoveredRequired && !isDemoPracticeMode)
            {
                ShowEmptyState(pool.Count);
                return;
            }

            currentQuestions.Clear();
            var shuffledPool = pool.OrderBy(_ => UnityEngine.Random.value).ToList();
            int questionCount = Mathf.Min(QuestionsPerSession, (shuffledPool.Count / 2) * 2);
            if (questionCount < 2) questionCount = Mathf.Min(2, shuffledPool.Count);

            for (int q = 0; q < questionCount; q++)
            {
                var target = shuffledPool[q];
                var question = new QuizQuestion
                {
                    targetObject = target,
                    snapshotTexture = LoadPhotoForObject(target.objectId),
                    choices = new List<ObjectInfo>()
                };

                // Pilihan benar
                question.choices.Add(target);

                // Ambil 2 pilihan pengecoh unik dari katalog (total 3 opsi)
                var distractors = catalog
                    .Where(c => c.objectId != target.objectId &&
                                c.vocabulary != null &&
                                c.vocabulary.japaneseWord != target.vocabulary.japaneseWord)
                    .OrderBy(_ => UnityEngine.Random.value)
                    .Take(2)
                    .ToList();

                question.choices.AddRange(distractors);

                // Acak urutan 3 pilihan
                question.choices = question.choices.OrderBy(_ => UnityEngine.Random.value).ToList();
                question.correctChoiceIndex = question.choices.IndexOf(target);

                currentQuestions.Add(question);
            }

            currentSpreadIndex = 0;
            correctAnswersCount = 0;

            if (emptyStateRoot != null) emptyStateRoot.SetActive(false);
            if (resultSummaryRoot != null) resultSummaryRoot.SetActive(false);
            if (activeQuizRoot != null) activeQuizRoot.SetActive(true);

            if (pageTurnCoroutine != null)
            {
                StopCoroutine(pageTurnCoroutine);
                pageTurnCoroutine = null;
            }
            isPageTurning = false;

            DisplayCurrentSpread();

            if (gameObject.activeInHierarchy)
            {
                pageTurnCoroutine = StartCoroutine(PlaySpreadPopInRoutine(forward: true));
            }
            else
            {
                leftPageSlot.ResetAllVisuals();
                rightPageSlot.ResetAllVisuals();
            }
        }

        private Texture2D LoadPhotoForObject(string objectId)
        {
            if (dictionaryManager == null || dictionaryManager.Photos == null) return null;
            var photoRecord = dictionaryManager.Photos.FirstOrDefault(p => p.objectId == objectId);
            if (photoRecord != null && !string.IsNullOrEmpty(photoRecord.imagePath))
            {
                return PhotoFileStore.Load(photoRecord.imagePath);
            }
            return null;
        }

        public void DisplayCurrentSpread()
        {
            int leftQuestionIdx = currentSpreadIndex * 2;
            int rightQuestionIdx = leftQuestionIdx + 1;

            if (leftQuestionIdx < currentQuestions.Count)
            {
                DisplayQuestionInSlot(leftPageSlot, currentQuestions[leftQuestionIdx], leftQuestionIdx + 1, currentQuestions.Count);
            }
            else
            {
                if (leftPageSlot.pageRoot != null) leftPageSlot.pageRoot.SetActive(false);
            }

            if (rightQuestionIdx < currentQuestions.Count)
            {
                DisplayQuestionInSlot(rightPageSlot, currentQuestions[rightQuestionIdx], rightQuestionIdx + 1, currentQuestions.Count);
            }
            else
            {
                if (rightPageSlot.pageRoot != null) rightPageSlot.pageRoot.SetActive(false);
            }

            UpdateNavigationButtons();
        }

        private void DisplayQuestionInSlot(QuizPageSlotUI slot, QuizQuestion q, int questionNumber, int totalQuestions)
        {
            if (slot.pageRoot != null) slot.pageRoot.SetActive(true);

            // 1. Header Badge & Prompt
            if (slot.questionBadgeText != null)
                slot.questionBadgeText.text = $"Mini Quiz - Question {questionNumber}/{totalQuestions}";

            if (slot.promptText != null)
                slot.promptText.text = "Which kotoba matches this photo?";

            // 2. Foto Pemain di Frame Polaroid
            if (slot.photoImage != null)
            {
                if (q.snapshotTexture != null)
                {
                    slot.photoImage.texture = q.snapshotTexture;
                    slot.photoImage.color = Color.white;
                    DictionaryPolaroidCardUI.ApplyAspectCropUV(slot.photoImage, q.snapshotTexture);
                    if (slot.photoFrame != null) slot.photoFrame.SetActive(true);
                }
                else
                {
                    slot.photoImage.texture = null;
                    slot.photoImage.color = new Color(0.92f, 0.90f, 0.95f, 0.6f);
                    if (slot.photoFrame != null) slot.photoFrame.SetActive(true);
                }
            }

            // 3. Tiga Pilihan Jawaban
            for (int i = 0; i < 3; i++)
            {
                if (i >= slot.choiceButtons.Length || slot.choiceButtons[i] == null) continue;

                if (i < q.choices.Count)
                {
                    slot.choiceButtons[i].gameObject.SetActive(true);
                    var choiceObj = q.choices[i];
                    var vocab = choiceObj.vocabulary;

                    // Nomor Opsi (1, 2, 3)
                    if (slot.choiceNumberTexts != null && i < slot.choiceNumberTexts.Length && slot.choiceNumberTexts[i] != null)
                    {
                        slot.choiceNumberTexts[i].text = (i + 1).ToString();
                    }

                    // Teks Kanji
                    if (slot.choiceJapaneseTexts != null && i < slot.choiceJapaneseTexts.Length && slot.choiceJapaneseTexts[i] != null)
                    {
                        string kanji = vocab != null && !string.IsNullOrEmpty(vocab.japaneseWord) ? vocab.japaneseWord : choiceObj.objectName;
                        slot.choiceJapaneseTexts[i].text = kanji;
                    }

                    // Teks Romaji
                    if (slot.choiceRomajiTexts != null && i < slot.choiceRomajiTexts.Length && slot.choiceRomajiTexts[i] != null)
                    {
                        string romaji = vocab != null && !string.IsNullOrEmpty(vocab.romaji) ? vocab.romaji : "";
                        slot.choiceRomajiTexts[i].text = romaji;
                    }

                    // Status Jawaban: Sudah dijawab atau belum
                    if (q.isAnswered)
                    {
                        slot.choiceButtons[i].interactable = false;

                        if (i == q.correctChoiceIndex && q.isCorrect)
                        {
                            // Benar
                            SetButtonVisual(slot, i, cardCorrectSprite, showCheckmark: true);
                        }
                        else if (i == q.selectedChoiceIndex && !q.isCorrect)
                        {
                            // Salah
                            SetButtonVisual(slot, i, cardWrongSprite, showCheckmark: false);
                        }
                        else
                        {
                            // Opsi lainnya
                            SetButtonVisual(slot, i, cardNormalSprite, showCheckmark: false);
                        }
                    }
                    else
                    {
                        slot.choiceButtons[i].interactable = true;
                        SetButtonVisual(slot, i, cardNormalSprite, showCheckmark: false);
                    }
                }
                else
                {
                    slot.choiceButtons[i].gameObject.SetActive(false);
                }
            }

            // 4. Footer Terjemahan
            if (slot.translationFooterText != null)
            {
                if (q.isAnswered && q.isCorrect)
                {
                    string jp = q.targetObject != null && q.targetObject.vocabulary != null ? q.targetObject.vocabulary.japaneseWord : "";
                    string tr = q.targetObject != null && q.targetObject.vocabulary != null ? q.targetObject.vocabulary.translation : "";
                    slot.translationFooterText.text = $"{jp} = {tr}";
                    slot.translationFooterText.gameObject.SetActive(true);
                }
                else
                {
                    slot.translationFooterText.text = "";
                    slot.translationFooterText.gameObject.SetActive(false);
                }
            }
        }

        private void SetButtonVisual(QuizPageSlotUI slot, int index, Sprite bgSprite, bool showCheckmark)
        {
            if (slot.choiceBackgrounds != null && index < slot.choiceBackgrounds.Length && slot.choiceBackgrounds[index] != null)
            {
                if (bgSprite != null)
                {
                    slot.choiceBackgrounds[index].sprite = bgSprite;
                    slot.choiceBackgrounds[index].type = Image.Type.Sliced;
                }
                slot.choiceBackgrounds[index].color = Color.white;
            }

            if (slot.choiceCheckmarkObjects != null && index < slot.choiceCheckmarkObjects.Length && slot.choiceCheckmarkObjects[index] != null)
            {
                slot.choiceCheckmarkObjects[index].SetActive(showCheckmark);
            }
        }

        private void OnChoiceSelected(bool isLeftPage, int choiceIndex)
        {
            int questionIdx = currentSpreadIndex * 2 + (isLeftPage ? 0 : 1);
            if (questionIdx >= currentQuestions.Count) return;

            var q = currentQuestions[questionIdx];
            if (q.isAnswered) return;

            q.isAnswered = true;
            q.selectedChoiceIndex = choiceIndex;
            q.isCorrect = (choiceIndex == q.correctChoiceIndex);

            var slot = isLeftPage ? leftPageSlot : rightPageSlot;

            // Kunci semua tombol pada soal ini
            for (int i = 0; i < slot.choiceButtons.Length; i++)
            {
                if (slot.choiceButtons[i] != null) slot.choiceButtons[i].interactable = false;
            }

            if (q.isCorrect)
            {
                correctAnswersCount++;
                SetButtonVisual(slot, choiceIndex, cardCorrectSprite, showCheckmark: true);

                if (slot.translationFooterText != null)
                {
                    string jp = q.targetObject != null && q.targetObject.vocabulary != null ? q.targetObject.vocabulary.japaneseWord : "";
                    string tr = q.targetObject != null && q.targetObject.vocabulary != null ? q.targetObject.vocabulary.translation : "";
                    slot.translationFooterText.text = $"{jp} = {tr}";
                    slot.translationFooterText.gameObject.SetActive(true);
                }

                if (pointRewardSystem != null)
                {
                    pointRewardSystem.EarnPoints(PointsPerCorrectAnswer);
                }
            }
            else
            {
                // Jawaban salah
                SetButtonVisual(slot, choiceIndex, cardWrongSprite, showCheckmark: false);

                if (slot.translationFooterText != null)
                {
                    slot.translationFooterText.text = "";
                    slot.translationFooterText.gameObject.SetActive(false);
                }
            }

            UpdateNavigationButtons();
            CheckAllQuestionsCompleted();
        }

        private void CheckAllQuestionsCompleted()
        {
            bool allDone = currentQuestions.Count > 0 && currentQuestions.All(q => q.isAnswered);
            if (allDone)
            {
                // Jika sudah menjawab semua soal dan berada di halaman terakhir, tampilkan rangkuman setelah jeda singkat
                if (currentSpreadIndex == TotalSpreads - 1)
                {
                    StartCoroutine(DelayedShowResults());
                }
            }
        }

        private IEnumerator DelayedShowResults()
        {
            yield return new WaitForSeconds(1.8f);
            ShowResultsSummary();
        }

        // ==========================================
        // NAVIGASI HALAMAN DENGAN ANIMASI DOMINO ANTAR SUB-ELEMEN
        // ==========================================

        public void PreviousSpread()
        {
            if (!CanGoPrevious) return;
            if (pageTurnCoroutine != null) StopCoroutine(pageTurnCoroutine);
            pageTurnCoroutine = StartCoroutine(DominoQuizPageTurnRoutine(forward: false));
        }

        public void NextSpread()
        {
            if (!CanGoNext)
            {
                // Jika sudah di spread terakhir dan semua soal selesai, tombol next bisa menampilkan rangkuman
                if (currentSpreadIndex == TotalSpreads - 1 && currentQuestions.All(q => q.isAnswered))
                {
                    ShowResultsSummary();
                }
                return;
            }

            if (pageTurnCoroutine != null) StopCoroutine(pageTurnCoroutine);
            pageTurnCoroutine = StartCoroutine(DominoQuizPageTurnRoutine(forward: true));
        }

        private IEnumerator DominoQuizPageTurnRoutine(bool forward)
        {
            isPageTurning = true;
            UpdateNavigationButtons();

            QuizPageSlotUI firstSlot = forward ? leftPageSlot : rightPageSlot;
            QuizPageSlotUI secondSlot = forward ? rightPageSlot : leftPageSlot;

            firstSlot.SetVisualState(1f, 1f);
            secondSlot.SetVisualState(1f, 1f);

            int count1 = firstSlot.GetAnimSubItems().Count;
            int count2 = secondSlot.GetAnimSubItems().Count;
            int maxItems = Mathf.Max(count1, count2);

            float totalFadeTime = Mathf.Max(0, maxItems - 1) * dominoStaggerDelay + pageStaggerDelay + dominoFadeOutDuration;
            float elapsed = 0f;

            // 1. Domino Fade-Out antar objek di sub-hierarchy LeftQuizPage & RightQuizPage
            while (elapsed < totalFadeTime)
            {
                elapsed += Time.unscaledDeltaTime;

                UpdateSlotFadeOut(firstSlot, elapsed, 0f);
                UpdateSlotFadeOut(secondSlot, elapsed, pageStaggerDelay);

                yield return null;
            }

            firstSlot.SetAllSubItemsVisualState(0f, 0.85f);
            secondSlot.SetAllSubItemsVisualState(0f, 0.85f);

            // 2. Ganti Data Halaman
            if (forward) currentSpreadIndex++;
            else currentSpreadIndex--;

            DisplayCurrentSpread();

            // 3. Pop-In Bouncy Berurutan antar objek sub-hierarchy
            yield return RunStaggeredPopIn(firstSlot, secondSlot);

            isPageTurning = false;
            pageTurnCoroutine = null;
            UpdateNavigationButtons();
        }

        private IEnumerator PlaySpreadPopInRoutine(bool forward)
        {
            isPageTurning = true;
            UpdateNavigationButtons();

            QuizPageSlotUI firstSlot = forward ? leftPageSlot : rightPageSlot;
            QuizPageSlotUI secondSlot = forward ? rightPageSlot : leftPageSlot;

            firstSlot.SetVisualState(1f, 1f);
            secondSlot.SetVisualState(1f, 1f);

            yield return RunStaggeredPopIn(firstSlot, secondSlot);

            isPageTurning = false;
            pageTurnCoroutine = null;
            UpdateNavigationButtons();
        }

        private IEnumerator RunStaggeredPopIn(QuizPageSlotUI firstSlot, QuizPageSlotUI secondSlot)
        {
            firstSlot.SetAllSubItemsVisualState(0f, 0.85f);
            secondSlot.SetAllSubItemsVisualState(0f, 0.85f);

            int count1 = firstSlot.GetAnimSubItems().Count;
            int count2 = secondSlot.GetAnimSubItems().Count;
            int maxItems = Mathf.Max(count1, count2);

            float totalPopTime = Mathf.Max(0, maxItems - 1) * dominoStaggerDelay + pageStaggerDelay + dominoPopInDuration;
            float elapsed = 0f;

            while (elapsed < totalPopTime)
            {
                elapsed += Time.unscaledDeltaTime;

                UpdateSlotPopIn(firstSlot, elapsed, 0f);
                UpdateSlotPopIn(secondSlot, elapsed, pageStaggerDelay);

                yield return null;
            }

            firstSlot.ResetAllVisuals();
            secondSlot.ResetAllVisuals();
        }

        private void UpdateSlotFadeOut(QuizPageSlotUI slot, float elapsed, float slotDelay)
        {
            if (slot.pageRoot == null || !slot.pageRoot.activeSelf) return;
            var items = slot.GetAnimSubItems();

            for (int i = 0; i < items.Count; i++)
            {
                float startTime = slotDelay + i * dominoStaggerDelay;
                float itemElapsed = elapsed - startTime;
                if (itemElapsed < 0f) continue;

                float t = Mathf.Clamp01(itemElapsed / dominoFadeOutDuration);
                float easeIn = t * t;
                float alpha = Mathf.Lerp(1f, 0f, easeIn);
                float scale = Mathf.Lerp(1f, 0.90f, easeIn);
                slot.SetSubItemVisualState(i, alpha, scale);
            }
        }

        private void UpdateSlotPopIn(QuizPageSlotUI slot, float elapsed, float slotDelay)
        {
            if (slot.pageRoot == null || !slot.pageRoot.activeSelf) return;
            var items = slot.GetAnimSubItems();

            for (int i = 0; i < items.Count; i++)
            {
                float startTime = slotDelay + i * dominoStaggerDelay;
                float itemElapsed = elapsed - startTime;
                if (itemElapsed < 0f) continue;

                float t = Mathf.Clamp01(itemElapsed / dominoPopInDuration);
                float easeOut = 1f - (1f - t) * (1f - t);
                float alpha = Mathf.Clamp01(t * 1.8f);
                float bounce = Mathf.Sin(t * Mathf.PI) * 0.08f;
                float currentScale = Mathf.Lerp(0.85f, 1f, easeOut) + bounce;

                slot.SetSubItemVisualState(i, alpha, currentScale);
            }
        }

        public void UpdateNavigationButtons()
        {
            if (bookSpread != null)
            {
                bookSpread.UpdateQuizNavigationState(CanGoPrevious, CanGoNext || (currentSpreadIndex == TotalSpreads - 1 && currentQuestions.All(q => q.isAnswered)));
            }
        }

        public void ShowResultsSummary()
        {
            if (activeQuizRoot != null) activeQuizRoot.SetActive(false);
            if (emptyStateRoot != null) emptyStateRoot.SetActive(false);
            if (resultSummaryRoot != null) resultSummaryRoot.SetActive(true);

            int total = currentQuestions.Count;
            float percent = total > 0 ? ((float)correctAnswersCount / total) * 100f : 0f;

            if (resultScoreText != null)
                resultScoreText.text = $"Score: {correctAnswersCount} / {total}";

            if (resultPercentageText != null)
                resultPercentageText.text = $"{Mathf.RoundToInt(percent)}%";

            if (resultTitleText != null)
            {
                if (percent >= 100f) resultTitleText.text = "Outstanding! / 完璧です！";
                else if (percent >= 80f) resultTitleText.text = "Great Job! / よくできました！";
                else if (percent >= 60f) resultTitleText.text = "Good Effort! / 頑張りました！";
                else resultTitleText.text = "Keep Practicing! / もう一度挑戦！";
            }

            if (resultPointsRewardText != null)
            {
                int pointsEarned = correctAnswersCount * PointsPerCorrectAnswer;
                resultPointsRewardText.text = $"+{pointsEarned} Points Awarded!";
            }

            if (bookSpread != null)
            {
                bookSpread.UpdateQuizNavigationState(false, false);
            }
        }
    }
}
