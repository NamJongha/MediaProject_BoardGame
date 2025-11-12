using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ItemUIManager : MonoBehaviour
{
    [SerializeField] private List<Button> itemButtons;
    [SerializeField] private TargetSelectUIManager targetUI;
    private Player currentPlayer;
    private IItemStrategy selectedItem;
    public static ItemUIManager Instance { get; private set; }

    private void Awake()
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

        //플레이어가 소유하고 있는 아이템에 따라 버튼 이미지 및 효과 변경
        for (int i = 0; i < itemButtons.Count; i++)
        {
            //플레이어 소유 아이템 갯수 이상의 버튼은 비활성화
            if (i >= playerItemList.Count)
            {
                itemButtons[i].gameObject.SetActive(false);
                continue;
            }
            
            Sprite ItemImage = playerItemList[i].GetItemSprite(); //GetItemSprite는 Strategy에 정의되어있음
            itemButtons[i].GetComponent<Image>().sprite = ItemImage;
            itemButtons[i].gameObject.SetActive(true);
            
            itemButtons[i].onClick.RemoveAllListeners();
            itemButtons[i].onClick.AddListener(() => OnItemButtonClicked(playerItemList[i]));
        }
    }

    private void OnItemButtonClicked(IItemStrategy item)
    {
        targetUI.OnTargetSelected -= HandleTargetSelected;
        targetUI.OnTargetSelected += HandleTargetSelected;
        
        //타겟 리스트 표시, 현재 플레이어를 제외하고 표시
        targetUI.ShowTargetList(currentPlayer);
        
        selectedItem = item;

        //아이템 선택 시 아이템 선택 버튼 비활성화
        foreach (Button button in itemButtons)
        {
            button.gameObject.SetActive(false);
        }
    }

    private void HandleTargetSelected(Player targetPlayer)
    {
        Debug.Assert(selectedItem != null);
        
        selectedItem.UseItem(targetPlayer);
        UpdateItemListUI();

        //아이템 효과가 한번만 나타나도록 이벤트 해제 및 selectedItem 비움
        targetUI.OnTargetSelected -= HandleTargetSelected;
        selectedItem = null;
    }
}
