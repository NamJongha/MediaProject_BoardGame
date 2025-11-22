using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace Manager.ItemStrategy
{
    public class RecoverStaminaItem : IItemStrategy
    {
        //image should be in Resoucres/Image/Item folder, and should be sprite type
        private string imagePath = "Image/Item/RecoverStaminaItem";
        private Sprite itemSprite;
        private string itemName = "RecoverStaminaItem";

        public RecoverStaminaItem()
        {
            //itemSprite = Resources.Load<Sprite>(imagePath);
            //Debug.Assert(itemSprite != null);
        }
        
        public void UseItem(Player target)
        {
            LogManager.Instance.Log($"Player {target.playerName} recovered stamina");
            target.ModifyStamina(4);
        }

        public Sprite GetItemSprite()
        {
            return itemSprite;
        }

        public string GetItemName()
        {
            return itemName;
        }
    }
}