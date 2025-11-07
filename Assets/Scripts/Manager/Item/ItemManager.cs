using System;
using System.Collections.Generic;
using Manager.ItemStrategy;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private List<IItemStrategy> itemList = new List<IItemStrategy>();
    private IItemStrategy recoverStaminaItem = new RecoverStaminaItem();
    private IItemStrategy ropeGunItem = new RopeGunItem();

    private void Awake()
    {
        itemList.Add(recoverStaminaItem);
        itemList.Add(ropeGunItem);
    }

    public IItemStrategy GetRandomItem()
    {
        int itemIndex = UnityEngine.Random.Range(0, itemList.Count);
        return itemList[itemIndex];
    }
}