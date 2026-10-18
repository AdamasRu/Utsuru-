using UnityEngine;

public class PlayerAnimDriver : MonoBehaviour
{
    public Animator animator;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        Vector3 horizontal = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        animator.SetFloat("Speed", horizontal.magnitude, 0.1f, Time.deltaTime);
    }
}