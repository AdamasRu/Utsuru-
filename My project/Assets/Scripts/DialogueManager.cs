using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeSelf;

    [Header("UI Komponen")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject interactionPrompt; // Objek teks [E] Talk di tengah layar
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Transform choicesContainer;    // Parent tombol pilihan
    [SerializeField] private GameObject choiceButtonPrefab;  // Prefab Tombol UI
    [Tooltip("Teks pada tombol selesai/lanjut setelah balasan NPC")]
    [SerializeField] private string closePromptText = "「次へ / Selesai」 (Tekan Spasi atau Klik)";

    [Header("Efek Suara Undertale")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float typingSpeed = 0.03f;      // Jeda antar huruf
    [SerializeField] private float pitchVariation = 0.08f;   // Variasi nada acak

    [Header("Player Reference")]
    [SerializeField] private FirstPersonController playerController;

    private Coroutine typingRoutine;
    private NPCData currentData;
    private string currentResponseText = "";
    private bool isTyping = false;
    private bool isShowingResponse = false;
    private bool isWaitingToClose = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (dialoguePanel == null || !dialoguePanel.activeSelf) return;

        bool spaceOrE = false;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            spaceOrE = true;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
                spaceOrE = true;
        }
#endif

        if (spaceOrE)
        {
            // 1. Jika sedang mengetik (soal atau balasan) -> Skip langsung selesai
            if (isTyping)
            {
                if (isShowingResponse)
                {
                    FinishTypingResponseInstantly();
                }
                else
                {
                    FinishTypingQuestionInstantly();
                }
            }
            // 2. Jika balasan sudah selesai tampil -> Tutup dialog
            else if (isWaitingToClose)
            {
                CloseDialogue();
            }
        }
    }

    public void StartDialogue(NPCData data, string customName = "")
    {
        if (data == null) return;
        currentData = data;
        isShowingResponse = false;
        isWaitingToClose = false;

        // Sembunyikan tulisan [E] Talk saat dialog dibuka
        ShowInteractionPrompt(false);

        dialoguePanel.SetActive(true);

        // Kunci pergerakan player & aktifkan kursor mouse
        if (playerController != null) playerController.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Gunakan nama kustom jika ada (misal: Chloe, Dave, Zulle)
        nameText.text = !string.IsNullOrEmpty(customName) ? customName : data.npcName;

        // Bersihkan tombol pilihan lama
        ClearChoices();

        // Mulai ketik pertanyaan dengan suara
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypewriterText(data.questionText, data, onComplete: ShowChoices));
    }

    private IEnumerator TypewriterText(string fullText, NPCData data, System.Action onComplete)
    {
        isTyping = true;
        bodyText.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            // Lewati tag formatting <color> agar tidak tampil mentah
            if (fullText[i] == '<')
            {
                int closingIndex = fullText.IndexOf('>', i);
                if (closingIndex != -1)
                {
                    bodyText.text += fullText.Substring(i, closingIndex - i + 1);
                    i = closingIndex;
                    continue;
                }
            }

            bodyText.text += fullText[i];

            // Efek suara Undertale blip tiap huruf non-spasi (gunakan data.voiceSound atau audioSource.clip bawaan)
            AudioClip soundToPlay = (data != null && data.voiceSound != null) ? data.voiceSound : (audioSource != null ? audioSource.clip : null);
            if (fullText[i] != ' ' && fullText[i] != '\n' && soundToPlay != null && audioSource != null)
            {
                float basePitch = (data != null) ? data.basePitch : 1.0f;
                audioSource.pitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
                audioSource.PlayOneShot(soundToPlay);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        onComplete?.Invoke();
    }

    private void FinishTypingQuestionInstantly()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        bodyText.text = currentData.questionText;
        isTyping = false;
        ShowChoices();
    }

    private void FinishTypingResponseInstantly()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        bodyText.text = currentResponseText;
        isTyping = false;
        ShowClosePromptButton();
    }

    private void ShowChoices()
    {
        ClearChoices();
        if (currentData.choices == null) return;

        for (int i = 0; i < currentData.choices.Length; i++)
        {
            DialogueChoice choice = currentData.choices[i];
            GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer);

            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = choice.choiceText;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnChoiceClicked(choice));
        }
    }

    private void OnChoiceClicked(DialogueChoice choice)
    {
        ClearChoices();
        isShowingResponse = true;

        // Tambah skor jika benar
        if (choice.isCorrect && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(currentData.scoreReward);
        }

        // Tentukan teks balasan dari NPC
        string response = choice.npcResponse;
        if (string.IsNullOrEmpty(response))
        {
            if (choice.isCorrect)
                response = "「そうですね！教えてくれてありがとう！」\n(Bener juga ya! Makasih sudah kasih tahu!)";
            else
                response = "「えっ、本当に…？ちょっと違う気がするよ〜。」\n(Eh masa sih...? Kayaknya bukan gitu deh〜.)";
        }

        // Tambahkan info perolehan poin
        if (choice.isCorrect)
            response += $"\n<color=#4CAF50>+{currentData.scoreReward} Poin!</color>";
        else
            response += "\n<color=#FF5252>+0 Poin</color>";

        currentResponseText = response;

        // Ketik balasan NPC dengan suara
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypewriterText(currentResponseText, currentData, onComplete: ShowClosePromptButton));
    }

    private void ShowClosePromptButton()
    {
        isWaitingToClose = true;
        ClearChoices();

        // Buat 1 tombol praktis: [ Tekan SPASI atau Klik untuk Selesai ]
        GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer);
        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            btnText.text = closePromptText;
        }

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(CloseDialogue);
    }

    public void CloseDialogue()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        isTyping = false;
        isShowingResponse = false;
        isWaitingToClose = false;
        ClearChoices();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // Kembalikan kontrol player & kunci kursor kembali
        if (playerController != null) playerController.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ClearChoices()
    {
        if (choicesContainer != null)
        {
            foreach (Transform child in choicesContainer)
                Destroy(child.gameObject);
        }
    }

    public void ShowInteractionPrompt(bool show)
    {
        // Jangan tampilkan tulisan [E] jika kotak dialog sedang terbuka
        if (dialoguePanel != null && dialoguePanel.activeSelf && show)
            return;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(show);
    }
}
