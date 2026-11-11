#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using StarterAssets;
using Bozo.ModularCharacters;

public class RestoreCompleteSetup : Editor
{
    private static readonly string[] DeletedMapPropNames = new string[]
    {
        "AE_Flower_Pot_02 (5)",
        "AE_Flower_Pot_05 (5)",
        "AE_Flower_Pot_08 (5)",
        "AE_Cone_02 (11)",
        "AE_Flower_Pot_08 (8)",
        "AE_Flower_Pot_09 (5)",
        "AE_Bicycle_03 (4)",
        "AE_Flower_Pot_06 (10)",
        "AE_Stone_04 (6)",
        "AE_Flower_Pot_06 (30)",
        "AE_Flower_Pot_01 (4)",
        "AE_Flower_Pot_06 (29)",
        "AE_Bicycle_02 (1)"
    };

    private struct MovedMapPropData
    {
        public string name;
        public Vector3 localPosition;

        public MovedMapPropData(string name, Vector3 localPosition)
        {
            this.name = name;
            this.localPosition = localPosition;
        }
    }

    private static readonly MovedMapPropData[] MovedMapProps = new MovedMapPropData[]
    {
        new MovedMapPropData("AE_Stone_02 (4)",      new Vector3(-35.44938f, -9.237442f, 60.42f)),
        new MovedMapPropData("AE_Flower_Pot_04 (4)", new Vector3(-35.72838f, -9.168442f, 60.145f)),
        new MovedMapPropData("AE_Flower_Pot_04 (5)", new Vector3( -7.969f,   -9.237442f, 35.132f)),
        new MovedMapPropData("AE_Flower_Pot_06 (31)",new Vector3( -8.78f,    -9.223442f, 50.114693f)),
        new MovedMapPropData("AE_Flower_Pot_01 (7)", new Vector3( -8.75f,    -9.237442f, 49.47f)),
    };

    private static readonly string[] CanonicalFootstepClipPaths = new string[]
    {
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_01.wav",
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_02.wav",
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_03.wav",
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_04.wav",
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_05.wav",
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_06.wav",
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_07.wav",
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_08.wav",
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_09.wav",
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_10.wav",
    };

    private const string CanonicalLandingClipPath =
        "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Land.wav";

    [InitializeOnLoadMethod]
    private static void SyncCanonicalSpawnPointsOnLoad()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += SyncSceneInEditMode;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += SyncSceneInEditMode;
        }
    }

    private static void SyncSceneInEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded || !scene.name.Contains("Day_Showcase")) return;

        bool changed = false;
        changed |= SyncNPCSpawnerData();
        changed |= RepairDayNightSetup(scene, createIfMissing: false, forceDuration: false);
        changed |= RepairFootstepsSetup(scene);
        changed |= ApplyUserMapOptimizations(scene);

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    [MenuItem("Tools/Utsuru/Pulihkan DayNight & Modif Map (Post-Merge)")]
    public static void PulihkanDayNightDanMap()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded) return;

        bool changed = false;
        changed |= RepairDayNightSetup(scene, createIfMissing: true, forceDuration: true);
        changed |= RepairFootstepsSetup(scene);
        changed |= ApplyUserMapOptimizations(scene);

        if (SyncNPCSpawnerData())
        {
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log("<color=green>[Utsuru] DayNightManager, PlayerFootsteps, Prop Map, dan SpawnPoints NPC berhasil dipulihkan!</color>");
    }

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
        else if (diagPanelObj.transform.Find("ContentGroup") == null)
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
        if (diagPanelObj == null || diagPanelObj.transform.Find("ContentGroup") == null)
        {
            SetPrivateField(diagMgr, "nameText", nameText);
            SetPrivateField(diagMgr, "bodyText", bodyText);
            SetPrivateField(diagMgr, "choicesContainer", choicesContainer);
        }
        SetPrivateField(diagMgr, "choiceButtonPrefab", choiceBtnPrefab);
        SetPrivateField(diagMgr, "audioSource", audioSrc);
        SetPrivateField(diagMgr, "playerController", player);

        // 5. Setup DayNightManager & Light_Night (pakai nama field asli DayNightManager & cegah duplikat Light_Night)
        RepairDayNightSetup(spawnerSceneFallback(canvas.gameObject.scene, transPanelObj), createIfMissing: true, forceDuration: true);

        // 6. Setup NPCSpawner & Kunci Posisi + Arah Hadap Canonical SpawnPoints (Point_1..Point_11)
        NPCSpawner spawner = Object.FindAnyObjectByType<NPCSpawner>();
        if (spawner == null)
        {
            GameObject spObj = new GameObject("NPCSpawner");
            spawner = spObj.AddComponent<NPCSpawner>();
        }

        SetPrivateField(spawner, "npcPrefab", npcPrefab);
        SetPrivateField(spawner, "numberOfNpcs", 7);

        // Load 11 Character Presets
        string[] presetNames = new string[] { "Ben", "Chloe", "Dave", "Galvon", "Javi", "Jennifer", "John", "Mary", "Sally", "Tristian", "Zulle" };
        List<DataObject> presets = new List<DataObject>();
        foreach (var pName in presetNames)
        {
            DataObject dObj = AssetDatabase.LoadAssetAtPath<DataObject>($"Assets/BoZo_StylizedModularCharacters/CustomCharacters/Resources/Base/{pName}.asset");
            if (dObj != null) presets.Add(dObj);
        }
        SetPrivateField(spawner, "characterPresets", presets.ToArray());

        // Paksa seluruh Point_1..Point_11 memakai posisi & rotasi kanonis scene saat ini
        NPCSpawner.ApplyCanonicalSpawnPoints(spawner);

        InjectNPCResponses.UpdateAllDialogue();

        // 8. Player Footsteps & Modifikasi Map
        RepairFootstepsSetup(spawner.gameObject.scene);
        ApplyUserMapOptimizations(spawner.gameObject.scene);

        // Tandai scene dirty dan simpan
        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(spawner.gameObject);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        EditorSceneManager.SaveScene(spawner.gameObject.scene);

        Debug.Log("<color=green>[Utsuru] RESTORE SELESAI 100%! Seluruh sistem, Hierarchy, Dialogue, DayNight, dan NPCSpawner telah kembali utuh dan siap dimainkan!</color>");
        EditorUtility.DisplayDialog("Restore Berhasil!", "Seluruh Hierarchy game kamu berhasil dikembalikan utuh dengan kotak pilihan lebar seperti semula!", "Mantap!");
    }

    private static Scene spawnerSceneFallback(Scene preferred, GameObject fallbackObj)
    {
        return preferred.isLoaded ? preferred : (fallbackObj != null ? fallbackObj.scene : EditorSceneManager.GetActiveScene());
    }

    private static bool RepairDayNightSetup(Scene scene, bool createIfMissing, bool forceDuration)
    {
        if (!scene.isLoaded) return false;
        bool changed = false;

        DayNightManager dayNightMgr = Object.FindAnyObjectByType<DayNightManager>();
        if (dayNightMgr == null)
        {
            if (!createIfMissing) return false;
            GameObject dnmObj = new GameObject("DayNightManager");
            dayNightMgr = dnmObj.AddComponent<DayNightManager>();
            changed = true;
        }

        // Cari Light_Day atau Light (termasuk jika nonaktif)
        GameObject lightDay = FindFirstSceneObjectByName(scene, "Light_Day") ?? FindFirstSceneObjectByName(scene, "Light");

        // Cari semua Light_Night (termasuk nonaktif) dan hapus duplikat jika ada lebih dari 1
        List<GameObject> lightNights = FindAllSceneObjectsByName(scene, "Light_Night");
        GameObject currentAssignedNight = GetPrivateField<GameObject>(dayNightMgr, "lightNightObject");
        GameObject keptLightNight = null;

        if (currentAssignedNight != null && lightNights.Contains(currentAssignedNight))
        {
            keptLightNight = currentAssignedNight;
        }
        else if (lightNights.Count > 0)
        {
            keptLightNight = lightNights[0];
        }
        else
        {
            GameObject nightLightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Japanese_Street/Scenes/Day_Showcase/Night_Lighting_Prefab.prefab");
            if (nightLightPrefab != null)
            {
                keptLightNight = (GameObject)PrefabUtility.InstantiatePrefab(nightLightPrefab, scene);
                keptLightNight.name = "Light_Night";
                keptLightNight.SetActive(false);
                changed = true;
            }
        }

        for (int i = 0; i < lightNights.Count; i++)
        {
            if (lightNights[i] != null && lightNights[i] != keptLightNight)
            {
                Undo.DestroyObjectImmediate(lightNights[i]);
                changed = true;
            }
        }

        // Cari TransitionPanel -> CutsceneTransitionUI
        CutsceneTransitionUI cutsceneUI = null;
        GameObject transPanel = FindFirstSceneObjectByName(scene, "TransitionPanel");
        if (transPanel != null)
        {
            cutsceneUI = transPanel.GetComponent<CutsceneTransitionUI>();
        }

        // Cari Player & StarterAssetsInputs
        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        StarterAssetsInputs playerInputs = player != null ? player.GetComponent<StarterAssetsInputs>() : Object.FindAnyObjectByType<StarterAssetsInputs>();

        // Load Skybox Day & Night asli
        Material daySkybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Japanese_Street/Materials/Sky_Day.mat");
        Material nightSkybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Japanese_Street/Materials/Sky_Night.mat");

        bool hadBrokenRefs =
            GetPrivateField<GameObject>(dayNightMgr, "lightDayObject") == null ||
            GetPrivateField<GameObject>(dayNightMgr, "lightNightObject") == null ||
            GetPrivateField<Material>(dayNightMgr, "daySkybox") == null ||
            GetPrivateField<Material>(dayNightMgr, "nightSkybox") == null ||
            GetPrivateField<FirstPersonController>(dayNightMgr, "playerController") == null ||
            GetPrivateField<StarterAssetsInputs>(dayNightMgr, "playerInputs") == null;

        Undo.RecordObject(dayNightMgr, "Repair DayNightManager");

        if (forceDuration || hadBrokenRefs)
        {
            float currentDur = GetPrivateFieldFloat(dayNightMgr, "durationInMinutes");
            if (Mathf.Abs(currentDur - 12f) > 0.01f)
            {
                SetPrivateField(dayNightMgr, "durationInMinutes", 12f);
                changed = true;
            }
        }

        if (cutsceneUI != null) changed |= SetFieldIfChanged(dayNightMgr, "cutsceneUI", cutsceneUI);
        if (player != null) changed |= SetFieldIfChanged(dayNightMgr, "playerController", player);
        if (playerInputs != null) changed |= SetFieldIfChanged(dayNightMgr, "playerInputs", playerInputs);
        if (lightDay != null) changed |= SetFieldIfChanged(dayNightMgr, "lightDayObject", lightDay);
        if (keptLightNight != null) changed |= SetFieldIfChanged(dayNightMgr, "lightNightObject", keptLightNight);
        if (daySkybox != null) changed |= SetFieldIfChanged(dayNightMgr, "daySkybox", daySkybox);
        if (nightSkybox != null) changed |= SetFieldIfChanged(dayNightMgr, "nightSkybox", nightSkybox);

        bool isNight = dayNightMgr.currentState == TimeOfDayState.Night;
        if (lightDay != null && lightDay.activeSelf == isNight)
        {
            lightDay.SetActive(!isNight);
            changed = true;
        }
        if (keptLightNight != null && keptLightNight.activeSelf != isNight)
        {
            keptLightNight.SetActive(isNight);
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(dayNightMgr);
        }

        return changed;
    }

    private static bool ApplyUserMapOptimizations(Scene scene)
    {
        if (!scene.isLoaded) return false;
        GameObject propsRoot = FindFirstSceneObjectByName(scene, "Props");
        if (propsRoot == null) return false;

        bool changed = false;
        Transform propsTr = propsRoot.transform;

        for (int i = 0; i < DeletedMapPropNames.Length; i++)
        {
            Transform target = propsTr.Find(DeletedMapPropNames[i]);
            if (target != null)
            {
                Undo.DestroyObjectImmediate(target.gameObject);
                changed = true;
            }
        }

        for (int i = 0; i < MovedMapProps.Length; i++)
        {
            MovedMapPropData data = MovedMapProps[i];
            Transform target = propsTr.Find(data.name);
            if (target != null && (target.localPosition - data.localPosition).sqrMagnitude > 1e-6f)
            {
                Undo.RecordObject(target, "Restore Map Prop Position");
                target.localPosition = data.localPosition;
                EditorUtility.SetDirty(target);
                changed = true;
            }
        }

        return changed;
    }

    private static bool RepairFootstepsSetup(Scene scene)
    {
        if (!scene.isLoaded) return false;

        bool changed = false;

        DialogueManager diagMgr = Object.FindAnyObjectByType<DialogueManager>();
        if (diagMgr != null)
        {
            AudioSource diagAudio = diagMgr.GetComponent<AudioSource>();
            if (diagAudio != null && diagAudio.playOnAwake)
            {
                Undo.RecordObject(diagAudio, "Disable DialogueManager PlayOnAwake");
                diagAudio.playOnAwake = false;
                EditorUtility.SetDirty(diagAudio);
                changed = true;
            }
        }

        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        if (player == null) return changed;

        PlayerFootsteps footsteps = player.GetComponent<PlayerFootsteps>();
        if (footsteps == null)
        {
            footsteps = Undo.AddComponent<PlayerFootsteps>(player.gameObject);
            changed = true;
        }

        AudioClip[] canonicalClips = new AudioClip[CanonicalFootstepClipPaths.Length];
        for (int i = 0; i < CanonicalFootstepClipPaths.Length; i++)
        {
            canonicalClips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(CanonicalFootstepClipPaths[i]);
        }
        AudioClip canonicalLanding = AssetDatabase.LoadAssetAtPath<AudioClip>(CanonicalLandingClipPath);

        SerializedObject so = new SerializedObject(footsteps);
        SerializedProperty clipsProp = so.FindProperty("footstepClips");
        SerializedProperty landingProp = so.FindProperty("landingClip");
        SerializedProperty walkProp = so.FindProperty("walkStepDistance");
        SerializedProperty sprintProp = so.FindProperty("sprintStepDistance");
        SerializedProperty volumeProp = so.FindProperty("footstepVolume");

        // Hanya isi jika array kosong atau masih ada elemen null agar klip custom milik user tidak tertimpa
        bool clipsNeedRepair = clipsProp != null && clipsProp.arraySize == 0;
        if (!clipsNeedRepair && clipsProp != null)
        {
            for (int i = 0; i < clipsProp.arraySize; i++)
            {
                if (clipsProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    clipsNeedRepair = true;
                    break;
                }
            }
        }

        if (clipsNeedRepair && clipsProp != null)
        {
            Undo.RecordObject(footsteps, "Restore PlayerFootsteps Audio");
            clipsProp.arraySize = canonicalClips.Length;
            for (int i = 0; i < canonicalClips.Length; i++)
            {
                clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = canonicalClips[i];
            }
            changed = true;
        }

        if (canonicalLanding != null && landingProp != null && landingProp.objectReferenceValue == null)
        {
            Undo.RecordObject(footsteps, "Restore PlayerFootsteps Audio");
            landingProp.objectReferenceValue = canonicalLanding;
            changed = true;
        }

        if (walkProp != null && walkProp.floatValue <= 0.01f)
        {
            Undo.RecordObject(footsteps, "Restore PlayerFootsteps Audio");
            walkProp.floatValue = 1.8f;
            changed = true;
        }

        if (sprintProp != null && sprintProp.floatValue <= 0.01f)
        {
            Undo.RecordObject(footsteps, "Restore PlayerFootsteps Audio");
            sprintProp.floatValue = 2.3f;
            changed = true;
        }

        if (volumeProp != null && volumeProp.floatValue <= 0.01f)
        {
            Undo.RecordObject(footsteps, "Restore PlayerFootsteps Audio");
            volumeProp.floatValue = 0.4f;
            changed = true;
        }

        if (changed)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(footsteps);
        }

        return changed;
    }

    private static bool SyncNPCSpawnerData()
    {
        NPCSpawner spawner = Object.FindAnyObjectByType<NPCSpawner>();
        if (spawner == null) return false;

        bool changed = false;
        if (NPCSpawner.ApplyCanonicalSpawnPoints(spawner))
        {
            EditorUtility.SetDirty(spawner);
            changed = true;
        }
        if (HasMissingDialogueQuestions(spawner))
        {
            InjectNPCResponses.UpdateAllDialogue(saveScene: false);
            changed = true;
        }
        return changed;
    }

    private static bool HasMissingDialogueQuestions(NPCSpawner spawner)
    {
        if (spawner == null || spawner.spawnLocations == null || spawner.spawnLocations.Length < 11)
            return true;

        for (int i = 0; i < spawner.spawnLocations.Length; i++)
        {
            ContextualSpawnPoint loc = spawner.spawnLocations[i];
            if (loc == null || loc.locationQuestions == null || loc.locationQuestions.Length == 0)
                return true;

            for (int q = 0; q < loc.locationQuestions.Length; q++)
            {
                NPCData data = loc.locationQuestions[q];
                if (data == null || string.IsNullOrEmpty(data.questionJapanese))
                    return true;
            }
        }

        return false;
    }

    private static GameObject FindFirstSceneObjectByName(Scene scene, string targetName)
    {
        List<GameObject> matches = FindAllSceneObjectsByName(scene, targetName);
        return matches.Count > 0 ? matches[0] : null;
    }

    private static List<GameObject> FindAllSceneObjectsByName(Scene scene, string targetName)
    {
        List<GameObject> results = new List<GameObject>();
        if (!scene.isLoaded) return results;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            CollectNamedObjectsRecursive(roots[i].transform, targetName, results);
        }
        return results;
    }

    private static void CollectNamedObjectsRecursive(Transform tr, string targetName, List<GameObject> results)
    {
        if (tr.name == targetName)
        {
            results.Add(tr.gameObject);
        }
        for (int i = 0; i < tr.childCount; i++)
        {
            CollectNamedObjectsRecursive(tr.GetChild(i), targetName, results);
        }
    }

    private static TMP_Text FindChildTMP(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static T GetPrivateField<T>(object target, string fieldName) where T : class
    {
        if (target == null) return null;
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        return field != null ? field.GetValue(target) as T : null;
    }

    private static float GetPrivateFieldFloat(object target, string fieldName)
    {
        if (target == null) return 0f;
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        return field != null ? (float)field.GetValue(target) : 0f;
    }

    private static bool SetFieldIfChanged(object target, string fieldName, object value)
    {
        if (target == null) return false;
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field == null) return false;

        object current = field.GetValue(target);
        if (Equals(current, value)) return false;

        field.SetValue(target, value);
        return true;
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
