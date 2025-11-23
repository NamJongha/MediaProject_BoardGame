using UnityEngine;

namespace Manager.ItemStrategy
{
    public class RopeGunItem : IItemStrategy
    {
        private int alpha = 4;
        private Sprite icon;
        public string GetName() => "Rope Gun";
        public bool RequiresTarget => false;

        public RopeGunItem(Sprite sprite)
        {
            icon = sprite;
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
            target.AddDiceBonus(alpha);
        }

        public Sprite GetItemSprite() => icon;
        
        public IItemStrategy Clone()
        {
            return new RopeGunItem(icon);
        }
    }
}