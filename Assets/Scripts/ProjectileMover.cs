using UnityEngine;

public class ProjectileMover : MonoBehaviour
{
    [Header("Vitesse du Projectile")]
    public float speed = 5f;
    public bool rotateTowardsTarget = true;

    private Vector3 targetPosition;
    private bool hasTarget = false;

    public void SetTarget(Vector3 target)
    {
        targetPosition = target;
        hasTarget = true;

        if (rotateTowardsTarget)
        {
            transform.LookAt(targetPosition);
        }
    }

    void Update()
    {
        if (!hasTarget) return;

        float step = speed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            hasTarget = false;
        }
    }
}