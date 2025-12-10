using UnityEngine;
using Vuforia;

public class markDist : MonoBehaviour
{
    public Vector3 delta;
    public bool goIn = false;

    void Update()
    {
        var trackableImage = GetComponent<ObserverBehaviour>();
        var statusImage = trackableImage.TargetStatus.Status;
        if (statusImage == Status.TRACKED)
        {
            goIn = true;
            delta = Camera.main.transform.position - transform.position;
        }
        else
        {
            goIn = false;
        }
    }
}