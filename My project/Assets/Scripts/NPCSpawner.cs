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
    [SerializeField] private int numberOfNpcs = 2;

    [Header("Preset Karakter BoZo (11 Karakter)")]
    [SerializeField] private DataObject[] characterPresets;

    private List<GameObject> activeNPCs = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        SpawnRandomNPCs();
    }

    public void SpawnRandomNPCs()
    {
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

            // 3. Ground Snapping (Menempelkan kaki tepat ke tanah via Raycast)
            Vector3 spawnPos = chosenLoc.pointTransform.position;
            // Tembak ray hanya 0.8m di atas titik spawn (bukan 5m agar tidak mengenai atap/kanopi/balkon)
            Vector3 rayStart = spawnPos + Vector3.up * 0.8f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                // Jangan gunakan hit jika tingginya berada di atas kepala spawn point (menghindari atap)
                if (hit.point.y <= spawnPos.y + 0.5f)
                {
                    spawnPos = hit.point;
                }
            }

            // 4. Munculkan NPC di lokasi tersebut
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
                // Prioritaskan nama dari soal (Inspector), jika tidak ada baru pakai nama model 
                string characterName = !string.IsNullOrEmpty(chosenQuestion.npcName) && chosenQuestion.npcName != "Warga"
                    ? chosenQuestion.npcName
                    : chosenPreset.name;

                interactable.SetNPCData(chosenQuestion, characterName);
            }
        }
    }
}
