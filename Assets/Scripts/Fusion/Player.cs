using Fusion;
using NUnit.Framework;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    private NetworkCharacterController _cc;

    [Networked] public bool isReady { get; set; }

    [Networked] public bool isPlayerTurn { get; set; }

    [Networked] public NetworkString<_16> playerName { get; set; }
    [SerializeField] private GameObject nameObject; //text to show name above the player
    private TextMeshProUGUI nameText;

    private int diceNum = 0;

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

            var lobby = FindFirstObjectByType<LobbyManager>();
            if (lobby != null)
            {
                Debug.Log("Found lobby manager");
                lobby.SetLocalPlayer(this);
                lobby.UpdateButtonState();
            }

            turnManager = FindFirstObjectByType<TurnManager>();

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
            rt = diceRollButtonInstance.GetComponent <RectTransform>();
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
                if (change == nameof(isReady))
                {
                    Debug.Log("this player changed ready state");
                }
                if (change == nameof(isPlayerTurn))
                {
                    Debug.Log("Player turn started");
                    if (Object.HasInputAuthority && playerButtonPrefab != null)
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
                    Debug.Log($"nameObject: {nameObject}, nameText: {nameText}");
                    if (nameText != null)
                    {
                        nameText.text = playerName.ToString();
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
        if(turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }
        
        //write dice roll script here,
        //this way calls host to roll the dice and synchronize to other clients
        //if you want to make each client do actual rolling dice, write script in OnDiceRollButtonClicked()
        //Same for other methods
    }

    private void OnUseItemButtonClicked()
    {
        if (Object.HasInputAuthority) {
            if (isPlayerTurn)
            {
                RPC_RequestUseItem();
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestUseItem()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }
    }

    private void OnViewMapButtonClicked()
    {
        if (Object.HasInputAuthority) {
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

    #region player movable while playing game
    public void ChangeIsPlayerTurn(bool state)
    {
        if ((Object.HasStateAuthority))
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
#endregion
}

//description below is about player's action on player's turn. Just let me know if you want to get it
//플레이어 턴에서 할 행동
//주사위 아이템 맵보기 중 하나 선택하기
//if 주사위
//  주사위 굴리기
//  나온 수만큼 전진
//  전진 중 갈림길 나오면 선택
//  갈림길 선택 후 다시 전진
//  이벤트칸 도착 시 이벤트
//  턴 종료
//else if 아이템
//  갖고 있는 아이템 중 하나 선택
//  아이템이 타겟형인 경우 플레이어 선택
//  선택한 아이템 능력 발동
//  주사위와 맵보기 중 선택
//else if 맵보기
//  플레이어 입력 수집
//  입력에 따라 카메라 이동
//  if 플레이어 맵보기 종료
//      주사위 아이템 맵보기 중 선택