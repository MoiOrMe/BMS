using UnityEngine;
using Vuforia;
using System.Collections.Generic;
using System.Linq;

public class CardManager : MonoBehaviour
{
    public int blueLayer = 8;
    public int redLayer = 9;

    private const float PROXIMITY_THRESHOLD = 0.05f;

    private bool wasInProximity = false;

    private List<ObserverBehaviour> allObservers = new List<ObserverBehaviour>();

    private Dictionary<ObserverBehaviour, float> firstDetectionTime = new Dictionary<ObserverBehaviour, float>();

    void Start()
    {
        allObservers = FindObjectsByType<ObserverBehaviour>(FindObjectsSortMode.None).ToList();

        Debug.Log($"[CardManager] Surveillance démarrée pour {allObservers.Count} cartes.");
    }

    void Update()
    {
        foreach (var observer in allObservers)
        {
            if (observer.TargetStatus.Status == Status.TRACKED)
            {
                if (!firstDetectionTime.ContainsKey(observer))
                {
                    firstDetectionTime.Add(observer, Time.time);
                    Debug.Log($"[Distance] {observer.gameObject.name} détectée pour la première fois à {Time.time:F2}s.");
                }
            }
        }

        CalculateCardDistance();
    }

    private void CalculateCardDistance()
    {
        var detectedCards = firstDetectionTime.Keys.ToList();

        if (detectedCards.Count < 2)
        {
            if (wasInProximity)
            {
                ActivateProximityAnimations(null, null, false);
                wasInProximity = false;
            }
            return;
        }

        var orderedCards = detectedCards
            .OrderBy(o => firstDetectionTime[o])
            .Take(2)
            .ToList();

        ObserverBehaviour card1 = orderedCards[0];
        ObserverBehaviour card2 = orderedCards[1];

        bool currentlyTracked = card1.TargetStatus.Status == Status.TRACKED && card2.TargetStatus.Status == Status.TRACKED;

        if (currentlyTracked)
        {
            float distance = Vector3.Distance(card1.transform.position, card2.transform.position);

            Debug.Log($"[Distance] Distance entre **{card1.gameObject.name}** et **{card2.gameObject.name}** : **{distance:F3} mètres**.");

            bool isInProximity = distance <= PROXIMITY_THRESHOLD;

            if (isInProximity != wasInProximity)
            {
                ActivateProximityAnimations(card1, card2, isInProximity);
                wasInProximity = isInProximity;
            }
        }
        else if (wasInProximity)
        {
            ActivateProximityAnimations(null, null, false);
            wasInProximity = false;
        }
    }

    private void ActivateProximityAnimations(ObserverBehaviour c1, ObserverBehaviour c2, bool state)
    {
        if (c1 != null && c1.transform.childCount > 0)
        {
            GameObject modelChild = c1.transform.GetChild(0).gameObject;

            modelChild.SendMessage("SetProximityActive", state, SendMessageOptions.DontRequireReceiver);
        }

        if (c2 != null && c2.transform.childCount > 0)
        {
            GameObject modelChild = c2.transform.GetChild(0).gameObject;
            modelChild.SendMessage("SetProximityActive", state, SendMessageOptions.DontRequireReceiver);
        }

        Debug.Log($"[Animation] Proximité : {state}. Animations activées/désactivées.");
    }
}