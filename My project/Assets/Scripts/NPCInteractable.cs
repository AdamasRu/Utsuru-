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
    private Transform playerTransform;
    private Quaternion defaultRotation;

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
        bool ePressed = Input.GetKeyDown(KeyCode.E);
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ePressed = true;
#endif

        if (isPlayerNearby && ePressed)
        {
            if (DialogueManager.Instance != null && npcData != null)
            {
                DialogueManager.Instance.StartDialogue(npcData, npcNameOverride);
            }
        }
    }

    public void SetNPCData(NPCData data, string characterName = "")
    {
        npcData = data;
        npcNameOverride = characterName;
    }

    private void OnTriggerEnter(Collider other)
    {
        CharacterController cc = other.GetComponentInParent<CharacterController>();
        if ((cc != null && cc.CompareTag("Player")) || other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerTransform = (cc != null) ? cc.transform : other.transform;

            // Munculkan tulisan [E] Talk di tengah layar
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