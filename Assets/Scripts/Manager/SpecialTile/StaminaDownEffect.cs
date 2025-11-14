using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "StaminaDownEffect", menuName = "SpecialTile/Effects/StaminaDown")]
public class StaminaDownEffect : SpecialTileEffectBase
{
    [SerializeField] int amount = -10;

    public override void ApplyEffect(Player target)
    {
        if (target == null || !target.Object.HasStateAuthority) return;
        target.ModifyStamina(amount);
        LogManager.Instance.Log($"[SpecialTileEffect] {target.playerName} 스태미너 {amount}");
    }
}