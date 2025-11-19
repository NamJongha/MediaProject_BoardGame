using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "StaminaUpEffect", menuName = "SpecialTile/Effects/StaminaUp")]
public class StaminaUpEffect : SpecialTileEffectBase
{
    [SerializeField] int amount = 10;

    public override void ApplyEffect(Player target)
    {
        if (target == null || !target.Object.HasStateAuthority) return;
        target.ModifyStamina(amount);
        LogManager.Instance.Log($"[SpecialTileEffect] {target.playerName} stamina +{amount}");
    }
}