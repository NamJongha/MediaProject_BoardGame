using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    private NetworkCharacterController _cc;
    private const int MaxStamina = 12;
    private const int MinStamina = 0;
    [Networked] public bool isReady { get; set; }
    [Networked] public bool isPlayerTurn { get; set; }
    [Networked] public NetworkString<_16> playerName { get; set; }
    [Networked] public int playerStamina { get; set; } = MaxStamina;
    [Networked] public int chosenBranchIndex { get; set; }
    [Networked] public NetworkBool branchSelected { get; set; }

    [SerializeField] private GameObject nameObject; //text to show name above the player
    private TextMeshProUGUI nameText;

    private int diceNum = 0;
    
    private List<IItemStrategy> playerItemList = new List<IItemStrategy>(3);

    private ChangeDetector changeDetector;
    private TurnManager turnManager;

    [SerializeField] private Button playerButtonPrefab;
    private Button endTurnButtonInstance;
    private Button diceRollButtonInstance;
    private Button useItemButtonInstance;
    private Button viewMapButtonInstance;

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
            turnManager = FindFirstObjectByType<TurnManager>();
            playerStamina = MaxStamina;

            LobbyManager lobby = FindFirstObjectByType<LobbyManager>();
            if (lobby != null)
            {
                Debug.Log("Found lobby manager");
                lobby.SetLocalPlayer(this);
                lobby.UpdateButtonState();
            }

            
            #region instantiate buttons

            endTurnButtonInstance = Instantiate(playerButtonPrefab);

            //instantiate player's menu button and rename it
            diceRollButtonInstance = Instantiate(playerButtonPrefab);
            diceRollButtonInstance.name = "DiceRollButton";
            useItemButtonInstance = Instantiate(playerButtonPrefab);
            useItemButtonInstance.name = "UseItemButton";
            viewMapButtonInstance = Instantiate(playerButtonPrefab);
            viewMapButtonInstance.name = "ViewMapButton";

            //Set button's parent to canvas
            endTurnButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);
            diceRollButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);
            useItemButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);
            viewMapButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);

            //Set button's position
            RectTransform rt;
            rt = endTurnButtonInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(365, -250);
            rt = diceRollButtonInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(365, 170);
            rt = useItemButtonInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(365, 120);
            rt = viewMapButtonInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(365, 70);

            //Set button's text
            endTurnButtonInstance.GetComponentInChildren<TMP_Text>().text = "End Turn";
            diceRollButtonInstance.GetComponentInChildren<TMP_Text>().text = "Roll Dice";
            useItemButtonInstance.GetComponentInChildren<TMP_Text>().text = "Use Item";
            viewMapButtonInstance.GetComponentInChildren<TMP_Text>().text = "View Map";

            //Add events on button
            endTurnButtonInstance.onClick.AddListener(OnEndTurnButtonClicked);

            //Initialize state
            endTurnButtonInstance.gameObject.SetActive(false);
            diceRollButtonInstance.gameObject.SetActive(false);
            useItemButtonInstance.gameObject.SetActive(false);
            viewMapButtonInstance.gameObject.SetActive(false);

            #endregion
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
                    Debug.Assert(playerButtonPrefab != null);
                    if (Object.HasInputAuthority)
                    {
                        //show button only when the player turn comes
                        endTurnButtonInstance.gameObject.SetActive(isPlayerTurn);
                        diceRollButtonInstance.gameObject.SetActive(isPlayerTurn);
                        useItemButtonInstance.gameObject.SetActive(isPlayerTurn);
                        viewMapButtonInstance.gameObject.SetActive(isPlayerTurn);
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

    #region Button Events

    private void OnEndTurnButtonClicked()
    {
        if (Object.HasInputAuthority)
        {
            if (isPlayerTurn)
            {
                RPC_RequestEndTurn();
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestEndTurn()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        turnManager.OnPlayerEndTurn(Object.InputAuthority); //this is for managing turn state in turnManager
    }

    private void OnDiceRollButtonClicked()
    {
        if (Object.HasInputAuthority)
        {
            if (isPlayerTurn)
            {
                RPC_RequestRollDice();
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestRollDice()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }
        int staminaLossCount = (MaxStamina - playerStamina) / 2;
        int maxDiceValue = Mathf.Max(1, 6 - staminaLossCount);

        int diceValue = UnityEngine.Random.Range(1, maxDiceValue + 1);

        LogManager.Instance.Log(
            $"{playerName} rolled {diceValue} (max: {maxDiceValue}, stamina: {playerStamina})"
        );
        
        // Start moving the player
        PlayerMover mover = GetComponent<PlayerMover>();

        // MoveStepsAndFinishTurn(steps, player, turnManager)
        mover.StartCoroutine(mover.MoveStepsAndFinishTurn(diceValue, this, turnManager));
        
        // Stamina decreases by 1 each turn
        playerStamina = Mathf.Max(0, playerStamina - 1);

        // Update Stamina Text and UI
        LogManager.Instance.Log($"{playerName} stamina is now {playerStamina}");
    }

    public void RequestDiceRoll()
    {
        OnDiceRollButtonClicked();
    }
    
    private void OnUseItemButtonClicked()
    {
        if (Object.HasInputAuthority)
        {
            if (isPlayerTurn)
            {
                RPC_RequestUseItem();
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestUseItem()
    {
        Debug.Assert(turnManager != null);
    }

    private void OnViewMapButtonClicked()
    {
        if (Object.HasInputAuthority)
        {
            if (isPlayerTurn)
            {
                RPC_RequestViewMap();
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestViewMap()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        //write view map script here
    }

    #endregion

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
    
    public void ModifyStamina(int amount)
    {
        playerStamina = Mathf.Clamp(playerStamina + amount, MinStamina, MaxStamina);
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ChooseBranch(int index)
    {
        chosenBranchIndex = index;
        branchSelected = true;
    }
}