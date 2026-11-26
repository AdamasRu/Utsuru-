using System;
using System.Collections.Generic;
using UnityEngine;
using Utsuru.Capture;

namespace Utsuru.Achievements
{
    /// <summary>
    /// Tracks player progress across photography, dictionary collection, NPC dialogues,
    /// and learning points to unlock achievements persistently per account.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Tooltip("Fallback account ID when SessionManager is not active.")]
        [SerializeField] private string fallbackAccountId = "player1";

        private const string SaveKey = "achievements";

        private IDataStorage storage;
        private AchievementSaveData data;
        private readonly HashSet<string> unlockedIds = new HashSet<string>();
        private readonly HashSet<string> claimedRewardIds = new HashSet<string>();
        private readonly HashSet<string> completedNpcKeys = new HashSet<string>();

        private DictionaryManager boundDictionary;
        private PointRewardSystem boundPoints;
        private bool isEvaluating;
        private bool pendingReevaluate;

        /// <summary>Fired whenever an achievement transitions from locked to unlocked.</summary>
        public event Action<AchievementDefinition> OnAchievementUnlocked;

        /// <summary>Fired whenever achievement state changes.</summary>
        public event Action OnAchievementsUpdated;

        public IReadOnlyList<AchievementDefinition> AllAchievements => AchievementCatalog.All;
        public int UnlockedCount => unlockedIds.Count;
        public int CompletedNpcCount => completedNpcKeys.Count;

        public static AchievementManager EnsureInstance()
        {
            if (Instance != null) return Instance;

            var existing = FindFirstObjectByType<AchievementManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var go = new GameObject("AchievementManager");
            Instance = go.AddComponent<AchievementManager>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AchievementManager] Duplicate instance detected; destroying duplicate.", this);
                Destroy(this);
                return;
            }
            Instance = this;

            storage = new LocalStorage();
            LoadProgress();
        }

        private void Start()
        {
            BindGameSystems();
            EvaluateAll();
        }

        private void OnDestroy()
        {
            UnbindGameSystems();
            if (Instance == this) Instance = null;
        }

        public string ResolveAccountId()
        {
            if (Utsuru.Authentication.SessionManager.Instance != null && Utsuru.Authentication.SessionManager.Instance.IsLoggedIn)
            {
                if (!string.IsNullOrEmpty(Utsuru.Authentication.SessionManager.Instance.Username))
                    return Utsuru.Authentication.SessionManager.Instance.Username;
                if (!string.IsNullOrEmpty(Utsuru.Authentication.SessionManager.Instance.LocalProfileId))
                    return Utsuru.Authentication.SessionManager.Instance.LocalProfileId;
            }
            return string.IsNullOrEmpty(fallbackAccountId) ? "player1" : fallbackAccountId;
        }

        public void LoadProgress()
        {
            if (storage == null) storage = new LocalStorage();
            string account = ResolveAccountId();

            if (!storage.TryLoad<AchievementSaveData>(account, SaveKey, out data) || data == null)
            {
                data = new AchievementSaveData();
            }

            unlockedIds.Clear();
            if (data.unlockedAchievementIds != null)
            {
                foreach (string id in data.unlockedAchievementIds)
                {
                    if (!string.IsNullOrEmpty(id)) unlockedIds.Add(id);
                }
            }

            claimedRewardIds.Clear();
            if (data.claimedRewardIds != null)
            {
                foreach (string id in data.claimedRewardIds)
                {
                    if (!string.IsNullOrEmpty(id)) claimedRewardIds.Add(id);
                }
            }

            completedNpcKeys.Clear();
            if (data.completedNpcKeys != null)
            {
                foreach (string npcKey in data.completedNpcKeys)
                {
                    if (!string.IsNullOrEmpty(npcKey)) completedNpcKeys.Add(npcKey);
                }
            }
        }

        public bool IsUnlocked(string achievementId)
        {
            return !string.IsNullOrEmpty(achievementId) && unlockedIds.Contains(achievementId);
        }

        public bool IsRewardClaimed(string achievementId)
        {
            return !string.IsNullOrEmpty(achievementId) && claimedRewardIds.Contains(achievementId);
        }

        /// <summary>
        /// Claims the star reward points for a completed achievement, adding them to the player's score/points.
        /// </summary>
        public bool ClaimReward(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return false;
            if (!unlockedIds.Contains(achievementId) || claimedRewardIds.Contains(achievementId))
                return false;

            AchievementDefinition def = AchievementCatalog.GetById(achievementId);
            if (def == null) return false;

            claimedRewardIds.Add(achievementId);
            if (data.claimedRewardIds == null) data.claimedRewardIds = new List<string>();
            if (!data.claimedRewardIds.Contains(achievementId))
                data.claimedRewardIds.Add(achievementId);

            Persist();

            BindGameSystems();
            if (def.rewardPoints > 0)
            {
                if (boundPoints != null)
                {
                    boundPoints.EarnPoints(def.rewardPoints);
                }
                else if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddScore(def.rewardPoints);
                }
            }

            Debug.Log($"[AchievementManager] Claimed +{def.rewardPoints} star points for '{def.title}'.");

            EvaluateAll();
            OnAchievementsUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Records a successfully completed NPC dialogue with a correct Japanese answer.
        /// Call from DialogueManager when the player selects a correct response.
        /// </summary>
        public void RecordNpcDialogueCorrect(string npcUniqueKey)
        {
            if (string.IsNullOrWhiteSpace(npcUniqueKey))
            {
                npcUniqueKey = $"npc_{completedNpcKeys.Count + 1}";
            }
            else
            {
                npcUniqueKey = npcUniqueKey.Trim();
            }

            bool added = completedNpcKeys.Add(npcUniqueKey);
            if (added)
            {
                if (data.completedNpcKeys == null) data.completedNpcKeys = new List<string>();
                if (!data.completedNpcKeys.Contains(npcUniqueKey))
                {
                    data.completedNpcKeys.Add(npcUniqueKey);
                }
                Persist();
            }

            EvaluateAll();
        }

        /// <summary>
        /// Re-binds to DictionaryManager and PointRewardSystem if needed and checks all achievement rules.
        /// </summary>
        public void EvaluateAll()
        {
            if (isEvaluating)
            {
                pendingReevaluate = true;
                return;
            }

            isEvaluating = true;
            try
            {
                int safetyPasses = 0;
                do
                {
                    pendingReevaluate = false;
                    safetyPasses++;

                    BindGameSystems();

                    bool anyNewlyUnlocked = false;

                    int savedPhotosCount = (boundDictionary != null && boundDictionary.Photos != null)
                        ? boundDictionary.Photos.Count
                        : 0;
                    int discoveredVocabCount = boundDictionary != null
                        ? boundDictionary.DiscoveredVocabularyCount
                        : 0;
                    int totalPoints = boundPoints != null
                        ? boundPoints.Points
                        : 0;
                    int distinctNpcs = completedNpcKeys.Count;

                    // 1. First Snap: Save at least 1 Polaroid photo into Dictionary
                    if (savedPhotosCount >= 1)
                        anyNewlyUnlocked |= TryUnlockInternal(AchievementCatalog.FirstSnapId);

                    // 2. Hajimemashite!: Complete at least 1 NPC dialogue with the correct Japanese answer
                    if (distinctNpcs >= 1)
                        anyNewlyUnlocked |= TryUnlockInternal(AchievementCatalog.HajimemashiteId);

                    // 3. Word Collector: Discover & register at least 10 different vocabularies
                    if (discoveredVocabCount >= 10)
                        anyNewlyUnlocked |= TryUnlockInternal(AchievementCatalog.WordCollectorId);

                    // 4. Master of Kaiwa: Complete conversations with 3 different NPCs using correct answers
                    if (distinctNpcs >= 3)
                        anyNewlyUnlocked |= TryUnlockInternal(AchievementCatalog.MasterOfKaiwaId);

                    // 5. Utsuru Master: Collect at least 100 total Learning Points
                    if (totalPoints >= 100)
                        anyNewlyUnlocked |= TryUnlockInternal(AchievementCatalog.UtsuruMasterId);

                    if (anyNewlyUnlocked)
                    {
                        Persist();
                        OnAchievementsUpdated?.Invoke();
                    }
                }
                while (pendingReevaluate && safetyPasses < 5);
            }
            finally
            {
                isEvaluating = false;
            }
        }

        private bool TryUnlockInternal(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId) || !unlockedIds.Add(achievementId))
                return false;

            if (data.unlockedAchievementIds == null)
                data.unlockedAchievementIds = new List<string>();

            if (!data.unlockedAchievementIds.Contains(achievementId))
                data.unlockedAchievementIds.Add(achievementId);

            if (data.unlockRecords == null)
                data.unlockRecords = new List<AchievementUnlockRecord>();

            data.unlockRecords.Add(new AchievementUnlockRecord
            {
                id = achievementId,
                unlockedAt = DateTime.UtcNow.ToString("o")
            });

            AchievementDefinition def = AchievementCatalog.GetById(achievementId);
            Debug.Log($"[AchievementManager] Achievement UNLOCKED: {def?.title ?? achievementId}");
            if (def != null)
            {
                OnAchievementUnlocked?.Invoke(def);
            }
            return true;
        }

        private void BindGameSystems()
        {
            var dict = DictionaryManager.Instance ?? FindFirstObjectByType<DictionaryManager>();
            if (dict != boundDictionary)
            {
                if (boundDictionary != null) boundDictionary.OnDictionaryChanged -= HandleDictionaryChanged;
                boundDictionary = dict;
                if (boundDictionary != null) boundDictionary.OnDictionaryChanged += HandleDictionaryChanged;
            }

            var pts = PointRewardSystem.Instance ?? FindFirstObjectByType<PointRewardSystem>();
            if (pts != boundPoints)
            {
                if (boundPoints != null) boundPoints.OnPointsChanged -= HandlePointsChanged;
                boundPoints = pts;
                if (boundPoints != null) boundPoints.OnPointsChanged += HandlePointsChanged;
            }
        }

        private void UnbindGameSystems()
        {
            if (boundDictionary != null)
            {
                boundDictionary.OnDictionaryChanged -= HandleDictionaryChanged;
                boundDictionary = null;
            }
            if (boundPoints != null)
            {
                boundPoints.OnPointsChanged -= HandlePointsChanged;
                boundPoints = null;
            }
        }

        private void HandleDictionaryChanged()
        {
            EvaluateAll();
        }

        private void HandlePointsChanged(int _)
        {
            EvaluateAll();
        }

        private void Persist()
        {
            if (storage == null) storage = new LocalStorage();
            storage.Save(ResolveAccountId(), SaveKey, data);
        }

        [ContextMenu("Reset Achievements Progress")]
        public void ResetAchievementsProgress()
        {
            unlockedIds.Clear();
            claimedRewardIds.Clear();
            completedNpcKeys.Clear();
            data = new AchievementSaveData();
            Persist();
            OnAchievementsUpdated?.Invoke();
            Debug.Log("[AchievementManager] Achievement progress has been reset.");
        }
    }
}
