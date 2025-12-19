using UnityEngine;
using Vuforia;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public enum GamePhase
{
    BonusTurnBlue,
    BonusTurnRed,
    ChampionSelect,
    Combat,
    Result
}

public class CardManager : MonoBehaviour
{
    [Header("Paramètres")]
    public float proximityThreshold = 0.15f;

    [Header("UI Vie")]
    public GameObject[] blueHearts;
    public GameObject[] redHearts;

    [Header("UI Inventaire Bonus (Fix 4)")]
    public TextMeshProUGUI blueInventoryText;
    public TextMeshProUGUI redInventoryText;

    [Header("UI Confirmation Bonus (Fix 5)")]
    public GameObject panelConfirmBonus;
    public TextMeshProUGUI confirmText;

    [Header("UI Jeu & Infos")]
    public GameObject panelPhaseInfo;
    public TextMeshProUGUI infoText;
    public GameObject btnSkipBonus;

    public GameObject panelRoundOver;
    public TextMeshProUGUI winnerText;

    public GameObject panelSameColorError;

    [Header("UI Fin de Partie")]
    public GameObject panelGameOver;
    public TextMeshProUGUI winnerTitleText;

    private GamePhase currentPhase;
    private int blueHealth = 3;
    private int redHealth = 3;
    private bool isGameOver = false;

    private int blueHealUses = 1;
    private int redHealUses = 1;
    private int blueBoostUses = 2;
    private int redBoostUses = 2;

    private bool blueBonusActive = false;
    private bool redBonusActive = false;

    private bool isWaitingForConfirmation = false;
    private BonusType pendingBonusType;

    private List<ObserverBehaviour> type1Cards = new List<ObserverBehaviour>();
    private List<ObserverBehaviour> type2Cards = new List<ObserverBehaviour>();
    private List<ObserverBehaviour> type3Cards = new List<ObserverBehaviour>();

    void Start()
    {
        if (panelRoundOver) panelRoundOver.SetActive(false);
        if (panelGameOver) panelGameOver.SetActive(false);
        if (panelPhaseInfo) panelPhaseInfo.SetActive(false);
        if (panelSameColorError) panelSameColorError.SetActive(false);
        if (panelConfirmBonus) panelConfirmBonus.SetActive(false);

        var allObservers = FindObjectsByType<ObserverBehaviour>(FindObjectsSortMode.None);
        foreach (var observer in allObservers)
        {
            if (observer.gameObject.CompareTag("Type 1")) type1Cards.Add(observer);
            else if (observer.gameObject.CompareTag("Type 2")) type2Cards.Add(observer);
            else if (observer.gameObject.CompareTag("Type 3")) type3Cards.Add(observer);
        }

        ResetGame();
    }

    void Update()
    {
        if (isGameOver) return;

        if (currentPhase == GamePhase.ChampionSelect)
        {
            if (CheckSameColorError()) return;
        }

        if (currentPhase == GamePhase.BonusTurnBlue || currentPhase == GamePhase.BonusTurnRed)
        {
            if (!isWaitingForConfirmation)
            {
                DetectBonusCardUsage();
            }
        }

        if (currentPhase == GamePhase.ChampionSelect || currentPhase == GamePhase.Combat)
        {
            HandleChampionCombat();
        }
    }

    private bool CheckSameColorError()
    {
        int blueCount = CountTrackedCards(type1Cards);
        int redCount = CountTrackedCards(type2Cards);

        if (blueCount >= 2 || redCount >= 2)
        {
            if (panelSameColorError) panelSameColorError.SetActive(true);
            return true;
        }
        else
        {
            if (panelSameColorError) panelSameColorError.SetActive(false);
            return false;
        }
    }

    private int CountTrackedCards(List<ObserverBehaviour> list)
    {
        int count = 0;
        foreach (var c in list)
        {
            if (c.TargetStatus.Status == Status.TRACKED || c.TargetStatus.Status == Status.EXTENDED_TRACKED) count++;
        }
        return count;
    }

    private void DetectBonusCardUsage()
    {
        ObserverBehaviour activeBonusCard = GetFirstTrackedCard(type3Cards);

        if (activeBonusCard != null)
        {
            var bonusCtrl = activeBonusCard.GetComponentInChildren<BonusCardController>();
            if (bonusCtrl != null)
            {
                PreCheckBonus(bonusCtrl.bonusType);
            }
        }
    }

    private void PreCheckBonus(BonusType type)
    {
        bool isBlueTurn = (currentPhase == GamePhase.BonusTurnBlue);
        bool hasStock = false;
        string bonusName = (type == BonusType.Heal) ? "Soin" : "Boost";

        if (isBlueTurn)
        {
            if (type == BonusType.Heal) hasStock = (blueHealUses > 0);
            else hasStock = (blueBoostUses > 0);
        }
        else
        {
            if (type == BonusType.Heal) hasStock = (redHealUses > 0);
            else hasStock = (redBoostUses > 0);
        }

        if (!hasStock)
        {
            StartCoroutine(ShowTemporaryMessage($"Plus de {bonusName} disponible !"));
            return;
        }

        OpenConfirmationPanel(type);
    }

    private void OpenConfirmationPanel(BonusType type)
    {
        isWaitingForConfirmation = true;
        pendingBonusType = type;

        string bName = (type == BonusType.Heal) ? "SOIN (+1 Vie)" : "BOOST (Dégâts x2)";
        if (confirmText) confirmText.text = $"Utiliser {bName} ?";
        if (panelConfirmBonus) panelConfirmBonus.SetActive(true);

        if (panelPhaseInfo) panelPhaseInfo.SetActive(false);
    }

    public void OnConfirmBonusYes()
    {
        CloseConfirmationPanel();

        bool isBlue = (currentPhase == GamePhase.BonusTurnBlue);

        if (ApplyBonusLogic(isBlue, pendingBonusType))
        {
            string color = isBlue ? "Bleu" : "Rouge";
            StartCoroutine(BonusValidatedSequence($"Bonus {color} Activé !"));
        }
    }

    public void OnConfirmBonusNo()
    {
        CloseConfirmationPanel();
        if (panelPhaseInfo) panelPhaseInfo.SetActive(true);
        StartCoroutine(PauseDetectionBriefly());
    }

    private void CloseConfirmationPanel()
    {
        if (panelConfirmBonus) panelConfirmBonus.SetActive(false);
    }

    IEnumerator PauseDetectionBriefly()
    {
        yield return new WaitForSeconds(1.0f);
        isWaitingForConfirmation = false;
    }

    private bool ApplyBonusLogic(bool isBlue, BonusType type)
    {
        if (isBlue)
        {
            if (type == BonusType.Heal && blueHealth < 3)
            {
                HealPlayer(true); blueHealUses--; return true;
            }
            else if (type == BonusType.DamageBoost && !blueBonusActive)
            {
                blueBonusActive = true; blueBoostUses--; return true;
            }
            else if (type == BonusType.Heal && blueHealth >= 3)
            {
                StartCoroutine(ShowTemporaryMessage("Vie déjà pleine !")); return false;
            }
        }
        else
        {
            if (type == BonusType.Heal && redHealth < 3)
            {
                HealPlayer(false); redHealUses--; return true;
            }
            else if (type == BonusType.DamageBoost && !redBonusActive)
            {
                redBonusActive = true; redBoostUses--; return true;
            }
            else if (type == BonusType.Heal && redHealth >= 3)
            {
                StartCoroutine(ShowTemporaryMessage("Vie déjà pleine !")); return false;
            }
        }
        return false;
    }

    IEnumerator ShowTemporaryMessage(string msg)
    {
        isWaitingForConfirmation = true;
        if (infoText) infoText.text = msg;
        yield return new WaitForSeconds(1.5f);

        if (currentPhase == GamePhase.BonusTurnBlue) UpdateInfoPanel("JOUEUR BLEU\nBonus ?", true);
        else UpdateInfoPanel("JOUEUR ROUGE\nBonus ?", true);

        isWaitingForConfirmation = false;
    }

    public void OnSkipBonusClicked()
    {
        if (currentPhase == GamePhase.BonusTurnBlue) StartRedBonusTurn();
        else if (currentPhase == GamePhase.BonusTurnRed) StartChampionPhase();
    }

    private void CheckGlobalStockAndStart()
    {
        UpdateInventoryUI();

        int totalStock = blueHealUses + blueBoostUses + redHealUses + redBoostUses;

        if (totalStock == 0)
        {
            Debug.Log("Plus de bonus disponibles : Passage direct au combat");
            StartChampionPhase();
        }
        else
        {
            StartBlueBonusTurn();
        }
    }

    private void StartBlueBonusTurn()
    {
        if (blueHealUses + blueBoostUses == 0)
        {
            StartRedBonusTurn();
            return;
        }

        currentPhase = GamePhase.BonusTurnBlue;
        isWaitingForConfirmation = false;
        UpdateInfoPanel("JOUEUR BLEU\nBonus ? (Montrez carte)", true);
    }

    private void StartRedBonusTurn()
    {
        if (redHealUses + redBoostUses == 0)
        {
            StartChampionPhase();
            return;
        }

        currentPhase = GamePhase.BonusTurnRed;
        isWaitingForConfirmation = false;
        UpdateInfoPanel("JOUEUR ROUGE\nBonus ? (Montrez carte)", true);
    }

    private void StartChampionPhase()
    {
        currentPhase = GamePhase.ChampionSelect;
        UpdateInfoPanel("PLACEZ VOS CHAMPIONS\n(Duel)", false);
    }

    IEnumerator BonusValidatedSequence(string message)
    {
        UpdateInventoryUI();
        GamePhase previousPhase = currentPhase;
        currentPhase = GamePhase.Result;

        UpdateInfoPanel(message, false);
        yield return new WaitForSeconds(2.0f);

        if (previousPhase == GamePhase.BonusTurnBlue) StartRedBonusTurn();
        else StartChampionPhase();
    }

    private void HandleChampionCombat()
    {
        ObserverBehaviour c1 = GetFirstTrackedCard(type1Cards);
        ObserverBehaviour c2 = GetFirstTrackedCard(type2Cards);

        if (c1 != null && c2 != null && currentPhase == GamePhase.ChampionSelect)
        {
            var ctrl1 = c1.GetComponentInChildren<BattleCharacterController>();
            var ctrl2 = c2.GetComponentInChildren<BattleCharacterController>();

            if (ctrl1 != null && ctrl2 != null)
            {
                ctrl1.FaceEnemy(ctrl2.transform.position);
                ctrl2.FaceEnemy(ctrl1.transform.position);

                if (Vector3.Distance(c1.transform.position, c2.transform.position) <= proximityThreshold)
                {
                    currentPhase = GamePhase.Combat;
                    ResolveCombatRound(ctrl1, ctrl2);
                }
            }
        }
    }

    private void ResolveCombatRound(BattleCharacterController p1, BattleCharacterController p2)
    {
        bool p1Wins = false; bool p2Wins = false;
        string resultMessage = ""; Color resultColor = Color.white;
        BattleCharacterController animatorSource = null;

        if (p1.unitClass == p2.unitClass)
        {
            p1.SetProximityActive(true); p2.SetProximityActive(true);
            resultMessage = "ÉGALITÉ !"; resultColor = Color.yellow;
            animatorSource = p1;
            blueBonusActive = false; redBonusActive = false;
        }
        else
        {
            if (p1.unitClass == UnitClass.Tank) { if (p2.unitClass == UnitClass.Knight) p1Wins = true; else p2Wins = true; }
            else if (p1.unitClass == UnitClass.Knight) { if (p2.unitClass == UnitClass.Mage) p1Wins = true; else p2Wins = true; }
            else if (p1.unitClass == UnitClass.Mage) { if (p2.unitClass == UnitClass.Tank) p1Wins = true; else p2Wins = true; }

            if (p1Wins)
            {
                p1.SetProximityActive(true); p2.SetProximityActive(false);
                resultMessage = "VICTOIRE BLEUE !"; resultColor = Color.cyan;
                animatorSource = p1;
                int dmg = blueBonusActive ? 2 : 1;
                TakeDamage(false, dmg);
            }
            else if (p2Wins)
            {
                p1.SetProximityActive(false); p2.SetProximityActive(true);
                resultMessage = "VICTOIRE ROUGE !"; resultColor = new Color(1f, 0.4f, 0.4f);
                animatorSource = p2;
                int dmg = redBonusActive ? 2 : 1;
                TakeDamage(true, dmg);
            }
            blueBonusActive = false; redBonusActive = false;
        }

        StartCoroutine(ShowResultDynamicDelay(animatorSource, resultMessage, resultColor));
    }

    IEnumerator ShowResultDynamicDelay(BattleCharacterController source, string message, Color color)
    {
        if (panelPhaseInfo) panelPhaseInfo.SetActive(false);
        float waitDuration = 2.0f;
        if (source != null && source.animator != null)
        {
            yield return new WaitForSeconds(0.2f);
            waitDuration = source.animator.GetCurrentAnimatorStateInfo(0).length;
        }
        else yield return new WaitForSeconds(0.2f);

        if (isGameOver) yield break;

        yield return new WaitForSeconds(waitDuration - 0.2f);
        if (source != null) source.SetProximityActive(false);

        if (winnerText != null) { winnerText.text = message + "\nRetirez les cartes"; winnerText.color = color; }
        if (panelRoundOver != null) panelRoundOver.SetActive(true);

        yield return new WaitUntil(() => GetFirstTrackedCard(type1Cards) == null && GetFirstTrackedCard(type2Cards) == null);

        if (panelRoundOver) panelRoundOver.SetActive(false);

        if (!isGameOver)
        {
            CheckGlobalStockAndStart();
        }
    }

    private void UpdateInventoryUI()
    {
        if (blueInventoryText) blueInventoryText.text = $"Soins: {blueHealUses} | Boosts: {blueBoostUses}";
        if (redInventoryText) redInventoryText.text = $"Soins: {redHealUses} | Boosts: {redBoostUses}";
    }

    private void UpdateInfoPanel(string text, bool showSkipButton)
    {
        if (panelPhaseInfo) panelPhaseInfo.SetActive(true);
        if (infoText) infoText.text = text;
        if (btnSkipBonus) btnSkipBonus.SetActive(showSkipButton);
    }

    private void HealPlayer(bool isBlue)
    {
        if (isBlue) { blueHealth++; UpdateHeartsUI(blueHearts, blueHealth); }
        else { redHealth++; UpdateHeartsUI(redHearts, redHealth); }
    }

    private void TakeDamage(bool isBlueTeam, int amount)
    {
        if (isBlueTeam)
        {
            blueHealth -= amount;
            if (blueHealth < 0) blueHealth = 0;
            UpdateHeartsUI(blueHearts, blueHealth);
            if (blueHealth == 0) StartCoroutine(ShowGameOverSequence(false));
        }
        else
        {
            redHealth -= amount;
            if (redHealth < 0) redHealth = 0;
            UpdateHeartsUI(redHearts, redHealth);
            if (redHealth == 0) StartCoroutine(ShowGameOverSequence(true));
        }
    }

    private void ResetGame()
    {
        blueHealth = 3; redHealth = 3;
        blueHealUses = 1; redHealUses = 1;
        blueBoostUses = 2; redBoostUses = 2;
        blueBonusActive = false; redBonusActive = false;

        UpdateHeartsUI(blueHearts, 3);
        UpdateHeartsUI(redHearts, 3);

        isGameOver = false;
        if (panelGameOver) panelGameOver.SetActive(false);

        CheckGlobalStockAndStart();
    }

    private ObserverBehaviour GetFirstTrackedCard(List<ObserverBehaviour> list)
    {
        foreach (var c in list) if (c.TargetStatus.Status == Status.TRACKED || c.TargetStatus.Status == Status.EXTENDED_TRACKED) return c;
        return null;
    }
    private void UpdateHeartsUI(GameObject[] heartsArray, int currentHealth)
    {
        for (int i = 0; i < heartsArray.Length; i++) heartsArray[i].SetActive(i < currentHealth);
    }
    IEnumerator ShowGameOverSequence(bool blueWonGame)
    {
        currentPhase = GamePhase.Result; isGameOver = true;
        yield return new WaitForSeconds(1.0f);
        if (panelRoundOver) panelRoundOver.SetActive(false);
        if (panelPhaseInfo) panelPhaseInfo.SetActive(false);
        if (blueWonGame) { winnerTitleText.text = "BLEU GAGNE !"; winnerTitleText.color = Color.cyan; }
        else { winnerTitleText.text = "ROUGE GAGNE !"; winnerTitleText.color = new Color(1f, 0.4f, 0.4f); }
        if (panelGameOver) panelGameOver.SetActive(true);
    }
    public void OnRestartBtnClicked() => ResetGame();
    public void OnQuitBtnClicked() => Application.Quit();
}