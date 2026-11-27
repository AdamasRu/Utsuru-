using System.IO;
using UnityEditor;
using UnityEngine;

namespace Utsuru.DictionaryUI.Editor
{
    public static class DictionaryAssetImporter
    {
        [MenuItem("Tools/Utsuru/Configure Dictionary Sprites")]
        public static void ConfigureSprites()
        {
            string rootPath = "Assets/UI/Dictionary";
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { rootPath });
            int modifiedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
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
                    if (path.EndsWith("Book_CoverCounterPill.png", System.StringComparison.OrdinalIgnoreCase))
                    {
                        Vector4 targetBorder = new Vector4(44f, 20f, 44f, 20f);
                        if (importer.spriteBorder != targetBorder)
                        {
                            importer.spriteBorder = targetBorder;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        modifiedCount++;
                        Debug.Log($"[DictionaryAssetImporter] Converted to Sprite: {path}");
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[DictionaryAssetImporter] Finished configuring {modifiedCount} dictionary sprites.");
        }

        [MenuItem("Tools/Utsuru/Configure Module Sprites")]
        public static int ConfigureModuleSprites()
        {
            string[] searchFolders = new[]
            {
                "Assets/Modules/Utsuru_Irodori_Aset_Visual_Lengkap/vector_page_reproductions",
                "Assets/Modules/Utsuru_Irodori_Panel_Ilustrasi_39_Materi",
                "Assets/Modules/Utsuru_Irodori_Aset_Visual_Lengkap/bitmap_assets_display"
            };

            AssetDatabase.StartAssetEditing();
            int modifiedCount = 0;
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
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
                        if (importer.maxTextureSize < 2048)
                        {
                            importer.maxTextureSize = 2048;
                            changed = true;
                        }

                        if (changed)
                        {
                            importer.SaveAndReimport();
                            modifiedCount++;
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[DictionaryAssetImporter] Finished configuring {modifiedCount} module sprites.");
            return modifiedCount;
        }
    }
}

