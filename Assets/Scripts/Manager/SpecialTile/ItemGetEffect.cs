using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemGetEffect", menuName = "SpecialTile/Effects/ItemGet")]
public class ItemGetEffect : SpecialTileEffectBase
{
    public override void ApplyEffect(Player target)
    {
        if (target == null || !target.Object.HasStateAuthority) return;
        LogManager.Instance.Log($"[SpecialTileEffect] {target.playerName} 아이템 획득!");
    }
}   