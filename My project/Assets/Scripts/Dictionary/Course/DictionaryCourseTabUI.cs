using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.Course
{
    /// <summary>
    /// Main controller for the Course Tab in Dictionary (Option C - Magazine Spread Layout).
    /// Left Page: Lesson Overview, Writing System, Grammar Notes, Culture & Context, Kanji.
    /// Right Page: Learning Activity, Illustration Panel, Key Phrases, Mini Quiz & Completion Status.
    /// </summary>
    public class DictionaryCourseTabUI : MonoBehaviour
    {
        [Header("Database & Manager References")]
        [SerializeField] private CourseDatabase database;
        [SerializeField] private DictionaryManager dictionaryManager;
        [SerializeField] private CourseQuizUI quizUI;

        [Header("Top Bar / Progress")]
        [SerializeField] private TMP_Text overallProgressLabel;

        [Header("Left Page (Lesson Info & Notes)")]
        [SerializeField] private TMP_Text topicBadgeLabel;
        [SerializeField] private TMP_Text lessonTitleLabel;
        [SerializeField] private TMP_Text lessonObjectiveLabel;
        [SerializeField] private TMP_Text aksaraNoteLabel;
        [SerializeField] private GameObject aksaraNoteSection;
        [SerializeField] private TMP_Text grammarNotesLabel;
        [SerializeField] private GameObject grammarNotesSection;
        [SerializeField] private TMP_Text culturalNoteLabel;
        [SerializeField] private GameObject culturalNoteSection;
        [SerializeField] private TMP_Text kanjiWordsLabel;
        [SerializeField] private GameObject kanjiWordsSection;

        [Header("Right Page (Activity & Practice)")]
        [SerializeField] private TMP_Text activityHeaderLabel;
        [SerializeField] private TMP_Text activityTitleLabel;
        [SerializeField] private RawImage illustrationImage;
        [SerializeField] private GameObject illustrationContainer;
        [SerializeField] private TMP_Text explanationLabel;
        [SerializeField] private TMP_Text keyPhrasesLabel;
        [SerializeField] private GameObject keyPhrasesSection;
        [SerializeField] private Button quizButton;
        [SerializeField] private TMP_Text quizButtonText;
        [SerializeField] private GameObject completedBadgeObject;
        [SerializeField] private Button prevActivityButton;
        [SerializeField] private Button nextActivityButton;
        [SerializeField] private TMP_Text activityIndicatorLabel;

        [Header("Lesson Navigation (Bottom Bar)")]
        [SerializeField] private Button prevLessonButton;
        [SerializeField] private Button nextLessonButton;
        [SerializeField] private TMP_Text lessonIndicatorLabel;

        private int currentLessonIndex = 0;
        private int currentActivityIndex = 0;

        private void Awake()
        {
            if (database == null) database = CourseDatabase.Instance ?? FindFirstObjectByType<CourseDatabase>();
            if (dictionaryManager == null) dictionaryManager = FindFirstObjectByType<DictionaryManager>();

            if (prevLessonButton != null) prevLessonButton.onClick.AddListener(OnPrevLesson);
            if (nextLessonButton != null) nextLessonButton.onClick.AddListener(OnNextLesson);

            if (prevActivityButton != null) prevActivityButton.onClick.AddListener(OnPrevActivity);
            if (nextActivityButton != null) nextActivityButton.onClick.AddListener(OnNextActivity);

            if (quizButton != null) quizButton.onClick.AddListener(OnQuizButtonClicked);
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (database == null) database = CourseDatabase.Instance ?? FindFirstObjectByType<CourseDatabase>();
            if (dictionaryManager == null) dictionaryManager = FindFirstObjectByType<DictionaryManager>();

            database.EnsureLoaded();

            if (database.AllLessons.Count == 0)
            {
                Debug.LogWarning("[DictionaryCourseTabUI] No lesson data available.");
                return;
            }

            // Restore last viewed lesson & activity
            if (dictionaryManager != null)
            {
                string lastLessonId = dictionaryManager.GetLastCourseLesson();
                if (!string.IsNullOrEmpty(lastLessonId))
                {
                    int idx = database.GetLessonIndex(lastLessonId);
                    if (idx >= 0)
                    {
                        currentLessonIndex = idx;
                        currentActivityIndex = Mathf.Clamp(dictionaryManager.GetLastCourseActivityIndex(), 0, 100);
                    }
                }
            }

            ValidateIndices();
            RenderSpread();
            UpdateOverallProgress();
        }

        private void ValidateIndices()
        {
            if (currentLessonIndex < 0) currentLessonIndex = 0;
            if (currentLessonIndex >= database.AllLessons.Count) currentLessonIndex = database.AllLessons.Count - 1;

            var lesson = database.GetLessonByIndex(currentLessonIndex);
            if (lesson != null && lesson.activities != null && lesson.activities.Count > 0)
            {
                if (currentActivityIndex < 0) currentActivityIndex = 0;
                if (currentActivityIndex >= lesson.activities.Count) currentActivityIndex = lesson.activities.Count - 1;
            }
            else
            {
                currentActivityIndex = 0;
            }
        }

        private void RenderSpread()
        {
            var lesson = database.GetLessonByIndex(currentLessonIndex);
            if (lesson == null) return;

            CourseTopic currentTopic = null;
            foreach (var topic in database.Topics)
            {
                if (topic.lessons != null && topic.lessons.Exists(l => l.lessonId == lesson.lessonId))
                {
                    currentTopic = topic;
                    break;
                }
            }

            RenderLeftPage(currentTopic, lesson);
            RenderRightPage(lesson);
            UpdateNavigation(lesson);

            if (dictionaryManager != null)
            {
                dictionaryManager.SaveCoursePosition(lesson.lessonId, currentActivityIndex);
            }
        }

        private void RenderLeftPage(CourseTopic topic, CourseLesson lesson)
        {
            // Topic Badge
            if (topicBadgeLabel != null)
            {
                if (topic != null)
                {
                    string topicSub = !string.IsNullOrEmpty(topic.titleEn) ? topic.titleEn : topic.titleId;
                    topicBadgeLabel.text = $"{topic.topicId}: {topic.titleJp} ({topicSub})";
                }
                else
                {
                    topicBadgeLabel.text = $"Lesson {lesson.lessonNumber}";
                }
            }

            // Lesson Title
            if (lessonTitleLabel != null)
            {
                string lessonSub = !string.IsNullOrEmpty(lesson.titleEn) ? lesson.titleEn : lesson.titleId;
                lessonTitleLabel.text = $"Lesson {lesson.lessonNumber}: {lesson.titleJp}\n<size=75%><color=#555555>{lessonSub}</color></size>";
            }

            // Objective
            if (lessonObjectiveLabel != null)
            {
                lessonObjectiveLabel.text = !string.IsNullOrEmpty(lesson.objective)
                    ? $"<b>Learning Objectives:</b>\n{lesson.objective}"
                    : "";
            }

            // Writing System (Aksara)
            bool hasAksara = !string.IsNullOrEmpty(lesson.aksaraNote);
            if (aksaraNoteSection != null) aksaraNoteSection.SetActive(hasAksara);
            if (aksaraNoteLabel != null && hasAksara)
            {
                aksaraNoteLabel.text = $"<b>Writing System:</b>\n{lesson.aksaraNote}";
            }

            // Grammar Notes
            bool hasGrammar = lesson.grammar != null && lesson.grammar.Count > 0;
            if (grammarNotesSection != null) grammarNotesSection.SetActive(hasGrammar);
            if (grammarNotesLabel != null && hasGrammar)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<b>Grammar Notes:</b>");
                foreach (var gr in lesson.grammar)
                {
                    sb.AppendLine($"• {gr.explanation}");
                }
                grammarNotesLabel.text = sb.ToString().TrimEnd();
            }

            // Cultural Note
            bool hasCulture = lesson.culturalNote != null && !string.IsNullOrEmpty(lesson.culturalNote.content);
            if (culturalNoteSection != null) culturalNoteSection.SetActive(hasCulture);
            if (culturalNoteLabel != null && hasCulture)
            {
                culturalNoteLabel.text = $"<b>Culture & Context:</b>\n{lesson.culturalNote.content}";
            }

            // Kanji Words
            bool hasKanji = lesson.kanjiWords != null && lesson.kanjiWords.Count > 0;
            if (kanjiWordsSection != null) kanjiWordsSection.SetActive(hasKanji);
            if (kanjiWordsLabel != null && hasKanji)
            {
                kanjiWordsLabel.text = $"<b>New Kanji:</b> {string.Join("   •   ", lesson.kanjiWords)}";
            }
        }

        private void RenderRightPage(CourseLesson lesson)
        {
            if (lesson.activities == null || lesson.activities.Count == 0)
            {
                if (activityHeaderLabel != null) activityHeaderLabel.text = "";
                if (activityTitleLabel != null) activityTitleLabel.text = "No activities available for this lesson.";
                if (illustrationContainer != null) illustrationContainer.SetActive(false);
                if (explanationLabel != null) explanationLabel.text = "";
                if (keyPhrasesSection != null) keyPhrasesSection.SetActive(false);
                if (quizButton != null) quizButton.gameObject.SetActive(false);
                if (completedBadgeObject != null) completedBadgeObject.SetActive(false);
                return;
            }

            var act = lesson.activities[currentActivityIndex];

            // Activity Header & Title
            if (activityHeaderLabel != null)
            {
                activityHeaderLabel.text = $"ACTIVITY {currentActivityIndex + 1} OF {lesson.activities.Count}";
            }

            if (activityTitleLabel != null)
            {
                string actSub = !string.IsNullOrEmpty(act.titleEn) ? act.titleEn : act.titleId;
                activityTitleLabel.text = $"{act.titleJp}\n<size=75%><color=#445566>{actSub}</color></size>";
            }

            // Panel Illustration
            if (illustrationImage != null)
            {
                bool hasImage = act.panelImages != null && act.panelImages.Count > 0;
                if (hasImage)
                {
                    Texture2D tex = database.LoadIllustration(act.panelImages[0]);
                    if (tex != null)
                    {
                        illustrationImage.texture = tex;
                        if (illustrationContainer != null) illustrationContainer.SetActive(true);
                    }
                    else
                    {
                        if (illustrationContainer != null) illustrationContainer.SetActive(false);
                    }
                }
                else
                {
                    if (illustrationContainer != null) illustrationContainer.SetActive(false);
                }
            }

            // Explanation
            if (explanationLabel != null)
            {
                explanationLabel.text = act.explanation ?? "";
            }

            // Key Phrases
            bool hasPhrases = act.keyPhrases != null && act.keyPhrases.Count > 0;
            if (keyPhrasesSection != null) keyPhrasesSection.SetActive(hasPhrases);
            if (keyPhrasesLabel != null && hasPhrases)
            {
                var sb = new StringBuilder();
                foreach (var phrase in act.keyPhrases)
                {
                    sb.AppendLine($"• {phrase}");
                }
                keyPhrasesLabel.text = sb.ToString().TrimEnd();
            }

            // Quiz & Completion Status
            bool isCompleted = dictionaryManager != null && dictionaryManager.IsCourseActivityCompleted(act.activityId);

            if (completedBadgeObject != null)
            {
                completedBadgeObject.SetActive(isCompleted);
            }

            if (quizButton != null)
            {
                quizButton.gameObject.SetActive(true);
                if (quizButtonText != null)
                {
                    quizButtonText.text = isCompleted ? "Retake Mini-Quiz" : "Take Mini-Quiz";
                }
            }

            // Activity Sub-Pagination
            if (activityIndicatorLabel != null)
            {
                activityIndicatorLabel.text = $"{currentActivityIndex + 1} / {lesson.activities.Count}";
            }

            if (prevActivityButton != null)
            {
                prevActivityButton.interactable = currentActivityIndex > 0;
            }

            if (nextActivityButton != null)
            {
                nextActivityButton.interactable = currentActivityIndex < lesson.activities.Count - 1;
            }
        }

        private void UpdateNavigation(CourseLesson lesson)
        {
            if (lessonIndicatorLabel != null)
            {
                lessonIndicatorLabel.text = $"Lesson {currentLessonIndex + 1} / {database.AllLessons.Count}";
            }

            if (prevLessonButton != null)
            {
                prevLessonButton.interactable = currentLessonIndex > 0;
            }

            if (nextLessonButton != null)
            {
                nextLessonButton.interactable = currentLessonIndex < database.AllLessons.Count - 1;
            }
        }

        private void UpdateOverallProgress()
        {
            if (overallProgressLabel == null || database == null) return;

            int total = database.TotalActivityCount;
            if (total == 0)
            {
                overallProgressLabel.text = "Course Progress: 0 / 0";
                return;
            }

            int completed = 0;
            if (dictionaryManager != null)
            {
                foreach (var lesson in database.AllLessons)
                {
                    if (lesson.activities == null) continue;
                    foreach (var act in lesson.activities)
                    {
                        if (dictionaryManager.IsCourseActivityCompleted(act.activityId))
                        {
                            completed++;
                        }
                    }
                }
            }

            int percent = Mathf.RoundToInt((float)completed / total * 100f);
            overallProgressLabel.text = $"Course Progress: {completed} / {total} Activities ({percent}%)";
        }

        private void OnPrevLesson()
        {
            if (currentLessonIndex > 0)
            {
                currentLessonIndex--;
                currentActivityIndex = 0;
                RenderSpread();
            }
        }

        private void OnNextLesson()
        {
            if (currentLessonIndex < database.AllLessons.Count - 1)
            {
                currentLessonIndex++;
                currentActivityIndex = 0;
                RenderSpread();
            }
        }

        private void OnPrevActivity()
        {
            var lesson = database.GetLessonByIndex(currentLessonIndex);
            if (lesson != null && currentActivityIndex > 0)
            {
                currentActivityIndex--;
                RenderRightPage(lesson);
                if (dictionaryManager != null) dictionaryManager.SaveCoursePosition(lesson.lessonId, currentActivityIndex);
            }
        }

        private void OnNextActivity()
        {
            var lesson = database.GetLessonByIndex(currentLessonIndex);
            if (lesson != null && lesson.activities != null && currentActivityIndex < lesson.activities.Count - 1)
            {
                currentActivityIndex++;
                RenderRightPage(lesson);
                if (dictionaryManager != null) dictionaryManager.SaveCoursePosition(lesson.lessonId, currentActivityIndex);
            }
        }

        private void OnQuizButtonClicked()
        {
            var lesson = database.GetLessonByIndex(currentLessonIndex);
            if (lesson == null || lesson.activities == null || currentActivityIndex >= lesson.activities.Count) return;

            var act = lesson.activities[currentActivityIndex];
            if (quizUI != null)
            {
                quizUI.StartQuiz(act, (passed) =>
                {
                    if (passed)
                    {
                        if (dictionaryManager != null)
                        {
                            dictionaryManager.MarkCourseActivityCompleted(act.activityId);
                        }

                        RenderRightPage(lesson);
                        UpdateOverallProgress();
                    }
                });
            }
        }
    }
}
