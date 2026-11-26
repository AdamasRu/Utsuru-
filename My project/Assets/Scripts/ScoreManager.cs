using UnityEngine;
using TMPro;
using Utsuru.Capture;

/// <summary>
/// ScoreManager: Pengelola tampilan skor di UI Day_Showcase / Dialog NPC.
/// Terintegrasi langsung dengan PointRewardSystem sebagai backend penyimpanan persisten terpadu.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private PointRewardSystem pointRewardSystem;

    // Nilai fallback in-memory hanya jika PointRewardSystem tidak ditemukan sama sekali di scene
    private int fallbackScore = 0;

    /// <summary>
    /// Nilai skor total pemain saat ini. Selalu mencerminkan data aktual dari PointRewardSystem.
    /// </summary>
    public int currentScore => pointRewardSystem != null ? pointRewardSystem.Points : fallbackScore;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        FindAndBindPointSystem();
    }

    private void Start()
    {
        if (pointRewardSystem == null)
        {
            FindAndBindPointSystem();
        }
        UpdateScoreUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (pointRewardSystem != null)
        {
            pointRewardSystem.OnPointsChanged -= HandlePointsChanged;
        }
    }

    /// <summary>
    /// Menghubungkan ScoreManager dengan instance PointRewardSystem dan mendengarkan event perubahan nilai poin.
    /// </summary>
    public void BindPointRewardSystem(PointRewardSystem system)
    {
        if (pointRewardSystem != null)
        {
            pointRewardSystem.OnPointsChanged -= HandlePointsChanged;
        }

        pointRewardSystem = system;

        if (pointRewardSystem != null)
        {
            pointRewardSystem.OnPointsChanged += HandlePointsChanged;
            UpdateScoreUI(pointRewardSystem.Points);
        }
    }

    private void FindAndBindPointSystem()
    {
        if (pointRewardSystem == null)
        {
            pointRewardSystem = PointRewardSystem.Instance ?? FindFirstObjectByType<PointRewardSystem>();
        }

        if (pointRewardSystem != null)
        {
            pointRewardSystem.OnPointsChanged -= HandlePointsChanged;
            pointRewardSystem.OnPointsChanged += HandlePointsChanged;
        }
    }

    private void HandlePointsChanged(int totalPoints)
    {
        UpdateScoreUI(totalPoints);
    }

    /// <summary>
    /// Menambah skor (dipanggil dari interaksi dialog NPC pada DialogueManager).
    /// Skor diteruskan ke PointRewardSystem agar tersimpan persisten ke LocalStorage
    /// dan otomatis menyinkronkan seluruh UI (ScoreText dan CameraHUD).
    /// </summary>
    public void AddScore(int amount)
    {
        if (pointRewardSystem == null)
        {
            FindAndBindPointSystem();
        }

        if (pointRewardSystem != null)
        {
            pointRewardSystem.EarnPoints(amount);
        }
        else
        {
            fallbackScore += amount;
            UpdateScoreUI(fallbackScore);
        }
    }

    /// <summary>
    /// Memperbarui label UI skor.
    /// </summary>
    public void UpdateScoreUI(int scoreToDisplay = -1)
    {
        int displayValue = scoreToDisplay >= 0 ? scoreToDisplay : currentScore;
        if (scoreText != null)
        {
            scoreText.text = "Skor: " + displayValue;
        }
    }
}