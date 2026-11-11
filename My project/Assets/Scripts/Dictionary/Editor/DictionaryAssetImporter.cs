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
    }
}

