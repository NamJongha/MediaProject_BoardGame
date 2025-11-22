using UnityEngine;

namespace Manager.ItemStrategy
{
    public class RopeGunItem : IItemStrategy
    {
        private int alpha = 4;
        
        //image should be in Resoucres/Image/Item folder, and should be sprite type
        private string imagePath = "Image/Item/RopeGunItem";
        private Sprite itemSprite;
        private string itemName = "RopeGunItem";

        public RopeGunItem()
        {
            //itemSprite = Resources.Load<Sprite>(imagePath);
            //Debug.Assert(itemSprite != null);
        }
        
        public void UseItem(Player target)
        {
            LogManager.Instance.Log($"Player {target.playerName} used rope gun");
            //move player dice number + alpha(change this alpha value for balance)
            
            //Two ways to implement this item
            // a: add condition bool usedRopeGun to player, and if this item used, make the condition true.
            //    If the condition is true, plus alpha number to player dice number after the player roll the dice.
            
            // b: plus alpha number to player dice number directly.
            //    If we choose this way, we have to think about when does the player dice number is set.
            //    The time when the item is used and the dice number is set are different. (use item -> roll(set) dice number)
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