using System.Collections.Generic;

namespace Utsuru.Achievements
{
    /// <summary>
    /// Master catalog of all achievements available in Utsuru.
    /// </summary>
    public static class AchievementCatalog
    {
        public const string FirstSnapId = "first_snap";
        public const string HajimemashiteId = "hajimemashite";
        public const string WordCollectorId = "word_collector";
        public const string MasterOfKaiwaId = "master_of_kaiwa";
        public const string UtsuruMasterId = "utsuru_master";

        private static readonly List<AchievementDefinition> definitions = new List<AchievementDefinition>
        {
            new AchievementDefinition(
                FirstSnapId,
                "First Snap",
                "Take your first object photo and save your first Polaroid into the Dictionary.",
                20
            ),
            new AchievementDefinition(
                HajimemashiteId,
                "Hajimemashite!",
                "Complete a dialogue with an NPC using the correct Japanese response.",
                40
            ),
            new AchievementDefinition(
                WordCollectorId,
                "Word Collector",
                "Discover and register at least 10 different vocabularies into the Dictionary Album.",
                60
            ),
            new AchievementDefinition(
                MasterOfKaiwaId,
                "Master of Kaiwa",
                "Complete conversations with 3 different NPCs using correct answers.",
                90
            ),
            new AchievementDefinition(
                UtsuruMasterId,
                "Utsuru Master",
                "Collect a total of 100 Learning Points from photography exploration and dialogues.",
                100
            )
        };

        public static IReadOnlyList<AchievementDefinition> All => definitions;

        public static AchievementDefinition GetById(string id)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].id == id) return definitions[i];
            }
            return null;
        }
    }
}
