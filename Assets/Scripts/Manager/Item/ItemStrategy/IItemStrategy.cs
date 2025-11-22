using UnityEngine;

public interface IItemStrategy
{
    public void UseItem(Player target);
    public Sprite GetItemSprite();
    public string GetItemName();
}
