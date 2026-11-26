using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Utsuru.Achievements.Editor
{
    /// <summary>
    /// Ensures all textures inside Assets/Achievements are imported as 2D UI Sprites
    /// with proper 9-slice borders, and creates the AchievementsBlur material in Resources.
    /// </summary>
    public static class AchievementAssetImporter
    {
        private const string AchievementsFolder = "Assets/Achievements";
        private const string BlurMaterialPath = "Assets/Resources/AchievementsBlur.mat";
        private const string BlurShaderName = "Hidden/Utsuru/UIBackgroundBlur";

        private static readonly Dictionary<string, Vector4> SliceBorders = new Dictionary<string, Vector4>
        {
            { "Achievements-Panel-Bg.png", new Vector4(56f, 56f, 56f, 56f) },
            { "Achievements-Box.png", new Vector4(48f, 36f, 48f, 36f) },
            { "Collect-Button-Shape.png", new Vector4(26f, 20f, 26f, 20f) },
            { "Achievements-Scrollbar-Track.png", new Vector4(10f, 18f, 10f, 18f) },
            { "Achievements-Scrollbar-Handle.png", new Vector4(10f, 18f, 10f, 18f) }
        };

        [MenuItem("Tools/Utsuru/Configure Achievement Sprites")]
        public static void ConfigureAssets()
        {
            if (!AssetDatabase.IsValidFolder(AchievementsFolder))
            {
                Debug.LogWarning($"[AchievementAssetImporter] Folder not found: {AchievementsFolder}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { AchievementsFolder });
            int modified = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool changed = false;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                string fileName = System.IO.Path.GetFileName(path);
                if (SliceBorders.TryGetValue(fileName, out Vector4 targetBorder))
                {
                    if ((importer.spriteBorder - targetBorder).sqrMagnitude > 0.01f)
                    {
                        importer.spriteBorder = targetBorder;
                        changed = true;
                    }
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    modified++;
                }
            }

            EnsureBlurMaterial();

            if (modified > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[AchievementAssetImporter] Configured {modified} Achievement sprite assets.");
            }
        }

        public static Material EnsureBlurMaterial()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(BlurMaterialPath);
            Shader blurShader = Shader.Find(BlurShaderName);
            if (blurShader == null)
            {
                blurShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/UtsuruUIBackgroundBlur.shader");
            }

            if (existing != null)
            {
                if (blurShader != null && existing.shader != blurShader)
                {
                    existing.shader = blurShader;
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                }
                return existing;
            }

            if (blurShader == null)
            {
                Debug.LogWarning($"[AchievementAssetImporter] Could not find shader '{BlurShaderName}'.");
                return null;
            }

            Material mat = new Material(blurShader)
            {
                name = "AchievementsBlur"
            };
            AssetDatabase.CreateAsset(mat, BlurMaterialPath);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
