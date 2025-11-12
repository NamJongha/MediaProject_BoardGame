using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Manager.TurnState;

public class TurnManager : NetworkBehaviour
{
    private Button turnDecideButton;
    private Button turnStartButton;

    [Networked, Capacity(4)] public NetworkArray<PlayerRef> PlayerOrder => default;
    [Networked] public TurnState curState { get; set; }
    [Networked] public int curTurnIndex { get; set; } = -1;

    private ITurnState curTurnState;
    
    private GameManager gameManager;

    private void Awake()
    {
        Button[] buttons = new Button[2];
        buttons = FindObjectsByType<Button>(sortMode: default);

        foreach (var btn in buttons)
        {
            if (btn.name == "OrderDecideButton") turnDecideButton = btn;
            else if (btn.name == "TurnStartButton") turnStartButton = btn;
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        turnDecideButton.onClick.AddListener(OnDecideButtonClicked);
        turnStartButton.onClick.AddListener(OnTurnStartButtonClicked);

        gameManager = FindFirstObjectByType<GameManager>();

        ResetOrder();

        if (Object.HasStateAuthority)
        {
            StartCoroutine(DelayedInitState());
        }
        else
        {
            ShowTurnDecideButton(false);
            ShowTurnStartButton(false);
        }
    }

    private IEnumerator DelayedInitState()
    {
        yield return null;
        yield return null;

        Debug.Log("TurnManager: DelayedInitState called, setting to WaitingForOrder");

        curTurnState = new WaitingForOrderState(this);
        curTurnState.OnStateEnter(); // waiting for order, initialize
    }

    private void OnDecideButtonClicked()
    {
        if (Object.HasStateAuthority)
        {
            ChangeState(new DecidingOrderState(this)); // waiting for order state -> deciding turn state
            DecideTurnOrder();
        }
    }

    private void OnTurnStartButtonClicked()
    {
        if (Object.HasStateAuthority)
        {
            ChangeState(new TurnStartState(this)); // deciding turn state -> turn start state
            StartTurn();
        }
    }

    private void DecideTurnOrder()
    {
        //each player roll the dice
        //after all the players roll it, order it from big num to small num
        //big number start first
        
        List<(PlayerRef player, int dice)> result = new List<(PlayerRef, int)>();

        foreach (var kvp in gameManager.GetPlayersList())
        {
            kvp.Value.GetComponent<Player>().RollTheDice();

            PlayerRef playerRef = kvp.Key;
            int dice = kvp.Value.GetComponent<Player>().GetDiceNum();

            LogManager.Instance.Log($"player {kvp.Value.GetComponent<Player>().playerName} dice number is {dice}");
            result.Add((playerRef, dice));
        }

        result.Sort((player1, player2) => player2.dice.CompareTo(player1.dice));

        string orderString = "";
        for (int i = 0; i < Runner.ActivePlayers.Count(); i++)
        {
            PlayerOrder.Set(i, result[i].player);
            Debug.Log(i + " order player: " + PlayerOrder[i]);
            NetworkObject playerObject = gameManager.GetPlayersList().GetValueOrDefault(PlayerOrder[i]);
            orderString = $"{orderString} {(i+1)}: {playerObject.GetComponent<Player>().playerName}";
        }
        
        LogManager.Instance.Log($"Set order is {orderString}");
    }

    public void StartTurn()
    {
        if (!Object.HasStateAuthority) return;

        if (curTurnIndex == -1) curTurnIndex = 0;
        else
        {
            curTurnIndex = (curTurnIndex + 1) % Runner.ActivePlayers.Count();
        }

        PlayerRef curPlayerRef = PlayerOrder.Get(curTurnIndex);
        Debug.Log("Cur Turn index: " + curTurnIndex);

        NetworkObject curPlayerObj = Runner.GetPlayerObject(curPlayerRef);
        Debug.Log("Cur Player Obj: + " + curPlayerObj.gameObject.name);

        curPlayerObj.GetComponent<Player>().ChangeIsPlayerTurn(true);
        Debug.Log(curPlayerRef + " turn started");
        LogManager.Instance.Log($"{curPlayerObj.GetComponent<Player>().playerName} turn started");
        
        CameraManager.Instance.SetPlayerCamera(curPlayerObj);
        
        ChangeState(new TurnActionState(this)); // turn start state -> turn action state
    }
    
    private IEnumerator DelayedStartTurn(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (Object.HasStateAuthority)
        {
            foreach (var kvp in gameManager.GetPlayersList())
            {
                kvp.Value.GetComponent<Player>().ChangeIsPlayerTurn(false);
            }

            yield return null; // wait for one frame to load it
            
            StartTurn(); // restart the turn after end (next player)
        }
    }

    public void OnPlayerEndTurn(PlayerRef player)
    {
        Debug.Assert(Object.HasStateAuthority && PlayerOrder.Get(curTurnIndex) == player);

        ChangeState(new TurnEndState(this));
        EndTurn();
    }

    private void EndTurn()
    {
        PlayerRef curPlayerRef = PlayerOrder.Get(curTurnIndex);
        NetworkObject curPlayerObj = Runner.GetPlayerObject(curPlayerRef);

        curPlayerObj.GetComponent<Player>().ChangeIsPlayerTurn(false);
        
        LogManager.Instance.Log($"{curPlayerObj.GetComponent<Player>().playerName} ended turn");

        if (Object.HasStateAuthority)
        {
            StartCoroutine(DelayedStartTurn(1.0f));
        }
        
        ChangeState(new TurnStartState(this)); // turn end state -> turn start state
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
}

#region enum for turn state

public enum TurnState //Finite State Machine maybe can use for network synchronization
{
    WaitingForOrder,
    DecidingOrder,
    TurnStart,
    TurnAction,
    TurnEnd,
    GameOver
}

#endregion