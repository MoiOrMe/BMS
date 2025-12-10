using UnityEngine;

public class KnightCharacterController : MonoBehaviour
{
    public Animator animator;

    private readonly int IsFightingHash = Animator.StringToHash("IsFighting");

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void SetProximityActive(bool isClose)
    {
        if (animator != null)
        {
            animator.SetBool(IsFightingHash, isClose);
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