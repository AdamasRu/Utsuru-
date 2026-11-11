using UnityEngine;

[System.Serializable]
public struct DialogueChoice
{
    [Tooltip("Teks pilihan dalam bahasa Jepang")]
    public string choiceText;

    [Tooltip("Teks pilihan dalam Romaji")]
    public string choiceRomaji;

    [Tooltip("Centang jika ini pilihan yang tepat/benar")]
    public bool isCorrect;

    [Tooltip("Poin yang didapatkan untuk pilihan ini (default: 20)")]
    public int scoreReward;

    [Header("Respon Balasan NPC")]
    [Tooltip("Balasan NPC dalam bahasa Jepang")]
    [TextArea(2, 4)]
    public string npcResponse;

    [Tooltip("Balasan NPC dalam Romaji")]
    [TextArea(2, 4)]
    public string npcResponseRomaji;

    [Tooltip("Balasan NPC dalam bahasa Inggris")]
    [TextArea(2, 4)]
    public string npcResponseEnglish;
}

[System.Serializable]
public class NPCData
{
    [Header("Identitas Karakter")]
    public string npcName = "Warga";
    public Sprite avatarPortrait;
    public string roleTag = "";
    public string jlptTag = "Formality: Casual & Friendly • JLPT N5";

    [Header("Pertanyaan (3 Layer)")]
    [Tooltip("Teks pertanyaan dalam bahasa Jepang")]
    [TextArea(2, 4)]
    public string questionJapanese = "";

    [Tooltip("Teks pertanyaan dalam Romaji")]
    public string questionRomaji = "";

    [Tooltip("Teks pertanyaan terjemahan bahasa Inggris / Indonesia")]
    public string questionEnglish = "";

    // Properti backward-compatibility untuk sistem lama
    public string questionText
    {
        get => !string.IsNullOrEmpty(questionJapanese) ? questionJapanese : "";
        set => questionJapanese = value;
    }

    [Header("Pilihan Jawaban")]
    public DialogueChoice[] choices;

    [Header("Hadiah Poin Default")]
    public int scoreReward = 20;

    [Header("Suara & Voice Line")]
    [Tooltip("Audio klip suara/voice line NPC untuk tombol Replay Audio")]
    public AudioClip voiceSound;
    [Range(0.5f, 2.0f)] public float basePitch = 1.0f;
}
