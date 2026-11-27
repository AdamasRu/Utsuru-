using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NPCInteractable : MonoBehaviour
{
    [Header("Data Soal")]
    [SerializeField] private NPCData npcData;

    [Header("Pengaturan Menoleh")]
    [Tooltip("Kecepatan NPC memutar badan")]
    [SerializeField] private float rotationSpeed = 5f;

    private bool isPlayerNearby = false;
    private string npcNameOverride = "";
    private Sprite npcAvatarOverride = null;
    private Transform playerTransform;
    private Quaternion defaultRotation;

    public bool HasCompletedDialogue { get; private set; }
    public DialogueChoice CompletedChoice { get; private set; }

    private void Start()
    {
        defaultRotation = transform.rotation;
    }

    private void Update()
    {
        HandleLookingAtPlayer();
        HandleInteractionInput();
    }

    private void HandleLookingAtPlayer()
    {
        if (isPlayerNearby && playerTransform != null)
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            directionToPlayer.y = 0f; // Kunci sumbu Y agar badan tetap tegak lurus

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
        else
        {
            // Kembali menghadap posisi asal saat player pergi
            transform.rotation = Quaternion.Slerp(transform.rotation, defaultRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private void HandleInteractionInput()
    {
        if (!isPlayerNearby || npcData == null || DialogueManager.Instance == null)
            return;

        // Jangan trigger dialog ulang jika dialog masih terbuka atau baru ditutup pada frame ini
        if (DialogueManager.Instance.IsDialogueActive || DialogueManager.Instance.WasClosedThisFrame)
            return;

        // Jangan trigger jika menu modal lain sedang terbuka
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
            return;
        if (Utsuru.Achievements.AchievementUIController.Instance != null && Utsuru.Achievements.AchievementUIController.Instance.IsOpen)
            return;
        if (Utsuru.Store.StoreUIController.Instance != null && Utsuru.Store.StoreUIController.Instance.IsOpen)
            return;

        bool ePressed = Input.GetKeyDown(KeyCode.E);
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ePressed = true;
#endif

        if (ePressed)
        {
            DialogueManager.Instance.StartDialogue(npcData, npcNameOverride, npcAvatarOverride, this);
        }
    }

    public void SetNPCData(NPCData data, string characterName = "", Sprite avatar = null)
    {
        npcData = data;
        npcNameOverride = characterName;
        npcAvatarOverride = avatar;
        HasCompletedDialogue = false;
        CompletedChoice = default;
    }

    public void MarkDialogueCompleted(DialogueChoice chosenResponse)
    {
        HasCompletedDialogue = true;
        CompletedChoice = chosenResponse;
    }

    private void OnTriggerEnter(Collider other)
    {
        CharacterController cc = other.GetComponentInParent<CharacterController>();
        if ((cc != null && cc.CompareTag("Player")) || other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerTransform = (cc != null) ? cc.transform : other.transform;

            // Munculkan tulisan [E] Talk di tengah layar (kecuali dialog sedang aktif)
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.ShowInteractionPrompt(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || (playerTransform != null && other.transform.IsChildOf(playerTransform)))
        {
            isPlayerNearby = false;
            playerTransform = null;

            // Sembunyikan tulisan [E] Talk saat menjauh
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.ShowInteractionPrompt(false);
        }
    }
}
