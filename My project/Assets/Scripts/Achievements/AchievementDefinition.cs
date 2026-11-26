using System;
using System.Collections.Generic;

namespace Utsuru.Achievements
{
    /// <summary>
    /// Static definition for a single game achievement.
    /// </summary>
    [Serializable]
    public class AchievementDefinition
    {
        public string id;
        public string title;
        public string description;
        public int rewardPoints;

        public AchievementDefinition(string id, string title, string description, int rewardPoints)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.rewardPoints = rewardPoints;
        }
    }

    [Serializable]
    public class AchievementUnlockRecord
    {
        public string id;
        public string unlockedAt; // ISO 8601
    }

    /// <summary>
    /// Serializable per-account achievement save data stored via IDataStorage.
    /// </summary>
    [Serializable]
    public class AchievementSaveData
    {
        public List<string> unlockedAchievementIds = new List<string>();
        public List<string> claimedRewardIds = new List<string>();
        public List<string> completedNpcKeys = new List<string>();
        public List<AchievementUnlockRecord> unlockRecords = new List<AchievementUnlockRecord>();
    }
}
