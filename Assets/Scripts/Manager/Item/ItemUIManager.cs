using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

//attach this script to Player character
public class ItemUIManager : MonoBehaviour
{
    public static ItemUIManager Instance { get; private set; }
    [SerializeField] private List<Button> itemButtons;
    [SerializeField] private TargetSelectUIManager targetUI;
    private Player currentPlayer;

    private void Start()
    {
        Instance = this;
    }

    public void ShowItemList(Player player)
    {
        currentPlayer = player;
        UpdateItemListUI();
    }

    public void UpdateItemListUI()
    {
        List<IItemStrategy> playerItemList = currentPlayer.GetItemList();
        Debug.Log(playerItemList[0].GetItemName());

        for (int i = 0; i < itemButtons.Count; i++)
        {
            if (i >= playerItemList.Count)
            {
                itemButtons[i].gameObject.SetActive(false);
                continue;
            }
            
            //Sprite ItemImage = playerItemList[i].GetItemSprite();
            //itemButtons[i].GetComponent<Image>().sprite = ItemImage;
            itemButtons[i].gameObject.SetActive(true);
            itemButtons[i].onClick.RemoveAllListeners();

            int index = i;
            itemButtons[i].onClick.AddListener(() => OnItemButtonClicked(playerItemList[index]));
        }
    }

    private void OnItemButtonClicked(IItemStrategy item)
    {
        if (item.GetItemName() == "RecoverStaminaItem")
        {
            item.UseItem(currentPlayer);
        }

        else
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

        foreach (Button itemButton in itemButtons)
        {
            itemButton.gameObject.SetActive(false);
        }
    }
}
