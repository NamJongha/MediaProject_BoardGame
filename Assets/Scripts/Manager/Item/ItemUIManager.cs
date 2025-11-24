using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class ItemUIManager : MonoBehaviour
{
    [SerializeField] private GameObject panelItemUI;
    [SerializeField] private Button[] itemButtons;  // 슬롯 3개
    [SerializeField] private Button closeButton;
    [SerializeField] private TargetSelectUIManager targetUI;
    [SerializeField] private TMP_Text noItemText;
    private Player currentPlayer;
    private Player currentOwner;
    //private PlayerRef ownerRef;
    
    private NetworkRunner runner;
    
    public static ItemUIManager Instance;
    
    private void Awake()
    {
        panelItemUI.SetActive(false);
        closeButton.onClick.RemoveAllListeners();
        
        closeButton.onClick.AddListener(() =>
        {
            if (currentPlayer != null)
            {
                currentPlayer.RequestCloseItemUI();   // ← Player → RPC → UI 닫기
            }
        });

        Instance = this;
    }
    
    public void ShowItemList(Player ownerPlayer/*, PlayerRef ownerRef*/)
    {
        panelItemUI.SetActive(true);

        currentOwner = ownerPlayer;  // UI 주인 저장
        //this.ownerRef = ownerRef;    // LocalPlayer와 비교할 때 사용
        currentPlayer = ownerPlayer; // UpdateItemListUI()에서 사용하는 값
        UpdateItemListUI();
        //RefreshButtons(ownerPlayer);
    }

    public void UpdateItemListUI()
    {
        List<IItemStrategy> playerItemList = currentPlayer.GetItemList();

        foreach (var b in itemButtons)
            b.gameObject.SetActive(false);

        if (playerItemList.Count == 0)
        {
            // 아이템 없음 표시 (임시 버튼 재활용 or 텍스트)
            noItemText.gameObject.SetActive(true);
            noItemText.text = "사용 가능한 아이템이 없습니다";
            return;
        }

        noItemText.gameObject.SetActive(false);
        
        for (int i = 0; i < itemButtons.Length; i++)
        {
            if (i >= playerItemList.Count)
            {
                itemButtons[i].gameObject.SetActive(false);
                continue;
            }
            
            Sprite ItemImage = playerItemList[i].GetItemSprite();
            itemButtons[i].GetComponent<Image>().sprite = ItemImage;
            itemButtons[i].gameObject.SetActive(true);
            itemButtons[i].onClick.RemoveAllListeners();
            
            int index = i;
            itemButtons[i].onClick.AddListener(() => OnItemButtonClicked(playerItemList[index]));
        }
    }

    private void OnItemButtonClicked(IItemStrategy item)
    {
        if (item.GetName() == "Recover Stamina")
        {
            item.UseItem(currentPlayer);
        }
        
        //Show target list (button, show player's name), except item used player
        
        //targetUI.ShowTargetList(currentPlayer);
        //
        //Player targetPlayer = targetUI.GetTargetPlayer();
        //if (targetPlayer == null)
        //{
        //    Debug.LogWarning("Target not selected!");
        //    return;
        //}
        //item.UseItem(targetPlayer);
        //UpdateItemListUI();
        //targetUI.ResetTargetPlayer();
        //
        //foreach (Button itemButton in itemButtons)
        //{
        //    itemButton.gameObject.SetActive(false);
        //}
    }
    
    public void CloseUI()
    {
        panelItemUI.SetActive(false);
    }
    
    //public void RefreshButtons(Player ownerPlayer)
    //{
    //    var items = ownerPlayer.GetItemList();
    //    var runner = FindFirstObjectByType<NetworkRunner>();
    //    
    //    // 🟩 Runner null 여부 먼저 확인
    //    bool isOwner = false;
    //    if (runner != null)
    //    {
    //        isOwner = (runner.LocalPlayer == ownerRef);
    //    }
//
    //    for (int i = 0; i < itemButtons.Length; i++)
    //    {
    //        if (i < items.Count)
    //        {
    //            itemButtons[i].gameObject.SetActive(true);
    //            itemButtons[i].GetComponent<Image>().sprite = items[i].GetItemSprite();
//
    //            Button btn = itemButtons[i];
//
    //            // 🟩 네트워크 준비되지 않으면 interactable = false
    //            btn.interactable = isOwner;
//
    //            int index = i;
    //            btn.onClick.RemoveAllListeners();
//
    //            if (isOwner)
    //            {
    //                btn.onClick.AddListener(() =>
    //                {
    //                    ownerPlayer.RequestUseItem(index);
    //                });
    //            }
    //        }
    //        else
    //        {
    //            itemButtons[i].gameObject.SetActive(false);
    //        }
    //    }
    //}
//
}
