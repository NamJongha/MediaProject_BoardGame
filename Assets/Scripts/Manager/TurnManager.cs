using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Manager.TurnState;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance { get; private set; }
    
    private ITurnState curTurnState;

    private GameManager gameManager;
    
    private Button turnDecideButton;
    private Button turnStartButton;
    private Button rollDiceButton;
    private Button useItemButton;
    private Button viewMapButton;
    private Button closeMapButton;
    
    private WaitingForOrderState waitingForOrderState;
    private DecidingOrderState  decidingOrderState;
    private TurnStartState turnStartState;
    private TurnActionState  turnActionState;
    private TurnEndState  turnEndState;
    private TurnGameOverState turnGameOverState;

    [Networked] [Capacity(4)] public NetworkArray<PlayerRef> PlayerOrder => default;
    [Networked] public TurnState curState { get; set; }
    [Networked] public int curTurnIndex { get; set; } = -1;
    private bool _initialized = false;

    private void Awake()
    {
        waitingForOrderState = new WaitingForOrderState(this);
        decidingOrderState = new DecidingOrderState(this);
        turnStartState = new TurnStartState(this);
        turnActionState = new TurnActionState(this);
        turnEndState = new TurnEndState(this);
        turnGameOverState   = new TurnGameOverState(this);
    }

    public override void Spawned()
    {
        base.Spawned();

        Debug.Log("TurnManager: Spawned()");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterTurnManager(this);
        }
        
        curTurnState = waitingForOrderState;

        gameManager = FindFirstObjectByType<GameManager>();

        StartCoroutine(WaitAndBindInitialButtons());

        ResetOrder();
    }
    
    private IEnumerator WaitAndBindInitialButtons()
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < 50; i++)
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var btn in buttons)
            {
                if (btn.name == "OrderDecideButton") //its name was wrong: turnDecideButton -> OrderDecideButton
                {
                    turnDecideButton = btn;
                    turnDecideButton.onClick.RemoveAllListeners();
                    turnDecideButton.onClick.AddListener(OnDecideButtonClicked);
                    Debug.Log("[TM] TurnDecideButton connected");
                }

                if (btn.name == "TurnStartButton")
                {
                    turnStartButton = btn;
                    turnStartButton.onClick.RemoveAllListeners();
                    turnStartButton.onClick.AddListener(OnTurnStartButtonClicked);
                    Debug.Log("[TM] TurnStartButton connected");
                }
            }

            // 필수 버튼 둘 다 연결되었으면 종료
            if (turnDecideButton != null && turnStartButton != null)
            {
                Debug.Log("[TM] Essential UI buttons connected");
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogWarning("[TM] Timeout binding essential buttons!");
    }

    private IEnumerator WaitAndBindRuntimeButtons()
    {
        Debug.Log("[TM] WaitAndBindRuntimeButtons START");

        // UIManager가 버튼들을 생성할 시간을 기다린다
        for (int i = 0; i < 50; i++)
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var btn in buttons)
            {
                switch (btn.name)
                {
                    case "DiceRollButton":
                        rollDiceButton = btn;
                        rollDiceButton.onClick.RemoveAllListeners();
                        rollDiceButton.onClick.AddListener(OnClickRollDice);
                        Debug.Log("[TM] DiceRollButton connected");
                        break;

                    case "UseItemButton":
                        useItemButton = btn;
                        useItemButton.onClick.RemoveAllListeners();
                        useItemButton.onClick.AddListener(OnClickOpenItemUI);
                        Debug.Log("[TM] UseItemButton connected");
                        break;
                    
                    case "ViewMapButton":
                        viewMapButton = btn;
                        viewMapButton.onClick.RemoveAllListeners();
                        viewMapButton.onClick.AddListener(OnClickViewMap);
                        Debug.Log("[TM] ViewMapButton connected");
                        break;
                    
                    case "CloseMapButton":
                        closeMapButton = btn;
                        closeMapButton.onClick.RemoveAllListeners();
                        closeMapButton.onClick.AddListener(OnClickCloseMap);
                        Debug.Log("[TM] CloseMapButton connected");
                        break;
                }
            }

            if (rollDiceButton != null && useItemButton != null)
            {
                Debug.Log("[TM] Runtime UI buttons connected!");
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogWarning("[TM] Timeout binding runtime buttons!");
    }

    private IEnumerator DelayedInitState()
    {
        yield return new WaitForSeconds(0.2f);

        Debug.Log("[TM] DelayedInitState → WaitingForOrder");   

        curTurnState = new WaitingForOrderState(this);
        curTurnState.OnStateEnter();
    }

    
    public void InitTurnFlow()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        StartCoroutine(DelayedInitState());
    }           

    private void OnDecideButtonClicked()
    {
        if (Object.HasStateAuthority)
        {
            ChangeState(decidingOrderState); // waiting for order state -> deciding turn state
            DecideTurnOrder();
        }
    }

    private void OnTurnStartButtonClicked()
    {
        if (Object.HasStateAuthority)
        {
            ChangeState(turnStartState); // deciding turn state -> turn start state
            StartTurn();
            StartCoroutine(WaitAndBindRuntimeButtons());
        }
    }

    private Player GetCurrentTurnPlayer()
    {
        foreach (var kvp in gameManager.GetPlayersList())
        {
            var p = kvp.Value.GetComponent<Player>();
            if (p.isPlayerTurn)
                return p;
        }

        return null;
    }

    private void OnClickRollDice()
    {
        LogManager.Instance.Log("Roll Dice Button Click detected");

        var current = GetCurrentTurnPlayer();
        if (current != null)
        {
            current.RequestRollDice();
        }
    }

    private void OnClickOpenItemUI()
    {
        LogManager.Instance.Log("Use Item Button Click detected");

        var current = GetCurrentTurnPlayer();
        if (current != null)
        {
            if (current != null)
            {
                current.RequestOpenItemUI();
            }
        }
    }

    private void OnClickViewMap()
    {
        LogManager.Instance.Log("Use Item Button Click detected");

        var current = GetCurrentTurnPlayer();
        if (current != null)
        {
            current.RequestControlMap();
        }
    }
    
    private void OnClickCloseMap()
    {
        LogManager.Instance.Log("Use Item Button Click detected");

        var current = GetCurrentTurnPlayer();
        if (current != null)
        {
            current.RequestControlMap();
        }
    }

    private void DecideTurnOrder()
    {
        //each player roll the dice
        //after all the players roll it, order it from big num to small num
        //big number start first

        List<(PlayerRef player, int dice)> result = new();

        foreach (var kvp in gameManager.GetPlayersList())
        {
            kvp.Value.GetComponent<Player>().RollTheDice();

            var playerRef = kvp.Key;
            var dice = kvp.Value.GetComponent<Player>().GetDiceNum();

            LogManager.Instance.Log($"player {kvp.Value.GetComponent<Player>().playerName} dice number is {dice}");
            result.Add((playerRef, dice));
        }

        result.Sort((player1, player2) => player2.dice.CompareTo(player1.dice));

        var orderString = "";
        for (var i = 0; i < Runner.ActivePlayers.Count(); i++)
        {
            PlayerOrder.Set(i, result[i].player);
            Debug.Log(i + " order player: " + PlayerOrder[i]);
            var playerObject = gameManager.GetPlayersList().GetValueOrDefault(PlayerOrder[i]);
            orderString = $"{orderString} {i + 1}: {playerObject.GetComponent<Player>().playerName}";
        }

        LogManager.Instance.Log($"Set order is {orderString}");
    }

    public void StartTurn()
    {
        if (!Object.HasStateAuthority) return;

        if (curTurnIndex == -1) curTurnIndex = 0;
        else
            curTurnIndex = (curTurnIndex + 1) % Runner.ActivePlayers.Count();

        PlayerRef curPlayerRef = PlayerOrder.Get(curTurnIndex);
        Debug.Log("Cur Turn index: " + curTurnIndex);

        NetworkObject curPlayerObj = Runner.GetPlayerObject(curPlayerRef);
        Debug.Log("Cur Player Obj: + " + curPlayerObj.gameObject.name);

        curPlayerObj.GetComponent<Player>().ChangeIsPlayerTurn(true);
        Debug.Log(curPlayerRef + " turn started");
        LogManager.Instance.Log($"{curPlayerObj.GetComponent<Player>().playerName} turn started");

        CameraManager.Instance.SetPlayerCamera(curPlayerObj);

        ChangeState(turnActionState); // turn start state -> turn action state
    }

    private IEnumerator DelayedStartTurn(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (Object.HasStateAuthority)
        {
            foreach (var kvp in gameManager.GetPlayersList())
                kvp.Value.GetComponent<Player>().ChangeIsPlayerTurn(false);

            yield return null; // wait for one frame to load it

            StartTurn(); // restart the turn after end (next player)
        }
    }

    public void OnPlayerEndTurn(PlayerRef player)
    {
        Debug.Assert(Object.HasStateAuthority && PlayerOrder.Get(curTurnIndex) == player);

        ChangeState(turnEndState);
        EndTurn();
    }

    private void EndTurn()
    {
        var curPlayerRef = PlayerOrder.Get(curTurnIndex);
        var curPlayerObj = Runner.GetPlayerObject(curPlayerRef);

        curPlayerObj.GetComponent<Player>().ChangeIsPlayerTurn(false);

        LogManager.Instance.Log($"{curPlayerObj.GetComponent<Player>().playerName} ended turn");

        if (Object.HasStateAuthority) StartCoroutine(DelayedStartTurn(1.0f));

        ChangeState(turnStartState); // turn end state -> turn start state
    }

    public void ResetOrder()
    {
        PlayerOrder.Clear();
    }

    public void ShowTurnDecideButton(bool show)
    {
        turnDecideButton.gameObject.SetActive(show);
    }

    public void ShowTurnStartButton(bool show)
    {
        turnStartButton.gameObject.SetActive(show);
    }

    public void ChangeState(ITurnState newState)
    {
        curTurnState.OnStateExit();
        curTurnState = newState;
        curTurnState.OnStateEnter();
    }
    public void EnterGameOverState()
    {
        ChangeState(turnGameOverState);
        
        // 네트워크 동기화 필요 시
        curState = TurnState.GameOver;
    }
}

#region enum for turn state

public enum TurnState //Finite State Machine maybe can use for network synchronization
{
    WaitingForOrder,
    DecidingOrder,
    TurnStart,
    TurnAction,
    TurnEnd,
    GameOver,
    TurnGameOver
}

#endregion