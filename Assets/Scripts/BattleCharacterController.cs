using UnityEngine;

public enum UnitClass
{
    Tank,
    Knight,
    Mage
}

public class BattleCharacterController : MonoBehaviour
{
    [Header("Paramètres de l'Unité")]
    public UnitClass unitClass;
    public Animator animator;

    [Header("Audio")]
    public AudioClip attackSound;
    private AudioSource audioSource;

    private readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private Quaternion initialRotation;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        initialRotation = transform.localRotation;
    }

    public void SetProximityActive(bool isClose)
    {
        if (animator != null)
        {
            bool isAlreadyAttacking = animator.GetBool(IsAttackingHash);

            if (isClose && !isAlreadyAttacking)
            {
                PlayAttackSound();
            }

            animator.SetBool(IsAttackingHash, isClose);
        }
    }

    private void PlayAttackSound()
    {
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
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