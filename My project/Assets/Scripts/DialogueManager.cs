using System.Collections;
using System.Collections.Generic;
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
    public static bool HideEnglishTranslation = false;
    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeSelf;

    [Header("Panel Utama")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject interactionPrompt; // [E] Bicara

    [Header("Avatar & Identitas Karakter")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text roleTagText;
    [SerializeField] private TMP_Text jlptTagText;

    [Header("Header Buttons")]
    [SerializeField] private Button replayAudioButton;
    [SerializeField] private Button closeButton;

    [Header("Konten Dialog (3 Layer)")]
    [SerializeField] private TMP_Text textJapanese;
    [SerializeField] private TMP_Text textRomaji;
    [SerializeField] private TMP_Text textEnglish;

    [Header("Pilihan & Interaksi")]
    [SerializeField] private TMP_Text choosePromptText; // CHOOSE YOUR KAIWA RESPONSE [1 or 2]:
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    [Header("Indikator Skor")]
    [SerializeField] private GameObject scoreBadgePill; // Badge ★ +20 Learning Points
    [SerializeField] private TMP_Text scoreBadgeText;

    [Header("Sprite Varian Tombol")]
    [SerializeField] private Sprite choiceCardNormalBg;
    [SerializeField] private Sprite choiceCardSelectedBg;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    public AudioSource VoiceAudioSource => audioSource;
    [SerializeField] private float typingSpeed = 0.025f;
    [SerializeField] private float pitchVariation = 0.08f;

    [Header("Player")]
    [SerializeField] private FirstPersonController playerController;

    private Coroutine typingRoutine;
    private NPCData currentData;
    private bool isTyping = false;
    private bool isShowingResponse = false;
    private bool isWaitingToClose = false;
    private List<DialogueChoice> currentActiveChoices = new List<DialogueChoice>();

    private string currentJpQuestion = "";
    private string currentRmQuestion = "";
    private string currentEnQuestion = "";

    private string currentJpResponse = "";
    private string currentRmResponse = "";
    private string currentEnResponse = "";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        UpdateSettingsVisibility();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseDialogue);
            closeButton.gameObject.SetActive(false);
        }
        if (replayAudioButton != null)
        {
            replayAudioButton.onClick.RemoveAllListeners();
            replayAudioButton.onClick.AddListener(PlayDialogueAudio);
        }

        // Pastikan background / border pada ScoreBadgePill dinonaktifkan (hanya teks murni)
        if (scoreBadgePill != null)
        {
            UnityEngine.UI.Image img = scoreBadgePill.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.enabled = false;
        }
    }

    private void Update()
    {
        if (dialoguePanel == null || !dialoguePanel.activeSelf) return;

        // 1. Shortcut Angka 1, 2, 3 untuk memilih jawaban secara instan
        if (!isShowingResponse && currentActiveChoices.Count > 0)
        {
            bool press1 = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            bool press2 = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            bool press3 = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
                    press1 = true;
                if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
                    press2 = true;
                if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
                    press3 = true;
            }
#endif

            if (press1 && currentActiveChoices.Count >= 1)
            {
                OnChoiceClicked(currentActiveChoices[0]);
                return;
            }
            if (press2 && currentActiveChoices.Count >= 2)
            {
                OnChoiceClicked(currentActiveChoices[1]);
                return;
            }
            if (press3 && currentActiveChoices.Count >= 3)
            {
                OnChoiceClicked(currentActiveChoices[2]);
                return;
            }
        }

        // 2. Tombol Tutup / Lanjut (Space, E, Enter, Escape)
        bool confirmClose = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) ||
                            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape);

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
                confirmClose = true;
        }
#endif

        if (confirmClose)
        {
            if (isTyping)
            {
                FinishTypingInstantly();
            }
            else if (isWaitingToClose)
            {
                CloseDialogue();
            }
        }
    }

    public void UpdateSettingsVisibility()
    {
        HideEnglishTranslation = PlayerPrefs.GetInt("Setting_HideEnglish", 0) == 1;
        if (textEnglish != null) textEnglish.gameObject.SetActive(!HideEnglishTranslation);
    }

    public void StartDialogue(NPCData data, string customName = "", Sprite customAvatar = null)
    {
        if (data == null) return;
        currentData = data;
        isShowingResponse = false;
        isWaitingToClose = false;
        currentActiveChoices.Clear();

        ShowInteractionPrompt(false);
        dialoguePanel.SetActive(true);
        UpdateSettingsVisibility();

        // Kunci gerakan pemain & munculkan kursor
        if (playerController != null) playerController.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pasang Nama Karakter & Sesuaikan Lebar Kapsul NamePill Otomatis
        string displayName = !string.IsNullOrEmpty(customName) ? customName : data.npcName;
        if (nameText != null)
        {
            nameText.text = displayName;
            if (nameText.transform.parent != null)
            {
                nameText.transform.parent.gameObject.SetActive(true);
                RectTransform pillRt = nameText.transform.parent as RectTransform;
                if (pillRt != null)
                {
                    // Hitung lebar teks secara akurat + beri padding kapsul 36px
                    float textWidth = nameText.preferredWidth;
                    float autoWidth = Mathf.Max(textWidth + 36f, 130f);
                    pillRt.sizeDelta = new Vector2(autoWidth, 36f);
                }
            }
        }

        // Pasang Role Tag (Contoh: "Shopkeeper", "Eco Neighbor", dsb.)
        if (roleTagText != null)
        {
            if (!string.IsNullOrEmpty(data.roleTag))
            {
                roleTagText.text = data.roleTag;
                if (roleTagText.transform.parent != null)
                {
                    roleTagText.transform.parent.gameObject.SetActive(true);
                    RectTransform rpRt = roleTagText.transform.parent as RectTransform;
                    if (rpRt != null)
                    {
                        float textWidth = roleTagText.preferredWidth;
                        float autoWidth = Mathf.Max(textWidth + 28f, 110f);
                        rpRt.sizeDelta = new Vector2(autoWidth, 34f);

                        // Geser posisi JLPT tag agar berjarak rapi di sebelah kanan RolePill
                        if (jlptTagText != null)
                        {
                            RectTransform jlptRt = jlptTagText.rectTransform;
                            if (jlptRt != null)
                            {
                                jlptRt.anchoredPosition = new Vector2(autoWidth + 16f, jlptRt.anchoredPosition.y);
                            }
                        }
                    }
                }
            }
            else
            {
                if (roleTagText.transform.parent != null)
                    roleTagText.transform.parent.gameObject.SetActive(false);

                if (jlptTagText != null)
                {
                    RectTransform jlptRt = jlptTagText.rectTransform;
                    if (jlptRt != null)
                    {
                        jlptRt.anchoredPosition = new Vector2(0f, jlptRt.anchoredPosition.y);
                    }
                }
            }
        }

        // Pasang JLPT Tag
        if (jlptTagText != null)
        {
            jlptTagText.text = !string.IsNullOrEmpty(data.jlptTag) ? data.jlptTag : "Formality: Casual & Friendly • JLPT N5";
            jlptTagText.gameObject.SetActive(true);
        }

        // Pasang Foto Avatar Sesuai Karakter yang Bicara
        Sprite avatarToUse = customAvatar != null ? customAvatar : data.avatarPortrait;
        if (avatarImage != null)
        {
            if (avatarToUse != null)
            {
                avatarImage.sprite = avatarToUse;
                avatarImage.gameObject.SetActive(true);
            }
            else
            {
                avatarImage.gameObject.SetActive(false);
            }
        }

        // Reset state tombol header
        if (replayAudioButton != null) replayAudioButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        if (scoreBadgePill != null) scoreBadgePill.SetActive(false);

        // Aktifkan petunjuk pemilihan & container pilihan
        if (choosePromptText != null)
        {
            int numChoices = (data.choices != null) ? data.choices.Length : 2;
            choosePromptText.text = $"CHOOSE YOUR KAIWA RESPONSE [1 to {numChoices}]:";
            choosePromptText.gameObject.SetActive(true);
        }
        if (choicesContainer != null) choicesContainer.gameObject.SetActive(true);

        // Mainkan audio jika ada
        PlayDialogueAudio();

        // Urai Teks Soal: Prioritaskan field terpisah (Japanese, Romaji, English) jika sudah diisi rapi
        string jpText = "";
        string rmText = "";
        string enText = "";

        if (!string.IsNullOrEmpty(data.questionJapanese) && !string.IsNullOrEmpty(data.questionRomaji) && !string.IsNullOrEmpty(data.questionEnglish))
        {
            jpText = data.questionJapanese;
            rmText = data.questionRomaji;
            enText = data.questionEnglish;
        }
        else
        {
            string rawText = data.questionText;
            if (!string.IsNullOrEmpty(rawText) && rawText.Contains("\n"))
            {
                string[] lines = rawText.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 1) jpText = lines[0].Trim();
                if (lines.Length >= 2) rmText = lines[1].Trim();
                if (lines.Length >= 3) enText = lines[2].Trim();
            }
            else
            {
                jpText = !string.IsNullOrEmpty(data.questionJapanese) ? data.questionJapanese : rawText;
                rmText = data.questionRomaji;
                enText = data.questionEnglish;
            }
        }

        currentJpQuestion = jpText;
        currentRmQuestion = rmText;
        currentEnQuestion = enText;

        int charSet = PlayerPrefs.GetInt("Setting_CharacterSet", 0);
        string displayJp = jpText;
        string displayRm = rmText;
        if (charSet == 2) // Romaji Only
        {
            displayJp = !string.IsNullOrEmpty(rmText) ? rmText : jpText;
            displayRm = "";
        }
        else if (charSet == 1) // Hiragana Only (hide romaji)
        {
            displayRm = "";
        }

        if (textRomaji != null)
            textRomaji.gameObject.SetActive(!string.IsNullOrEmpty(displayRm));

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypewriterRoutine(displayJp, displayRm, enText, onComplete: () =>
        {
            ShowChoices(data.choices);
        }));
    }

    private IEnumerator TypewriterRoutine(string jp, string romaji, string english, System.Action onComplete)
    {
        isTyping = true;
        if (textJapanese != null) textJapanese.text = "";
        if (textRomaji != null) textRomaji.text = "";
        if (textEnglish != null) textEnglish.text = "";

        // Ketik teks Jepang huruf per huruf
        for (int i = 0; i < jp.Length; i++)
        {
            if (jp[i] == '<')
            {
                int closingIndex = jp.IndexOf('>', i);
                if (closingIndex != -1)
                {
                    if (textJapanese != null) textJapanese.text += jp.Substring(i, closingIndex - i + 1);
                    i = closingIndex;
                    continue;
                }
            }

            if (textJapanese != null) textJapanese.text += jp[i];

            if (jp[i] != ' ' && jp[i] != '\n' && audioSource != null)
            {
                AudioClip clip = (currentData != null && currentData.voiceSound != null) ? currentData.voiceSound : audioSource.clip;
                if (clip != null)
                {
                    float basePitch = (currentData != null) ? currentData.basePitch : 1.0f;
                    audioSource.pitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
                    float voiceVol = PlayerPrefs.GetFloat("Setting_VoiceVol", 75f) / 100f;
                    audioSource.PlayOneShot(clip, voiceVol);
                }
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        // Tampilkan teks Romaji dan Terjemahan Inggris
        if (textRomaji != null) textRomaji.text = romaji;
        if (textEnglish != null) textEnglish.text = english;

        isTyping = false;
        onComplete?.Invoke();
    }

    private void FinishTypingInstantly()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        isTyping = false;

        int charSet = PlayerPrefs.GetInt("Setting_CharacterSet", 0);

        if (!isShowingResponse)
        {
            string displayJp = currentJpQuestion;
            string displayRm = currentRmQuestion;

            if (charSet == 2) // Romaji Only
            {
                displayJp = !string.IsNullOrEmpty(currentRmQuestion) ? currentRmQuestion : currentJpQuestion;
                displayRm = "";
            }
            else if (charSet == 1) // Hiragana Only (hide Romaji)
            {
                displayRm = "";
            }

            if (textJapanese != null) textJapanese.text = displayJp;
            if (textRomaji != null)
            {
                textRomaji.text = displayRm;
                textRomaji.gameObject.SetActive(!string.IsNullOrEmpty(displayRm));
            }
            if (textEnglish != null)
            {
                textEnglish.text = currentEnQuestion;
                textEnglish.gameObject.SetActive(!HideEnglishTranslation);
            }

            ShowChoices(currentData.choices);
        }
        else
        {
            string displayJp = currentJpResponse;
            string displayRm = currentRmResponse;

            if (charSet == 2) // Romaji Only
            {
                displayJp = !string.IsNullOrEmpty(currentRmResponse) ? currentRmResponse : currentJpResponse;
                displayRm = "";
            }
            else if (charSet == 1) // Hiragana Only (hide Romaji)
            {
                displayRm = "";
            }

            if (textJapanese != null) textJapanese.text = displayJp;
            if (textRomaji != null)
            {
                textRomaji.text = displayRm;
                textRomaji.gameObject.SetActive(!string.IsNullOrEmpty(displayRm));
            }
            if (textEnglish != null)
            {
                textEnglish.text = currentEnResponse;
                textEnglish.gameObject.SetActive(!HideEnglishTranslation);
            }

            isWaitingToClose = true;
            if (closeButton != null) closeButton.gameObject.SetActive(true);
        }
    }

    private void ShowChoices(DialogueChoice[] choices)
    {
        ClearChoices();
        currentActiveChoices.Clear();
        if (choices == null || choices.Length == 0) return;

        for (int i = 0; i < choices.Length; i++)
        {
            DialogueChoice choice = choices[i];
            currentActiveChoices.Add(choice);

            if (choiceButtonPrefab != null && choicesContainer != null)
            {
                GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer);
                ChoiceButtonUI btnUI = btnObj.GetComponent<ChoiceButtonUI>();

                int choiceNumber = i + 1;
                Sprite bgSprite = (i == 0 && choiceCardSelectedBg != null) ? choiceCardSelectedBg : choiceCardNormalBg;
                Color? badgeColor = (i == 0) ? new Color(0.36f, 0.24f, 0.15f) : new Color(0.88f, 0.85f, 0.82f);

                // Pisahkan bahasa Jepang dan terjemahan di dalam kurung jika ada
                string jpChoice = choice.choiceText;
                string rmChoice = choice.choiceRomaji;
                if (string.IsNullOrEmpty(rmChoice) && jpChoice != null && jpChoice.Contains("(") && jpChoice.Contains(")"))
                {
                    int pOpen = jpChoice.IndexOf('(');
                    int pClose = jpChoice.IndexOf(')', pOpen);
                    if (pOpen != -1 && pClose != -1)
                    {
                        rmChoice = jpChoice.Substring(pOpen + 1, pClose - pOpen - 1).Trim();
                        jpChoice = jpChoice.Substring(0, pOpen).Trim();
                    }
                }

                int charSet = PlayerPrefs.GetInt("Setting_CharacterSet", 0);
                string displayJp = jpChoice;
                string displayRm = rmChoice;
                if (charSet == 2)
                {
                    displayJp = !string.IsNullOrEmpty(rmChoice) ? rmChoice : jpChoice;
                    displayRm = "";
                }
                else if (charSet == 1)
                {
                    displayRm = "";
                }

                if (btnUI != null)
                {
                    btnUI.Setup(choiceNumber, displayJp, displayRm, () => OnChoiceClicked(choice), bgSprite, badgeColor);
                    if (btnUI.romajiText != null)
                        btnUI.romajiText.gameObject.SetActive(!string.IsNullOrEmpty(displayRm));
                }
                else
                {
                    TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
                    if (btnText != null) btnText.text = $"{choiceNumber}. {choice.choiceText}";
                    Button btn = btnObj.GetComponent<Button>();
                    if (btn != null) btn.onClick.AddListener(() => OnChoiceClicked(choice));
                }
            }
        }
    }

    private void OnChoiceClicked(DialogueChoice choice)
    {
        ClearChoices();
        isShowingResponse = true;

        // Sembunyikan prompt pemilihan
        if (choosePromptText != null) choosePromptText.gameObject.SetActive(false);
        if (choicesContainer != null) choicesContainer.gameObject.SetActive(false);

        // Hanya tambahkan skor jika jawaban BENAR!
        if (choice.isCorrect)
        {
            int reward = choice.scoreReward > 0 ? choice.scoreReward : (currentData.scoreReward > 0 ? currentData.scoreReward : 100);
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(reward);
            }

            if (scoreBadgePill != null)
            {
                scoreBadgePill.SetActive(true);
                if (scoreBadgeText != null)
                {
                    scoreBadgeText.text = $"+{reward} Points";
                }
            }
        }
        else
        {
            // Jawaban SALAH: Jangan beri skor dan sembunyikan badge skor!
            if (scoreBadgePill != null)
            {
                scoreBadgePill.SetActive(false);
            }
        }

        // Urai respon balasan NPC (Jepang, Romaji, Indonesia/Inggris)
        string rawResponse = choice.npcResponse;
        string jpResponse = "";
        string rmResponse = "";
        string enResponse = "";

        if (!string.IsNullOrEmpty(rawResponse) && rawResponse.Contains("\n"))
        {
            string[] rLines = rawResponse.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (rLines.Length >= 1) jpResponse = rLines[0].Trim();
            if (rLines.Length >= 2) rmResponse = rLines[1].Trim();
            if (rLines.Length >= 3) enResponse = rLines[2].Trim();
        }
        else
        {
            jpResponse = rawResponse;
            rmResponse = choice.npcResponseRomaji;
            enResponse = choice.npcResponseEnglish;
        }

        currentJpResponse = jpResponse;
        currentRmResponse = rmResponse;
        currentEnResponse = enResponse;

        // Munculkan tombol close (X) di pojok kanan atas
        if (closeButton != null) closeButton.gameObject.SetActive(true);
        isWaitingToClose = true;

        int charSet = PlayerPrefs.GetInt("Setting_CharacterSet", 0);
        string displayJp = jpResponse;
        string displayRm = rmResponse;
        if (charSet == 2)
        {
            displayJp = !string.IsNullOrEmpty(rmResponse) ? rmResponse : jpResponse;
            displayRm = "";
        }
        else if (charSet == 1)
        {
            displayRm = "";
        }

        if (textRomaji != null)
            textRomaji.gameObject.SetActive(!string.IsNullOrEmpty(displayRm));

        // Ketik balasan
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypewriterRoutine(displayJp, displayRm, enResponse, onComplete: () =>
        {
            isWaitingToClose = true;
        }));
    }

    public void PlayDialogueAudio()
    {
        if (audioSource == null) return;
        AudioClip clip = (currentData != null && currentData.voiceSound != null) ? currentData.voiceSound : audioSource.clip;
        if (clip != null)
        {
            audioSource.pitch = (currentData != null) ? currentData.basePitch : 1.0f;
            float voiceVol = PlayerPrefs.GetFloat("Setting_VoiceVol", 75f) / 100f;
            audioSource.PlayOneShot(clip, voiceVol);
        }
    }

    public void CloseDialogue()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        isTyping = false;
        isShowingResponse = false;
        isWaitingToClose = false;
        ClearChoices();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (scoreBadgePill != null) scoreBadgePill.SetActive(false);

        // Kembalikan kontrol player & kunci kursor kembali
        if (playerController != null) playerController.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ClearChoices()
    {
        currentActiveChoices.Clear();
        if (choicesContainer != null)
        {
            foreach (Transform child in choicesContainer)
                Destroy(child.gameObject);
        }
    }

    public void ShowInteractionPrompt(bool show)
    {
        if (dialoguePanel != null && dialoguePanel.activeSelf && show)
            return;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(show);
    }
}
