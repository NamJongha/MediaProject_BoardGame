using UnityEngine;

public interface IItemStrategy
{
    bool RequiresTarget { get; }
    public string GetName();
    public void UseItem(Player target);
    public Sprite GetItemSprite();
    IItemStrategy Clone();
}
