using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.Achievements
{
    /// <summary>
    /// UI component representing a single row card in the Achievements modal list.
    /// Supports 3 states:
    /// 1) Locked: bright medal, stars + target points.
    /// 2) Completed & Unclaimed: Collect button shown so player can claim star points.
    /// 3) Claimed: green vector check on both Medal and Stars.
    /// </summary>
    public class AchievementItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text pointsText;
        [SerializeField] private Image medalFrameImage;
        [SerializeField] private Image medalIconImage;
        [SerializeField] private GameObject greenCheckObject;
        [SerializeField] private Image starsIconImage;
        [SerializeField] private GameObject starsCheckObject;
        [SerializeField] private Button collectButton;

        private static readonly Color LockedTint = Color.white;
        private static readonly Color ClaimedTint = new Color(1f, 1f, 1f, 0.42f);

        private static readonly Vector2 StarsNormalPos = new Vector2(368f, 18f);
        private static readonly Vector2 StarsNormalSize = new Vector2(144f, 78f);
        private static readonly Vector2 PointsNormalPos = new Vector2(368f, -38f);
        private static readonly Vector2 PointsNormalSize = new Vector2(160f, 40f);

        private static readonly Vector2 StarsCollectPos = new Vector2(332f, 28f);
        private static readonly Vector2 StarsCollectSize = new Vector2(96f, 52f);
        private static readonly Vector2 PointsCollectPos = new Vector2(416f, 28f);
        private static readonly Vector2 PointsCollectSize = new Vector2(92f, 40f);

        private string currentAchievementId;
        private Action<string> onCollectCallback;

        private void Awake()
        {
            if (collectButton != null)
            {
                collectButton.onClick.RemoveAllListeners();
                collectButton.onClick.AddListener(HandleCollectClicked);
            }
        }

        public void ConfigureReferences(
            TMP_Text title,
            TMP_Text description,
            TMP_Text points,
            Image medalFrame,
            Image medalIcon,
            GameObject greenCheck,
            Image starsIcon,
            GameObject starsCheck = null,
            Button collectBtn = null)
        {
            titleText = title;
            descriptionText = description;
            pointsText = points;
            medalFrameImage = medalFrame;
            medalIconImage = medalIcon;
            greenCheckObject = greenCheck;
            starsIconImage = starsIcon;
            starsCheckObject = starsCheck;
            collectButton = collectBtn;
        }

        public void Setup(AchievementDefinition definition, bool isUnlocked)
        {
            Setup(definition, isUnlocked, isUnlocked, null);
        }

        public void Setup(AchievementDefinition definition, bool isUnlocked, bool isRewardClaimed, Action<string> onCollectClicked)
        {
            if (definition == null) return;

            currentAchievementId = definition.id;
            onCollectCallback = onCollectClicked;

            if (collectButton != null)
            {
                collectButton.onClick.RemoveAllListeners();
                collectButton.onClick.AddListener(HandleCollectClicked);
            }

            if (titleText != null)
                titleText.text = definition.title;

            if (descriptionText != null)
                descriptionText.text = definition.description;

            bool readyToCollect = isUnlocked && !isRewardClaimed;
            bool fullyCompleted = isUnlocked && isRewardClaimed;

            // Left Medal: checked only after reward has been collected
            if (greenCheckObject != null)
                greenCheckObject.SetActive(fullyCompleted);

            if (medalIconImage != null)
                medalIconImage.color = fullyCompleted ? ClaimedTint : LockedTint;

            if (medalFrameImage != null)
                medalFrameImage.color = Color.white;

            // Right Stars & Collect Button
            if (collectButton != null)
            {
                collectButton.gameObject.SetActive(readyToCollect);
                collectButton.interactable = readyToCollect;
            }

            if (starsCheckObject != null)
                starsCheckObject.SetActive(fullyCompleted);

            if (starsIconImage != null)
            {
                starsIconImage.color = fullyCompleted ? ClaimedTint : LockedTint;
                RectTransform starsRt = starsIconImage.rectTransform;
                starsRt.anchoredPosition = readyToCollect ? StarsCollectPos : StarsNormalPos;
                starsRt.sizeDelta = readyToCollect ? StarsCollectSize : StarsNormalSize;
            }

            if (pointsText != null)
            {
                pointsText.text = readyToCollect ? $"+{definition.rewardPoints}" : definition.rewardPoints.ToString();
                pointsText.fontSize = readyToCollect ? 30f : 34f;
                RectTransform ptsRt = pointsText.rectTransform;
                ptsRt.anchoredPosition = readyToCollect ? PointsCollectPos : PointsNormalPos;
                ptsRt.sizeDelta = readyToCollect ? PointsCollectSize : PointsNormalSize;
            }
        }

        private void HandleCollectClicked()
        {
            if (!string.IsNullOrEmpty(currentAchievementId))
            {
                onCollectCallback?.Invoke(currentAchievementId);
            }
        }
    }
}
