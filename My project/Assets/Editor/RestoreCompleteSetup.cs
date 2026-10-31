#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets;
using Bozo.ModularCharacters;

public class RestoreCompleteSetup : Editor
{
    [MenuItem("Tools/Utsuru/Perbesar Kotak Pilihan (Sesuai Ukuran Asli)")]
    public static void FixChoicesSize()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        GameObject diagPanel = null;
        if (canvas != null)
        {
            Transform t = canvas.transform.Find("DialogueBoxPanel");
            if (t != null) diagPanel = t.gameObject;
        }
        if (diagPanel == null) diagPanel = GameObject.Find("DialogueBoxPanel");

        if (diagPanel != null)
        {
            Transform ccTr = diagPanel.transform.Find("ChoicesContainer");
            if (ccTr != null)
            {
                RectTransform crt = ccTr.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0, 0);
                crt.anchorMax = new Vector2(1, 0);
                crt.pivot = new Vector2(0.5f, 0);
                crt.anchoredPosition = new Vector2(0, 20);
                crt.sizeDelta = new Vector2(-60, 65);

                HorizontalLayoutGroup hlg = ccTr.GetComponent<HorizontalLayoutGroup>();
                if (hlg == null) hlg = ccTr.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 15;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;

                EditorUtility.SetDirty(diagPanel);
                EditorSceneManager.MarkSceneDirty(diagPanel.scene);
                EditorSceneManager.SaveScene(diagPanel.scene);
                Debug.Log("<color=green>[Utsuru] ChoicesContainer berhasil diperbesar persis seperti Gambar 1!</color>");
                EditorUtility.DisplayDialog("Sukses!", "Ukuran kotak pilihan berhasil diperbesar lebar dan pas seperti semula!", "OK");
                return;
            }
        }
        Debug.LogError("DialogueBoxPanel / ChoicesContainer tidak ditemukan!");
    }

    [MenuItem("Tools/Utsuru/RESTORE SEMUA SISTEM GAME (1-KLIK KEMBALI NORMAL)")]
    public static void RestoreAll()
    {
        // 1. Dapatkan referensi Font & Sprite & Audio
        TMP_FontAsset japaneseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/YUGOTHB SDF.asset");
        if (japaneseFont == null)
            japaneseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        Sprite dialogBoxSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/dialogbox.png");
        GameObject choiceBtnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ChoiceButtonPrefab.prefab");
        GameObject npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPC_Warga.prefab");
        GameObject nightLightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Japanese_Street/Scenes/Day_Showcase/Night_Lighting_Prefab.prefab");
        AudioClip blipVoice = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sans-talk-sound.mp3");

        // Cari Player
        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();

        // 2. Setup Canvas & UI
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Skor Text
        TMP_Text scoreText = FindChildTMP(canvas.transform, "ScoreText");
        if (scoreText == null)
        {
            GameObject stObj = new GameObject("ScoreText");
            stObj.transform.SetParent(canvas.transform, false);
            scoreText = stObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = "Skor: 0";
            scoreText.fontSize = 36;
            scoreText.font = japaneseFont;
            scoreText.alignment = TextAlignmentOptions.TopRight;
            RectTransform rt = stObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-40, -40);
            rt.sizeDelta = new Vector2(300, 60);
        }

        // Interaction Prompt [E] Talk
        TMP_Text promptText = FindChildTMP(canvas.transform, "InteractionPrompt");
        if (promptText == null)
        {
            GameObject prObj = new GameObject("InteractionPrompt");
            prObj.transform.SetParent(canvas.transform, false);
            promptText = prObj.AddComponent<TextMeshProUGUI>();
            promptText.text = "[E] Bicara";
            promptText.fontSize = 34;
            promptText.font = japaneseFont;
            promptText.alignment = TextAlignmentOptions.Center;
            RectTransform rt = prObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -60);
            rt.sizeDelta = new Vector2(300, 60);
            prObj.SetActive(false);
        }

        // Cutscene Transition Panel
        Transform transPanelTr = canvas.transform.Find("TransitionPanel");
        GameObject transPanelObj = transPanelTr != null ? transPanelTr.gameObject : null;
        if (transPanelObj == null)
        {
            transPanelObj = new GameObject("TransitionPanel");
            transPanelObj.transform.SetParent(canvas.transform, false);
            Image bg = transPanelObj.AddComponent<Image>();
            bg.color = Color.black;
            CanvasGroup cg = transPanelObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            transPanelObj.AddComponent<CutsceneTransitionUI>();

            RectTransform rt = transPanelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            GameObject ttObj = new GameObject("TransitionText");
            ttObj.transform.SetParent(transPanelObj.transform, false);
            TMP_Text tt = ttObj.AddComponent<TextMeshProUGUI>();
            tt.text = "Beberapa jam kemudian...";
            tt.fontSize = 42;
            tt.font = japaneseFont;
            tt.alignment = TextAlignmentOptions.Center;
            RectTransform trt = ttObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(800, 100);

            CutsceneTransitionUI cutscene = transPanelObj.GetComponent<CutsceneTransitionUI>();
            SetPrivateField(cutscene, "canvasGroup", cg);
            SetPrivateField(cutscene, "transitionText", tt);

            transPanelObj.SetActive(false);
        }

        // DialogueBoxPanel
        Transform diagPanelTr = canvas.transform.Find("DialogueBoxPanel");
        GameObject diagPanelObj = diagPanelTr != null ? diagPanelTr.gameObject : null;
        TMP_Text nameText = null;
        TMP_Text bodyText = null;
        Transform choicesContainer = null;

        if (diagPanelObj == null)
        {
            diagPanelObj = new GameObject("DialogueBoxPanel");
            diagPanelObj.transform.SetParent(canvas.transform, false);
            Image img = diagPanelObj.AddComponent<Image>();
            if (dialogBoxSprite != null) img.sprite = dialogBoxSprite;
            img.color = Color.white;

            RectTransform rt = diagPanelObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 40);
            rt.sizeDelta = new Vector2(1050, 240);

            // NameText
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(diagPanelObj.transform, false);
            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Warga";
            nameText.fontSize = 28;
            nameText.fontStyle = FontStyles.Bold;
            nameText.font = japaneseFont;
            RectTransform nrt = nameObj.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 1);
            nrt.anchorMax = new Vector2(0, 1);
            nrt.pivot = new Vector2(0, 1);
            nrt.anchoredPosition = new Vector2(30, -15);
            nrt.sizeDelta = new Vector2(300, 40);

            // BodyText
            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(diagPanelObj.transform, false);
            bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyText.text = "Teks pertanyaan dialog...";
            bodyText.fontSize = 22;
            bodyText.font = japaneseFont;
            RectTransform brt = bodyObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0, 1);
            brt.anchoredPosition = new Vector2(30, -55);
            brt.sizeDelta = new Vector2(-60, 100);

            // ChoicesContainer (LEBAR DAN PAS SEPERTI GAMBAR 1)
            GameObject ccObj = new GameObject("ChoicesContainer");
            ccObj.transform.SetParent(diagPanelObj.transform, false);
            HorizontalLayoutGroup hlg = ccObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            RectTransform crt = ccObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0);
            crt.anchorMax = new Vector2(1, 0);
            crt.pivot = new Vector2(0.5f, 0);
            crt.anchoredPosition = new Vector2(0, 20);
            crt.sizeDelta = new Vector2(-60, 65);
            choicesContainer = ccObj.transform;

            diagPanelObj.SetActive(false);
        }
        else
        {
            nameText = diagPanelObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
            bodyText = diagPanelObj.transform.Find("BodyText")?.GetComponent<TMP_Text>();
            choicesContainer = diagPanelObj.transform.Find("ChoicesContainer");

            if (choicesContainer != null)
            {
                RectTransform crt = choicesContainer.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0, 0);
                crt.anchorMax = new Vector2(1, 0);
                crt.pivot = new Vector2(0.5f, 0);
                crt.anchoredPosition = new Vector2(0, 20);
                crt.sizeDelta = new Vector2(-60, 65);

                HorizontalLayoutGroup hlg = choicesContainer.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    hlg.spacing = 15;
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.childControlWidth = true;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = true;
                    hlg.childForceExpandHeight = true;
                }
            }
        }

        // 3. Setup ScoreManager
        ScoreManager scoreMgr = Object.FindAnyObjectByType<ScoreManager>();
        if (scoreMgr == null)
        {
            GameObject smObj = new GameObject("ScoreManager");
            scoreMgr = smObj.AddComponent<ScoreManager>();
        }
        SetPrivateField(scoreMgr, "scoreText", scoreText);

        // 4. Setup DialogueManager
        DialogueManager diagMgr = Object.FindAnyObjectByType<DialogueManager>();
        if (diagMgr == null)
        {
            GameObject dmObj = new GameObject("DialogueManager");
            diagMgr = dmObj.AddComponent<DialogueManager>();
        }
        AudioSource audioSrc = diagMgr.GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = diagMgr.gameObject.AddComponent<AudioSource>();

        SetPrivateField(diagMgr, "dialoguePanel", diagPanelObj);
        SetPrivateField(diagMgr, "interactionPrompt", promptText != null ? promptText.gameObject : null);
        SetPrivateField(diagMgr, "nameText", nameText);
        SetPrivateField(diagMgr, "bodyText", bodyText);
        SetPrivateField(diagMgr, "choicesContainer", choicesContainer);
        SetPrivateField(diagMgr, "choiceButtonPrefab", choiceBtnPrefab);
        SetPrivateField(diagMgr, "audioSource", audioSrc);
        SetPrivateField(diagMgr, "playerController", player);

        // 5. Setup DayNightManager & Light_Night
        GameObject lightDay = GameObject.Find("Light");
        GameObject lightNight = GameObject.Find("Light_Night");
        if (lightNight == null && nightLightPrefab != null)
        {
            lightNight = (GameObject)PrefabUtility.InstantiatePrefab(nightLightPrefab);
            lightNight.name = "Light_Night";
            lightNight.SetActive(false);
        }

        DayNightManager dayNightMgr = Object.FindAnyObjectByType<DayNightManager>();
        if (dayNightMgr == null)
        {
            GameObject dnmObj = new GameObject("DayNightManager");
            dayNightMgr = dnmObj.AddComponent<DayNightManager>();
        }
        SetPrivateField(dayNightMgr, "lightDay", lightDay);
        SetPrivateField(dayNightMgr, "lightNight", lightNight);
        if (transPanelObj != null)
        {
            SetPrivateField(dayNightMgr, "cutsceneUI", transPanelObj.GetComponent<CutsceneTransitionUI>());
        }

        // 6. Setup SpawnPoints
        GameObject spawnPointsParent = GameObject.Find("SpawnPoints");
        if (spawnPointsParent == null)
        {
            spawnPointsParent = new GameObject("SpawnPoints");
        }

        Vector3[] pointPositions = new Vector3[]
        {
            new Vector3(-1.5f, 0.05f, 5.0f),     // 1. Vending Machine 1
            new Vector3(-2.5f, 0.05f, 12.0f),    // 2. Toko
            new Vector3(-9.15f, 0.05f, -5.29f),  // 3. Vending Machine Kopi (Point_3 asli)
            new Vector3(-5.701f, 0.05f, 29.776f),// 4. Toko Awning (Point_4 asli)
            new Vector3(4.5f, 0.05f, 15.0f),     // 5. Parkir Mobil
            new Vector3(1.0f, 0.05f, 0.0f),      // 6. Zebracross
            new Vector3(3.5f, 0.05f, -8.0f),     // 7. Vending Machine 2
            new Vector3(-3.0f, 0.05f, 20.0f),    // 8. Toko Tutup
            new Vector3(-12.88f, 0.05f, -15.02f),// 9. Tanaman Depan Rumah (Point_9 asli)
            new Vector3(-7.5f, 0.05f, -12.0f),   // 10. Tong Sampah Daur Ulang
            new Vector3(-5.0f, 0.05f, 35.0f)     // 11. Lampu Merah
        };

        Transform[] spawnPoints = new Transform[11];
        for (int i = 0; i < 11; i++)
        {
            string ptName = $"Point_{i + 1}";
            Transform pt = spawnPointsParent.transform.Find(ptName);
            if (pt == null)
            {
                GameObject ptObj = new GameObject(ptName);
                ptObj.transform.SetParent(spawnPointsParent.transform, false);
                pt = ptObj.transform;
            }
            pt.position = pointPositions[i];
            spawnPoints[i] = pt;
        }

        // 7. Setup NPCSpawner
        NPCSpawner spawner = Object.FindAnyObjectByType<NPCSpawner>();
        if (spawner == null)
        {
            GameObject spObj = new GameObject("NPCSpawner");
            spawner = spObj.AddComponent<NPCSpawner>();
        }

        SetPrivateField(spawner, "npcPrefab", npcPrefab);
        SetPrivateField(spawner, "numberOfNpcs", 6);

        // Load 11 Character Presets
        string[] presetNames = new string[] { "Ben", "Chloe", "Dave", "Galvon", "Javi", "Jennifer", "John", "Mary", "Sally", "Tristian", "Zulle" };
        List<DataObject> presets = new List<DataObject>();
        foreach (var pName in presetNames)
        {
            DataObject dObj = AssetDatabase.LoadAssetAtPath<DataObject>($"Assets/BoZo_StylizedModularCharacters/CustomCharacters/Resources/Base/{pName}.asset");
            if (dObj != null) presets.Add(dObj);
        }
        SetPrivateField(spawner, "characterPresets", presets.ToArray());

        // Setup 11 Contextual Spawn Points
        spawner.spawnLocations = new ContextualSpawnPoint[11];
        for (int i = 0; i < 11; i++)
        {
            spawner.spawnLocations[i] = new ContextualSpawnPoint();
            spawner.spawnLocations[i].pointTransform = spawnPoints[i];
        }

        InjectNPCResponses.UpdateAllDialogue();

        // 8. Player Footsteps
        if (player != null && player.GetComponent<PlayerFootsteps>() == null)
        {
            player.gameObject.AddComponent<PlayerFootsteps>();
        }

        // Tandai scene dirty dan simpan
        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(spawner.gameObject);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        EditorSceneManager.SaveScene(spawner.gameObject.scene);

        Debug.Log("<color=green>[Utsuru] RESTORE SELESAI 100%! Seluruh sistem, Hierarchy, Dialogue, DayNight, dan NPCSpawner telah kembali utuh dan siap dimainkan!</color>");
        EditorUtility.DisplayDialog("Restore Berhasil!", "Seluruh Hierarchy game kamu berhasil dikembalikan utuh dengan kotak pilihan lebar seperti semula!", "Mantap!");
    }

    private static TMP_Text FindChildTMP(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
#endif
