using UnityEngine;

[System.Serializable]
public struct DialogueChoice
{
    [Tooltip("Teks pada tombol pilihan")]
    public string choiceText;

    [Tooltip("Centang jika ini jawaban yang benar")]
    public bool isCorrect;

    [Tooltip("Balasan / reaksi dari NPC saat player memilih opsi ini")]
    [TextArea(2, 4)]
    public string npcResponse;
}

[System.Serializable]
public class NPCData
{
    [Header("Nama")]
    public string npcName = "Warga";

    [Header("Pertanyaan")]
    [TextArea(2, 4)]
    public string questionText = "Tulis pertanyaan di sini...";

    [Header("Pilihan Jawaban")]
    public DialogueChoice[] choices;

    [Header("Hadiah Poin")]
    public int scoreReward = 100;

    [Header("Suara (Opsional)")]
    [Tooltip("Jika dikosongkan, akan otomatis memakai suara default")]
    public AudioClip voiceSound;
    [Range(0.5f, 2.0f)] public float basePitch = 1.0f;
}
