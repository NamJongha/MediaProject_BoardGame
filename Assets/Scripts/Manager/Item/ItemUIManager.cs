using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ItemUIManager : MonoBehaviour
{
    [SerializeField] private List<Button> itemButtons;
    [SerializeField] private TargetSelectUIManager targetUI;
    private Player currentPlayer;
    
    public void ShowItemList(Player player)
    {
        currentPlayer = player;
        UpdateItemListUI();
    }

    public void UpdateItemListUI()
    {
        List<IItemStrategy> playerItemList = currentPlayer.GetItemList();

        for (int i = 0; i < itemButtons.Count; i++)
        {
            Sprite ItemImage = playerItemList[i].GetItemSprite();
            itemButtons[i].GetComponent<Image>().sprite = ItemImage;
            itemButtons[i].gameObject.SetActive(true);
            itemButtons[i].onClick.RemoveAllListeners();
            itemButtons[i].onClick.AddListener(() => OnItemButtonClicked(playerItemList[i]));

            if (i > playerItemList.Count)
            {
                itemButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnItemButtonClicked(IItemStrategy item)
    {
        //Show target list (button, show player's name), except item used player
        targetUI.ShowTargetList(currentPlayer);
        Player targetPlayer = new Player();
        //player with chosen name will be the target
        targetPlayer = targetUI.GetTargetPlayer();
        item.UseItem(targetPlayer);
        UpdateItemListUI();
        targetUI.ResetTargetPlayer();
    }
}
