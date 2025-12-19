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

    [Header("VFX (Effets Spéciaux)")]
    public GameObject attackParticlePrefab;
    public Transform particleSpawnPoint;
    public float particleLifetime = 2.0f;
    private Vector3 lastKnownEnemyPosition;

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
                SpawnParticleEffect();
            }

            animator.SetBool(IsAttackingHash, isClose);
        }
    }

    private void SpawnParticleEffect()
    {
        if (attackParticlePrefab != null)
        {
            Vector3 spawnPos;
            Quaternion spawnRot;

            if (particleSpawnPoint != null)
            {
                spawnPos = particleSpawnPoint.position;
                spawnRot = particleSpawnPoint.rotation;
            }
            else
            {
                spawnPos = transform.position + Vector3.up * 0.5f;
                spawnRot = transform.rotation;
            }

            GameObject vfx = Instantiate(attackParticlePrefab, spawnPos, spawnRot);

            if (unitClass == UnitClass.Mage)
            {
                ProjectileMover mover = vfx.GetComponent<ProjectileMover>();

                if (mover != null)
                {
                    mover.SetTarget(lastKnownEnemyPosition + Vector3.up * 0.5f);
                }
                else
                {
                    Debug.LogWarning("Le Mage attaque, mais son prefab de particule n'a pas le script 'ProjectileMover' !");
                }
            }
            Destroy(vfx, particleLifetime);
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
        lastKnownEnemyPosition = targetPosition;

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