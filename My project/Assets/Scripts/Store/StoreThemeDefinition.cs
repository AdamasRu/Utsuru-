using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utsuru.Store
{
    /// <summary>
    /// Definisi statis sebuah tema visual buku Kamus (t_store_theme pada DPPL).
    /// </summary>
    [Serializable]
    public class StoreThemeDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public int pricePoints;
        public bool isDefaultFree;
        public string bookAssetPath;
        public Color swatchTopColor;
        public Color swatchBottomColor;

        public StoreThemeDefinition(
            string id,
            string displayName,
            string description,
            int pricePoints,
            bool isDefaultFree,
            string bookAssetPath,
            Color swatchTopColor,
            Color swatchBottomColor)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.pricePoints = pricePoints;
            this.isDefaultFree = isDefaultFree;
            this.bookAssetPath = bookAssetPath;
            this.swatchTopColor = swatchTopColor;
            this.swatchBottomColor = swatchBottomColor;
        }
    }

    /// <summary>
    /// Catatan pembelian sebuah tema oleh pemain (t_player_theme pada DPPL).
    /// </summary>
    [Serializable]
    public class ThemePurchaseRecord
    {
        public string themeId;
        public string purchasedAt; // ISO 8601
    }

    /// <summary>
    /// Data persistensi lokal per akun untuk kepemilikan dan tema aktif Kamus.
    /// </summary>
    [Serializable]
    public class StoreThemeSaveData
    {
        public List<string> ownedThemeIds = new List<string>();
        public string activeThemeId = StoreThemeCatalog.DefaultThemeId;
        public List<ThemePurchaseRecord> purchaseRecords = new List<ThemePurchaseRecord>();
    }

    /// <summary>
    /// Katalog master seluruh tema Kamus (Plain default + 5 tema Store).
    /// </summary>
    public static class StoreThemeCatalog
    {
        public const string DefaultThemeId = "plain";
        public const string SakuraThemeId = "sakura";
        public const string BotanicalThemeId = "botanical";
        public const string AutumnThemeId = "autumn";
        public const string MidnightThemeId = "midnight";
        public const string SummerThemeId = "summer";

        private static readonly List<StoreThemeDefinition> allThemes = new List<StoreThemeDefinition>
        {
            new StoreThemeDefinition(
                DefaultThemeId,
                "Plain Theme",
                "Tema buku kamus bawaan yang klasik dan bersih.",
                0,
                true,
                "Assets/UI/Dictionary/Book/Book_Plain.png",
                new Color(0.961f, 0.918f, 0.855f, 1f),
                new Color(0.871f, 0.796f, 0.690f, 1f)
            ),
            new StoreThemeDefinition(
                SakuraThemeId,
                "Sakura Theme",
                "Nuansa merah muda lembut terinspirasi kelopak sakura.",
                300,
                false,
                "Assets/UI/Dictionary/Book/Book_Pink.png",
                new Color(1.000f, 0.871f, 0.890f, 1f),
                new Color(1.000f, 0.718f, 0.773f, 1f)
            ),
            new StoreThemeDefinition(
                BotanicalThemeId,
                "Botanical Theme",
                "Nuansa hijau segar taman botani Jepang.",
                300,
                false,
                "Assets/UI/Dictionary/Book/Book_Green.png",
                new Color(0.871f, 1.000f, 0.871f, 1f),
                new Color(0.635f, 0.961f, 0.706f, 1f)
            ),
            new StoreThemeDefinition(
                AutumnThemeId,
                "Autumn Theme",
                "Nuansa jingga hangat dedaunan musim gugur.",
                300,
                false,
                "Assets/UI/Dictionary/Book/Book_Orange.png",
                new Color(1.000f, 0.890f, 0.749f, 1f),
                new Color(1.000f, 0.737f, 0.478f, 1f)
            ),
            new StoreThemeDefinition(
                MidnightThemeId,
                "Midnight Theme",
                "Nuansa ungu lavender malam yang tenang.",
                300,
                false,
                "Assets/UI/Dictionary/Book/Book_Purple.png",
                new Color(0.820f, 0.812f, 1.000f, 1f),
                new Color(0.647f, 0.639f, 0.980f, 1f)
            ),
            new StoreThemeDefinition(
                SummerThemeId,
                "Summer Theme",
                "Nuansa kuning cerah penuh semangat musim panas.",
                300,
                false,
                "Assets/UI/Dictionary/Book/Book_Yellow.png",
                new Color(1.000f, 0.973f, 0.725f, 1f),
                new Color(1.000f, 0.941f, 0.482f, 1f)
            )
        };

        private static readonly List<StoreThemeDefinition> storeThemes = new List<StoreThemeDefinition>
        {
            allThemes[1], // Sakura
            allThemes[2], // Botanical
            allThemes[3], // Autumn
            allThemes[4], // Midnight
            allThemes[5]  // Summer
        };

        /// <summary>Seluruh tema termasuk tema default Plain (untuk tab Apply di Dictionary).</summary>
        public static IReadOnlyList<StoreThemeDefinition> All => allThemes;

        /// <summary>Daftar 5 tema berbayar yang dijual di Store.</summary>
        public static IReadOnlyList<StoreThemeDefinition> StoreItems => storeThemes;

        public static StoreThemeDefinition GetById(string themeId)
        {
            if (string.IsNullOrEmpty(themeId)) return null;
            for (int i = 0; i < allThemes.Count; i++)
            {
                if (string.Equals(allThemes[i].id, themeId, StringComparison.OrdinalIgnoreCase))
                    return allThemes[i];
            }
            return null;
        }
    }
}
