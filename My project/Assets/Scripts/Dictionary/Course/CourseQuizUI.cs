using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.Course
{
    /// <summary>
    /// Mini quiz modal to test understanding of lesson materials before an activity is marked completed.
    /// Operates when Time.timeScale = 0 (using WaitForSecondsRealtime).
    /// </summary>
    public class CourseQuizUI : MonoBehaviour
    {
        [Header("Root & Backdrop")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Button backdropCloseButton;
        [SerializeField] private Button closeButton;

        [Header("Header")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text questionCounterLabel;

        [Header("Question & Choices")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TMP_Text[] optionLabels;

        [Header("Feedback")]
        [SerializeField] private TMP_Text feedbackLabel;

        [Header("Status Colors")]
        [SerializeField] private Color normalOptionColor = new Color(0.92f, 0.90f, 0.85f, 1f);
        [SerializeField] private Color correctOptionColor = new Color(0.35f, 0.75f, 0.4f, 1f);
        [SerializeField] private Color wrongOptionColor = new Color(0.85f, 0.3f, 0.25f, 1f);

        private CourseActivity currentActivity;
        private Action<bool> completionCallback;
        private int currentQuestionIndex = 0;
        private bool isAnswering = false;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(CloseQuiz);
            if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(CloseQuiz);

            if (optionButtons != null)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    int index = i;
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }

            if (modalRoot != null) modalRoot.SetActive(false);
        }

        public void StartQuiz(CourseActivity activity, Action<bool> onCompleted)
        {
            if (activity == null || activity.quizQuestions == null || activity.quizQuestions.Count == 0)
            {
                Debug.LogWarning("[CourseQuizUI] Activity has no quiz questions!");
                onCompleted?.Invoke(true);
                return;
            }

            currentActivity = activity;
            completionCallback = onCompleted;
            currentQuestionIndex = 0;
            isAnswering = false;

            if (titleLabel != null)
            {
                string titleSub = !string.IsNullOrEmpty(activity.titleEn) ? activity.titleEn : activity.titleId;
                titleLabel.text = $"Mini Quiz: {activity.titleJp} ({titleSub})";
            }

            if (modalRoot != null) modalRoot.SetActive(true);

            DisplayCurrentQuestion();
        }

        private void DisplayCurrentQuestion()
        {
            if (currentActivity == null || currentQuestionIndex >= currentActivity.quizQuestions.Count)
            {
                FinishQuiz(true);
                return;
            }

            isAnswering = false;
            var q = currentActivity.quizQuestions[currentQuestionIndex];

            if (questionCounterLabel != null)
            {
                questionCounterLabel.text = $"Question {currentQuestionIndex + 1} of {currentActivity.quizQuestions.Count}";
            }

            if (questionText != null)
            {
                questionText.text = q.question;
            }

            if (feedbackLabel != null)
            {
                feedbackLabel.text = "";
            }

            int optionCount = q.options != null ? q.options.Count : 0;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < optionCount)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    optionButtons[i].interactable = true;
                    SetButtonColor(optionButtons[i], normalOptionColor);

                    if (optionLabels != null && i < optionLabels.Length && optionLabels[i] != null)
                    {
                        optionLabels[i].text = q.options[i];
                    }
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnOptionSelected(int selectedIndex)
        {
            if (isAnswering || currentActivity == null) return;
            if (currentQuestionIndex >= currentActivity.quizQuestions.Count) return;

            var q = currentActivity.quizQuestions[currentQuestionIndex];
            isAnswering = true;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].interactable = false;
            }

            bool isCorrect = (selectedIndex == q.correctIndex);

            if (isCorrect)
            {
                SetButtonColor(optionButtons[selectedIndex], correctOptionColor);
                if (feedbackLabel != null)
                {
                    feedbackLabel.color = correctOptionColor;
                    feedbackLabel.text = "Correct! Well done!";
                }

                StartCoroutine(AdvanceAfterDelay(true));
            }
            else
            {
                SetButtonColor(optionButtons[selectedIndex], wrongOptionColor);
                if (q.correctIndex >= 0 && q.correctIndex < optionButtons.Length)
                {
                    SetButtonColor(optionButtons[q.correctIndex], correctOptionColor);
                }

                if (feedbackLabel != null)
                {
                    feedbackLabel.color = wrongOptionColor;
                    feedbackLabel.text = "Not quite, try again!";
                }

                StartCoroutine(AdvanceAfterDelay(false));
            }
        }

        private IEnumerator AdvanceAfterDelay(bool correct)
        {
            // Realtime wait since Time.timeScale = 0 when Dictionary is open
            yield return new WaitForSecondsRealtime(1.4f);

            if (correct)
            {
                currentQuestionIndex++;
                if (currentQuestionIndex >= currentActivity.quizQuestions.Count)
                {
                    FinishQuiz(true);
                }
                else
                {
                    DisplayCurrentQuestion();
                }
            }
            else
            {
                DisplayCurrentQuestion();
            }
        }

        private void FinishQuiz(bool success)
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            completionCallback?.Invoke(success);
        }

        public void CloseQuiz()
        {
            StopAllCoroutines();
            if (modalRoot != null) modalRoot.SetActive(false);
            completionCallback?.Invoke(false);
        }

        private void SetButtonColor(Button btn, Color color)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = color;
        }
    }
}
