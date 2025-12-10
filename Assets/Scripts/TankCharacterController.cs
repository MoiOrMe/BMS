using UnityEngine;

public class TankCharacterController : MonoBehaviour
{
    public Animator animator;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetBool("IsAttacking", true);

            Invoke("StopAttacking", 1.0f);
        }
    }
    void StopAttacking()
    {
        animator.SetBool("IsAttacking", false);
    }
}