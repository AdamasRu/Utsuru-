using System.Collections.Generic;
using UnityEngine;
using Bozo.ModularCharacters; // Namespace sistem outfit BoZo

[System.Serializable]
public class ContextualSpawnPoint
{
    [Tooltip("Nama penanda (contoh: Depan Vending Machine, Dekat Sepeda, dsb.)")]
    public string locationName = "Lokasi Baru";

    [Tooltip("Transform titik spawn di jalan/trotoar")]
    public Transform pointTransform;

    [Tooltip("Daftar soal khusus yang relevan dengan lokasi ini")]
    public NPCData[] locationQuestions;
}

public class NPCSpawner : MonoBehaviour
{
    public static NPCSpawner Instance { get; private set; }

    [Header("Prefab NPC")]
    [SerializeField] private GameObject npcPrefab;

    [Header("Titik Spawn & Soal Berdasarkan Lokasi")]
    [SerializeField] public ContextualSpawnPoint[] spawnLocations;

    [Tooltip("Berapa banyak NPC yang ingin dimunculkan bersamaan")]
    [SerializeField] private int numberOfNpcs = 7;

    [Header("Kunci Posisi & Arah Spawn (Anti-Geser Saat Merge)")]
    [Tooltip("Jika aktif, seluruh titik spawn Point_1..Point_11 selalu dikunci ke posisi & arah hadap kanonis scene saat ini di branch mana pun.")]
    [SerializeField] private bool lockCanonicalSpawnPoints = true;

    private struct CanonicalSpawnPointData
    {
        public string pointName;
        public string locationName;
        public Vector3 position;
        public Quaternion rotation;
        public string questionJapanese;

        public CanonicalSpawnPointData(string pointName, string locationName, Vector3 position, Quaternion rotation, string questionJapanese)
        {
            this.pointName = pointName;
            this.locationName = locationName;
            this.position = position;
            this.rotation = rotation;
            this.questionJapanese = questionJapanese;
        }
    }

    private static readonly CanonicalSpawnPointData[] CanonicalSpawnPoints = new CanonicalSpawnPointData[]
    {
        new CanonicalSpawnPointData("Point_1",  "VendingMachine",  new Vector3(-4.648f,  0.05f,  24.69f),  new Quaternion(0f, -0.1784914f,  0f,  0.9839415f),  "「自販機にお札が入らないな…小銭持ってる？」"),
        new CanonicalSpawnPointData("Point_2",  "Toko",            new Vector3( 5.71f,   0.05f,  13.09f),  new Quaternion(0f,  0.69232297f, 0f, -0.72158784f), "「いらっしゃい！何を探しているのかい？」"),
        new CanonicalSpawnPointData("Point_3",  "Sampah",          new Vector3(-6.029f,  0.05f,  -0.3f),    new Quaternion(0f,  0f,          0f,  1f),          "「ペットボトルのキャップは外して捨てるんだっけ？」"),
        new CanonicalSpawnPointData("Point_4",  "Bunga",           new Vector3(-3.163f,  0.05f,  34.684f), new Quaternion(0f, -0.901942f,   0f,  0.4318573f),  "「このお花、綺麗に咲いたでしょう？」"),
        new CanonicalSpawnPointData("Point_5",  "Mobil",           new Vector3( 1.402f,  0.05f, -37.65f),  new Quaternion(0f,  0f,          0f,  1f),          "「ここに少し車を止めても平気かな？」"),
        new CanonicalSpawnPointData("Point_6",  "Zebracross",      new Vector3( 3.12f,   0.05f, -16.57f),  new Quaternion(0f, -0.68627435f, 0f,  0.72734284f), "「いい天気ですね。どちらへお出かけですか？」"),
        new CanonicalSpawnPointData("Point_7",  "VendingMachine2", new Vector3(17.664f,  0.05f,  -9.66f),  new Quaternion(0f, -0.12609576f, 0f,  0.9920181f),  "「喉が渇いたな。どっちがいいと思う？」"),
        new CanonicalSpawnPointData("Point_8",  "TokoTutup",       new Vector3(63.804f,  0.126f, -14.448f), new Quaternion(0f,  0.40020856f, 0f,  0.9164241f),  "「シャッターが閉まってる…今日休みかな？」"),
        new CanonicalSpawnPointData("Point_9",  "PipadanGas",      new Vector3(-11.46f,  0.05f, -10.49f),  new Quaternion(0f,  0f,          0f,  1f),          "「壁にガスメーターがたくさん並んでいて面白いね。」"),
        new CanonicalSpawnPointData("Point_10", "Sampah2",         new Vector3(-37.16f,  0.05f, -16.73f),  new Quaternion(0f,  0.7279224f,  0f,  0.68565965f), "「青いゴミ箱があるね。今日は資源ごみの日？」"),
        new CanonicalSpawnPointData("Point_11", "LampuMerah",      new Vector3(-2.706f,  0.05f,  47.562f), new Quaternion(0f,  0.7020071f,  0f,  0.71216995f), "「車は通ってないけど、青になるまで待とうね。」"),
    };

    [Header("Preset Karakter BoZo (11 Karakter)")]
    [SerializeField] private DataObject[] characterPresets;

    [Header("Foto Avatar Karakter BoZo (Sesuai Model 3D)")]
    [SerializeField] private Sprite[] characterAvatars;

    private List<GameObject> activeNPCs = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (lockCanonicalSpawnPoints)
        {
            ApplyCanonicalSpawnPoints(this);
        }
    }

    private void Start()
    {
        SpawnRandomNPCs();
    }

    [ContextMenu("Kunci Ulang Titik & Arah Hadap Spawn Saat Ini")]
    public void EnforceCanonicalSpawnPointsContextMenu()
    {
        ApplyCanonicalSpawnPoints(this);
    }

    public static bool ApplyCanonicalSpawnPoints(NPCSpawner spawner)
    {
        if (spawner == null) return false;
        bool changed = false;

        GameObject spawnPointsParent = null;
        if (spawner.spawnLocations != null)
        {
            foreach (var loc in spawner.spawnLocations)
            {
                if (loc != null && loc.pointTransform != null && loc.pointTransform.parent != null && loc.pointTransform.parent.name == "SpawnPoints")
                {
                    spawnPointsParent = loc.pointTransform.parent.gameObject;
                    break;
                }
            }
        }
        if (spawnPointsParent == null)
        {
            spawnPointsParent = GameObject.Find("SpawnPoints");
        }
        if (spawnPointsParent == null)
        {
            spawnPointsParent = new GameObject("SpawnPoints");
            changed = true;
        }

        Transform parentTr = spawnPointsParent.transform;
        if ((parentTr.position - Vector3.zero).sqrMagnitude > 1e-6f ||
            Quaternion.Angle(parentTr.rotation, Quaternion.identity) > 0.001f ||
            (parentTr.localScale - Vector3.one).sqrMagnitude > 1e-6f)
        {
            parentTr.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            parentTr.localScale = Vector3.one;
            changed = true;
        }

        Transform[] canonicalTransforms = new Transform[CanonicalSpawnPoints.Length];
        for (int i = 0; i < CanonicalSpawnPoints.Length; i++)
        {
            CanonicalSpawnPointData data = CanonicalSpawnPoints[i];
            Transform pt = parentTr.Find(data.pointName);
            if (pt == null)
            {
                GameObject ptObj = new GameObject(data.pointName);
                ptObj.transform.SetParent(parentTr, false);
                pt = ptObj.transform;
                changed = true;
            }

            if ((pt.position - data.position).sqrMagnitude > 1e-6f ||
                Quaternion.Angle(pt.rotation, data.rotation) > 0.001f ||
                (pt.localScale - Vector3.one).sqrMagnitude > 1e-6f)
            {
                pt.SetPositionAndRotation(data.position, data.rotation);
                pt.localScale = Vector3.one;
                changed = true;
            }

            canonicalTransforms[i] = pt;
        }

        if (spawner.spawnLocations == null || spawner.spawnLocations.Length < CanonicalSpawnPoints.Length)
        {
            System.Array.Resize(ref spawner.spawnLocations, CanonicalSpawnPoints.Length);
            changed = true;
        }

        for (int i = 0; i < spawner.spawnLocations.Length; i++)
        {
            if (spawner.spawnLocations[i] == null)
            {
                spawner.spawnLocations[i] = new ContextualSpawnPoint();
                changed = true;
            }

            int targetIdx = i < CanonicalSpawnPoints.Length ? i : -1;
            string currentLocName = spawner.spawnLocations[i].locationName;
            if (!string.IsNullOrEmpty(currentLocName))
            {
                for (int k = 0; k < CanonicalSpawnPoints.Length; k++)
                {
                    if (string.Equals(currentLocName.Trim(), CanonicalSpawnPoints[k].locationName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        targetIdx = k;
                        break;
                    }
                }
            }

            if (targetIdx >= 0 && targetIdx < canonicalTransforms.Length)
            {
                if (string.IsNullOrEmpty(currentLocName) || currentLocName == "Lokasi Baru")
                {
                    spawner.spawnLocations[i].locationName = CanonicalSpawnPoints[targetIdx].locationName;
                    changed = true;
                }

                if (spawner.spawnLocations[i].pointTransform != canonicalTransforms[targetIdx])
                {
                    spawner.spawnLocations[i].pointTransform = canonicalTransforms[targetIdx];
                    changed = true;
                }
            }
        }

        changed |= EnsureCanonicalQuestionJapanese(spawner);
        return changed;
    }

    public static bool EnsureCanonicalQuestionJapanese(NPCSpawner spawner)
    {
        if (spawner == null || spawner.spawnLocations == null) return false;
        bool changed = false;

        for (int i = 0; i < spawner.spawnLocations.Length; i++)
        {
            ContextualSpawnPoint loc = spawner.spawnLocations[i];
            if (loc == null || loc.locationQuestions == null || loc.locationQuestions.Length == 0) continue;

            int canonicalIdx = -1;
            if (!string.IsNullOrEmpty(loc.locationName))
            {
                string trimmed = loc.locationName.Trim();
                for (int k = 0; k < CanonicalSpawnPoints.Length; k++)
                {
                    if (string.Equals(trimmed, CanonicalSpawnPoints[k].locationName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        canonicalIdx = k;
                        break;
                    }
                }
            }
            if (canonicalIdx < 0 && i < CanonicalSpawnPoints.Length)
            {
                canonicalIdx = i;
            }
            if (canonicalIdx < 0) continue;

            string canonicalJp = CanonicalSpawnPoints[canonicalIdx].questionJapanese;
            for (int q = 0; q < loc.locationQuestions.Length; q++)
            {
                NPCData data = loc.locationQuestions[q];
                if (data != null && string.IsNullOrEmpty(data.questionJapanese))
                {
                    data.questionJapanese = canonicalJp;
                    changed = true;
                }
            }
        }

        return changed;
    }

    public void SpawnRandomNPCs()
    {
        if (lockCanonicalSpawnPoints)
        {
            ApplyCanonicalSpawnPoints(this);
        }
        else
        {
            EnsureCanonicalQuestionJapanese(this);
        }

        // 1. Bersihkan NPC lama jika ada
        foreach (var npc in activeNPCs)
        {
            if (npc != null) Destroy(npc);
        }
        activeNPCs.Clear();

        if (spawnLocations == null || spawnLocations.Length == 0 || npcPrefab == null || characterPresets == null || characterPresets.Length == 0)
        {
            Debug.LogWarning("[NPCSpawner] Pastikan prefab, preset BoZo, dan spawn locations sudah terisi!");
            return;
        }

        // 2. Acak titik lokasi yang akan memunculkan NPC
        List<ContextualSpawnPoint> availableLocations = new List<ContextualSpawnPoint>(spawnLocations);
        int spawnCount = Mathf.Min(numberOfNpcs, availableLocations.Count);

        // KUNCI ANTI-KEMBAR: Buat salinan daftar karakter yang siap dicoret setelah terpilih
        List<DataObject> availablePresets = new List<DataObject>(characterPresets);

        for (int i = 0; i < spawnCount; i++)
        {
            int randomIndex = Random.Range(0, availableLocations.Count);
            ContextualSpawnPoint chosenLoc = availableLocations[randomIndex];
            availableLocations.RemoveAt(randomIndex); // Hapus lokasi agar 1 tempat tidak diisi 2 orang

            if (chosenLoc.pointTransform == null) continue;

            // 3. Ground Snapping aman (hanya koreksi Y dalam toleransi tipis agar kaki menyentuh tanah tanpa geser X/Z atau naik ke prop/pagar)
            Vector3 spawnPos = chosenLoc.pointTransform.position;
            Vector3 rayStart = spawnPos + Vector3.up * 0.5f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.point.y <= spawnPos.y + 0.15f && hit.point.y >= spawnPos.y - 0.35f)
                {
                    spawnPos.y = hit.point.y;
                }
            }

            // 4. Munculkan NPC di lokasi & arah hadap titik spawn
            GameObject newNPC = Instantiate(npcPrefab, spawnPos, chosenLoc.pointTransform.rotation);
            activeNPCs.Add(newNPC);

            // 5. PILIH KARAKTER UNIK (ANTI-KEMBAR!)
            if (availablePresets.Count == 0)
                availablePresets = new List<DataObject>(characterPresets); // Isi ulang jika jumlah NPC > jumlah preset

            int presetIndex = Random.Range(0, availablePresets.Count);
            DataObject chosenPreset = availablePresets[presetIndex];
            availablePresets.RemoveAt(presetIndex); // HAPUS KARAKTER INI AGAR TIDAK BISA DIPILIH NPC LAIN!

            OutfitSystem outfit = newNPC.GetComponentInChildren<OutfitSystem>();
            if (outfit != null && chosenPreset != null)
            {
                outfit.LoadFromObject(chosenPreset);
            }

            // 6. Pilih soal khusus lokasi tersebut
            NPCInteractable interactable = newNPC.GetComponent<NPCInteractable>();
            if (interactable != null && chosenLoc.locationQuestions != null && chosenLoc.locationQuestions.Length > 0)
            {
                NPCData chosenQuestion = chosenLoc.locationQuestions[Random.Range(0, chosenLoc.locationQuestions.Length)];
                
                // Gunakan nama NPC warga lokal sesuai soal di lokasi tersebut (format Figma: Nama (カナ))!
                string characterName = GetFigmaFormattedName(chosenQuestion.npcName);
                Sprite avatarSprite = (chosenPreset != null) ? GetAvatarForPreset(chosenPreset.name) : chosenQuestion.avatarPortrait;

                interactable.SetNPCData(chosenQuestion, characterName, avatarSprite);
            }
        }
    }

    public static string GetFigmaFormattedName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Hana (ハナ)";
        name = name.Trim();

        switch (name)
        {
            case "Aoi": return "Aoi (アオイ)";
            case "Ren": return "Ren (レン)";
            case "Sora": return "Sora (ソラ)";
            case "Kaoru": return "Kaoru (カオル)";
            case "Makoto": return "Makoto (マコト)";
            case "Haru": return "Haru (ハル)";
            case "Hikaru": return "Hikaru (ヒカル)";
            case "Yuuki": return "Yuuki (ユウキ)";
            case "Ibuki": return "Ibuki (イブキ)";
            case "Chihiro": return "Chihiro (チヒロ)";
            case "Rin": return "Rin (リン)";
            case "Natsuki": return "Natsuki (ナツキ)";
            case "Hana": return "Hana (ハナ)";
            default:
                if (name.Contains("(") && name.Contains(")")) return name;
                return name;
        }
    }

    public Sprite GetAvatarForPreset(string presetName)
    {
        if (characterAvatars != null)
        {
            foreach (var s in characterAvatars)
            {
                if (s != null && s.name.Equals(presetName, System.StringComparison.OrdinalIgnoreCase))
                    return s;
            }
        }
        return null;
    }
}
