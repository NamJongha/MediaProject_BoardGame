using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemGetEffect", menuName = "SpecialTile/Effects/ItemGet")]
public class ItemGetEffect : SpecialTileEffectBase
{
    public override void ApplyEffect(Player target)
    {
        if (ItemManager.Instance == null)
        {
            Debug.LogError("[ItemGetEffect] ItemManager.Instance가 없습니다!");
            return;
        }

        ItemManager.Instance.GiveRandomItemToPlayer(target);
    }

}   