using System;
using UnityEngine;

namespace Utsuru.DictionaryUI
{
    [Serializable]
    public class IrodoriDatabase
    {
        public string version;
        public string title;
        public string description;
        public int totalChapters;
        public int totalMaterials;
        public HiraganaKatakanaEntry[] hiraganaKatakana;
        public ChapterEntry[] chapters;

        /// <summary>
        /// Muat database modul dari file JSON di Assets/Modules/IrodoriModulesData.json
        /// </summary>
        public static IrodoriDatabase LoadFromProject()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Modules/IrodoriModulesData.json");
            if (!System.IO.File.Exists(path))
            {
                Debug.LogError($"[IrodoriDatabase] File tidak ditemukan di: {path}");
                return null;
            }

            string json = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
            return JsonUtility.FromJson<IrodoriDatabase>(json);
        }
    }

    [Serializable]
    public class HiraganaKatakanaEntry
    {
        public string id;
        public string title;
        public string subtitle;
        public string priority;
        public string description;
        public int spreadIndex;
        public PageRef[] pages;
    }

    [Serializable]
    public class PageRef
    {
        public int pageNumber;
        public string label;
        public string imagePath;
    }

    [Serializable]
    public class ChapterEntry
    {
        public int chapterId;
        public string chapterTitle;
        public string chapterSubtitle;
        public int moduleCount;
        public ModuleEntry[] modules;
    }

    [Serializable]
    public class ModuleEntry
    {
        public string id;
        public string title;
        public string priority;
        public string description;
        public string sourcePage;
        public int pdfPage;
        public PanelImageRef[] panelImages;
        public int granularAssetTotal;
        public string[] granularAssetSamples;
        public ActivityEntry[] activities;
    }

    [Serializable]
    public class PanelImageRef
    {
        public string panelName;
        public string fileName;
        public string assetPath;
        public int pdfPage;
        public string sourceLabel;
    }

    [Serializable]
    public class ActivityEntry
    {
        public string activityId;
        public int lesson;
        public int actNum;
        public string japanesePhrase;
        public string indonesianTitle;
        public string explanation;
        public string keywords;
    }
}
