using System;
using System.Collections.Generic;
using UnityEngine;
using Utsuru.Capture;

namespace Utsuru.Localization
{
    public enum GameLanguage
    {
        English = 0,
        Indonesian = 1
    }

    [Serializable]
    public class LanguagePreferenceData
    {
        public string language = "English";
    }

    [Serializable]
    public class VocabItemTranslation
    {
        public string objectId;
        public string objectName;
        public string translation;
        public string exampleSentence;
    }

    [Serializable]
    public class VocabTranslationCatalog
    {
        public List<VocabItemTranslation> items = new List<VocabItemTranslation>();
    }

    public class LocalizationManager : MonoBehaviour
    {
        private static LocalizationManager instance;
        public static LocalizationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityEngine.Object.FindFirstObjectByType<LocalizationManager>();
                }
                if (instance != null)
                {
                    instance.EnsureCatalogsLoaded();
                }
                return instance;
            }
            private set => instance = value;
        }

        public static event Action<GameLanguage> OnLanguageChanged;

        [Header("Konfigurasi")]
        [SerializeField] private GameLanguage defaultLanguage = GameLanguage.English;

        private GameLanguage currentLanguage = GameLanguage.English;
        public GameLanguage CurrentLanguage => currentLanguage;
        public bool IsIndonesian => currentLanguage == GameLanguage.Indonesian;

        private readonly Dictionary<string, string> uiEnglish = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> uiIndonesian = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, VocabItemTranslation> vocabIndonesian = new Dictionary<string, VocabItemTranslation>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> dialogueIndonesian = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private IDataStorage storage;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureCatalogsLoaded();
        }

        public void EnsureCatalogsLoaded()
        {
            if (storage == null) storage = new LocalStorage();
            if (uiEnglish.Count == 0)
            {
                LoadCatalogs();
                LoadLanguagePreference();
            }
        }

        private void LoadCatalogs()
        {
            // 1. UI English
            TextAsset uiEnAsset = Resources.Load<TextAsset>("Localization/ui_en");
            if (uiEnAsset != null && !string.IsNullOrEmpty(uiEnAsset.text))
            {
                ParseSimpleJsonDict(uiEnAsset.text, uiEnglish);
            }

            // 2. UI Indonesian
            TextAsset uiIdAsset = Resources.Load<TextAsset>("Localization/ui_id");
            if (uiIdAsset != null && !string.IsNullOrEmpty(uiIdAsset.text))
            {
                ParseSimpleJsonDict(uiIdAsset.text, uiIndonesian);
            }

            // 3. Vocab Indonesian
            TextAsset vocabIdAsset = Resources.Load<TextAsset>("Localization/vocab_id");
            if (vocabIdAsset != null && !string.IsNullOrEmpty(vocabIdAsset.text))
            {
                try
                {
                    var cat = JsonUtility.FromJson<VocabTranslationCatalog>(vocabIdAsset.text);
                    if (cat != null && cat.items != null)
                    {
                        foreach (var item in cat.items)
                        {
                            if (item != null && !string.IsNullOrEmpty(item.objectId))
                            {
                                vocabIndonesian[item.objectId] = item;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LocalizationManager] Gagal parse vocab_id.json: {e.Message}");
                }
            }

            // 4. Dialogue Indonesian
            TextAsset dialIdAsset = Resources.Load<TextAsset>("Localization/dialogue_id");
            if (dialIdAsset != null && !string.IsNullOrEmpty(dialIdAsset.text))
            {
                ParseSimpleJsonDict(dialIdAsset.text, dialogueIndonesian);
            }
        }

        private void ParseSimpleJsonDict(string json, Dictionary<string, string> targetDict)
        {
            try
            {
                targetDict.Clear();
                if (string.IsNullOrEmpty(json)) return;

                string[] lines = json.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (!line.StartsWith("\"")) continue;

                    int firstQuote = line.IndexOf('"');
                    int secondQuote = line.IndexOf('"', firstQuote + 1);
                    if (firstQuote == -1 || secondQuote == -1) continue;

                    string key = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);

                    int colon = line.IndexOf(':', secondQuote + 1);
                    if (colon == -1) continue;

                    int valStart = line.IndexOf('"', colon + 1);
                    if (valStart == -1) continue;

                    int valEnd = line.LastIndexOf('"');
                    if (valEnd <= valStart) continue;

                    string val = line.Substring(valStart + 1, valEnd - valStart - 1)
                        .Replace("\\\"", "\"")
                        .Replace("\\n", "\n")
                        .Replace("\\r", "\r")
                        .Replace("\\t", "\t")
                        .Replace("\\\\", "\\");

                    targetDict[key] = val;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationManager] Gagal parse dictionary JSON: {ex.Message}");
            }
        }

        public void SetLanguage(GameLanguage newLanguage)
        {
            if (currentLanguage == newLanguage) return;

            currentLanguage = newLanguage;
            SaveLanguagePreference();

            Debug.Log($"<color=#6B4A33><b>[LocalizationManager] Bahasa diubah ke: {currentLanguage}</b></color>");
            OnLanguageChanged?.Invoke(currentLanguage);
        }

        public string GetText(string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? "";

            if (currentLanguage == GameLanguage.Indonesian)
            {
                if (uiIndonesian.TryGetValue(key, out string idVal) && !string.IsNullOrEmpty(idVal))
                    return idVal;
            }

            if (uiEnglish.TryGetValue(key, out string enVal) && !string.IsNullOrEmpty(enVal))
                return enVal;

            return fallback ?? key;
        }

        public string GetVocabTranslation(string objectId, string originalTranslation)
        {
            if (currentLanguage == GameLanguage.Indonesian && !string.IsNullOrEmpty(objectId))
            {
                if (vocabIndonesian.TryGetValue(objectId, out var item) && !string.IsNullOrEmpty(item.translation))
                    return item.translation;
            }
            return originalTranslation ?? "";
        }

        public string GetObjectName(string objectId, string originalName)
        {
            if (currentLanguage == GameLanguage.Indonesian && !string.IsNullOrEmpty(objectId))
            {
                if (vocabIndonesian.TryGetValue(objectId, out var item) && !string.IsNullOrEmpty(item.objectName))
                    return item.objectName;
            }
            return originalName ?? "";
        }

        public string GetExampleSentence(string objectId, string originalSentence)
        {
            if (currentLanguage == GameLanguage.Indonesian && !string.IsNullOrEmpty(objectId))
            {
                if (vocabIndonesian.TryGetValue(objectId, out var item) && !string.IsNullOrEmpty(item.exampleSentence))
                    return item.exampleSentence;
            }
            return originalSentence ?? "";
        }

        public string GetDialogueTranslation(string jpText, string originalEn)
        {
            if (currentLanguage == GameLanguage.Indonesian && !string.IsNullOrEmpty(jpText))
            {
                if (dialogueIndonesian.TryGetValue(jpText.Trim(), out string idVal))
                    return idVal;
            }
            return originalEn ?? "";
        }

        public string GetCategoryText(string category)
        {
            if (string.IsNullOrEmpty(category))
                return GetText("CATEGORY_PLACES", "PLACES (場所)");

            switch (category.Trim().ToLowerInvariant())
            {
                case "places":
                case "road":
                    return GetText("CATEGORY_PLACES", "PLACES (場所)");
                case "transport":
                    return GetText("CATEGORY_TRANSPORT", "TRANSPORT (交通)");
                case "sign":
                    return GetText("CATEGORY_SIGN", "SIGNS (看板・標識)");
                case "facility":
                    return GetText("CATEGORY_FACILITY", "FACILITY (施設)");
                case "infrastructure":
                    return GetText("CATEGORY_INFRASTRUCTURE", "INFRASTRUCTURE (設備)");
                case "nature":
                    return GetText("CATEGORY_NATURE", "NATURE (自然)");
                case "residential":
                    return GetText("CATEGORY_RESIDENTIAL", "RESIDENTIAL (住宅)");
                default:
                    string catFmt = GetText("CATEGORY_GENERAL", "{0} (Category)");
                    return string.Format(catFmt, category.ToUpperInvariant());
            }
        }

        private void SaveLanguagePreference()
        {
            // 1. Simpan via IDataStorage (DPPL arsitektur)
            if (storage == null) storage = new LocalStorage();
            storage.Save("default", "LanguagePreference", new LanguagePreferenceData { language = currentLanguage.ToString() });

            // 2. Simpan via PlayerPrefs untuk akses instan
            PlayerPrefs.SetInt("Setting_Language", (int)currentLanguage);
            PlayerPrefs.Save();
        }

        private void LoadLanguagePreference()
        {
            bool loaded = false;

            // 1. Coba load dari IDataStorage (DPPL arsitektur)
            if (storage != null && storage.TryLoad("default", "LanguagePreference", out LanguagePreferenceData data))
            {
                if (Enum.TryParse(data.language, out GameLanguage savedLang))
                {
                    currentLanguage = savedLang;
                    loaded = true;
                }
            }

            // 2. Fallback PlayerPrefs
            if (!loaded)
            {
                int pref = PlayerPrefs.GetInt("Setting_Language", (int)defaultLanguage);
                currentLanguage = (GameLanguage)Mathf.Clamp(pref, 0, 1);
            }
        }
    }
}

