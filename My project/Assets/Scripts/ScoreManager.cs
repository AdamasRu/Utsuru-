using UnityEngine;
using TMPro;
using Utsuru.Localization;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoreText;
    public int currentScore { get; private set; } = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        UpdateScoreUI();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(GameLanguage lang)
    {
        UpdateScoreUI();
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            string fmt = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText("SCORE_HUD", "Score: {0}")
                : "Score: {0}";
            scoreText.text = string.Format(fmt, currentScore);
        }
    }
}
