using System.Collections.Generic;
using UnityEngine;
using Manager.ItemStrategy;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;

    private Dictionary<ItemType, IItemStrategy> itemFactory = new Dictionary<ItemType, IItemStrategy>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeFactory();
    }

    private void InitializeFactory()
    {
        string recoverIconPath = "Image/Item/RecoverStaminaItem";
        string ropeGunIconPath = "Image/Item/RopeGunItem";
        
        Sprite recoverIcon = Resources.Load<Sprite>(recoverIconPath);
        Sprite ropeGunIcon = Resources.Load<Sprite>(ropeGunIconPath);

        var icon = Resources.Load<Sprite>(recoverIconPath);
        Debug.Log($"[DEBUG] Try load rope icon from {recoverIconPath} → Result = {icon}");
        
        if (recoverIcon == null)
            Debug.LogError("[ItemManager] Recover icon not found! Path = " + recoverIconPath);
        if (ropeGunIcon == null)
            Debug.LogError("[ItemManager] RopeGun icon not found! Path = " + ropeGunIconPath);

        // 아이템 원본 생성 (팩토리 초기화)
        itemFactory[ItemType.Recover] = new RecoverStaminaItem(recoverIcon);
        itemFactory[ItemType.RopeGun] = new RopeGunItem(ropeGunIcon);

        Debug.Log("[ItemManager] Item factory initialized");
    }

    // 아이템 복제 생성
    private IItemStrategy CreateItem(ItemType type)
    {
        if (!itemFactory.ContainsKey(type))
        {
            Debug.LogError($"[ItemManager] Unknown ItemType: {type}");
            return null;
        }

        return itemFactory[type].Clone();
    }

    // 플레이어에게 아이템 전달
    public void GiveRandomItemToPlayer(Player player)
    {
        // ItemType이 Enum이므로 전체 항목 목록 얻기
        ItemType[] itemTypes = (ItemType[])System.Enum.GetValues(typeof(ItemType));

        if (itemTypes.Length == 0)
        {
            Debug.LogError("[ItemManager] No ItemTypes available!");
            return;
        }

        // 랜덤으로 타입을 하나 뽑음
        ItemType randomType = itemTypes[UnityEngine.Random.Range(0, itemTypes.Length)];

        // 아이템 생성
        IItemStrategy item = CreateItem(randomType);

        if (item == null)
        {
            Debug.LogError("[ItemManager] Failed to create random item.");
            return;
        }

        // 플레이어 인벤토리에 추가
        player.AddItem(item);

        LogManager.Instance.Log($"[ItemManager] Randomly gave {randomType} to player {player.playerName}");
    }

}

public enum ItemType
{
    Recover,
    RopeGun
}
