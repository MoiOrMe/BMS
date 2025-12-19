using UnityEngine;

public enum UnitClass
{
    Tank,
    Knight,
    Mage
}

public class BattleCharacterController : MonoBehaviour
{
    public UnitClass unitClass;
    public Animator animator;

    private readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    private Quaternion initialRotation;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        initialRotation = transform.localRotation;
    }

    public void SetProximityActive(bool isClose)
    {
        if (animator != null)
        {
            animator.SetBool(IsAttackingHash, isClose);
        }
    }

    public void FaceEnemy(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;

        Vector3 projectedDirection = Vector3.ProjectOnPlane(direction, transform.up);

        if (projectedDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(projectedDirection, transform.up);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    public void ResetFacing()
    {
        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialRotation, Time.deltaTime * 5f);
    }
}