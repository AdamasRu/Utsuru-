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
    /// <summary>
    /// Mengelola fitur Kuis Interaktif pada Tab Quiz di dalam buku Dictionary.
    /// Soal di-generate secara dinamis berdasarkan kosakata / objek yang telah ditemukan pemain di kota.
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

        [Header("Halaman Kiri (Pertanyaan & Foto)")]
        [SerializeField] private TextMeshProUGUI progressBadgeText;
        [SerializeField] private TextMeshProUGUI scoreBadgeText;
        [SerializeField] private RawImage photoPreviewImage;
        [SerializeField] private GameObject photoFrameContainer;
        [SerializeField] private TextMeshProUGUI promptLabel;
        [SerializeField] private TextMeshProUGUI questionTargetWordText;
        [SerializeField] private TextMeshProUGUI categoryBadgeText;

        [Header("Halaman Kanan (Pilihan Jawaban)")]
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Image[] choiceBackgrounds;
        [SerializeField] private Image[] choiceLetterBadgeImages;
        [SerializeField] private TextMeshProUGUI[] choiceOptionLetters;
        [SerializeField] private TextMeshProUGUI[] choiceJapaneseTexts;
        [SerializeField] private TextMeshProUGUI[] choiceRomajiTexts;

        [Header("Feedback Jawaban & Tombol Lanjut")]
        [SerializeField] private GameObject feedbackContainer;
        [SerializeField] private Image feedbackBannerImage;
        [SerializeField] private TextMeshProUGUI feedbackBannerText;
        [SerializeField] private TextMeshProUGUI feedbackSentenceText;
        [SerializeField] private Button nextQuestionButton;
        [SerializeField] private TextMeshProUGUI nextButtonText;

        [Header("Hasil Akhir (Result Summary)")]
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultScoreText;
        [SerializeField] private TextMeshProUGUI resultPercentageText;
        [SerializeField] private TextMeshProUGUI resultPointsRewardText;
        [SerializeField] private Button playAgainButton;

        [Header("Empty / Locked State")]
        [SerializeField] private TextMeshProUGUI emptyProgressText;
        [SerializeField] private Button demoPracticeButton;

        [Header("Palet Warna Pilihan Jawaban")]
        [SerializeField] private Color normalCardColor = new Color(0.99f, 0.99f, 1f, 0.98f);
        [SerializeField] private Color correctCardColor = new Color(0.86f, 0.97f, 0.88f, 1f); // Fresh soft mint pastel
        [SerializeField] private Color wrongCardColor = new Color(0.99f, 0.88f, 0.90f, 1f);   // Soft pastel rose
        [SerializeField] private Color normalBadgeColor = new Color(0.38f, 0.32f, 0.65f, 1f); // Royal purple
        [SerializeField] private Color correctBadgeColor = new Color(0.18f, 0.68f, 0.38f, 1f); // Vibrant emerald green
        [SerializeField] private Color wrongBadgeColor = new Color(0.82f, 0.24f, 0.28f, 1f);   // Crimson red
        [SerializeField] private Color normalTextColor = new Color(0.18f, 0.14f, 0.26f, 1f);
        [SerializeField] private Color normalRomajiColor = new Color(0.48f, 0.44f, 0.58f, 1f);
        [SerializeField] private Color correctTextColor = new Color(0.08f, 0.42f, 0.18f, 1f);  // Deep forest green
        [SerializeField] private Color correctRomajiColor = new Color(0.18f, 0.52f, 0.26f, 1f);
        [SerializeField] private Color wrongTextColor = new Color(0.65f, 0.14f, 0.18f, 1f);
        [SerializeField] private Color wrongRomajiColor = new Color(0.72f, 0.25f, 0.30f, 1f);

        // Data Kuis Sesi Berjalan
        private struct QuizQuestion
        {
            public ObjectInfo targetObject;
            public Texture2D snapshotTexture;
            public List<ObjectInfo> choices;
            public int correctChoiceIndex;
        }

        private const int MinDiscoveredRequired = 4;
        private const int QuestionsPerSession = 5;
        private const int PointsPerCorrectAnswer = 10;

        private readonly List<QuizQuestion> currentQuestions = new List<QuizQuestion>();
        private int currentQuestionIndex = 0;
        private int correctAnswersCount = 0;
        private bool isAnswerSubmitted = false;
        private bool isDemoPracticeMode = false;

        private void Awake()
        {
            if (dictionaryManager == null)
                dictionaryManager = FindFirstObjectByType<DictionaryManager>();
            if (pointRewardSystem == null)
                pointRewardSystem = FindFirstObjectByType<PointRewardSystem>();
            if (bookSpread == null)
                bookSpread = GetComponentInParent<DictionaryBookSpreadUI>();

            if (choiceButtons != null)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    int index = i;
                    if (choiceButtons[i] != null)
                    {
                        choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(index));
                    }
                }
            }

            if (nextQuestionButton != null)
                nextQuestionButton.onClick.AddListener(OnNextButtonClicked);

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(StartQuizSession);

            if (demoPracticeButton != null)
                demoPracticeButton.onClick.AddListener(() =>
                {
                    isDemoPracticeMode = true;
                    StartQuizSession();
                });
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            StartOrRefreshQuiz();
        }

        public void StartOrRefreshQuiz()
        {
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
            if (catalog == null || catalog.Count < 4)
            {
                Debug.LogWarning("[DictionaryQuizSpreadUI] Katalog kosakata belum cukup untuk membuat kuis.");
                return;
            }

            // Pool pertanyaan diambil dari discovered (atau seluruh katalog jika mode demo practice)
            List<ObjectInfo> pool = isDemoPracticeMode ? catalog.ToList() : GetDiscoveredObjects();
            if (pool.Count < MinDiscoveredRequired && !isDemoPracticeMode)
            {
                ShowEmptyState(pool.Count);
                return;
            }

            // Buat 5 pertanyaan acak (atau sebanyak pool yang ada)
            currentQuestions.Clear();
            var shuffledPool = pool.OrderBy(x => UnityEngine.Random.value).ToList();
            int questionCount = Mathf.Min(QuestionsPerSession, shuffledPool.Count);

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

                // Ambil 3 pilihan pengecoh unik dari katalog
                var distractors = catalog
                    .Where(c => c.objectId != target.objectId &&
                                c.vocabulary != null &&
                                c.vocabulary.japaneseWord != target.vocabulary.japaneseWord)
                    .OrderBy(x => UnityEngine.Random.value)
                    .Take(3)
                    .ToList();

                question.choices.AddRange(distractors);

                // Acak urutan 4 pilihan
                question.choices = question.choices.OrderBy(x => UnityEngine.Random.value).ToList();
                question.correctChoiceIndex = question.choices.IndexOf(target);

                currentQuestions.Add(question);
            }

            currentQuestionIndex = 0;
            correctAnswersCount = 0;

            if (emptyStateRoot != null) emptyStateRoot.SetActive(false);
            if (resultSummaryRoot != null) resultSummaryRoot.SetActive(false);
            if (activeQuizRoot != null) activeQuizRoot.SetActive(true);

            DisplayCurrentQuestion();
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

        private void DisplayCurrentQuestion()
        {
            if (currentQuestionIndex >= currentQuestions.Count)
            {
                ShowResultsSummary();
                return;
            }

            var q = currentQuestions[currentQuestionIndex];
            isAnswerSubmitted = false;

            // 1. Update Status Badges (Halaman Kiri)
            if (progressBadgeText != null)
                progressBadgeText.text = $"Question {currentQuestionIndex + 1} / {currentQuestions.Count}";

            if (scoreBadgeText != null)
                scoreBadgeText.text = $"Score: {correctAnswersCount}";

            // 2. Foto Pemain / Placeholder
            if (photoPreviewImage != null)
            {
                if (q.snapshotTexture != null)
                {
                    photoPreviewImage.texture = q.snapshotTexture;
                    photoPreviewImage.color = Color.white;
                    if (photoFrameContainer != null) photoFrameContainer.SetActive(true);
                }
                else
                {
                    photoPreviewImage.texture = null;
                    photoPreviewImage.color = new Color(0.9f, 0.9f, 0.95f, 0.5f);
                    if (photoFrameContainer != null) photoFrameContainer.SetActive(false);
                }
            }

            // 3. Teks Pertanyaan & Target Kata
            if (promptLabel != null)
                promptLabel.text = "What is the Japanese word for:";

            string wordDisplay = "";
            string categoryDisplay = "General";

            if (q.targetObject != null)
            {
                if (q.targetObject.vocabulary != null && !string.IsNullOrEmpty(q.targetObject.vocabulary.translation))
                {
                    wordDisplay = q.targetObject.vocabulary.translation;
                    categoryDisplay = string.IsNullOrEmpty(q.targetObject.vocabulary.category) ? "General" : q.targetObject.vocabulary.category;
                }
                else
                {
                    wordDisplay = q.targetObject.objectName ?? "Unknown";
                }
            }

            if (questionTargetWordText != null)
                questionTargetWordText.text = wordDisplay;

            if (categoryBadgeText != null)
                categoryBadgeText.text = $"Category: {categoryDisplay}";

            // 4. Reset & Isi Pilihan Jawaban (Halaman Kanan)
            if (choiceButtons != null)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    if (choiceButtons[i] == null) continue;

                    if (i < q.choices.Count)
                    {
                        choiceButtons[i].gameObject.SetActive(true);
                        choiceButtons[i].interactable = true;

                        // Reset Warna Default
                        if (choiceBackgrounds != null && i < choiceBackgrounds.Length && choiceBackgrounds[i] != null)
                            choiceBackgrounds[i].color = normalCardColor;

                        if (choiceLetterBadgeImages != null && i < choiceLetterBadgeImages.Length && choiceLetterBadgeImages[i] != null)
                            choiceLetterBadgeImages[i].color = normalBadgeColor;

                        var choiceItem = q.choices[i];
                        var vocab = choiceItem.vocabulary;

                        if (choiceJapaneseTexts != null && i < choiceJapaneseTexts.Length && choiceJapaneseTexts[i] != null)
                        {
                            choiceJapaneseTexts[i].color = normalTextColor;
                            string jpDisplay;
                            if (vocab != null)
                            {
                                if (!string.IsNullOrEmpty(vocab.kana) && vocab.kana != vocab.japaneseWord)
                                    jpDisplay = $"<b>{vocab.japaneseWord}</b> <size=82%>({vocab.kana})</size>";
                                else
                                    jpDisplay = $"<b>{vocab.japaneseWord}</b>";
                            }
                            else
                            {
                                jpDisplay = choiceItem.objectName ?? "";
                            }
                            choiceJapaneseTexts[i].text = jpDisplay;
                        }

                        if (choiceRomajiTexts != null && i < choiceRomajiTexts.Length && choiceRomajiTexts[i] != null)
                        {
                            choiceRomajiTexts[i].color = normalRomajiColor;
                            choiceRomajiTexts[i].text = vocab != null && !string.IsNullOrEmpty(vocab.romaji) ? vocab.romaji : "";
                        }
                    }
                    else
                    {
                        choiceButtons[i].gameObject.SetActive(false);
                    }
                }
            }

            // Sembunyikan area feedback dan tombol next
            if (feedbackContainer != null) feedbackContainer.SetActive(false);
            if (nextQuestionButton != null) nextQuestionButton.gameObject.SetActive(false);

            if (choiceButtons != null && choiceButtons.Length > 0 && choiceButtons[0] != null)
            {
                var choicesRt = choiceButtons[0].transform.parent as RectTransform;
                if (choicesRt != null && choicesRt.gameObject.activeInHierarchy)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(choicesRt);
                }
            }
        }

        private void OnChoiceSelected(int selectedIndex)
        {
            if (isAnswerSubmitted) return;
            isAnswerSubmitted = true;

            var q = currentQuestions[currentQuestionIndex];
            bool isCorrect = (selectedIndex == q.correctChoiceIndex);

            // Kunci semua tombol agar tidak bisa diklik ulang
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null) choiceButtons[i].interactable = false;
            }

            // Beri highlight warna tombol
            if (isCorrect)
            {
                correctAnswersCount++;
                if (choiceBackgrounds != null && selectedIndex < choiceBackgrounds.Length && choiceBackgrounds[selectedIndex] != null)
                    choiceBackgrounds[selectedIndex].color = correctCardColor;

                if (choiceLetterBadgeImages != null && selectedIndex < choiceLetterBadgeImages.Length && choiceLetterBadgeImages[selectedIndex] != null)
                    choiceLetterBadgeImages[selectedIndex].color = correctBadgeColor;

                if (choiceJapaneseTexts != null && selectedIndex < choiceJapaneseTexts.Length && choiceJapaneseTexts[selectedIndex] != null)
                    choiceJapaneseTexts[selectedIndex].color = correctTextColor;

                if (choiceRomajiTexts != null && selectedIndex < choiceRomajiTexts.Length && choiceRomajiTexts[selectedIndex] != null)
                    choiceRomajiTexts[selectedIndex].color = correctRomajiColor;

                // Tambahkan poin hadiah jika ada PointRewardSystem
                if (pointRewardSystem != null)
                {
                    pointRewardSystem.EarnPoints(PointsPerCorrectAnswer);
                }
            }
            else
            {
                // Tombol salah: Mawar lembut + badge crimson
                if (choiceBackgrounds != null && selectedIndex < choiceBackgrounds.Length && choiceBackgrounds[selectedIndex] != null)
                    choiceBackgrounds[selectedIndex].color = wrongCardColor;

                if (choiceLetterBadgeImages != null && selectedIndex < choiceLetterBadgeImages.Length && choiceLetterBadgeImages[selectedIndex] != null)
                    choiceLetterBadgeImages[selectedIndex].color = wrongBadgeColor;

                if (choiceJapaneseTexts != null && selectedIndex < choiceJapaneseTexts.Length && choiceJapaneseTexts[selectedIndex] != null)
                    choiceJapaneseTexts[selectedIndex].color = wrongTextColor;

                if (choiceRomajiTexts != null && selectedIndex < choiceRomajiTexts.Length && choiceRomajiTexts[selectedIndex] != null)
                    choiceRomajiTexts[selectedIndex].color = wrongRomajiColor;

                // Sorot jawaban yang benar: Mint lembut + badge emerald
                if (choiceBackgrounds != null && q.correctChoiceIndex < choiceBackgrounds.Length && choiceBackgrounds[q.correctChoiceIndex] != null)
                    choiceBackgrounds[q.correctChoiceIndex].color = correctCardColor;

                if (choiceLetterBadgeImages != null && q.correctChoiceIndex < choiceLetterBadgeImages.Length && choiceLetterBadgeImages[q.correctChoiceIndex] != null)
                    choiceLetterBadgeImages[q.correctChoiceIndex].color = correctBadgeColor;

                if (choiceJapaneseTexts != null && q.correctChoiceIndex < choiceJapaneseTexts.Length && choiceJapaneseTexts[q.correctChoiceIndex] != null)
                    choiceJapaneseTexts[q.correctChoiceIndex].color = correctTextColor;

                if (choiceRomajiTexts != null && q.correctChoiceIndex < choiceRomajiTexts.Length && choiceRomajiTexts[q.correctChoiceIndex] != null)
                    choiceRomajiTexts[q.correctChoiceIndex].color = correctRomajiColor;
            }

            // Tampilkan Feedback Banner & Contoh Kalimat
            if (feedbackContainer != null) feedbackContainer.SetActive(true);

            if (feedbackBannerImage != null)
                feedbackBannerImage.color = isCorrect ? correctBadgeColor : wrongBadgeColor;

            if (feedbackBannerText != null)
                feedbackBannerText.text = isCorrect ? "CORRECT! (正解)" : "INCORRECT (不正解)";

            if (feedbackSentenceText != null)
            {
                string example = (q.targetObject != null && q.targetObject.vocabulary != null)
                    ? q.targetObject.vocabulary.exampleSentence
                    : "";
                feedbackSentenceText.text = !string.IsNullOrEmpty(example) ? example : "Practice makes perfect!";
            }

            // Aktifkan Tombol Next / Finish
            if (nextQuestionButton != null)
            {
                nextQuestionButton.gameObject.SetActive(true);
                if (nextButtonText != null)
                {
                    bool isLast = (currentQuestionIndex >= currentQuestions.Count - 1);
                    nextButtonText.text = isLast ? "See Results >" : "Next Question >";
                }
            }
        }

        private void OnNextButtonClicked()
        {
            currentQuestionIndex++;
            DisplayCurrentQuestion();
        }

        private void ShowResultsSummary()
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
        }
    }
}
