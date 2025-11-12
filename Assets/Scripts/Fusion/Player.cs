using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    private NetworkCharacterController _cc;

    [Networked] public bool isReady { get; set; }
    [Networked] public bool isPlayerTurn { get; set; }
    [Networked] public NetworkString<_16> playerName { get; set; }
    [Networked] public int playerStamina { get; set; } = 12;

    [SerializeField] private GameObject nameObject; //text to show name above the player
    private TextMeshProUGUI nameText;

    private int diceNum = 0;
    
    private List<IItemStrategy> playerItemList = new List<IItemStrategy>(3);

    private ChangeDetector changeDetector;
    private TurnManager turnManager;
    
    private PlayerUIManager playerUIManager;

    private void Awake()
    
    {
        _cc = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        Debug.Log("Player just spawned");

        changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        nameText = nameObject.GetComponent<TextMeshProUGUI>();

        if (Object.HasInputAuthority)
        {
            string _playerName = PlayerPrefs.GetString("playerName", $"Player_{Object.InputAuthority.PlayerId}"); //second variable is default value
            RPC_SetPlayerName(_playerName);

            LobbyManager lobby = FindFirstObjectByType<LobbyManager>();
            if (lobby != null)
            {
                Debug.Log("Found lobby manager");
                lobby.SetLocalPlayer(this);
                lobby.UpdateButtonState();
            }
            
            playerUIManager =  this.gameObject.GetComponent<PlayerUIManager>();

            turnManager = FindFirstObjectByType<TurnManager>();
        }
    }

    public override void Render()
    {
        base.Render();

        if (nameText != null && !string.IsNullOrEmpty(playerName.ToString()))
        {
            if (nameText.text != playerName.ToString())
            {
                nameText.text = playerName.ToString();
            }

            foreach (var change in changeDetector.DetectChanges(this))
            {
                if (change == nameof(isPlayerTurn))
                {
                    Debug.Log("isPlayerTurnChanged");
                    if (Object.HasInputAuthority)
                    {
                        //show button only when the player turn comes
                        playerUIManager.SetButtonsActive(isPlayerTurn);
                    }
                }

                if (change == nameof(playerName))
                {
                    Debug.Assert(nameText != null);
                    nameText.text = playerName.ToString();
                    if (Object.HasStateAuthority)
                    {
                        LogManager.Instance.Log($"{playerName} joined the session");
                    }
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            if (isPlayerTurn)
            {
                data.direction.Normalize();
                _cc.Move(5 * data.direction * Runner.DeltaTime);
            }
            else
            {
                Vector3 gravity = new Vector3(0, -1, 0);
                _cc.Move(5 * gravity * Runner.DeltaTime);

                transform.up = Vector3.up;
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }

        Vector3 euler = gameObject.transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0, euler.y, 0);
    }

    //Player Name Setting
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string name)
    {
        playerName = name;
    }

    #region player ready in lobby

    public void ChangeReady()
    {
        if (Object.HasStateAuthority)
        {
            isReady = !isReady;
        }
        else
        {
            RPC_RequestChangeReady(!isReady);
        }

        string msg = isReady ? "not ready" : "ready";
        LogManager.Instance.Log($"{playerName} {msg}");
    }

    //isReady should be changed by the Host -> client send change to host with rpc
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestChangeReady(bool state)
    {
        isReady = state;
    }

    public void resetReady()
    {
        if (Object.HasStateAuthority)
        {
            if (Object.InputAuthority == Runner.LocalPlayer) isReady = true;
            else isReady = false;
        }
    }

    #endregion

    #region player turn state

    public void ChangeIsPlayerTurn(bool state)
    {
        if ((Runner.IsServer))
        {
            isPlayerTurn = state;
        }
        else
        {
            RPC_RequestChangeIsPlayerTurn(state);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestChangeIsPlayerTurn(bool state)
    {
        isPlayerTurn = state;
    }

    public void ResetIsPlayerTurn()
    {
        isPlayerTurn = false;
    }

    #endregion

    #region player dice logic

    public void RollTheDice()
    {
        int dice = Random.Range(1, 7);
        diceNum = dice;
    }

    public int GetDiceNum()
    {
        return diceNum;
    }

    public void SetDiceNum(int diceNum)
    {
        this.diceNum = diceNum;
    }

    #endregion

    public List<IItemStrategy> GetItemList()
    {
        return playerItemList;
    }
}