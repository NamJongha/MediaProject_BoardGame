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
    private Player targetPlayer;

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
        foreach (Button button in targetListButtons)
        {
            button.onClick.RemoveAllListeners();
        }
        
        currentPlayer = curPlayer;
        int playerListIndex = 0;
        
        foreach (KeyValuePair<PlayerRef, NetworkObject> player in playerList)
        {
            Debug.Assert(playerList.Count <= 4 && playerList != null);
            if (player.Value.GetComponent<Player>().playerName == currentPlayer.playerName)
            {
                playerListIndex++;
            }

            else
            {
                Button targetButton = targetListButtons[playerListIndex];
                targetButton.GetComponent<TextMeshProUGUI>().text = player.Value.GetComponent<Player>().playerName.ToString();
                targetButton.onClick.AddListener(() => OnTargetButtonClicked(targetButton));
                playerListIndex++;
            }
        }
        
        foreach (Button button in targetListButtons)
        {
            button.gameObject.SetActive(true);
        }
    }

    public void ShowTargetList(Player curPlayer)
    {
        UpdateTargetList(curPlayer);
    }

    private void OnTargetButtonClicked(Button btn)
    {
        //if target is selected
        //return target player's reference or network object or Player component

        TargetSelected(btn);
        foreach (Button button in targetListButtons)
        {
            button.gameObject.SetActive(false);
        }
    }

    private void TargetSelected(Button targetButton)
    {
        string playerName = targetButton.GetComponent<TextMeshProUGUI>().text;
        Player targetPlayer = new Player();
        
        foreach (KeyValuePair<PlayerRef, NetworkObject> player in playerList)
        {
            NetworkObject playerObject = player.Value;
            if (playerObject.GetComponent<Player>().playerName == playerName)
            {
                targetPlayer = playerObject.GetComponent<Player>();
                break;
            }
        }

        this.targetPlayer = targetPlayer;
    }

    public Player GetTargetPlayer()
    {
        return targetPlayer;
    }

    public void ResetTargetPlayer()
    {
        targetPlayer = null;
    }
}
