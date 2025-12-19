using UnityEngine;

public enum BonusType
{
    Heal,
    DamageBoost
}

public class BonusCardController : MonoBehaviour
{
    [Header("Type de Bonus")]
    public BonusType bonusType;
}