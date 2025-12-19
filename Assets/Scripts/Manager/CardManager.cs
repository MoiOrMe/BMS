using UnityEngine;
using Vuforia;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class CardManager : MonoBehaviour
{
    [Header("Paramètres")]
    public float proximityThreshold = 0.15f;

    [Header("Interface de Vie (UI)")]
    public GameObject[] blueHearts;
    public GameObject[] redHearts;

    [Header("Interface de Jeu (Messages)")]
    public GameObject panelRoundOver;
    public TextMeshProUGUI winnerText;
    public GameObject panelNewRound;
    public GameObject panelSameColorError;

    [Header("Interface Fin de Partie")]
    public GameObject panelGameOver;
    public TextMeshProUGUI winnerTitleText;

    private int blueHealth = 3;
    private int redHealth = 3;
    private bool roundTermine = false;
    private bool isGameOver = false;

    private List<ObserverBehaviour> type1Cards = new List<ObserverBehaviour>();
    private List<ObserverBehaviour> type2Cards = new List<ObserverBehaviour>();

    void Start()
    {
        if (panelRoundOver) panelRoundOver.SetActive(false);
        if (panelNewRound) panelNewRound.SetActive(false);
        if (panelGameOver) panelGameOver.SetActive(false);
        if (panelSameColorError) panelSameColorError.SetActive(false);

        ResetGame();

        var allObservers = FindObjectsByType<ObserverBehaviour>(FindObjectsSortMode.None);
        foreach (var observer in allObservers)
        {
            if (observer.gameObject.CompareTag("Type 1")) type1Cards.Add(observer);
            else if (observer.gameObject.CompareTag("Type 2")) type2Cards.Add(observer);
        }

        ShowNewRoundMessage();
    }

    void Update()
    {
        if (isGameOver) return;

        // --- DÉTECTION DES ERREURS ---
        int activeBlueCount = CountTrackedCards(type1Cards);
        int activeRedCount = CountTrackedCards(type2Cards);

        if (activeBlueCount >= 2 || activeRedCount >= 2)
        {
            if (panelSameColorError) panelSameColorError.SetActive(true);

            if (panelNewRound) panelNewRound.SetActive(false);

            return;
        }
        else
        {
            if (panelSameColorError) panelSameColorError.SetActive(false);
        }

        ObserverBehaviour c1 = GetFirstTrackedCard(type1Cards);
        ObserverBehaviour c2 = GetFirstTrackedCard(type2Cards);

        // --- RESET ---
        if (c1 == null && c2 == null)
        {
            if (roundTermine == true)
            {
                roundTermine = false;
                StopAllAttacks();

                if (panelRoundOver) panelRoundOver.SetActive(false);
                ShowNewRoundMessage();
            }
        }

        // --- COMBAT ---
        if (c1 != null && c2 != null)
        {
            var ctrl1 = c1.GetComponentInChildren<BattleCharacterController>();
            var ctrl2 = c2.GetComponentInChildren<BattleCharacterController>();

            if (ctrl1 != null && ctrl2 != null)
            {
                ctrl1.FaceEnemy(ctrl2.transform.position);
                ctrl2.FaceEnemy(ctrl1.transform.position);

                float distance = Vector3.Distance(c1.transform.position, c2.transform.position);
                bool inRange = distance <= proximityThreshold;

                if (inRange)
                {
                    if (roundTermine == false)
                    {
                        roundTermine = true;
                        ResolveCombatRound(ctrl1, ctrl2);
                    }
                }
                else
                {
                    if (roundTermine == false)
                    {
                        ctrl1.SetProximityActive(false);
                        ctrl2.SetProximityActive(false);
                    }
                }
            }
        }
    }
    private int CountTrackedCards(List<ObserverBehaviour> list)
    {
        int count = 0;
        foreach (var card in list)
        {
            if (card.TargetStatus.Status == Status.TRACKED ||
                card.TargetStatus.Status == Status.EXTENDED_TRACKED)
            {
                count++;
            }
        }
        return count;
    }


    private void ResolveCombatRound(BattleCharacterController p1, BattleCharacterController p2)
    {
        bool p1Wins = false;
        bool p2Wins = false;
        string resultMessage = "";
        Color resultColor = Color.white;
        BattleCharacterController animatorSource = null;

        if (p1.unitClass == p2.unitClass)
        {
            p1.SetProximityActive(true); p2.SetProximityActive(true);
            resultMessage = "ÉGALITÉ !"; resultColor = Color.yellow;
            animatorSource = p1;
        }
        else
        {
            if (p1.unitClass == UnitClass.Tank)
            {
                if (p2.unitClass == UnitClass.Knight) p1Wins = true;
                else if (p2.unitClass == UnitClass.Mage) p2Wins = true;
            }
            else if (p1.unitClass == UnitClass.Knight)
            {
                if (p2.unitClass == UnitClass.Mage) p1Wins = true;
                else if (p2.unitClass == UnitClass.Tank) p2Wins = true;
            }
            else if (p1.unitClass == UnitClass.Mage)
            {
                if (p2.unitClass == UnitClass.Tank) p1Wins = true;
                else if (p2.unitClass == UnitClass.Knight) p2Wins = true;
            }

            if (p1Wins)
            {
                p1.SetProximityActive(true); p2.SetProximityActive(false);
                resultMessage = "VICTOIRE BLEUE !"; resultColor = Color.cyan;
                animatorSource = p1;
                TakeDamage(false);
            }
            else if (p2Wins)
            {
                p1.SetProximityActive(false); p2.SetProximityActive(true);
                resultMessage = "VICTOIRE ROUGE !"; resultColor = new Color(1f, 0.4f, 0.4f);
                animatorSource = p2;
                TakeDamage(true);
            }
        }
        if (!isGameOver)
        {
            StartCoroutine(ShowResultDynamicDelay(animatorSource, resultMessage, resultColor));
        }
    }

    IEnumerator ShowResultDynamicDelay(BattleCharacterController source, string message, Color color)
    {
        float waitDuration = 2.0f;
        if (source != null && source.animator != null)
        {
            yield return new WaitForSeconds(0.2f);
            AnimatorStateInfo stateInfo = source.animator.GetCurrentAnimatorStateInfo(0);
            waitDuration = stateInfo.length;
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        if (isGameOver) yield break;

        yield return new WaitForSeconds(waitDuration - 0.2f);

        if (source != null) source.SetProximityActive(false);

        if (winnerText != null)
        {
            winnerText.text = message + "\nRetirez les cartes";
            winnerText.color = color;
        }
        if (panelRoundOver != null) panelRoundOver.SetActive(true);
    }

    private void TakeDamage(bool isBlueTeam)
    {
        if (isBlueTeam)
        {
            blueHealth--;
            UpdateHeartsUI(blueHearts, blueHealth);
            if (blueHealth <= 0)
            {
                StartCoroutine(ShowGameOverSequence(false));
            }
        }
        else
        {
            redHealth--;
            UpdateHeartsUI(redHearts, redHealth);
            if (redHealth <= 0)
            {
                StartCoroutine(ShowGameOverSequence(true));
            }
        }
    }

    IEnumerator ShowGameOverSequence(bool blueWonGame)
    {
        isGameOver = true;

        yield return new WaitForSeconds(1.0f);

        StopAllAttacks();

        if (panelRoundOver) panelRoundOver.SetActive(false);
        if (panelNewRound) panelNewRound.SetActive(false);

        if (blueWonGame)
        {
            winnerTitleText.text = "BLEU GAGNE !";
            winnerTitleText.color = Color.cyan;
        }
        else
        {
            winnerTitleText.text = "ROUGE GAGNE !";
            winnerTitleText.color = new Color(1f, 0.4f, 0.4f);
        }

        if (panelGameOver) panelGameOver.SetActive(true);
    }

    public void OnRestartBtnClicked()
    {
        if (panelGameOver) panelGameOver.SetActive(false);
        ResetGame();
    }

    public void OnQuitBtnClicked()
    {
        Debug.Log("Quitter le jeu...");
        Application.Quit();
    }

    private void ShowNewRoundMessage()
    {
        if (panelNewRound != null && !isGameOver)
        {
            panelNewRound.SetActive(true);
            CancelInvoke("HideNewRoundPanel");
            Invoke("HideNewRoundPanel", 2.0f);
        }
    }

    private void HideNewRoundPanel()
    {
        if (panelNewRound != null) panelNewRound.SetActive(false);
    }

    private void StopAllAttacks()
    {
        foreach (var card in type1Cards)
            card.GetComponentInChildren<BattleCharacterController>()?.SetProximityActive(false);
        foreach (var card in type2Cards)
            card.GetComponentInChildren<BattleCharacterController>()?.SetProximityActive(false);
    }

    private void UpdateHeartsUI(GameObject[] heartsArray, int currentHealth)
    {
        for (int i = 0; i < heartsArray.Length; i++)
        {
            heartsArray[i].SetActive(i < currentHealth);
        }
    }

    private void ResetGame()
    {
        Debug.Log("Reset du jeu");
        blueHealth = 3;
        redHealth = 3;
        UpdateHeartsUI(blueHearts, 3);
        UpdateHeartsUI(redHearts, 3);
        roundTermine = false;
        isGameOver = false;

        if (panelRoundOver) panelRoundOver.SetActive(false);
        ShowNewRoundMessage();
    }

    private ObserverBehaviour GetFirstTrackedCard(List<ObserverBehaviour> list)
    {
        foreach (var card in list)
        {
            if (card.TargetStatus.Status == Status.TRACKED ||
                card.TargetStatus.Status == Status.EXTENDED_TRACKED) return card;
        }
        return null;
    }
}