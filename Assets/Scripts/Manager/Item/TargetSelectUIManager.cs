using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetSelectUIManager : MonoBehaviour
{
    [SerializeField] private List<Button> targetListButtons;
    private Dictionary<PlayerRef, NetworkObject> playerList;
    private Player currentPlayer;
    
    public event Action<Player> OnTargetSelected;

    private void Awake()
    {
        playerList = GameManager.Instance.GetPlayersList();
        
        foreach (Button button in targetListButtons)
        {
            button.gameObject.SetActive(false);
        }
    }

    private void UpdateTargetList(Player curPlayer)
    {
        Debug.Assert(playerList.Count <= 4 && playerList != null);
        
        foreach (Button button in targetListButtons)
        {
            button.onClick.RemoveAllListeners();
        }
        
        currentPlayer = curPlayer;
        int buttonIndex = 0;
        
        foreach (KeyValuePair<PlayerRef, NetworkObject> playerKVP in playerList)
        {
            Player player = playerKVP.Value.GetComponent<Player>();
            
            if (player.playerName == currentPlayer.playerName)
            {
                continue;
            }
            
            Debug.Assert(buttonIndex < targetListButtons.Count, "buttonIndex >= targetListButtons.Count");

            Button targetButton = targetListButtons[buttonIndex];
            targetButton.gameObject.SetActive(true);
            targetButton.GetComponentInChildren<TextMeshProUGUI>().text = player.playerName.ToString();
            targetButton.onClick.RemoveAllListeners();

            Player targetPlayer = player;
            targetButton.onClick.AddListener(() => OnTargetButtonClicked(targetPlayer));
            
            buttonIndex++;
        }
    }

    public void ShowTargetList(Player curPlayer)
    {
        UpdateTargetList(curPlayer);
    }

    private void OnTargetButtonClicked(Player targetPlayer)
    {
        //if target is selected
        //return target player's reference or network object or Player component

        OnTargetSelected?.Invoke(targetPlayer);
        
        //타겟 선택 시 타겟 선택 버튼 비활성화
        foreach (Button button in targetListButtons)
        {
            button.gameObject.SetActive(false);
        }
    }
}
