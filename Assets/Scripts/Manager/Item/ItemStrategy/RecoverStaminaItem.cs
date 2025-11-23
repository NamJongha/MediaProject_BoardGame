using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace Manager.ItemStrategy
{
    public class RecoverStaminaItem : IItemStrategy
    {   
        //image should be in Resoucres/Image/Item folder, and should be sprite type
        private Sprite icon;
        public string GetName() => "Recover Stamina";
        public bool RequiresTarget => false;

        public RecoverStaminaItem(Sprite sprite)
        {
            icon = sprite;
        }

        public void UseItem(Player target)
        {
            LogManager.Instance.Log($"Player {target.playerName} recovered stamina");
            target.ModifyStamina(+4);
        }

        public Sprite GetItemSprite() => icon;
        
        public IItemStrategy Clone()
        {
            return new RecoverStaminaItem(icon);
        }
    }
}